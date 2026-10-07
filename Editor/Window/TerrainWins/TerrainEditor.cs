using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using FileMode = System.IO.FileMode;

namespace LZ.WarGameMap.MapEditor
{
    public class TerrainEditor : BrushMapEditor {

        public override string EditorName => MapEditorEnum.TerrainEditor;

        TerrainConstructor TerrainCtor;
        HexmapConstructor HexCtor;

        protected override void InitEditor() {
            base.InitEditor();
            TerrainCtor = EditorSceneManager.TerrainCtor;
            HexCtor = EditorSceneManager.HexCtor;

            // read terrain Setting from path
            InitMapSetting();
        }

        protected override BrushMapSetting GetBrushMapSetting()
        {
            return new BrushMapSetting(false, true);
        }

        #region 构建地形-高度图流程

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("生成Runtime的地块资产")]
        public bool genRuntimeClusterMesh = false;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("自动减面生成LOD")]        // 自动LOD为多线程过程，编写代码时需要谨慎，禁止job、协程等 与 多线程混用
        public bool shouldGenLODBySimplify = false;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("是否生成河流")]
        public bool shouldGenRiver = false;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("Ter地图材质")]
        public Material terMaterial;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("River数据")]
        public MapRiverData mapRvData;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("当前使用的高度图数据")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("构建起点-左下角地块索引")]    // cluster index start
        public Vector2Int leftDownClsIdx;

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("构建终点-右上角地块索引")]    // cluster index end
        public Vector2Int rightUpClsIdx;

        [FoldoutGroup("构建地形-高度图流程", 0)]
        [Button("初始化地形", ButtonSizes.Medium)]
        private void GenerateTerrain() {
            int tileNumARow = terSet.clusterSize / terSet.tileSize;

            DebugUtility.Log($"the map size is : {terSet.terrainSize}");
            DebugUtility.Log($"the cluster size : {terSet.clusterSize}, the tile size : {terSet.tileSize}, there are {tileNumARow} tiles per line");

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, null, terMaterial, mapRvData);
        }

        [FoldoutGroup("构建地形-高度图流程")]
        [Button("构建地块_指定范围地块", ButtonSizes.Medium)]
        private void BuildCluster_ForEdit()
        {
            List<Vector2Int> clusterIdxList = GetBuildClusterTargets();
            BuildClusters_ForEdit(clusterIdxList);
        }

        [FoldoutGroup("构建地形-高度图流程")]
        [Button("构建地块_所有", ButtonSizes.Medium)]
        private void BuildAllClusters_ForEdit()
        {
            List<Vector2Int> clusterIdxList = GetAllBuildClusterTargets();
            BuildClusters_ForEdit(clusterIdxList);
        }

        private bool isLodOperationRunning; // 判断是否正在生成

        [FoldoutGroup("构建地形-高度图流程")]
        [LabelText("构建时不覆盖已有地块文件）")]
        public bool noOverride;

        [FoldoutGroup("构建地形-高度图流程")]
        [Button("一键式保存_指定范围地块", ButtonSizes.Medium)]
        private async void BuildAndSaveTerrainMeshInRange()
        {
            await SaveTerrainLODAssets(true);
        }

        [FoldoutGroup("构建地形-高度图流程")]
        [Button("一键式保存_所有", ButtonSizes.Medium)]
        private async void BuildAndSaveTerrainMesh()
        {
            await SaveTerrainLODAssets(false);
        }

        private async Task SaveTerrainLODAssets(bool specifiedRange)
        {
            if (isLodOperationRunning)
            {
                return;
            }
            isLodOperationRunning = true;
            CancellationTokenSource cancellation = new CancellationTokenSource();
            try
            {
                Directory.CreateDirectory(exportHandleMeshPath);
                List<Vector2Int> availableTargets = GetAllBuildClusterTargets();
                HashSet<Vector2Int> availableIndices = new HashSet<Vector2Int>(availableTargets);
                List<Vector2Int> targets = availableTargets;
                if (specifiedRange)
                {
                    targets = GetBuildClusterTargets();
                }
                int savedFiles = 0;
                int skippedFiles = 0;
                TerrainCtor.InitTerrainData(terSet, heightDataModels);
                int highestLod = terSet.LODLevel - 1;
                for (int i = 0; i < targets.Count; i++)
                {
                    Vector2Int index = targets[i];
                    int longitude = index.x + terSet.startLL.x;
                    int latitude = index.y + terSet.startLL.y;
                    bool hasHeightData = availableIndices.Contains(index);
                    if (!hasHeightData)
                    {
                        Debug.LogError($"缺少高度数据，跳过地块：索引 {index}，经纬度 {longitude}, {latitude}。");
                        continue;
                    }
                    // 调用 CreateClusterData 直接创建 cluster 数据
                    TerrainCluster cluster = TerrainCtor.CreateClusterData(longitude, latitude);
                    try
                    {
                        for (int lod = highestLod; lod >= 0; lod--)
                        {
                            string fileName = TerrainSettingSO.GetClusterFileName(longitude, latitude, lod);
                            string outputFile = Path.Combine(exportHandleMeshPath, fileName);
                            bool skipExisting = noOverride && File.Exists(outputFile);
                            if (skipExisting)
                            {
                                skippedFiles++;
                                continue;
                            }
                            int currentLod = lod;
                            Action<int> reportProgress = completedTiles =>
                            {
                                float tileProgress = (float)completedTiles / cluster.TileList.Count;
                                float clusterProgress = (highestLod - currentLod + tileProgress) / terSet.LODLevel;
                                float progress = (i + clusterProgress) / targets.Count;
                                string message = $"地块 {longitude}, {latitude}，LOD{currentLod}，Tile {completedTiles}/{cluster.TileList.Count}";
                                bool canceled = EditorUtility.DisplayCancelableProgressBar("构建并保存地形", message, progress);
                                if (canceled)
                                {
                                    cancellation.Cancel();
                                }
                            };
                            // 设置 mesh 数据，因为我们保存 mesh 需要通过 cluster（gameobject）
                            await TerrainCtor.SetMeshData_ByLOD(cluster, lod, cancellation.Token, reportProgress);
                            cancellation.Token.ThrowIfCancellationRequested();
                            SaveClusterLOD(cluster, lod);
                            savedFiles++;

                            // 持久化mesh完毕，释放cluster的所有mesh数据
                            foreach (TerrainTile tile in cluster.TileList)
                            {
                                tile.ReleaseLODData(lod);
                            }
                        }
                    }
                    finally
                    {
                        cluster.Dispose();
                    }
                }
                AssetDatabase.Refresh();
                TerrainAddressablesEditor.AddTerrainMeshToAB(exportHandleMeshPath);
                Debug.Log($"地形保存完成：保存 {savedFiles} 个 LOD 文件，跳过已有文件 {skippedFiles} 个。");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("已取消地形保存，已完成的 LOD 文件保留。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                cancellation.Dispose();
                EditorUtility.ClearProgressBar();
                isLodOperationRunning = false;
            }
        }

        [FoldoutGroup("构建地形-高度图流程")]
        [Button("一键式加载_指定范围地块", ButtonSizes.Medium)]
        private async void LoadLowestTerrainLODInRange()
        {
            await LoadTerrainLODAssets(true, GetBuildClusterTargets());
        }

        [FoldoutGroup("构建地形-高度图流程")]
        [Button("一键式加载_所有", ButtonSizes.Medium)]
        private async void LoadLowestTerrainLOD()
        {
            await LoadTerrainLODAssets(true);
        }

        private async Task LoadTerrainLODAssets(bool lowestOnly, List<Vector2Int> targets = null)
        {
            if (isLodOperationRunning)
            {
                return;
            }
            isLodOperationRunning = true;
            try
            {
                string pattern = TerrainSettingSO.GetClusterFileSuffixName();
                if (lowestOnly)
                {
                    pattern = "*_LOD0_terrain_cluster.bytes";
                }
                // 加载所有 cluster 的地块 lod，获取它们的 filename
                string[] files;
                if (targets == null)
                {
                    files = Directory.GetFiles(exportHandleMeshPath, pattern);
                }
                else
                {
                    files = new string[targets.Count];
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Vector2Int index = targets[i];
                        int longitude = index.x + terSet.startLL.x;
                        int latitude = index.y + terSet.startLL.y;
                        string fileName = TerrainSettingSO.GetClusterFileName(longitude, latitude, 0);
                        files[i] = Path.Combine(exportHandleMeshPath, fileName);
                    }
                }
                int loadedFiles = 0;
                if (files.Length == 0)
                {
                    Debug.Log("没有找到匹配的地形 LOD 资产。");
                    return;
                }
                int previewLod = terSet.LODLevel - 1;
                if (lowestOnly)
                {
                    previewLod = 0;
                }
                TerrainCtor.InitTerrainPreview(mapSet, terSet, terMaterial, previewLod);

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    string name = Path.GetFileName(file);
                    bool parsed = TerrainSettingSO.TryParseClusterFileName(name, out long longitude, out long latitude, out int lod);
                    if (!parsed)
                    {
                        throw new InvalidDataException("Invalid LOD filename: " + name);
                    }
                    float progress = (float)i / files.Length;
                    bool canceled = EditorUtility.DisplayCancelableProgressBar("加载地形 LOD", name, progress);
                    if (canceled)
                    {
                        throw new OperationCanceledException();
                    }
                    try
                    {
                        using (FileStream stream = File.OpenRead(file))
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            int lon = checked((int)longitude);
                            int lat = checked((int)latitude);
                            // 加载完毕，应用 file 资产到 gameobject 上去显示
                            if (lowestOnly)
                            {
                                TerrainCtor.SetMeshData_LOD0ToAll(reader, lon, lat);
                            }
                            else
                            {
                                TerrainCtor.LoadPersistedCluster(reader, lon, lat, lod);
                            }
                        }
                        loadedFiles++;
                    }
                    catch (Exception exception)
                    {
                        Vector2Int index = new Vector2Int((int)longitude - terSet.startLL.x, (int)latitude - terSet.startLL.y);
                        Debug.LogError($"加载失败，跳过地块：索引 {index}，经纬度 {longitude}, {latitude}，文件 {file}。{exception.Message}");
                    }
                    await Task.Yield();
                }
                TerrainCtor.UpdateTerrain();
                Debug.Log($"地形加载完成：成功 {loadedFiles}/{files.Length} 个 LOD 文件，预览 LOD{previewLod}。");
            }
            catch (OperationCanceledException)
            {
                TerrainCtor.ClearClusterObj();
                Debug.Log("已取消加载并清理本次预览。");
            }
            catch (Exception exception)
            {
                TerrainCtor.ClearClusterObj();
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                isLodOperationRunning = false;
            }
        }

        // 输入 cluster 和 lodlevel， 保存它们
        private void SaveClusterLOD(TerrainCluster cluster, int lodLevel)
        {
            string fileName = TerrainSettingSO.GetClusterFileName(cluster.longitude, cluster.latitude, lodLevel);
            string outputFile = Path.Combine(exportHandleMeshPath, fileName);
            string temporaryFile = outputFile + ".tmp";
            try
            {
                using (FileStream stream = new FileStream(temporaryFile, FileMode.Create, FileAccess.Write))
                using (BufferedStream buffered = new BufferedStream(stream))
                using (BinaryWriter writer = new BinaryWriter(buffered))
                {
                    TerrainCtor.WriteClusterLOD(writer, cluster, lodLevel);
                }
                if (File.Exists(outputFile))
                {
                    File.Replace(temporaryFile, outputFile, null);
                }
                else
                {
                    File.Move(temporaryFile, outputFile);
                }
            }
            finally
            {
                if (File.Exists(temporaryFile))
                {
                    File.Delete(temporaryFile);
                }
            }
        }

        // 从当前的 heightdatamodel 里面获取所有要构建地块
        private List<Vector2Int> GetAllBuildClusterTargets()
        {
            List<Vector2Int> clusterIdxList = new List<Vector2Int>();
            HashSet<Vector2Int> addedClusterIndices = new HashSet<Vector2Int>();
            Vector2Int startLL = terSet.startLL;
            foreach (HeightDataModel model in heightDataModels)
            {
                foreach (HeightData heightData in model.HeightDataList)
                {
                    int clusterX = heightData.longitude - startLL.x;
                    int clusterY = heightData.latitude - startLL.y;
                    Vector2Int clusterIdx = new Vector2Int(clusterX, clusterY);
                    bool isNewCluster = addedClusterIndices.Add(clusterIdx);
                    if (isNewCluster)
                    {
                        clusterIdxList.Add(clusterIdx);
                    }
                }
            }
            return clusterIdxList;
        }

        // 构建指定 索引的 地块
        private void BuildClusters_ForEdit(List<Vector2Int> clusterIdxList)
        {
            if (heightDataModels == null)
            {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }
            if (TerrainCtor == null)
            {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainGenTask terrainGenTask = new TerrainGenTask(heightDataModels, terSet, TerrainCtor,
                clusterIdxList, shouldGenRiver, shouldGenLODBySimplify, genRuntimeClusterMesh);
            int taskID = TaskManager.GetInstance().StartProgress(TaskTickLevel.Medium, terrainGenTask);
            TerGenTaskPop.GetPopInstance().ShowBasePop(terrainGenTask);
            terrainGenTask.StartTask(taskID);
        }

        private List<Vector2Int> GetBuildClusterTargets()
        {
            int clusterNum = (rightUpClsIdx.y - leftDownClsIdx.y + 1) * (rightUpClsIdx.x - leftDownClsIdx.x + 1);
            List<Vector2Int> clusterIdxList = new List<Vector2Int>(clusterNum);
            for (int i = leftDownClsIdx.x; i <= rightUpClsIdx.x; i++)
            {
                for (int j = leftDownClsIdx.y; j <= rightUpClsIdx.y; j++)
                {
                    Vector2Int clusterIdx = new Vector2Int(i, j);
                    clusterIdxList.Add(clusterIdx);
                }
            }
            return clusterIdxList;
        }

        [FoldoutGroup("构建地形-高度图流程", 0)]
        [Button("刷新地形", ButtonSizes.Medium)]
        private void ShowTerrain() 
        {
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.UpdateTerrain();
        }

        [FoldoutGroup("构建地形-高度图流程", 0)]
        [Button("清空地形", ButtonSizes.Medium)]
        private void ClearHeightMesh() 
        {
            if (TerrainCtor == null) {
                Debug.LogError("do not init height ctor!");
                return;
            }

            TerrainCtor.ClearClusterObj();
            Debug.Log("clear ter cluster over");
        }

        #endregion

        #region 构建地形-Hex流程[Deprecated]

        // TODO : 下面一整块在后续都会被去除掉！！不再使用高度图来构建 Hex 的地图，可能仅会通过高度图确定某个地区的地形
        // 然后再用新的类cv的流程去构建地图

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [LabelText("当前操作Hex地图对象")]
        //public HexMapSO rawHexMapSO;
        public string temp = "占位符";

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [LabelText("当前Hex地图材质")]
        public Material hexMaterial;

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [LabelText("当前Hex地图纹理")]
        public Texture2D rawHexMapTexture;

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [LabelText("导出位置")]
        public string exportHexMapSOPath = MapStoreEnum.TerrainHexMapPath;

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [LabelText("起始经纬度")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [LabelText("当前操作的cluster索引")]
        public Vector2Int curClusterIdx_Hex;

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [Button("生成RawHexMapSO", ButtonSizes.Medium)]
        private void GenerateRawHexMap() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, terSet, hexSet, null);

            // TODO : 不应该使用 高度图生成 RawHexMapSO
            //rawHexMapSO = CreateInstance<HexMapSO>();
            //rawHexMapSO.InitRawHexMap(EditorSceneManager.hexSet.mapWidth, EditorSceneManager.hexSet.mapHeight);
            //HexCtor.GenerateRawHexMap(startLongitudeLatitude, rawHexMapSO, heightDataManager);

        }

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [Button("生成RawHexMap纹理", ButtonSizes.Medium)]
        private void GenerateRawHexTexture() {
            //if (rawHexMapSO == null) {
            //    Debug.LogError("rawHexMapSO is null!");
            //    return;
            //}

            //rawHexMapTexture = new Texture2D(rawHexMapSO.mapWidth, rawHexMapSO.mapHeight);
            //foreach (var gridTerrainData in rawHexMapSO.GridTerrainDataList) {
            //    Vector2Int pos = gridTerrainData.GetHexPos();
            //    Color color = gridTerrainData.GetTerrainColor();
            //    // TODO : 下面的生成步骤还是有问题！没有照顾到 hex 坐标的特性
            //    //Vector2Int fixed_pos = new Vector2Int(rawHexMapTexture.width - pos.x, rawHexMapTexture.height - pos.y);
            //    //Vector2Int fixed_pos = new Vector2Int(pos.x, rawHexMapTexture.height - pos.y);
            //    //Vector2Int fixed_pos = new Vector2Int(rawHexMapTexture.width - pos.x, pos.y);
            //    //Vector2Int fixed_pos = new Vector2Int(pos.y, pos.x);
            //    Vector2Int fixed_pos = new Vector2Int(pos.x, pos.y);
            //    rawHexMapTexture.SetPixel(fixed_pos.x, fixed_pos.y, color);
            //}
            //Debug.Log($"generate hex texture : {rawHexMapTexture.width}x{rawHexMapTexture.height}");
        }

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [Button("保存RawHexMapSO", ButtonSizes.Medium)]
        private void SaveRawHexMap() {

            CheckExportPath();
            //string soName = $"RawHexMap_{rawHexMapSO.mapWidth}x{rawHexMapSO.mapHeight}_{UnityEngine.Random.Range(0, 100)}.asset";
            //string RawHexPath = exportHexMapSOPath + $"/{soName}";
            //AssetDatabase.CreateAsset(rawHexMapSO, RawHexPath);
            //Debug.Log($"successfully create Hex Map, path : {RawHexPath}");
        }

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [Button("保存RawHexMap纹理", ButtonSizes.Medium)]
        private void SaveRawHexTexture() {
            if (rawHexMapTexture == null) {
                Debug.LogError("rawHexMapTexture is null!");
                return;
            }

            CheckExportPath();
            string textureName = $"hexTexture_{rawHexMapTexture.width}x{rawHexMapTexture.height}_{UnityEngine.Random.Range(0, 100)}";
            TextureUtility.SaveTextureAsAsset(exportHexMapSOPath, textureName, rawHexMapTexture);
        }

        private void CheckExportPath() {
            string mapSOFolerName = AssetsUtility.GetFolderFromPath(exportHexMapSOPath);
            if (!AssetDatabase.IsValidFolder(exportHexMapSOPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.TerrainRootPath, mapSOFolerName);
            }
        }

        [FoldoutGroup("构建地形-Hex流程[Deprecated]")]
        [Button("生成Hex版本Terrain", ButtonSizes.Medium)]
        private void GenerateTerrainByHex() {
            if (EditorSceneManager.HexSet == null) {
                Debug.LogError("hex Set is null!");
                return;
            }
            //if (rawHexMapSO == null) {
            //    Debug.LogError("rawHexMapSO is null!");
            //    return;
            //}
            if (TerrainCtor == null) {
                Debug.LogError("TerrainCtor is null!");
                return;
            }
            

            //rawHexMapSO.UpdateGridTerrainData();
            
            // TODO : hex 流程有待完善
            //TerrainCtor.BuildCluster(curClusterIdx_Hex.x, curClusterIdx_Hex.y); // ?

            // Terrain的size和hexmap的size不一定要对应
            // 第一步：继续按TerrainCtor的方式去生成 TerrainMesh（cluster-tile的结构）
            // 第二步：遍历生成mesh的时候，找到该点对应的hex格子
            // 第三步：根据hex格子高度，设置vert高度；根据hex格子坡度，调整vert
        }

        #endregion


        // TODO : UNCOMPLETE
        #region 地形网格持久化

        [FoldoutGroup("地形持久化")]
        [LabelText("地形资产所在路径"), ReadOnly]
        public string exportHandleMeshPath = MapStoreEnum.TerrainMeshSerializedPath;    // TerrainMeshAssetPath


        [FoldoutGroup("地形持久化")]
        [Button("导出当前地形为资产", ButtonSizes.Medium)]
        private void ExportTerrainAsMesh()
        {
            Directory.CreateDirectory(exportHandleMeshPath);
            foreach (TerrainCluster cluster in TerrainCtor.ClusterList)
            {
                if (!cluster.IsLoaded)
                {
                    continue;
                }
                for (int lod = 0; lod < terSet.LODLevel; lod++)
                {
                    bool completeLod = true;
                    foreach (TerrainTile tile in cluster.TileList)
                    {
                        TerrainMeshData data = tile.GetLODMeshes()[lod];
                        if (data == null)
                        {
                            completeLod = false;
                            break;
                        }
                        data.BuildOriginMeshWrapper();
                    }
                    if (completeLod)
                    {
                        SaveClusterLOD(cluster, lod);
                    }
                }
            }
            AssetDatabase.Refresh();
            TerrainAddressablesEditor.AddTerrainMeshToAB(exportHandleMeshPath);
        }

        [FoldoutGroup("地形持久化")]
        [Button("测试-刷新group", ButtonSizes.Medium)]
        private void RefreshTerrainLoader()
        {
            TerrainAddressablesEditor.AddTerrainMeshToAB(exportHandleMeshPath);
        }


        [FoldoutGroup("地形持久化")]
        [Button("导入资产到当前地形", ButtonSizes.Medium)]
        // TODO: Reuse the Runtime import entry after the one-click loading flow is implemented and verified.
        private async void ImportMeshToTerrain()
        {
            await LoadTerrainLODAssets(false);
        }

        #endregion


        #region 地形减面

        [FoldoutGroup("地形减面")]
        [LabelText("当前简化的cluster索引")]
        public Vector2Int simplifyClsIdx;

        [FoldoutGroup("地形减面")]
        [LabelText("当前简化的tile索引")]
        public Vector2Int simplifyTileIdx;

        [FoldoutGroup("地形减面")]
        [LabelText("顶点优化目标")]
        public float simplifyTarget = 0.5f;

        // TODO ： 
        [FoldoutGroup("地形减面")]
        [Button("对当前Mesh进行减面", ButtonSizes.Medium)]
        private void ExeMeshReduction() {
            // NOTE : qem: https://zhuanlan.zhihu.com/p/547256817
            if (TerrainCtor == null) {
                Debug.LogError($"terrian ctor is null, static ctor statu: {EditorSceneManager.TerrainCtor != null}!");
                return;
            }
            TerrainCtor.ExeSimplify(simplifyClsIdx.x, simplifyClsIdx.y, simplifyTileIdx.x, simplifyTileIdx.y, simplifyTarget);
        }

        #endregion

        public override void Destory() {
            if (rawHexMapTexture != null) {
                GameObject.DestroyImmediate(rawHexMapTexture);
                rawHexMapTexture = null;
            }
        }

    }

}
