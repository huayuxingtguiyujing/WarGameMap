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
            if (Instance == null) {
                Instance = new EditorSceneManager();
            }
            return Instance;
        }


        public static MapRuntimeSetting mapSet { get; private set; }
        public static TerrainSettingSO terSet { get; private set; }
        public static HexSettingSO hexSet { get; private set; }

        public static TerrainConstructor TerrainCtor {  get; private set; }
        public static HexmapConstructor HexCtor { get; private set; }

        public static MapSceneObjs mapScene { get; private set; }


        #region 初始化Editor的Scene内容

        static EditorSceneManager() {
            GetSetSO();
            InitScene();
            InitCtor();

            EditorSceneManager.GetInstance().InitTerScene();
            EditorSceneManager.GetInstance().InitHexScene();
            Debug.Log("update editor scene over, terSet : {terSet != null}, hexSet : {hexSet != null}");
        }

        public EditorSceneManager() {
            GetSetSO();
            InitScene();
            InitCtor();

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

            Debug.Log($"SO load statu : {terSet != null}, {hexSet != null}, {mapSet != null}");
        }

        private static void FindSO<T>(T so, string assetName) where T : ScriptableObject {
            if (so == null) {
                string terrainSettingPath = MapStoreEnum.WarGameMapSettingPath + "/" + assetName;
                so = AssetDatabase.LoadAssetAtPath<T>(terrainSettingPath);
                if (so == null) {
                    Debug.Log($"Terrain Setting not found in path : {terrainSettingPath}");
                }
            }
        }

        private static void InitScene() {
            if (mapScene == null) {
                mapScene = new MapSceneObjs();
            }
            
            mapScene.InitMapObj();
        }

        private static void InitCtor() {
            // init terrain cons
            TerrainCtor = mapScene.mapRootObj.GetComponent<TerrainConstructor>();
            if (TerrainCtor == null) {
                TerrainCtor = mapScene.mapRootObj.AddComponent<TerrainConstructor>();
            }
            TerrainCtor.SetMapPrefab(mapScene.mapRootObj.transform, mapScene.heightMeshParentObj.transform);

            // init hex cons
            HexCtor = mapScene.mapRootObj.GetComponent<HexmapConstructor>();
            if (HexCtor == null) {
                HexCtor = mapScene.mapRootObj.AddComponent<HexmapConstructor>();
            }
            HexCtor.SetHexSetting(hexSet, mapScene.hexClusterParentObj.transform, null);
        }


        // TODO : call it when destory the editor window
        public static void Dispose() {
            // TODO : un register all gizmos event

            // TODO : 销毁所有scene里头的物体
            mapScene.Dispose();
        }

        #endregion


        static bool ShowingTer = false;

        static bool ShowingHex = false;

        private double updateTimeInterval = 3.0f;

        private double lastUpdateTime = 0;

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

                } else {

                }
                ShowingHex = showHex;
            }
        }

        // call it to load terrain mesh data
        public void LoadTerScene(int scope, List<MeshAssetBinder> clusterMeshDatas, List<HeightDataModel> heightDataModels, Material material) {
            this.heightDataModels = heightDataModels;
            InitTerScene();

            // TODO : hexSet 也要从 持久化文件里面读取
            TerrainCtor.InitTerrainCons(mapSet, terSet.GetTerrainSetting(), hexSet, heightDataModels, null, material);

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
            if (now - lastUpdateTime < updateTimeInterval) {
                return;
            }
            lastUpdateTime = now;

            TerrainCtor.UpdateTerrain();

            //Camera curCamera = Camera.main;
            //curCamera.transform.position = new Vector3(0, 0, 0);

            // update terrain lod and show statu when camera changes
            // TODO : 在这里更新 terrain tile


            // 1.要先判断当前 camera 位于的cluster序号
            // 2.在camera所处的 3*3 格子内进行加载，如果发现没有在内存 就尝试读取文件，如果没有文件就放弃


            // TODO : 
            // 根据传入的scope，初始化摄像机周围一圈scope内的地块...
            // 要在 TerrainEditor 中生成 地形Mesh，然后再应用到这里的实时加载！
            // MeshData 要通过 TerrainCons的方法传入Cons里面，Cons封装所有对地形 Mesh的操作
            // 如果没有地形Mesh 资源，那么无法加载！
        }


        #region Scene Terrain 场景构建

        int fontSize = 10;

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
                string clusterTxt = $"地块_{info.LL.x}_{info.LL.y}_LOD{info.curLODLevel}";
                GizmosUtils.DrawRect(info.leftDown, info.rightUp, GizmosUtils.GetRandomColor(info.LL.x + info.LL.y));
                GizmosUtils.DrawText(new Vector3(info.center.x, 0, info.center.y), clusterTxt, fontSize, Color.black);
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


        // TODO : load hex message....
        public void InitHexScene() {

        }

    }

    [Serializable]
    public class MapSceneObjs : IDisposable {

        // obj in scene
        public GameObject mapRootObj {  get; private set; }

        public GameObject heightMeshParentObj { get; private set; }

        public GameObject hexClusterParentObj { get; private set; }

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
                heightMeshParentObj = GameObject.Find(MapSceneEnum.HeightParentName);
                if (heightMeshParentObj == null) {
                    heightMeshParentObj = new GameObject(MapSceneEnum.HeightParentName);
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

        }

    }
}
