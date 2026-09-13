using System.Collections.Generic;
using UnityEngine;
using LZ.WarGameMap.Runtime.Model;
using System;
using System.Threading.Tasks;
using LZ.WarGameMap.Runtime.Enums;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LZ.WarGameMap.Runtime
{
    public class WGMapManager : MonoBehaviour
    {
        
        // TODO ： 这些组件全部需要动态加载，但是AB加载功能还没有封装完成，需要考虑怎么处理
        // 现阶段先直接挂接（8.3日），属性是public，后续改为private
        #region 初始化地图需要的功能组件

        public TerrainConstructor TerrainCtor;
        public HexmapConstructor HexCtor;
        public MapRenderConstructor RenderCtor;

        public HexSettingSO hexSet;
        public TerrainSettingSO terSet;
        public MapRuntimeSetting mapSet;

        public GridTerrainSO gridTerrainSO;
        public CountrySO countrySO;

        [Header("地图主材质")]
        public Material mainMaterial;
        [Header("地貌材质")]
        public Material terrainLandformMat;
        [Header("河流材质")]
        public Material riverMaterial;

        [Header("高度数据模型列表")]
        public List<HeightDataModel> heightDataModels;
        [Header("河流数据")]
        public MapRiverData mapRvData;

        [Header("生成选项")]
        public bool shouldGenRiver = false;
        public bool shouldGenLODBySimplify = false;
        public bool genRuntimeClusterMesh = false;

        #endregion

        #region Runtime initialization

        public bool IsInitialized { get; private set; }
        private bool initializationStarted;
        private bool initializationRunning;
        private bool isDestroyed;
        private MapRuntimeAssetsSO runtimeAssets;
        private readonly List<AsyncOperationHandle> assetHandles = new List<AsyncOperationHandle>();
        private GameObject runtimeRoot;
        private Material runtimeLandformMaterial;
        private Texture2DArray albedoArray;
        private Texture2DArray normalArray;
        private Texture2D regionTexture;
        private ComputeShader regionSdfShader;

        // 依次加载资源并等待全部地块构建成功，不自动驱动地图更新。
        public async Task InitWarGameMap()
        {
            if (initializationStarted || isDestroyed)
            {
                throw new InvalidOperationException("Map initialization may only be started once on a live manager.");
            }
            initializationStarted = true;
            initializationRunning = true;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (transform.position != Vector3.zero || transform.rotation != Quaternion.identity || transform.lossyScale != Vector3.one)
                {
                    throw new InvalidOperationException("The map manager must use world origin, identity rotation and unit scale.");
                }
                Debug.Log("[WarGameMap] 初始化资源加载。");
                await LoadRuntimeAssets();
                CheckManagerAlive();
                PrepareRuntimeComponents();
                InitLandform();
                await LoadTerrain();
                CheckManagerAlive();
                TerrainCtor.SetTerrainGened();
                // TODO：后续在此接入 HexCtor 和河流初始化。
                IsInitialized = true;
                Debug.Log($"[WarGameMap] 初始化完成，地块 {runtimeAssets.clusters.Count}，耗时 {watch.ElapsedMilliseconds} ms。");
            }
            catch (Exception exception)
            {
                IsInitialized = false;
                Debug.LogError($"[WarGameMap] 初始化失败，耗时 {watch.ElapsedMilliseconds} ms：{exception}");
                ReleaseRuntimeAssets();
                throw;
            }
            finally
            {
                initializationRunning = false;
            }
        }

        // 清单中的引用通过 Addressables 加载，Shader 随材质依赖进入内存。
        private async Task LoadRuntimeAssets()
        {
            var initialization = Addressables.InitializeAsync(false);
            assetHandles.Add(initialization);
            await initialization.Task;
            CheckManagerAlive();
            if (initialization.Status != AsyncOperationStatus.Succeeded)
            {
                throw new InvalidOperationException("Addressables initialization failed.", initialization.OperationException);
            }
            runtimeAssets = await LoadAsset<MapRuntimeAssetsSO>(MapStoreEnum.RuntimeManifestAddress);
            if (runtimeAssets.assets == null || runtimeAssets.heightDataModels == null || runtimeAssets.heightDataModels.Count == 0
                || runtimeAssets.clusters == null || runtimeAssets.clusters.Count == 0)
            {
                throw new InvalidOperationException("The runtime map manifest is incomplete.");
            }
            terSet = await LoadRole<TerrainSettingSO>(MapRuntimeAssetRole.TerrainSetting);
            mapSet = await LoadRole<MapRuntimeSetting>(MapRuntimeAssetRole.RuntimeSetting);
            hexSet = await LoadRole<HexSettingSO>(MapRuntimeAssetRole.HexSetting);
            gridTerrainSO = await LoadRole<GridTerrainSO>(MapRuntimeAssetRole.GridTerrain);
            countrySO = await LoadRole<CountrySO>(MapRuntimeAssetRole.Country);
            terrainLandformMat = await LoadRole<Material>(MapRuntimeAssetRole.LandformMaterial);
            albedoArray = await LoadRole<Texture2DArray>(MapRuntimeAssetRole.AlbedoArray);
            normalArray = await LoadRole<Texture2DArray>(MapRuntimeAssetRole.NormalArray);
            regionTexture = await LoadRole<Texture2D>(MapRuntimeAssetRole.RegionTexture);
            regionSdfShader = await LoadRole<ComputeShader>(MapRuntimeAssetRole.RegionSDFShader);
            heightDataModels = new List<HeightDataModel>();
            foreach (var reference in runtimeAssets.heightDataModels)
            {
                if (reference == null || !reference.RuntimeKeyIsValid())
                {
                    throw new InvalidOperationException("A height data reference is missing.");
                }
                var height = await LoadAsset<HeightDataModel>(reference.RuntimeKey);
                if (height.singleHeightFileSize <= 0)
                {
                    throw new InvalidOperationException("Invalid height data size: " + height.name);
                }
                heightDataModels.Add(height);
            }
            Debug.Log($"[WarGameMap] 配置与渲染资源加载完成，高度数据 {heightDataModels.Count}。");
        }

        private Task<T> LoadRole<T>(MapRuntimeAssetRole role) where T : UnityEngine.Object
        {
            MapRuntimeAssetEntry selected = null;
            foreach (var entry in runtimeAssets.assets)
            {
                if (entry == null || entry.role != role) continue;
                if (selected != null)
                {
                    throw new InvalidOperationException("Duplicate map resource role: " + role);
                }
                selected = entry;
            }
            if (selected == null || selected.asset == null || !selected.asset.RuntimeKeyIsValid())
            {
                throw new InvalidOperationException("Missing map resource role: " + role);
            }
            return LoadAsset<T>(selected.asset.RuntimeKey);
        }

        private async Task<T> LoadAsset<T>(object key) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(key);
            assetHandles.Add(handle);
            await handle.Task;
            CheckManagerAlive();
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                throw new InvalidOperationException("Map resource loading failed: " + key, handle.OperationException);
            }
            return handle.Result;
        }

        // 只创建并管理本次启动所需的节点和组件。
        private void PrepareRuntimeComponents()
        {
            runtimeRoot = new GameObject(MapEnum.MapRootName);
            runtimeRoot.transform.SetParent(transform, false);
            var clusters = new GameObject(MapEnum.ClusterParentName);
            clusters.transform.SetParent(runtimeRoot.transform, false);
            var rivers = new GameObject("rivers");
            rivers.transform.SetParent(runtimeRoot.transform, false);
            runtimeLandformMaterial = new Material(terrainLandformMat);
            TerrainCtor = runtimeRoot.AddComponent<TerrainConstructor>();
            TerrainCtor.SetMapPrefab(runtimeRoot.transform, clusters.transform, rivers.transform);
            TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, null, runtimeLandformMaterial, null);
            RenderCtor = runtimeRoot.AddComponent<MapRenderConstructor>();
        }

        private void InitLandform(){
            RenderCtor.InitMapRenderCons(terSet, hexSet, mapSet, gridTerrainSO, countrySO);
            RenderCtor.SetLandformResources(albedoArray, normalArray, regionTexture, regionSdfShader);
            RenderCtor.InitRuntimeLandformMaterial(runtimeLandformMaterial);
        }

        // 加载地图中的地块Mesh，不要用 InitTerrain，它耗时很高
        private async Task LoadTerrain()
        {
            var coordinates = new HashSet<Vector3Int>();
            int completed = 0;
            foreach (var cluster in runtimeAssets.clusters)
            {
                CheckManagerAlive();
                if (cluster == null)
                {
                    throw new InvalidOperationException("The manifest contains an empty cluster entry.");
                }
                var coordinate = new Vector3Int(cluster.longitude, cluster.latitude, cluster.lodLevel);
                bool validIndex = cluster.idxX >= 0 && cluster.idxY >= 0
                    && cluster.idxX < TerrainCtor.terrainWidth && cluster.idxY < TerrainCtor.terrainHeight;
                bool matchingCoordinate = cluster.idxX == cluster.longitude - terSet.startLL.x
                    && cluster.idxY == cluster.latitude - terSet.startLL.y;
                if (!validIndex || !matchingCoordinate || !coordinates.Add(coordinate))
                {
                    throw new InvalidOperationException("Invalid or duplicate cluster coordinate: " + coordinate);
                }
                Debug.Log($"[WarGameMap] 加载地块 {coordinate}，进度 {completed}/{runtimeAssets.clusters.Count}。");
                await TerrainCtor.ExportClusterByBinary(cluster.longitude, cluster.latitude, cluster.lodLevel);
                CheckManagerAlive();
                var loadedCluster = TerrainCtor.ClusterList[cluster.idxX, cluster.idxY];
                if (!loadedCluster.IsLoaded)
                {
                    throw new InvalidOperationException("Cluster construction did not complete: " + coordinate);
                }
                completed++;
                Debug.Log($"[WarGameMap] 地块 {coordinate} 完成，进度 {completed}/{runtimeAssets.clusters.Count}。");
            }
        }

        private void CheckManagerAlive()
        {
            if (isDestroyed)
            {
                throw new OperationCanceledException("Map manager was destroyed during initialization.");
            }
        }

        private void OnDestroy()
        {
            isDestroyed = true;
            IsInitialized = false;
            if (!initializationRunning)
            {
                ReleaseRuntimeAssets();
            }
        }

        // 仅释放本管理器取得的资源，旧地块加载器的句柄仍由其现有逻辑负责。
        private void ReleaseRuntimeAssets()
        {
            if (runtimeRoot != null) Destroy(runtimeRoot);
            if (runtimeLandformMaterial != null) Destroy(runtimeLandformMaterial);
            runtimeRoot = null;
            runtimeLandformMaterial = null;
            for (int i = assetHandles.Count - 1; i >= 0; i--)
            {
                if (assetHandles[i].IsValid()) Addressables.Release(assetHandles[i]);
            }
            assetHandles.Clear();
        }

        #endregion

        private void InitTerrain(){
            int tileNumARow = terSet.clusterSize / terSet.tileSize;

            DebugUtility.Log($"the map size is : {terSet.terrainSize}");
            DebugUtility.Log($"the cluster size : {terSet.clusterSize}, the tile size : {terSet.tileSize}, there are {tileNumARow} tiles per line");

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            if (heightDataModels == null) {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }

            // 初始化地块
            TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, null, mainMaterial, mapRvData);
        
            List<Vector2Int> clusterIdxList = GetBuildClusterTargets();

            // 实际构建地块Mesh
            TerrainGenTask terrainGenTask = new TerrainGenTask(heightDataModels, terSet, TerrainCtor, 
                clusterIdxList, shouldGenRiver, shouldGenLODBySimplify, genRuntimeClusterMesh);
            int taskID = TaskManager.GetInstance().StartProgress(TaskTickLevel.Medium, terrainGenTask);
            // TODO: TerGenTaskPop 是 Editor 代码，Runtime 中不可用，后续需要用 Runtime 的进度UI
            // TerGenTaskPop.GetPopInstance().ShowBasePop(terrainGenTask);
            terrainGenTask.StartTask(taskID);
        }

        /// 默认构建所有 cluster
        private List<Vector2Int> GetBuildClusterTargets()
        {
            List<Vector2Int> result = new List<Vector2Int>();
            int clusterHeight = terSet.terrainSize.x/ terSet.clusterSize;
            int clusterWidth = terSet.terrainSize.z / terSet.clusterSize;
            for (int x = 0; x < clusterHeight; x++)
            {
                for (int y = 0; y < clusterWidth; y++)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
            return result;
        }

        private void InitRiverAndWater(){

        }

        private void InitRegion(){

        }

        private void InitGamePlay(){

        }

    }
}
