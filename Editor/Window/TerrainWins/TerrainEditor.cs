
using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using Debug = UnityEngine.Debug;

namespace LZ.WarGameMap.MapEditor
{
    
    public class TerrainEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.TerrainEditor;

        GameObject mapRootObj;
        GameObject heightMeshParentObj;

        TerrainConstructor TerrainCtor;

        //[FoldoutGroup("配置scene", -1)]
        //[AssetSelector(Filter = "t:Prefab")]
        //public GameObject SignPrefab;

        [FoldoutGroup("配置scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: 没有初始化Scene")]
        public string warningMessage = "请点击按钮初始化!";

        [FoldoutGroup("配置scene", -1)]
        [Button("初始化地形配置")]
        protected override void InitEditor() {
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

            TerrainCtor = mapRootObj.GetComponent<TerrainConstructor>();
            if (TerrainCtor == null) {
                TerrainCtor = mapRootObj.AddComponent<TerrainConstructor>();
            }

            TerrainCtor.SetMapPrefab(mapRootObj.transform, heightMeshParentObj.transform);

            // read terrain Setting from path
            InitMapSetting();
            notInitScene = false;
        }
        
        protected override void InitMapSetting() {
            base.InitMapSetting();
            if (terSet == null) {
                string terrainSettingPath = MapStoreEnum.WarGameMapSettingPath + "/TerrainSetting_Default.asset";
                terSet = AssetDatabase.LoadAssetAtPath<TerrainSettingSO>(terrainSettingPath);
                if (terSet == null) {
                    // create it !
                    terSet = CreateInstance<TerrainSettingSO>();
                    AssetDatabase.CreateAsset(terSet, terrainSettingPath);
                    Debug.Log($"successfully create Terrain Setting, path : {terrainSettingPath}");
                } 
            }
        }

        #region 构建地形

        [FoldoutGroup("构建地形", 0)]
        [LabelText("地形设置")]
        public TerrainSettingSO terSet;

        [FoldoutGroup("构建地形", 0)]
        [Button("初始化地形", ButtonSizes.Medium)]
        private void GenerateTerrain() {

            int tileNumARow = terSet.clusterSize / terSet.tileSize;

            Debug.Log($"the map size is : {terSet.terrainSize}");
            Debug.Log($"the cluster size : {terSet.clusterSize}, the tile size : {terSet.tileSize}, there are {tileNumARow} tiles per line");

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.InitHeightCons(terSet.GetTerrainSetting(), heightDataModels);
        }

        [FoldoutGroup("构建地形", 0)]
        [Button("更新地形", ButtonSizes.Medium)]
        private void ShowTerrain() {

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.UpdateTerrain();

        }

        [FoldoutGroup("构建地形", 0)]
        [Button("清空地形", ButtonSizes.Medium)]
        private void ClearHeightMesh() {
            if (TerrainCtor == null) {
                Debug.LogError("do not init height ctor!");
                return;
            }

            TerrainCtor.ClearClusterObj();
        }


