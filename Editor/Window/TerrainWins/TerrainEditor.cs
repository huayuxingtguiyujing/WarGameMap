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

        //TerrainSettingSO terSet;
        //HexSettingSO hexSet;

        //[FoldoutGroup("����scene", -1)]
        //[AssetSelector(Filter = "t:Prefab")]
        //public GameObject SignPrefab;

        protected override void InitEditor() {
            base.InitEditor();
            TerrainCtor = EditorSceneManager.TerrainCtor;
            HexCtor = EditorSceneManager.HexCtor;

            // read terrain Setting from path
            InitMapSetting();
        }

        //protected override void InitMapSetting() {
        //    base.InitMapSetting();
        //}


        #region ��������-�߶�ͼ����

        [FoldoutGroup("��������-�߶�ͼ����")]
        [LabelText("�Զ���������LOD")]        // �Զ�LODΪ���̹߳��̣���д����ʱ��Ҫ��������ֹjob��Э�̵� �� ���̻߳���
        public bool shouldGenLODBySimplify = false;

        [FoldoutGroup("��������-�߶�ͼ����")]
        [LabelText("�Ƿ����ɺ���")]
        public bool shouldGenRiver = false;

        [FoldoutGroup("��������-�߶�ͼ����")]
        [LabelText("Ter��ͼ����")]
        public Material terMaterial;

        [FoldoutGroup("��������-�߶�ͼ����")]
        [LabelText("River����")]
        public MapRiverData mapRvData;

        [FoldoutGroup("��������-�߶�ͼ����")]
        [LabelText("��ǰʹ�õĸ߶�ͼ����")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("��������-�߶�ͼ����")]
        [LabelText("��ǰ��Ҫ�����ĵؿ�����")]    // cluster
        public List<Vector2Int> clusterIdxList;

        [FoldoutGroup("��������-�߶�ͼ����", 0)]
        [Button("��ʼ������", ButtonSizes.Medium)]
        private void GenerateTerrain() {
            int tileNumARow = terSet.clusterSize / terSet.tileSize;

            DebugUtility.Log($"the map size is : {terSet.terrainSize}");
            DebugUtility.Log($"the cluster size : {terSet.clusterSize}, the tile size : {terSet.tileSize}, there are {tileNumARow} tiles per line");

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, rawHexMapSO, terMaterial, mapRvData);
        }

        [FoldoutGroup("��������-�߶�ͼ����")]
        [Button("�����ؿ�Mesh", ButtonSizes.Medium)]
        private void BuildCluster() {
            if (heightDataModels == null) {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            // TODO : ������ cluster��ʱ������
            TerrainGenTask terrainGenTask = new TerrainGenTask(heightDataModels, terSet, TerrainCtor, 
                clusterIdxList, shouldGenRiver, shouldGenLODBySimplify);
            int taskID = TaskManager.GetInstance().StartProgress(TaskTickLevel.Medium, terrainGenTask);
            TerGenTaskPop.GetPopInstance().ShowBasePop(terrainGenTask);
            terrainGenTask.StartTask(taskID);
        }

        [FoldoutGroup("��������-�߶�ͼ����", 0)]
        [Button("ˢ�µ���", ButtonSizes.Medium)]
        private void ShowTerrain() {
            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.UpdateTerrain();
        }

        [FoldoutGroup("��������-�߶�ͼ����", 0)]
        [Button("��յ���", ButtonSizes.Medium)]
        private void ClearHeightMesh() {
            if (TerrainCtor == null) {
                Debug.LogError("do not init height ctor!");
                return;
            }

            TerrainCtor.ClearClusterObj();
            Debug.Log("clear ter cluster over");
        }

        #endregion


        #region ��������-Hex����

        // TODO : ����һ�����ں������ᱻȥ������������ʹ�ø߶�ͼ������ Hex �ĵ�ͼ�����ܽ���ͨ���߶�ͼȷ��ĳ�������ĵ���
        // Ȼ�������µ���cv������ȥ������ͼ

        [FoldoutGroup("��������-Hex����")]
        [LabelText("��ǰ����Hex��ͼ����")]
        public HexMapSO rawHexMapSO;

        [FoldoutGroup("��������-Hex����")]
        [LabelText("��ǰHex��ͼ����")]
        public Material hexMaterial;

        [FoldoutGroup("��������-Hex����")]
        [LabelText("��ǰHex��ͼ����")]
        public Texture2D rawHexMapTexture;

        [FoldoutGroup("��������-Hex����")]
        [LabelText("����λ��")]
        public string exportHexMapSOPath = MapStoreEnum.TerrainHexMapPath;

        [FoldoutGroup("��������-Hex����")]
        [LabelText("��ʼ��γ��")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        [FoldoutGroup("��������-Hex����")]
        [LabelText("��ǰ������cluster����")]
        public Vector2Int curClusterIdx_Hex;


        [FoldoutGroup("��������-Hex����")]
        [Button("����RawHexMapSO", ButtonSizes.Medium)]
        private void GenerateRawHexMap() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, terSet, hexSet, rawHexMapSO);

            // TODO : ��Ӧ��ʹ�� �߶�ͼ���� RawHexMapSO
            rawHexMapSO = CreateInstance<HexMapSO>();
            rawHexMapSO.InitRawHexMap(EditorSceneManager.hexSet.mapWidth, EditorSceneManager.hexSet.mapHeight);
            HexCtor.GenerateRawHexMap(startLongitudeLatitude, rawHexMapSO, heightDataManager);

        }

        [FoldoutGroup("��������-Hex����")]
        [Button("����RawHexMap����", ButtonSizes.Medium)]
        private void GenerateRawHexTexture() {
            if (rawHexMapSO == null) {
                Debug.LogError("rawHexMapSO is null!");
                return;
            }

            rawHexMapTexture = new Texture2D(rawHexMapSO.width, rawHexMapSO.height);
            foreach (var gridTerrainData in rawHexMapSO.GridTerrainDataList) {
                Vector2Int pos = gridTerrainData.GetHexPos();
                Color color = gridTerrainData.GetTerrainColor();
                // TODO : ��������ɲ��軹�������⣡û���չ˵� hex ���������
                //Vector2Int fixed_pos = new Vector2Int(rawHexMapTexture.width - pos.x, rawHexMapTexture.height - pos.y);
                //Vector2Int fixed_pos = new Vector2Int(pos.x, rawHexMapTexture.height - pos.y);
                //Vector2Int fixed_pos = new Vector2Int(rawHexMapTexture.width - pos.x, pos.y);
                //Vector2Int fixed_pos = new Vector2Int(pos.y, pos.x);
                Vector2Int fixed_pos = new Vector2Int(pos.x, pos.y);
                rawHexMapTexture.SetPixel(fixed_pos.x, fixed_pos.y, color);
            }
            Debug.Log($"generate hex texture : {rawHexMapTexture.width}x{rawHexMapTexture.height}");
        }

        [FoldoutGroup("��������-Hex����")]
        [Button("����RawHexMapSO", ButtonSizes.Medium)]
        private void SaveRawHexMap() {

            CheckExportPath();
            string soName = $"RawHexMap_{rawHexMapSO.width}x{rawHexMapSO.height}_{UnityEngine.Random.Range(0, 100)}.asset";
            string RawHexPath = exportHexMapSOPath + $"/{soName}";
            AssetDatabase.CreateAsset(rawHexMapSO, RawHexPath);
            Debug.Log($"successfully create Hex Map, path : {RawHexPath}");
        }

        [FoldoutGroup("��������-Hex����")]
        [Button("����RawHexMap����", ButtonSizes.Medium)]
        private void SaveRawHexTexture() {
            if (rawHexMapTexture == null) {
                Debug.LogError("rawHexMapTexture is null!");
                return;
            }

            CheckExportPath();
            string textureName = $"hexTexture_{rawHexMapTexture.width}x{rawHexMapTexture.height}_{UnityEngine.Random.Range(0, 100)}";
            TextureUtility.GetInstance().SaveTextureAsAsset(exportHexMapSOPath, textureName, rawHexMapTexture);
        }

        private void CheckExportPath() {
            string mapSOFolerName = AssetsUtility.GetFolderFromPath(exportHexMapSOPath);
            if (!AssetDatabase.IsValidFolder(exportHexMapSOPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.TerrainRootPath, mapSOFolerName);
            }
        }


        [FoldoutGroup("��������-Hex����")]
        [Button("����Hex�汾Terrain", ButtonSizes.Medium)]
        private void GenerateTerrainByHex() {
            if (EditorSceneManager.hexSet == null) {
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
            

            //rawHexMapSO.UpdateGridTerrainData();
            
            // TODO : hex �����д�����
            //TerrainCtor.BuildCluster(curClusterIdx_Hex.x, curClusterIdx_Hex.y); // ?

            // Terrain��size��hexmap��size��һ��Ҫ��Ӧ
            // ��һ����������TerrainCtor�ķ�ʽȥ���� TerrainMesh��cluster-tile�Ľṹ��
            // �ڶ�������������mesh��ʱ���ҵ��õ��Ӧ��hex����
            // ������������hex���Ӹ߶ȣ�����vert�߶ȣ�����hex�����¶ȣ�����vert
        }

        #endregion


        // TODO : UNCOMPLETE
        #region ��������־û�


        [FoldoutGroup("��������־û�")]
        [LabelText("��ǰ�����ĵ���Mesh����")]
        public UnityEngine.Object curHandleTerrainMeshDatas;

        [FoldoutGroup("��������־û�")]
        [LabelText("��ǰ������Mesh·��")]
        public string curHandleMeshPath;

        [FoldoutGroup("��������־û�")]
        [LabelText("��������Mesh��·��")]
        public string exportHandleMeshPath = MapStoreEnum.TerrainMeshSerializedPath;    // TerrainMeshAssetPath


        [FoldoutGroup("��������־û�")]
        [Button("������ǰ����Ϊ����", ButtonSizes.Medium)]
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
                    if (!cluster.IsLoaded) {
                        continue;
                    }

                    Vector2Int LL = new Vector2Int(cluster.longitude, cluster.latitude);
                    string outputFile = AssetsUtility.CombinedPath(exportHandleMeshPath,
                        GetMeshDataName(terrainWidth, terrainHeight, LL));

                    // NOTE : ����������������ļ�һ��cluster��80mb������Ϊ��
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
                        if (!clusters[i, j].IsLoaded) {
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

                // NOTE : ����Ҫ�޸�ÿ���ļ���cls��Ŀʱ����������
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


        [FoldoutGroup("��������־û�")]
        [Button("�������񵽵�ǰ����", ButtonSizes.Medium)]
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
                // NOTE : ��ɾ
                //TerrainSetting trSet = new TerrainSetting();
                //trSet.ReadFromBinary(reader);
                //int terrainWidth = trSet.terrainSize.x;
                //int terrainHeight = trSet.terrainSize.z;

                // TODO : terSet hexSet ���Ҫ�� �־û��ļ������ȡ
                TerrainCtor.InitTerrainCons(mapSet, terSet, hexSet, heightDataModels, rawHexMapSO, terMaterial, null);

                int validClusterNum = reader.ReadInt32();
                for (int i = 0; i < validClusterNum; i++) {
                    TerrainCluster cls = new TerrainCluster();
                    cls.ReadFromBinary(reader);

                    TerrainCtor.ExportClusterByBinary(cls.idxX, cls.idxY, cls.longitude, cls.latitude, reader);
                }
            }
            AssetDatabase.Refresh();
        }


        [FoldoutGroup("��������־û�")]
        [Button("��ȡ��������", ButtonSizes.Medium)]
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

        [FoldoutGroup("��������־û�")]
        [Button("�����������", ButtonSizes.Medium)]
        private void SaveCurMeshData() {
            // TODO : Ҳ��������� ��
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


        // TODO : UNCOMPLETE
        #region ���μ���

        [FoldoutGroup("���μ���")]
        [LabelText("��ǰ�򻯵�cluster����")]
        public Vector2Int simplifyClsIdx;

        [FoldoutGroup("���μ���")]
        [LabelText("��ǰ�򻯵�tile����")]
        public Vector2Int simplifyTileIdx;

        [FoldoutGroup("���μ���")]
        [LabelText("�����Ż�Ŀ��")]
        public float simplifyTarget = 0.5f;

        // TODO �� 
        [FoldoutGroup("���μ���")]
        [Button("�Ե�ǰMesh���м���", ButtonSizes.Medium)]
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
