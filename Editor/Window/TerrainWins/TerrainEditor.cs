using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        [Button("构建地块Mesh", ButtonSizes.Medium)]
        private void BuildCluster_ForEdit() 
        {
            if (heightDataModels == null) {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }
            List<Vector2Int> clusterIdxList = GetBuildClusterTargets();

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

        #region 构建地形-Hex流程

        // TODO : 下面一整块在后续都会被去除掉！！不再使用高度图来构建 Hex 的地图，可能仅会通过高度图确定某个地区的地形
        // 然后再用新的类cv的流程去构建地图

        [FoldoutGroup("构建地形-Hex流程")]
        [LabelText("当前操作Hex地图对象")]
        //public HexMapSO rawHexMapSO;
        public string temp = "占位符";

        [FoldoutGroup("构建地形-Hex流程")]
        [LabelText("当前Hex地图材质")]
        public Material hexMaterial;

        [FoldoutGroup("构建地形-Hex流程")]
        [LabelText("当前Hex地图纹理")]
        public Texture2D rawHexMapTexture;

        [FoldoutGroup("构建地形-Hex流程")]
        [LabelText("导出位置")]
        public string exportHexMapSOPath = MapStoreEnum.TerrainHexMapPath;

        [FoldoutGroup("构建地形-Hex流程")]
        [LabelText("起始经纬度")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        [FoldoutGroup("构建地形-Hex流程")]
        [LabelText("当前操作的cluster索引")]
        public Vector2Int curClusterIdx_Hex;

        [FoldoutGroup("构建地形-Hex流程")]
        [Button("生成RawHexMapSO", ButtonSizes.Medium)]
        private void GenerateRawHexMap() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, terSet, hexSet, null);

            // TODO : 不应该使用 高度图生成 RawHexMapSO
            //rawHexMapSO = CreateInstance<HexMapSO>();
            //rawHexMapSO.InitRawHexMap(EditorSceneManager.hexSet.mapWidth, EditorSceneManager.hexSet.mapHeight);
            //HexCtor.GenerateRawHexMap(startLongitudeLatitude, rawHexMapSO, heightDataManager);

        }

        [FoldoutGroup("构建地形-Hex流程")]
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

        [FoldoutGroup("构建地形-Hex流程")]
        [Button("保存RawHexMapSO", ButtonSizes.Medium)]
        private void SaveRawHexMap() {

            CheckExportPath();
            //string soName = $"RawHexMap_{rawHexMapSO.mapWidth}x{rawHexMapSO.mapHeight}_{UnityEngine.Random.Range(0, 100)}.asset";
            //string RawHexPath = exportHexMapSOPath + $"/{soName}";
            //AssetDatabase.CreateAsset(rawHexMapSO, RawHexPath);
            //Debug.Log($"successfully create Hex Map, path : {RawHexPath}");
        }

        [FoldoutGroup("构建地形-Hex流程")]
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

        [FoldoutGroup("构建地形-Hex流程")]
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


        [FoldoutGroup("地形网格持久化")]
        [LabelText("当前操作的地形Mesh数据")]
        public UnityEngine.Object curHandleTerrainMeshDatas;

        [FoldoutGroup("地形网格持久化")]
        [LabelText("当前操作的Mesh路径")]
        public string curHandleMeshPath;

        [FoldoutGroup("地形网格持久化")]
        [LabelText("导出地形Mesh的路径")]
        public string exportHandleMeshPath = MapStoreEnum.TerrainMeshSerializedPath;    // TerrainMeshAssetPath


        [FoldoutGroup("地形网格持久化")]
        [Button("导出当前地形为网格", ButtonSizes.Medium)]
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

            int exportClusterNum = 0;
            for (int i = 0; i < terrainWidth; i++) {
                for (int j = 0; j < terrainHeight; j++) {
                    TerrainCluster cluster = clusters[i, j];
                    if (!cluster.IsInited) {
                        continue;
                    }

                    Vector2Int LL = new Vector2Int(cluster.longitude, cluster.latitude);
                    string outputFile = AssetsUtility.CombinedPath(exportHandleMeshPath,
                        GetMeshDataName(terrainWidth, terrainHeight, LL));

                    // NOTE : 用这个方法导出的文件一个cluster有80mb，引以为戒
                    //ExportTerrainAsMesh_Obj(outputFile);
                    ExportTerrainAsMesh_Binary(i, j, cluster, outputFile);
                    exportClusterNum++;
                }
            }

            stopwatch.Stop();
            Debug.Log($"{exportClusterNum} cluster terrain mesh has been exported to: {exportHandleMeshPath}, cost : {stopwatch.ElapsedMilliseconds} ms");
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
                        if (!clusters[i, j].IsInited) {
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

        private void ExportTerrainAsMesh_Binary(int i, int j, TerrainCluster cluster, string outputFile) {

            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write))
            using (BufferedStream bufferedStream = new BufferedStream(fs))
            using (BinaryWriter writer = new BinaryWriter(bufferedStream)) {
                // NOTE : how to serilize mesh data
                // set : map setting
                // cls : a cluster start
                // tl : a tile start

                // write cur terrainSetting to file
                terSet.GetTerrainSetting().WriteToBinary(writer);

                // NOTE : 当需要修改每个文件的cls数目时，操作这里
                //int validClusterNum = TerrainCtor.GetValidClusterNum();
                int validClusterNum = 1;
                writer.Write(validClusterNum);

                TDList<TerrainCluster> clusters = TerrainCtor.ClusterList;
                int terrainWidth = clusters.GetLength(1);
                int terrainHeight = clusters.GetLength(0);

                cluster.WriteToBinary(writer);
                TerrainCtor.ImportClusterToBinary(i, j, writer);
                writer.Flush();
            }
            AssetDatabase.Refresh();
        }

        private string GetMeshDataName(int terrainWidth, int terrainHeight, Vector2Int LL) {
            DateTime dateTime = DateTime.Now;
            long timeSign = dateTime.Ticks / 1000;
            return string.Format("ClusterMesh_n{0}_e{1}_MapSize{2}x{3}_{4}", LL.x, LL.y, terrainWidth, terrainHeight, timeSign);
        }


        [FoldoutGroup("地形网格持久化")]
        [Button("导入网格到当前地形", ButtonSizes.Medium)]
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
                // NOTE : 勿删
                //TerrainSetting trSet = new TerrainSetting();
                //trSet.ReadFromBinary(reader);
                //int terrainWidth = trSet.terrainSize.x;
                //int terrainHeight = trSet.terrainSize.z;

                // TODO : terSet hexSet 最好要从 持久化文件里面读取
                TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, null, terMaterial, null);

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

            curHandleMeshPath = AssetsUtility.TransToAssetPath(meshPath);
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
