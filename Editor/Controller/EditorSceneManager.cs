using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static LZ.WarGameMap.Runtime.TerrainMeshDataBinder;

namespace LZ.WarGameMap.MapEditor
{
    [InitializeOnLoad]
    public class EditorSceneManager
    {
        static EditorSceneManager Instance;

        public static EditorSceneManager GetInstance() {
            GetSetSO();
            InitScene();
            InitCtor();
            if (Instance == null) {
                Instance = new EditorSceneManager();
            }
            return Instance;
        }

        public static void DisposeInstance() {
            Instance = null;
        }


        public static MapRuntimeSetting mapSet { get; private set; }
        public static TerrainSettingSO terSet { get; private set; }
        public static HexSettingSO hexSet { get; private set; }

        public static TerrainConstructor TerrainCtor {  get; private set; }
        public static HexmapConstructor HexCtor { get; private set; }

        public static MapSceneObjs mapScene { get; private set; }


        #region ��ʼ��Editor��Scene����

        static EditorSceneManager() {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !Application.isPlaying) {
                GetSetSO();
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

        private static void GetSetSO() {
            if (terSet == null) {
                string terrainSettingPath = MapStoreEnum.WarGameMapSettingPath + "/TerrainSetting_Default.asset";
                terSet = AssetDatabase.LoadAssetAtPath<TerrainSettingSO>(terrainSettingPath);
                if (terSet == null) {
                    Debug.Log($"Terrain Setting not found in path : {terrainSettingPath}");
                }
            }
            if (hexSet == null) {
                string hexSettingPath = MapStoreEnum.WarGameMapSettingPath + "/HexSetting_Default.asset";
                hexSet = AssetDatabase.LoadAssetAtPath<HexSettingSO>(hexSettingPath);
                if (hexSet == null) {
                    Debug.Log($"Hex Setting not found in path : {hexSettingPath}");
                }
            }
            if (mapSet == null) {
                string mapSettingPath = MapStoreEnum.WarGameMapSettingPath + "/TerrainRuntimeSet_Default.asset";
                mapSet = AssetDatabase.LoadAssetAtPath<MapRuntimeSetting>(mapSettingPath);
                if (mapSet == null) {
                    Debug.Log($"Map runtime Setting not found in path : {mapSettingPath}");
                }
            }

            //FindSO<TerrainSettingSO>(terSet, "TerrainSetting_Default.asset");
            //FindSO<HexSettingSO>(hexSet, "HexSetting_Default.asset");
            //FindSO<MapRuntimeSetting>(mapSet, "TerrainRuntimeSet_Default.asset");

            //Debug.Log($"SO load statu : {terSet != null}, {hexSet != null}, {mapSet != null}");
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

        }


        // TODO : call it when destory the editor window
        public static void Dispose() {
            // TODO : un register all gizmos event

            // TODO : ��������scene��ͷ������
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
            Debug.Log($"switch editor panel : {showTer}, {showHex}");
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

        // call it to load terrain mesh data
        public void LoadTerScene(int scope, List<MeshAssetBinder> clusterMeshDatas, List<HeightDataModel> heightDataModels, Material material) {
            this.heightDataModels = heightDataModels;
            InitTerScene();

            // TODO : hexSet ҲҪ�� �־û��ļ������ȡ
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

        // call it and register it to update Ter Scene
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
            // TODO : ����֮ǰ�Ķ�Hex�ı༭������� Scene ����
            // TODO : hex �༭��� �ŵ�һ�Ŵ��������棬���ǵ� ȫ��ͼ�� HexGrid һ�� 3000 x 3000������Ӧ�ÿ��Էŵ���

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


        #region Scene Terrain ��������

        int terFontSize = 10;

        TerMeshGenMethod meshGenMethod;

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
                string clusterTxt = $"�ؿ�_{info.LL.x}_{info.LL.y}_LOD{info.curLODLevel}";
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

                // todo : �ⲿ�������⣡����
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


        #region Scene Hex ��������

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
                // NOTE : Ŀǰ�� Hex ���ɷ�ʽ�ǵڶ�������ƫ�� (OffsetHexCoord)
                // ��߽磺��һ��Hex���ӵ����
                // �ұ߽磺��һ��Hex���ӵ��ұ�
                // �±߽磺��һ��Hex���ӵĵ�
                // �ϱ߽磺������Hex���ӵ����Ͻ�/���Ͻ�

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
            // TODO : ������� Hex Scene �� Gizmos ����
            clusterIdxBoundDict = new Dictionary<Vector2Int, ClusterBound>();
        }

        private void DrawHexMes() {

            // NOTE : ��Ǳ�ڷ��գ�������Ҫ�� HexCons ��UpdateHex ������ͬ��
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
                string clusterTxt = $"������Ⱥ_{info.clusterIdxX}_{info.clusterIdxY}";
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

        public void Dispose() {
            GameObject.DestroyImmediate(hexClusterParentObj);
            GameObject.DestroyImmediate(heightMeshParentObj);
            GameObject.DestroyImmediate(mapRootObj);
        }

        public void InitMapObj() {
            if (mapRootObj == null) {
                mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
                if (mapRootObj == null) {
                    mapRootObj = new GameObject(MapSceneEnum.MapRootName);
                }
            }

            if (heightMeshParentObj == null) {
                heightMeshParentObj = GameObject.Find(MapSceneEnum.TerrainParentName);
                if (heightMeshParentObj == null) {
                    heightMeshParentObj = new GameObject(MapSceneEnum.TerrainParentName);
                }
            }
            heightMeshParentObj.transform.SetParent(mapRootObj.transform);

            if (hexClusterParentObj == null) {
                hexClusterParentObj = GameObject.Find(MapSceneEnum.HexClusterParentName);
                if (hexClusterParentObj == null) {
                    hexClusterParentObj = new GameObject(MapSceneEnum.HexClusterParentName);
                }
            }
            hexClusterParentObj.transform.parent = mapRootObj.transform;

            if (hexTextureParentObj == null) {
                hexTextureParentObj = GameObject.Find(MapSceneEnum.HexTextureParentName);
                if (hexTextureParentObj == null) {
                    hexTextureParentObj = new GameObject(MapSceneEnum.HexTextureParentName);
                }
            }
            hexTextureParentObj.transform.parent = mapRootObj.transform;

            if (riverDataParentObj == null) {
                riverDataParentObj = GameObject.Find(MapSceneEnum.RiverDataParentName);
                if (riverDataParentObj == null) {
                    riverDataParentObj = new GameObject(MapSceneEnum.RiverDataParentName);
                }
            }
            riverDataParentObj.transform.parent = mapRootObj.transform;

            // RiverMeshParentName
            if (riverMeshParentObj == null)
            {
                riverMeshParentObj = GameObject.Find(MapSceneEnum.RiverMeshParentName);
                if (riverMeshParentObj == null)
                {
                    riverMeshParentObj = new GameObject(MapSceneEnum.RiverMeshParentName);
                }
            }
            riverMeshParentObj.transform.parent = mapRootObj.transform;

        }

    }
}
