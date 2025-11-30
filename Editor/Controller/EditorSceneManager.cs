using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using static LZ.WarGameMap.Runtime.TerrainMeshDataBinder;
using LZ.WarGameMap.Runtime.Model;

namespace LZ.WarGameMap.MapEditor
{
    [InitializeOnLoad]
    public class EditorSceneManager
    {
        static EditorSceneManager Instance;

        public static EditorSceneManager GetInstance() {
            InitSO();
            InitScene();
            InitCtor();
            if (Instance == null) {
                Instance = new EditorSceneManager();
                UnityEditorManager.RegisterUpdate(Instance.UpdateSceneHex);
                UnityEditorManager.RegisterUpdate(Instance.UpdateSceneTer);
            }
            return Instance;
        }


        static GridTerrainSO gridTerrainSO;
        public static GridTerrainSO GridTerrainSO { get { return gridTerrainSO; } }

        static CountrySO countrySO;
        public static CountrySO CountrySO { get { return countrySO; } }


        static MapRuntimeSetting mapSet;
        public static MapRuntimeSetting MapSet { get { return mapSet; } }

        static TerrainSettingSO terSet;
        public static TerrainSettingSO TerSet { get { return terSet; } }

        static HexSettingSO hexSet;
        public static HexSettingSO HexSet { get { return hexSet; } }

        public static TerrainConstructor TerrainCtor {  get; private set; }
        public static HexmapConstructor HexCtor { get; private set; }
        public static MapRenderConstructor RenderCtor { get; private set; }

        public static MapSceneObjs mapScene { get; private set; }


        #region 初始化Editor的Scene内容