        [FoldoutGroup("懒构建地形")]
        [LabelText("当前使用的高度图数据")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前使用的法线纹理")]    // 一个cluster对应一张法线纹理
        public Texture2D curHandleNormalTex;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前操作的cluster索引")]
        public Vector2Int clusterIdx;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前使用的经度")]
        public int longitude;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前使用的纬度")]
        public int latitude;

        [FoldoutGroup("懒构建地形")]
        [Button("构建对应cluster的Mesh", ButtonSizes.Medium)]
        private void BuildCluster() {

            if (heightDataModels == null) {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            foreach (var model in heightDataModels) {
                if (model.ExistHeightData(longitude, latitude)) {
                    //HeightData heightData = model.GetHeightData(longitude, latitude);
                    TerrainCtor.BuildCluster(clusterIdx.x, clusterIdx.y, longitude, latitude);
                    return;
                }
            }

            Debug.LogError($"unable to find heightdata, longitude : {longitude}, latitude : {latitude}");

        }

        [FoldoutGroup("懒构建地形")]
        [Button("构建对应cluster的法线", ButtonSizes.Medium)]
        private void BuildClusterNormal() {

            if (curHandleNormalTex == null) {
                Debug.LogError("no normal texture, so you can not build the mesh");
                return;
            }

            TerrainCtor.BuildClusterNormal(clusterIdx.x, clusterIdx.y, curHandleNormalTex);
        }

        #endregion


        #region 使用Hex构建地形

        [FoldoutGroup("使用Hex构建地形")]
        [LabelText("地图配置")]
        public HexSettingSO hexSet;

        [FoldoutGroup("使用Hex构建地形")]
        [LabelText("当前使用的HexMapSO")]
        [Tooltip("必须使用 hexSet 对应生成的 rawHexMapSO！否则会出错")]
        public RawHexMapSO rawHexMapSO;

        [FoldoutGroup("使用Hex构建地形")]
        [LabelText("当前操作的cluster索引")]
        public Vector2Int curClusterIdx_Hex;

        [FoldoutGroup("使用Hex构建地形")]
        [Button("生成Terrain", ButtonSizes.Medium)]
        private void GenerateTerrainByHex() {
            if (hexSet == null) {
                Debug.LogError("hex Set is null!");
                return;
            }
            if (rawHexMapSO == null) {
                Debug.LogError("rawHexMapSO is null!");
                return;
            }
            if (TerrainCtor == null) {
                Debug.LogError("TerrainCtor is null!");
                return;
            }
            rawHexMapSO.UpdateGridTerrainData();
            TerrainCtor.InitHexCons(hexSet, rawHexMapSO);
            TerrainCtor.BuildCluster(curClusterIdx_Hex.x, curClusterIdx_Hex.y, -1, -1);
            // Terrain的size和hexmap的size不一定要对应
            // 第一步：继续按TerrainCtor的方式去生成 TerrainMesh（cluster-tile的结构）
            // 第二步：遍历生成mesh的时候，找到该点对应的hex格子
            // 第三步：根据hex格子高度，设置vert高度；根据hex格子坡度，调整vert
        }

        #endregion


        #region 地形网格持久化


        [FoldoutGroup("地形网格持久化")]
        [LabelText("当前操作的地形Mesh数据")]
        public UnityEngine.Object curHandleTerrainMeshDatas;

        [FoldoutGroup("地形网格持久化")]
        [LabelText("当前操作的Mesh路径")]
        public string curHandleMeshPath;

        [FoldoutGroup("地形网格持久化")]
        [LabelText("导出地形Mesh的路径")]
        public string exportHandleMeshPath = MapStoreEnum.TerrainMeshPath;


        [FoldoutGroup("地形网格持久化")]
        [Button("导出地形为网格", ButtonSizes.Medium)]
        private void ExportTerrainAsMesh() {
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            TDList<TerrainCluster> clusters = TerrainCtor.ClusterList;
            int terrainWidth = clusters.GetLength(1);
            int terrainHeight = clusters.GetLength(0);
            int clusterNums = clusters.GetLength(0) * clusters.GetLength(1);

            string outputFile = AssetsUtility.GetInstance().CombinedPath(exportHandleMeshPath,
                GetMeshDataName(terrainWidth, terrainHeight, clusterNums));

            // NOTE : 用这个方法导出的文件一个cluster有100mb，引以为戒
            //ExportTerrainAsMesh_Obj(outputFile);
            ExportTerrainAsMesh_Binary(outputFile);

            stopwatch.Stop();
            Debug.Log($"terrain mesh data has been added to: {outputFile}, cost : {stopwatch.ElapsedMilliseconds} ms");
        }

        private void ExportTerrainAsMesh_Obj(string outputFile) {

            TDList<TerrainCluster> clusters = TerrainCtor.ClusterList;
            int terrainWidth = clusters.GetLength(1);
            int terrainHeight = clusters.GetLength(0);

            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            using (BufferedStream bufferedStream = new BufferedStream(fs))
            using (StreamWriter writer = new StreamWriter(bufferedStream)) {
                // NOTE : how to serilize mesh data
                // set : map setting
                // cls : a cluster start
                // tl : a tile start

                // write cur terrainSetting to file
                string setInfo = string.Format("{0},{1}", "set", terSet.GetTerrainSetting().ToString());
                writer.WriteLine(setInfo);

                for (int i = 0; i < terrainWidth; i++) {
                    for (int j = 0; j < terrainHeight; j++) {
                        if (!clusters[i, j].IsValid) {
                            continue;
                        }

                        //// write cluster setting to file
                        //string clsInfo = string.Format("{0},{1}", "cls", clusters[i, j].GetClusterInfo());
                        //writer.WriteLine(clsInfo);

                        //StringBuilder tileSb = new StringBuilder();
                        //TDList<TerrainTile> tiles = clusters[i, j].TileList;
                        //foreach (var tile in tiles) {
                        //    // write tile setting to file
                        //    tileSb.Clear();
                        //    tileSb.Append($"tl,");
                        //    tileSb.Append(tile.GetTileInfo());
                        //    writer.WriteLine(tileSb.ToString());

                        //    // write every mesh to file
                        //    TerrainMeshData[] meshDatas = tile.GetLODMeshes();
                        //    foreach (var terrainMesh in meshDatas) {
                        //        terrainMesh.SerializeTerrainMesh(writer);
                        //    }
                        //}
                        writer.Flush();
                    }
                }
            }

        }

        private void ExportTerrainAsMesh_Binary(string outputFile) {

            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            using (BufferedStream bufferedStream = new BufferedStream(fs))
            using (BinaryWriter writer = new BinaryWriter(bufferedStream)) {
                // NOTE : how to serilize mesh data
                // set : map setting
                // cls : a cluster start
                // tl : a tile start

                // write cur terrainSetting to file
                terSet.GetTerrainSetting().WriteToBinary(writer);

                int validClusterNum = TerrainCtor.GetValidClusterNum();
                writer.Write(validClusterNum);

                TDList<TerrainCluster> clusters = TerrainCtor.ClusterList;
                int terrainWidth = clusters.GetLength(1);
                int terrainHeight = clusters.GetLength(0);

                for (int i = 0; i < terrainWidth; i++) {
                    for (int j = 0; j < terrainHeight; j++) {
                        // write cluster setting to file
                        clusters[i, j].WriteToBinary(writer);
                        TerrainCtor.ImportClusterToBinary(i, j, writer);
                        writer.Flush();
                    }
                }
            }
            AssetDatabase.Refresh();
        }

        private string GetMeshDataName(int terrainWidth, int terrainHeight, int clusterNum) {
            DateTime dateTime = DateTime.Now;
            long timeSign = dateTime.Ticks / 1000;
            return string.Format("TerrainMesh_{0}x{1}_{2}Clusters_{3}", terrainWidth, terrainHeight, clusterNum, timeSign);
        }


        [FoldoutGroup("地形网格持久化")]
        [Button("导入网格到地形", ButtonSizes.Medium)]
        private void ImportMeshToTerrain() {
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            if (curHandleTerrainMeshDatas == null) {
                Debug.LogError("cur TerrainMeshDatas is null");
                return;
            }

            if (curHandleMeshPath == null) {
                Debug.LogError("cur TerrainMeshDatas path is null");
                return;
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            ImportMeshToTerrain_Binary(curHandleMeshPath);

            stopwatch.Stop();
            Debug.Log($"mesh data trans to terrain: {curHandleMeshPath}, cost : {stopwatch.ElapsedMilliseconds} ms");
        }

        private void ImportMeshToTerrain_Binary(string curHandleMeshPath) {

            using (FileStream fs = new FileStream(curHandleMeshPath, FileMode.Open, FileAccess.Read))
            using (BufferedStream bufferedStream = new BufferedStream(fs))
            using (BinaryReader reader = new BinaryReader(bufferedStream)) {

                TerrainSetting trSet = new TerrainSetting();
                trSet.ReadFromBinary(reader);
                int terrainWidth = trSet.terrainSize.x;
                int terrainHeight = trSet.terrainSize.z;

                TerrainCtor.InitHeightCons(trSet, heightDataModels);

                int validClusterNum = reader.ReadInt32();
                for (int i = 0; i < validClusterNum; i++) {
                    TerrainCluster cls = new TerrainCluster();
                    cls.ReadFromBinary(reader);

                    TerrainCtor.ExportClusterByBinary(cls.idxX, cls.idxY, cls.longitude, cls.latitude, reader);
                }
            }
            AssetDatabase.Refresh();
        }


        [FoldoutGroup("地形网格持久化")]
        [Button("读取地形网格", ButtonSizes.Medium)]
        private void ReadMeshDataPath() {

            // read terrain mesh datas, get the path
            string meshPath = EditorUtility.OpenFilePanel("Import Terrain Mesh Data", "", "");
            if (meshPath == "") {
                Debug.LogError("you do not get the file");
                return;
            }

            curHandleMeshPath = AssetsUtility.GetInstance().TransToAssetPath(meshPath);
            curHandleTerrainMeshDatas = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(curHandleMeshPath);
            if (curHandleTerrainMeshDatas == null) {
                Debug.LogError(string.Format("can not load terrain mesh data from this path: {0}", curHandleMeshPath));
                return;
            }

        }

        [FoldoutGroup("地形网格持久化")]
        [Button("保存地形网格", ButtonSizes.Medium)]
        private void SaveCurMeshData() {
            // TODO : 也许不用做这个 ？
            // save cur handle TerrainMeshDatas
            if (curHandleTerrainMeshDatas == null) {
                Debug.LogError("cur TerrainMeshDatas is null");
                return;
            }

            if (curHandleMeshPath == null) {
                Debug.LogError("cur TerrainMeshDatas path is null");
                return;
            }


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
                Debug.LogError("terrian ctor is null!");
                return;
            }
            TerrainCtor.ExeSimplify(simplifyClsIdx.x, simplifyClsIdx.y, simplifyTileIdx.x, simplifyTileIdx.y, simplifyTarget);
        }

        #endregion

    }

}