        static EditorSceneManager() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying) {
                InitSO();
                InitScene();
                InitCtor();
                EditorSceneManager.GetInstance().InitTerScene();
                EditorSceneManager.GetInstance().InitHexScene();
                //Debug.Log($"update editor scene over, terSet : {terSet != null}, hexSet : {hexSet != null}");
            }
        }

        public EditorSceneManager() {

            // this init just prepare light data, and do not load mesh data
            InitTerScene();
            InitHexScene();
        }

        private static void InitSO() {
            BaseMapEditor.FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");
            BaseMapEditor.FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");
            BaseMapEditor.FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");
        
            BaseMapEditor.FindOrCreateSO<GridTerrainSO>(ref gridTerrainSO, MapStoreEnum.GamePlayGridTerrainDataPath, "GridTerrainSO_Default.asset");
            BaseMapEditor.FindOrCreateSO<CountrySO>(ref countrySO, MapStoreEnum.GamePlayCountryDataPath, "CountrySO_256x256.asset");
        }

        private static void InitScene() {
            if (mapScene == null) {
                mapScene = new MapSceneObjs();
            }
            
            mapScene.InitMapObj();
        }

        private static void InitCtor() {
            // init terrain cons
            if(TerrainCtor == null) {
                TerrainCtor = mapScene.mapRootObj.GetComponent<TerrainConstructor>();
                if (TerrainCtor == null) {
                    TerrainCtor = mapScene.mapRootObj.AddComponent<TerrainConstructor>();
                }
            }
            TerrainCtor.SetMapPrefab(mapScene.mapRootObj.transform, mapScene.heightMeshParentObj.transform, mapScene.riverMeshParentObj.transform);

            // init hex cons
            if (HexCtor == null) {
                HexCtor = mapScene.mapRootObj.GetComponent<HexmapConstructor>();
                if (HexCtor == null) {
                    HexCtor = mapScene.mapRootObj.AddComponent<HexmapConstructor>();
                }
            }
            HexCtor.SetHexSetting(hexSet, mapScene.hexClusterParentObj.transform, null);

            // RenderCtor
            if (RenderCtor == null)
            {
                RenderCtor = mapScene.mapRootObj.GetComponent<MapRenderConstructor>();
                if (RenderCtor == null)
                {
                    RenderCtor = mapScene.mapRootObj.AddComponent<MapRenderConstructor>();
                }
            }
        }

        // Call it when destory the editor window
        public static void Dispose() {
            // TODO : un register all gizmos event

            UnityEditorManager.UnregisterUpdate(Instance.UpdateSceneTer);
            UnityEditorManager.UnregisterUpdate(Instance.UpdateSceneHex);

            mapScene.Dispose();
            Instance = null;
        }

        #endregion


        static bool ShowingTer = false;

        static bool ShowingHex = false;

        private double updateTimeInterval_Ter = 0.5f;

        private double lastUpdateTime_Ter = 0;

        private double updateTimeInterval_Hex = 0.3f;

        private double lastUpdateTime_Hex = 0;

        // call it to decide whether show Ter Scene
        public void UpdateSceneView(bool showTer, bool showHex) {
            DebugUtility.Log($"switch editor panel : {showTer}, {showHex}");
            if(ShowingTer != showTer) {
                if (showTer) {
                    GizmosCtrl.GetInstance().RegisterGizmoEvent(DrawTerrainMes);
                } else {
                    GizmosCtrl.GetInstance().UnregisterGizmoEvent(DrawTerrainMes);
                }
                ShowingTer = showTer;
            }

            // TODO : show hex / gizmos ...
            if (ShowingHex != showHex) {
                if (showHex) {
                    GizmosCtrl.GetInstance().RegisterGizmoEvent(DrawHexMes);
                } else {
                    GizmosCtrl.GetInstance().UnregisterGizmoEvent(DrawHexMes);
                }
                ShowingHex = showHex;
            }
        }

        // Call it to load terrain mesh data
        public void LoadTerScene(int scope, List<MeshAssetBinder> clusterMeshDatas, List<HeightDataModel> heightDataModels, Material material) {
            this.heightDataModels = heightDataModels;
            InitTerScene();

            // TODO : hexSet 也要从 持久化文件里面读取
            TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, null, material, null);

            foreach (var binder in clusterMeshDatas)
            {
                string curHandleMeshPath = binder.assetPath;
                ImportMeshToTerrain_Binary(curHandleMeshPath);
            }

            //TerrainCtor.UpdateTerrain();
        }

        public void ClearTerScene() {
            TerrainCtor.ClearClusterObj();
        }

        // Call it and register it to update Ter Scene
        public void UpdateSceneTer() {
            if (!ShowingTer) {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now - lastUpdateTime_Ter < updateTimeInterval_Ter) {
                return;
            }
            lastUpdateTime_Ter = now;

            TerrainCtor.UpdateTerrain();
        }

        public void LoadHexScene(Material hexMat) {
            // TODO : 加载之前的对Hex的编辑结果进入 Scene 里面
            // TODO : hex 编辑结果 放到一张大纹理里面，考虑到 全地图的 HexGrid 一共 3000 x 3000，所以应该可以放得下
            InitHexScene();

            HexCtor.InitHexConsRectangle(hexMat);
        }

        public void ClearHexScene() {
            HexCtor.ClearClusterObj();
        }

        public void UpdateSceneHex() {
            if (!ShowingHex) {
                return;
            }

            //Debug.Log("now update scene hex!");
            double now = EditorApplication.timeSinceStartup;
            if (now - lastUpdateTime_Hex < updateTimeInterval_Hex) {
                return;
            }
            lastUpdateTime_Hex = now;
            HexCtor.UpdateHex();
            //Debug.Log("update scene hex over!");
        }

        public void LoadMapRenderer(Material mainMat, Material riverMat)
        {
            RenderCtor.InitMapRenderCons(terSet, hexSet, mapSet, gridTerrainSO, countrySO);
            RenderCtor.InitMaterial(mainMat, riverMat);
        }


        #region Scene Terrain 场景构建

        int terFontSize = 10;

        [Serializable]
        struct TerClusterInfo {
            // this struct indicates the info that the cluster should show in scene
            public Vector2 leftDown;
            public Vector2 rightUp;
            public Vector2 center;

            public Vector2Int LL; // longitudeAndLatitude
            public int curLODLevel;

            // load this terrain cluster in to scene
            public bool isLoadInScene;
            // does this cluster show in scene
            public bool isShowInScene;

            public TerClusterInfo(Vector2 leftDown, Vector2 rightUp, Vector2Int longitudeAndLatitude) {
                this.leftDown = leftDown;
                this.rightUp = rightUp;
                center = (leftDown + rightUp) / 2;
                this.LL = longitudeAndLatitude;
                this.curLODLevel = 0;
                this.isLoadInScene = false;
                this.isShowInScene = false;
            }

            public void ChangeLoadStatu(bool isLoadInScene) {
                this.isLoadInScene = isLoadInScene;
            }

            public void ChangeShowStatu(bool isShowInScene) {
                this.isShowInScene = isShowInScene;
            }
        }

        List<TerClusterInfo> terClusterInfoList;
        Dictionary<Vector2Int, TerClusterInfo> terClusterInfoDic;

        List<HeightDataModel> heightDataModels;

        private void InitTerScene() {
            //Debug.Log(terSet == null);
            terClusterInfoList = new List<TerClusterInfo>(terSet.terrainSize.x * terSet.terrainSize.z);
            terClusterInfoDic = new Dictionary<Vector2Int, TerClusterInfo>();

            Vector2Int startLL = terSet.startLL;
            for (int i = 0; i < terSet.terrainSize.x; i++) {
                for (int j = 0; j < terSet.terrainSize.z; j++) {
                    Vector2 clusterStart = new Vector2(i * terSet.clusterSize, j * terSet.clusterSize);
                    Vector2 clusterRightUp = clusterStart + new Vector2(terSet.clusterSize, terSet.clusterSize);
                    Vector2Int LL = startLL + new Vector2Int(i, j);
                    TerClusterInfo terClusterInfo = new TerClusterInfo(clusterStart, clusterRightUp, LL);

                    terClusterInfoList.Add(terClusterInfo);
                    terClusterInfoDic.Add(LL, terClusterInfo);
                }
            }
        }

        private void DrawTerrainMes() {
            Vector2 terLeftDown = new Vector2(0, 0);
            Vector2 terRightUp = new Vector2(terSet.clusterSize * terSet.terrainSize.x, terSet.clusterSize * terSet.terrainSize.z);
            GizmosUtils.DrawRect(terLeftDown, terRightUp, GizmosUtils.GetRandomColor(0));

            foreach (var info in terClusterInfoList)
            {
                string clusterTxt = $"地块_{info.LL.x}_{info.LL.y}_LOD{info.curLODLevel}";
                GizmosUtils.DrawRect(info.leftDown, info.rightUp, GizmosUtils.GetRandomColor(info.LL.x + info.LL.y));
                GizmosUtils.DrawText(new Vector3(info.center.x, 0, info.center.y), clusterTxt, terFontSize, Color.black);
            }
        }

        private Vector2Int ImportMeshToTerrain_Binary(string curHandleMeshPath) {
            Vector2Int LL = new Vector2Int();
            using (FileStream fs = new FileStream(curHandleMeshPath, FileMode.Open, FileAccess.Read))
            using (BufferedStream bufferedStream = new BufferedStream(fs))
            using (BinaryReader reader = new BinaryReader(bufferedStream)) {

                TerrainSetting trSet = new TerrainSetting();
                trSet.ReadFromBinary(reader);
                int terrainWidth = trSet.terrainSize.x;
                int terrainHeight = trSet.terrainSize.z;
                if(trSet != terSet.GetTerrainSetting()) {
                    Debug.LogError($"this meshFile'setting is not equal to cur terSet : {trSet.ToString()}");
                    return Vector2Int.zero;
                }

                // TODO : hex set should also read from the file

                // todo : 这部分有问题！！！
                int validClusterNum = reader.ReadInt32();
                for (int i = 0; i < validClusterNum; i++) {
                    TerrainCluster cls = new TerrainCluster();
                    cls.ReadFromBinary(reader);
                    LL = cls.GetClusterLL();
                    
                    TerrainCtor.ExportClusterByBinary(cls.idxX, cls.idxY, cls.longitude, cls.latitude, reader);
                }
            }

            Debug.Log($"export {LL} cls, path : {curHandleMeshPath}");
            return LL;
        }

        #endregion


        #region Scene Hex 场景构建

        int hexFontSize = 8;

        public struct ClusterBound {
            public int clusterIdxX { get; private set; }
            public int clusterIdxY { get; private set; }

            public Vector2 leftDown, rightDown;
            public Vector2 leftUp, rightUp;
            public Vector2 center;

            public float width, height;

            public ClusterBound(int i, int j, int hexSize, int clusterSize) {
                this.clusterIdxX = i;
                this.clusterIdxY = j;
                // NOTE : 目前的 Hex 生成方式是第二行向左偏移 (OffsetHexCoord)
                // 左边界：第一行Hex格子的左边
                // 右边界：第一行Hex格子的右边
                // 下边界：第一行Hex格子的底
                // 上边界：最上行Hex格子的左上角/右上角

                float left = i * (Mathf.Sqrt(3) * hexSize * clusterSize);
                float right = (i + 1) * (Mathf.Sqrt(3) * hexSize * clusterSize);
                float up, down;
                if (clusterSize % 2 == 0) {
                    up = (j + 1) * (hexSize * 3 * clusterSize / 2);
                    down = j * (hexSize * 3 * clusterSize / 2);
                } else {
                    // cluster size can not be odd
                    up = (j + 1) * (hexSize * 3 * clusterSize / 2 + hexSize);
                    down = j * (hexSize * 3 * clusterSize / 2 + hexSize);
                }

                width = right - left;
                height = up - right;

                leftDown = new Vector2(left, down);
                rightDown = new Vector2(right, down);
                leftUp = new Vector2(left, up);
                rightUp = new Vector2(right, up);
                center = (leftDown + rightUp) / 2;
            }

        }

        Dictionary<Vector2Int, ClusterBound> clusterIdxBoundDict;


        // TODO : load hex message....
        public void InitHexScene() {
            // TODO : 这里放置 Hex Scene 的 Gizmos 数据
            clusterIdxBoundDict = new Dictionary<Vector2Int, ClusterBound>();
        }

        private void DrawHexMes() {
            // NOTE : 有潜在风险，这里需要与 HexCons 的UpdateHex 处进行同步
            Vector3 cameraPos = Camera.main.transform.position;
            Vector2Int clsIdx = ClusterSize.GetClusterIdxByPos(cameraPos);
            clusterIdxBoundDict.Clear();

            // hex scope is small, so the data is small
            int hexScope = hexSet.hexAOIScope / 2;
            for (int i = -hexScope + clsIdx.x; i <= hexScope + clsIdx.x; i++) {
                for (int j = -hexScope + clsIdx.y; j <= hexScope + clsIdx.y; j++) {
                    // not valid hex cluster index
                    if (i < 0 || i > hexSet.mapWidth - 1 || j < 0 || j > hexSet.mapHeight - 1) {
                        continue;
                    }
                    var curIdx = new Vector2Int(i, j);
                    ClusterBound clusterBound = new ClusterBound(i, j, hexSet.hexGridSize, hexSet.clusterSize);
                    clusterIdxBoundDict.Add(curIdx, clusterBound);
                }
            }

            foreach (var info in clusterIdxBoundDict.Values)
            {
                string clusterTxt = $"六边形群_{info.clusterIdxX}_{info.clusterIdxY}";
                GizmosUtils.DrawRect(info.leftDown, info.rightUp, GizmosUtils.GetRandomColor(info.clusterIdxX + info.clusterIdxY));
                GizmosUtils.DrawText(new Vector3(info.center.x, 0, info.center.y), clusterTxt, hexFontSize, Color.black);
            }
        }

        #endregion
    }

    [Serializable]
    public class MapSceneObjs : IDisposable {

        // obj in scene
        public GameObject mapRootObj {  get; private set; }

        public GameObject heightMeshParentObj { get; private set; }

        public GameObject hexClusterParentObj { get; private set; }

        public GameObject hexTextureParentObj { get; private set; }

        public GameObject riverDataParentObj { get; private set; }
        public GameObject riverMeshParentObj { get; private set; }

        public GameObject mountainParentObj { get; private set; }

        public void Dispose() {
            GameObject.DestroyImmediate(hexClusterParentObj);
            GameObject.DestroyImmediate(heightMeshParentObj);
            GameObject.DestroyImmediate(mapRootObj);
        }

        public void InitMapObj() {
            if (mapRootObj == null) 
            {
                mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
                if (mapRootObj == null) {
                    mapRootObj = new GameObject(MapSceneEnum.MapRootName);
                }
            }

            if (heightMeshParentObj == null) 
            {
                heightMeshParentObj = GameObject.Find(MapSceneEnum.TerrainParentName);
                if (heightMeshParentObj == null) {
                    heightMeshParentObj = new GameObject(MapSceneEnum.TerrainParentName);
                }
            }
            heightMeshParentObj.transform.SetParent(mapRootObj.transform);

            if (hexClusterParentObj == null) 
            {
                hexClusterParentObj = GameObject.Find(MapSceneEnum.HexClusterParentName);
                if (hexClusterParentObj == null) {
                    hexClusterParentObj = new GameObject(MapSceneEnum.HexClusterParentName);
                }
            }
            hexClusterParentObj.transform.SetParent(mapRootObj.transform);

            if (hexTextureParentObj == null) 
            {
                hexTextureParentObj = GameObject.Find(MapSceneEnum.HexTextureParentName);
                if (hexTextureParentObj == null) {
                    hexTextureParentObj = new GameObject(MapSceneEnum.HexTextureParentName);
                }
            }
            hexTextureParentObj.transform.SetParent(mapRootObj.transform);

            if (riverDataParentObj == null) 
            {
                riverDataParentObj = GameObject.Find(MapSceneEnum.RiverDataParentName);
                if (riverDataParentObj == null) {
                    riverDataParentObj = new GameObject(MapSceneEnum.RiverDataParentName);
                }
            }
            riverDataParentObj.transform.SetParent(mapRootObj.transform);

            // RiverMeshParentName
            if (riverMeshParentObj == null)
            {
                riverMeshParentObj = GameObject.Find(MapSceneEnum.RiverMeshParentName);
                if (riverMeshParentObj == null)
                {
                    riverMeshParentObj = new GameObject(MapSceneEnum.RiverMeshParentName);
                }
            }
            riverMeshParentObj.transform.SetParent(mapRootObj.transform);

            
            if (mountainParentObj == null)
            {
                mountainParentObj = GameObject.Find(MapSceneEnum.MountainParentName);
                if (mountainParentObj == null)
                {
                    mountainParentObj = new GameObject(MapSceneEnum.MountainParentName);
                }
            }
            mountainParentObj.transform.SetParent(mapRootObj.transform);
        }
        
    }
}
