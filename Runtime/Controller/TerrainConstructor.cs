using LZ.WarGameCommon;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    /// <summary>
    /// 此类用于构建地形Mesh，基于高度图应用到 MeshRender 上
    /// </summary>
    [ExecuteInEditMode]
    public class TerrainConstructor : MonoBehaviour
    {

        [SerializeField] Transform originPoint;
        [SerializeField] Transform heightClusterParent;
        [SerializeField] Transform signParent;

        [SerializeField] GameObject signPrefab;

        // cluster and tile setting
        int clusterWidth;
        int clusterHeight;

        Vector3Int terrainSize; 
        Vector3Int clusterSize; 
        int tileSize;
        int LODLevel;

        // cluster list
        private TDList<TerrainCluster> clusterList;

        // height data
        private HeightDataManager heightDataManager;

        private bool hasInit = false;

        #region init height cons

        public void SetMapPrefab(Transform originPoint, Transform heightClusterParent, Transform signParent, GameObject signPrefab) {
            this.originPoint = originPoint;
            this.heightClusterParent = heightClusterParent;
            this.signParent = signParent;
            this.signPrefab = signPrefab;
        }

        public void InitHeightCons(Vector3Int terrainSize, Vector3Int clusterSize, int tileSize, int LODLevel, List<HeightDataModel> heightDataModels) {
            heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, clusterSize);

            this.terrainSize = terrainSize;
            this.clusterSize = clusterSize;
            this.tileSize = tileSize;
            this.LODLevel = LODLevel;

            // 目前是这样的：
            // 大世界区分 TerrainCluster，cluster 对应单独的一个高度图 tif 文件
            // TerrainCluster 区分为多个 TileMesh，每个 TileMesh 拥有对应多个 LOD 层级的 mesh
            clusterWidth = terrainSize.x;
            clusterHeight = terrainSize.z;

            clusterList = new TDList<TerrainCluster>(clusterWidth, clusterHeight);
            //for (int i = 0;  i < clusterHeight; i++) {
            //    for(int j = 0;  j < clusterWidth; j++) {
            //        //GameObject clusterGo = CreateHeightCluster(i, j);
            //        // TODO : 懒初始化，一次性初始化太多内存肯定会炸的
            //        //clusterList[i, j].InitTerrainCluster(clusterSize, tileSize, LODLevel, heightData, clusterGo);
            //    }
            //}

            hasInit = true;
            Debug.Log(string.Format($"successfully generate total terrain!  create {clusterWidth}*{clusterHeight}"));
        }

        public void BuildCluster(int i, int j, int longitude, int latitude) {
            

            if (i < 0 || i >= clusterHeight || j < 0 || j >= clusterWidth) {
                Debug.LogError($"wrong index : {i}, {j}");
                return;
            }

            if (!clusterList[i, j].IsValid) {
                GameObject clusterGo = CreateHeightCluster(i, j);
                clusterList[i, j].InitTerrainCluster(i, j, longitude, latitude, clusterSize, tileSize, LODLevel, heightDataManager, clusterGo);
            }

            Debug.Log($"handle cluster successfully, use heightData : {longitude}, {latitude}");
        }

        private GameObject CreateHeightCluster(int idxX, int idxY) {
            GameObject go = new GameObject();
            go.transform.parent = heightClusterParent;
            go.name = string.Format("heightCluster_{0}_{1}", idxX, idxY);
            return go;
        }

        public void ClearHeightObj() {
            clusterWidth = 0;
            clusterHeight = 0;
            heightClusterParent.ClearObjChildren();
        }

        public void ClearSignObj() {
            signParent.ClearObjChildren();
        }

        #endregion


        #region runtime updating


        public void UpdateTerrain() {

            // NOTE : 当前直接使用主摄像机位置判断是否要加载此块
            Vector3 cameraPos = Camera.main.transform.position;

            foreach (var cluster in clusterList) {
                if (cluster.IsValid) {
                    int x = cluster.idxX;
                    int y = cluster.idxY;

                    int leftIdx = x - 1;
                    int rightIdx = x + 1;
                    int upIdx = y + 1;
                    int downIdx = y - 1;

                    int[,] direction = new int[4, 2]{
                        {leftIdx, y},{rightIdx, y}, {x, upIdx}, {x, downIdx}
                    };

                    //  find neighbours, the sequence is : left, right, up, down;
                    List<TerrainCluster> terrainClusters = new List<TerrainCluster>(4) { null, null, null, null};
                    for(int i = 0; i < 4; i++) {
                        int neighborX = direction[i, 0];
                        int neighborY = direction[i, 1];
                        if (clusterList.IsValidIndex(neighborX, neighborY) && clusterList[neighborX, neighborY].IsValid) {
                            terrainClusters[i] = clusterList[neighborX, neighborY];
                        }
                    }

                    TDList<int> fullLodLevelMap = clusterList[x, y].GetFullLODLevelMap(cameraPos, 
                        terrainClusters[0], terrainClusters[1], terrainClusters[2], terrainClusters[3]);
                    cluster.UpdateTerrainCluster(fullLodLevelMap);
                }
            }

            Debug.Log(string.Format("update terrain cluster num {0}", clusterList.GetLength(0) * clusterList.GetLength(0)));
        }

        // TODO : quad tree!

        #endregion


        // NOTE : 不要删掉！！
        /*public void SetHeights(int idxW, int idxH, float[,] heights) {
            if (idxH < 0 || idxH >= heightClustersList.GetLength(0)) {
                Debug.LogError(string.Format("the index is not valid: {0}, {1}", idxW, idxH));
                return;
            }
            if (idxW < 0 || idxW >= heightClustersList.GetLength(1)) {
                Debug.LogError(string.Format("the index is not valid: {0}, {1}", idxW, idxH));
                return;
            }

            heightClustersList[idxW, idxH].SetHeights(heights);
        }*/
        /*public void InitHeightCons(int tileNumWidth, int tileNumHeight, int tileWidth, int tileHeight, int gridSize) {
            heightClustersList = new TDList<HeightCluster>(tileWidth, tileHeight);

            for (int i = 0; i < tileHeight; i++) {
                for (int j = 0; j < tileWidth; j++) {
                    //HeightCluster rec = CreateHeightCluster(j, i, clusterWidth, clusterHeight, gridSize);
                    //heightClustersList[i, j] = rec;
                }
            }

            // set tile's neighbor
            for (int i = 0; i < tileHeight; i++) {
                for (int j = 0; j < tileWidth; j++) {

                }
            }
        }
*/
        /*private HeightCluster CreateHeightCluster(int idxW, int idxH, int clusterWidth, int clusterHeight, int gridSize) {
            GameObject go = new GameObject();
            go.transform.parent = heightClusterParent;
            go.name = string.Format("heightCluster_{0}_{1}", idxW, idxH);

            // caculate tile's start point
            float startX = idxW * clusterWidth * gridSize;
            float startZ = idxH * clusterHeight * gridSize;
            Vector3 startPoint = new Vector3(startX, 0, startZ);

            HeightCluster heightCluster = go.AddComponent<HeightCluster>();
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            heightCluster.SetPrefab(signPrefab, signParent);
            heightCluster.InitHeightCluster(clusterWidth, clusterHeight, gridSize, startPoint);
            return heightCluster;
        }
*//*
        #region use quad tree LOD init and dynamic switching

        *//*List<GameObject> heightClusters;
        List<QuadTreeTerrainNode> terrainNodes;
        Mesh[] terrainMeshes;

        // use 3 queue to decide which lod level should this tile be
        private Queue<QuadTreeTerrainNode> bufferA;
        private Queue<QuadTreeTerrainNode> bufferB;
        private Queue<QuadTreeTerrainNode> bufferFinal;
        private HashSet<QuadTreeTerrainNode> bufferFinalSet;

        private bool AbleToSwitchingLOD = false;

        private float updateTimeRec = 0;
        private float updateTileTime = 1.0f;



        public class QuadTreeTerrainNode {

            public int curTileIdx;

            public int tileSize;

            public Vector3 tileCenter;

            public int lodLevel;

            public int lodTileNum;

            public int[] NeighborIdxs;

            public QuadTreeTerrainNode() { this.NeighborIdxs = new int[4] { -1, -1, -1, -1 }; }

            public QuadTreeTerrainNode(int curTileIdx, int lodLevel, int lodTileNum, Vector3 tileCenter, int tileSize) {
                
                this.lodLevel = lodLevel;
                this.lodTileNum = lodTileNum;

                this.curTileIdx = curTileIdx;
                this.tileSize = tileSize;
                this.tileCenter = tileCenter;
                this.NeighborIdxs = new int[4] { -1, -1, -1, -1 };
            }

            public int[] GetChildrenIdxs() {
                // curTileIdx = 5:
                // power = 1, curLevelStartIdx = 5, nextLevelStartIdx = 21
                int power = GetPowerOfFour(curTileIdx);
                int curLevelStartIdx = 0;
                for(int i = 0; i <= power; i++) {
                    curLevelStartIdx += (int)Mathf.Pow(4, i);
                }
                int nextLevelStartIdx = curLevelStartIdx + (int)Mathf.Pow(4, power + 1);

                // nextLevelTileNum = 4, totalIdx = 0, cur_w = 0, cur_h = 0;
                int nextLevelTileNum = lodTileNum * 2;
                int totalIdx = curTileIdx - curLevelStartIdx;
                int cur_w = totalIdx % lodTileNum;
                int cur_h = totalIdx / lodTileNum;

                int nextLevel_w = cur_w * 2;
                int nextLevel_h = cur_h * 2;
                int nextLevel_next_h = cur_h * 2 + 1;

                int[] childrenIdx = new int[4];
                childrenIdx[0] = nextLevel_h * nextLevelTileNum + nextLevel_w + nextLevelStartIdx;
                childrenIdx[1] = nextLevel_h * nextLevelTileNum + nextLevel_w + nextLevelStartIdx + 1;
                childrenIdx[2] = nextLevel_next_h * nextLevelTileNum + nextLevel_w + nextLevelStartIdx;
                childrenIdx[3] = nextLevel_next_h * nextLevelTileNum + nextLevel_w + nextLevelStartIdx + 1;
                return childrenIdx;
            }

            private int GetPowerOfFour(int num) {
                int ans = 0;
                while(num > 0) {
                    num = (num - 1) / 4;
                    if(num > 0) {
                        ans++;
                    }
                }
                return ans;
            }


        }

        public void InitHeightCons(Mesh[] meshes, int maxLodLevel, Material terrainMaterial) {
            // TODO: should load all meshes as asset, and start switching lod
            //terrainMeshAB = ;
            terrainMeshes = meshes;

            bufferA = new Queue<QuadTreeTerrainNode>();
            bufferB = new Queue<QuadTreeTerrainNode>();
            bufferFinal = new Queue<QuadTreeTerrainNode>();
            bufferFinalSet = new HashSet<QuadTreeTerrainNode>();

            InitMeshGo(terrainMeshes, maxLodLevel, terrainMaterial);
            bufferFinal.Enqueue(terrainNodes[0]);

            AbleToSwitchingLOD = true;
        }

        private void InitMeshGo(Mesh[] meshes, int maxLodLevel, Material terrainMaterial) {
            heightClusters = new List<GameObject>(meshes.Length);
            terrainNodes = new List<QuadTreeTerrainNode>(meshes.Length);
            int curLODLevel = maxLodLevel - 1;
            int lodLevelNodeNum = 1;

            int i = 0;
            while (i < meshes.Length) {
                int curLevelWH = (int)Mathf.Sqrt(lodLevelNodeNum);
                for(int j =  0; j < curLevelWH; j++) {
                    for(int k = 0; k < curLevelWH; k++) {
                        if (i >= meshes.Length) {
                            Debug.LogError("wrong meshes num, unfit to quad tree node nums!");
                            break;
                        }

                        GameObject go = new GameObject();
                        go.transform.parent = heightClusterParent;
                        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
                        go.name = meshes[i].name;
                        //meshFilter.mesh = meshes[i];
                        meshRenderer.sharedMaterial = terrainMaterial;
                        //meshRenderer.material = terrainMaterial;
                        heightClusters.Add(go);

                        int meshVertexCount = meshes[i].vertices.Length;
                        Vector3 nodeCenter = (meshes[i].vertices[meshVertexCount - 1] + meshes[i].vertices[0]) / 2;
                        int tileGridSize = (int)(meshes[i].vertices[meshVertexCount - 1].x - meshes[i].vertices[0].x);
                        QuadTreeTerrainNode node = new QuadTreeTerrainNode(
                            i, curLODLevel, curLevelWH, nodeCenter, tileGridSize
                        );
                        terrainNodes.Add(node);

                        i++;
                    }
                }

                lodLevelNodeNum *= 4;
                curLODLevel--;
            }

            Debug.Log(string.Format("init height clusters over! you new {0} objs, new {1} nodes, get {2} meshes", 
                heightClusters.Count, terrainNodes.Count, meshes.Length)
            );
        }

        private void UpdateLOD() {
            if (!AbleToSwitchingLOD) {
                return;
            }
            
            Vector3 cameraPos = Camera.main.transform.position;
            //Debug.Log(cameraPos);
            
            // 仅加入第一个节点到 buffer A 中
            bufferFinal.Clear();
            bufferA.Enqueue(terrainNodes[0]);

            // 判断 buffer a 里的节点是否需要划分，不需要则加入 bufferFinal，需要则加入 buffer b
            // 把 buffer b 里的节点 加入 buffer a
            // 直到 buffer a b 都没有节点为止，结束
            while (bufferA.Count > 0 || bufferB.Count > 0) {
                QuadTreeTerrainNode cur = bufferA.Dequeue();
                if (ShouldRefined(cur, cameraPos)) {
                    if (cur.lodLevel <= 0) {
                        bufferFinal.Enqueue(cur);
                    } else {
                        int[] childrenIdx = cur.GetChildrenIdxs();
                        foreach(var i in childrenIdx) {
                            if (i >= terrainNodes.Count || i < 0) {
                                Debug.LogError(string.Format("out of nodes range, cur node lod level: {0}, idx: {1}", cur.lodLevel, i));
                            }

                            bufferB.Enqueue(terrainNodes[i]);
                        }
                    }
                } else {
                    bufferFinal.Enqueue(cur);
                }

                // switch a b buffer
                while(bufferB.Count > 0) {
                    QuadTreeTerrainNode node = bufferB.Dequeue();
                    bufferA.Enqueue(node);
                }
                bufferB.Clear();

            }

            // 遍历 buffer final, 依次激活 buffer final 对应层次的 mesh, 如果 节点 存在于 set, 那么移除掉并且无需激活，否则要激活
            // 最后遍历 set, 把存在于此的节点取消激活
            foreach (var node in bufferFinal)
            {
                if (bufferFinalSet.Contains(node)) {
                    bufferFinalSet.Remove(node);
                } else {
                    // set this tile active
                    int tileIdx = node.curTileIdx;
                    var tileObj = heightClusters[tileIdx];
                    tileObj.GetComponent<MeshFilter>().mesh = terrainMeshes[tileIdx];
                }
            }

            Debug.Log(string.Format("{0} tiles should show in screen", bufferFinal.Count));

            foreach (var node in bufferFinalSet)
            {
                // set this tile unactive
                int tileIdx = node.curTileIdx;
                var tileObj = heightClusters[tileIdx];
                tileObj.GetComponent<MeshFilter>().mesh = null;
            }

            bufferFinalSet.Clear();
            foreach (var node in bufferFinal)
            {
                bufferFinalSet.Add(node);
            }
        }

        private bool ShouldRefined(QuadTreeTerrainNode node, Vector3 centerPos) {
            return Vector3.Distance(node.tileCenter, centerPos) < node.tileSize * 2; 
        }

        // TODO : 
        private void UpdateTileNeighbours() {
            // use this function to update Tile's neighbor, and then tile could know which LOD its neighbor is
            if (!AbleToSwitchingLOD) {
                return;
            }

            int[] lodLevels = null; // record every tile's lod level

        }

        private void Update() {
            //float time = Time.deltaTime;
            //if (updateTimeRec < updateTileTime) {
            //    updateTimeRec += time;
            //    return;
            //} else {
            //    updateTimeRec = 0;
            //}

            //UpdateLOD();
            //UpdateTileNeighbours();
        }*//*
        #endregion*/

    }

    // NOTE : 一个 cluster 对应一个TIF高度图文件，包含了多个 TerrainTile
    public class TerrainCluster {

        public int idxX { get; private set; }
        public int idxY { get; private set; }

        public int longitude { get; private set; }
        public int latitude { get; private set; }

        public bool IsValid { get; private set; }


        Vector3Int terrainClusterSize;
        int tileSize;
        int LODLevel;

        HeightDataManager heightDataManager;        // this class can help you sample height data

        TDList<TerrainTile> terrainTileList;
        GameObject clusterGo;

        public TerrainCluster() { IsValid = false; }

        public void InitTerrainCluster(int idxX, int idxY, int longitude, int latitude, Vector3Int terrainClusterSize, int tileSize, int LODLevel, HeightDataManager heightDataManager, GameObject clusterGo) {
            this.idxX = idxX;
            this.idxY = idxY;

            this.heightDataManager = heightDataManager;
            this.longitude = longitude;
            this.latitude = latitude;

            this.terrainClusterSize = terrainClusterSize;
            this.tileSize = tileSize;
            this.LODLevel = LODLevel;

            this.clusterGo = clusterGo;

            Vector3 startPoint = new Vector3(terrainClusterSize.x * idxX, 0, terrainClusterSize.z * idxY);
            InitTerrainCluster(startPoint, heightDataManager);

            IsValid = true;
        }

        private void InitTerrainCluster(Vector3 clusterStartPoint, HeightDataManager heightDataManager) {
            int tileNumPerLine = terrainClusterSize.x / tileSize;

            //Debug.Log(string.Format("the cluster size : {0}x{1}, because the size of cluster is {2}, so there are {3} tiles in a row", 
            //    terrainSize.x, terrainSize.z, tileSize, tileNumPerLine));

            terrainTileList = new TDList<TerrainTile>(tileNumPerLine, tileNumPerLine);
            int[] lodLevels = new int[LODLevel];
            for (int i = 0; i < LODLevel; i++) {
                lodLevels[i] = i;
            }

            for (int i = 0; i < tileNumPerLine; i++) {
                for (int j = 0; j < tileNumPerLine; j++) {
                    MeshFilter meshFilter = CreateHeightTile(i, j);
                    terrainTileList[i, j].InitTileMeshData(i, j, longitude, latitude, clusterStartPoint, meshFilter,  lodLevels);
                }
            }

            // generate mesh data for every LOD level
            int curLODLevel = LODLevel - 1;
            while (curLODLevel >= 0) {
                // when num fix == 1, the vert num per line is equal to tileSize
                int vertexNumFix = (int)Mathf.Pow(2, (LODLevel - curLODLevel - 1));
                if (vertexNumFix > tileSize) {
                    // wrong
                    break;
                }

                for (int i = 0; i < tileNumPerLine; i++) {
                    for (int j = 0; j < tileNumPerLine; j++) {
                        terrainTileList[i, j].SetMeshData(curLODLevel, tileSize, vertexNumFix, terrainClusterSize, heightDataManager);
                    }
                }

                curLODLevel--;
            }

            Debug.Log(string.Format("successfully generate terrain tiles! "));
        }

        private MeshFilter CreateHeightTile(int idxX, int idxY) {
            GameObject tileGo = new GameObject();
            tileGo.transform.parent = clusterGo.transform;
            tileGo.name = string.Format("heightTile_{0}_{1}", idxX, idxY);

            MeshFilter meshFilter = tileGo.AddComponent<MeshFilter>();
            tileGo.AddComponent<MeshRenderer>();
            return meshFilter;
        }


        #region update terrain ; fix LOD seam

        public void UpdateTerrainCluster(TDList<int> fullLodLevelMap) {

            foreach (var tileMeshData in terrainTileList) {
                // use full lod map, so index offset is 1
                int x = tileMeshData.tileIdxX + 1;
                int y = tileMeshData.tileIdxY + 1;
                int lodLevel = fullLodLevelMap[x, y];

                int left = x - 1;
                int right = x + 1;
                int top = y + 1;
                int bottom = y - 1;

                // check if should fix LOD seam
                int fixSeamDirection = 10000;
                int[,] direction = new int[4, 2]{
                    {left, y},{right, y}, {x, top}, {x, bottom}
                };

                for (int i = 0; i < 4; i++) {
                    int idxX = direction[i, 0];
                    int idxY = direction[i, 1];
                    if (fullLodLevelMap.IsValidIndex(idxX, idxY) && lodLevel > fullLodLevelMap[idxX, idxY]) {
                        fixSeamDirection |= (1 << i);
                    }
                }

                // 不管LOD层级有没有发生改变，都必须重新刷新mesh，因为要处理LOD接缝
                Mesh mesh = tileMeshData.GetMesh(lodLevel, fixSeamDirection);
                tileMeshData.SetMesh(mesh);
            }
            //Debug.Log(string.Format("update successfully! handle tile num {0}", terrainTileList.GetLength(0)));
        }


        internal TDList<int> GetFullLODLevelMap(Vector3 cameraPos, TerrainCluster left, TerrainCluster right, TerrainCluster up, TerrainCluster down) {
            int tileWidth = terrainTileList.GetLength(1);
            int tileHeight = terrainTileList.GetLength(0);

            TDList<int> lodLevelMap = GetLODLevelMap(cameraPos);
            TDList<int> fullLodLevelMap = new TDList<int>(tileWidth + 2, tileHeight + 2);

            // copy cluster's lodlevelmap to fulllodlevelmap
            for(int i = 1; i < tileWidth + 1; i++) {
                for(int j = 1; j < tileHeight + 1; j++) {
                    fullLodLevelMap[i, j] = lodLevelMap[i - 1, j - 1];
                }
            }

            // copy egde's lodlevelmap to fulllodlevelmap. careful for the sequence
            List<int> leftLODLevel = new List<int>() { -1, -1, -1, -1};
            if (left != null) {
                leftLODLevel = left.GetEdgeLODLevel(cameraPos, GetEdgeDir.Right);
            }
            for(int i = 0; i < tileHeight; i ++) {
                fullLodLevelMap[0, i + 1] = leftLODLevel[i];
            }

            List<int> rightLODLevel = new List<int>() { -1, -1, -1, -1 };
            if (right != null) {
                rightLODLevel = right.GetEdgeLODLevel(cameraPos, GetEdgeDir.Left);
            }
            for (int i = 0; i < tileHeight; i++) {
                fullLodLevelMap[tileHeight + 1, i + 1] = rightLODLevel[i];
            }

            List<int> upLODLevel = new List<int>() { -1, -1, -1, -1 };
            if (up != null) {
                upLODLevel = up.GetEdgeLODLevel(cameraPos, GetEdgeDir.Down);
            }
            for (int i = 0; i < tileWidth; i++) {
                fullLodLevelMap[i + 1, tileWidth + 1] = upLODLevel[i];
            }

            List<int> downLODLevel = new List<int>() { -1, -1, -1, -1 };
            if (down != null) {
                downLODLevel = down.GetEdgeLODLevel(cameraPos, GetEdgeDir.Up);
            }
            for (int i = 0; i < tileWidth; i++) {
                fullLodLevelMap[i + 1, 0] = downLODLevel[i];
            }

            return fullLodLevelMap;
        }

        private TDList<int> GetLODLevelMap(Vector3 cameraPos) {
            int tileWidth = terrainTileList.GetLength(1);
            int tileHeight = terrainTileList.GetLength(0);
            TDList<int> lodLevelMap = new TDList<int>(tileWidth, tileHeight);

            // get every tile's LOD level
            foreach (var tileMeshData in terrainTileList) {
                int x = tileMeshData.tileIdxX;
                int y = tileMeshData.tileIdxY;
                int lodLevel = tileMeshData.GetRefinedLODLevel(cameraPos);
                lodLevelMap[x, y] = lodLevel;
            }
            return lodLevelMap;
        }

        private enum GetEdgeDir {
            Left, Right, Up, Down
        }

        private List<int> GetEdgeLODLevel(Vector3 cameraPos, GetEdgeDir edgeDir) {
            int tileWidth = terrainTileList.GetLength(1);
            int tileHeight = terrainTileList.GetLength(0);

            // 一般而言 tileWidth = tileHeight
            List<int> lodLevels = new List<int>(4);

            if (edgeDir == GetEdgeDir.Left || edgeDir == GetEdgeDir.Right) {
                for (int i = 0; i < tileHeight; i++) {
                    lodLevels.Add(-1);
                }
            } else {
                for (int i = 0; i < tileWidth; i++) {
                    lodLevels.Add(-1);
                }
            }
            

            switch (edgeDir) {
                case GetEdgeDir.Left:
                    for (int i = 0; i < tileHeight; i++) {
                        int lodLevel = terrainTileList[0, i].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Right:
                    for (int i = 0; i < tileHeight; i++) {
                        int lodLevel = terrainTileList[tileWidth - 1, i].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Up:
                    for (int i = 0; i < tileWidth; i++) {
                        int lodLevel = terrainTileList[i, tileHeight - 1].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Down:
                    for (int i = 0; i < tileWidth; i++) {
                        int lodLevel = terrainTileList[i, 0].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
            }
            return lodLevels;
        }

        #endregion

    }


    public class TerrainTile {

        public int tileIdxX { get; private set; }
        public int tileIdxY { get; private set; }

        public int longitude { get; private set; }
        public int latitude { get; private set; }

        public int curLODLevel { get; private set; }

        Vector3 clusterStartPoint;
        int tileSize;
        int[] LODLevels;
        MeshData[] LODMeshes;

        HeightDataManager heightDataManager;      // 引用类型 存放高度数据

        public Vector3 tileCenterPos { get; private set; }

        private MeshFilter meshFilter;

        class MeshData {

            public int curLODLevel { get; private set; }

            Vector3[] vertexs = new Vector3[1];
            Vector3[] fixedVertexs = new Vector3[1];
            Vector3[] outofMeshVertexs = new Vector3[1];
            Vector3[] fixedOutMeshVertexs = new Vector3[1];

            int[,] vertexIndiceMap = new int[1, 1];         // map the (x, y) to index in vertexs/outofMeshVertexs

            Vector3[] normals = new Vector3[1];
            Vector2[] uvs = new Vector2[1];

            Color[] colors = new Color[1];

            int[] triangles = new int[1];
            int[] outOfMeshTriangles = new int[1];

            private int triangleIndex = 0;
            private int outOfMeshTriangleIndex = 0;

            private int vertexPerLine;
            private int vertexPerLineFixed;

            public void InitMeshData(int gridNumPerLine, int gridNumPerLineFixed, int vertexPerLine, int vertexPerLineFixed) {
                this.vertexPerLine = vertexPerLine;
                this.vertexPerLineFixed = vertexPerLineFixed;
                vertexs = new Vector3[vertexPerLine * vertexPerLine];
                outofMeshVertexs = new Vector3[vertexPerLine * 4 + 4];
                colors = new Color[vertexPerLine * vertexPerLine];
                vertexIndiceMap = new int[vertexPerLineFixed, vertexPerLineFixed];

                normals = new Vector3[vertexPerLine * vertexPerLine];
                uvs = new Vector2[vertexPerLine * vertexPerLine];

                triangles = new int[gridNumPerLine * gridNumPerLine * 2 * 3];
                outOfMeshTriangles = new int[(gridNumPerLine + 1) * 4 * 2 * 3];

                triangleIndex = 0;
                outOfMeshTriangleIndex = 0;
            }

            public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertIndex) {
                if (vertIndex < 0) {
                    outofMeshVertexs[- vertIndex - 1] = vertexPosition;
                    //fixedOutMeshVertexs[- vertIndex - 1] = vertexPosition;
                } else {
                    vertexs[vertIndex] = vertexPosition;
                    //fixedVertexs[vertIndex] = vertexPosition;
                    uvs[vertIndex] = uv;
                    normals[vertIndex] = new Vector3(0, 1, 0);

                    colors[vertIndex] = GetColorByHeight(vertexPosition.y);
                }
            }

            static Color lowLandColor = new Color(0.13f, 0.54f, 0.13f); // 深绿色，低地
            static Color midLandColor = new Color(0.61f, 0.80f, 0.19f); // 浅绿色，中地
            static Color highLandColor = new Color(0.85f, 0.65f, 0.13f); // 棕黄色，高地
            static Color mountainColor = new Color(0.50f, 0.50f, 0.50f); // 灰色，山地
            static Color snowColor = new Color(1.00f, 1.00f, 1.00f); // 白色，雪地

            private Color GetColorByHeight(float height) {
                if (height < 10f)
                    return lowLandColor; // 低地
                else if (height < 15f)
                    return midLandColor; // 中地
                else if (height < 23f)
                    return highLandColor; // 高地
                else if (height < 30f)
                    return mountainColor; // 山地
                else
                    return snowColor; // 雪地
            }

            // GPT 提供：湿度、温度、海拔混合公式
            private Color CalculateTerrainColor(float temperature, float humidity, float height) {
                float mexHeight = 40;
                float snowLine = 40;

                Color baseColor = Color.green;
                baseColor.r += Mathf.Clamp(temperature - 20, 0, 10) * 0.05f;    // 温度影响
                baseColor.g += humidity * 0.1f;                                 // 湿度影响
                baseColor *= 1.0f - (height / mexHeight);                       // 海拔影响
                if (height > snowLine) {
                    baseColor = Color.Lerp(baseColor, Color.white, (height - snowLine) / (mexHeight - snowLine)); // 高山雪地
                }
                return baseColor;
            }


            public void AddTriangle(int a, int b, int c, int i = 0, int j = 0) {
                if (a < 0 || b < 0 || c < 0) {
                    if (outOfMeshTriangleIndex + 1 > outOfMeshTriangles.Length - 1) {
                        Debug.LogError(string.Format("triangle idx : {0}, {1} !", i, j));
                        Debug.LogError(string.Format("out of bound! cur idx : {0}, cur a : {1}, cur b : {2}, cur c : {3}, length : {4}", outOfMeshTriangleIndex, a, b, c, outOfMeshTriangles.Length));
                    }
                    outOfMeshTriangles[outOfMeshTriangleIndex] = a;
                    outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
                    outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
                    outOfMeshTriangleIndex += 3;
                } else {
                    triangles[triangleIndex] = a;
                    triangles[triangleIndex + 1] = b;
                    triangles[triangleIndex + 2] = c;
                    triangleIndex += 3;
                }
            }

            // 此处代码参考：Procedural-Landmass-Generation-master\Proc Gen E21
            public void RecaculateNormal() {

                int triangleCount = triangles.Length / 3;
                for (int i = 0; i < triangleCount; i++) {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = triangles[normalTriangleIndex];
                    int vertexIndexB = triangles[normalTriangleIndex + 1];
                    int vertexIndexC = triangles[normalTriangleIndex + 2];

                    Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                    //Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                    normals[vertexIndexA] += triangleNormal;
                    normals[vertexIndexB] += triangleNormal;
                    normals[vertexIndexC] += triangleNormal;
                }

                // border triangle, caculate their value to normal
                int borderTriangleCount = outOfMeshTriangles.Length / 3;
                for (int i = 0; i < borderTriangleCount; i++) {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                    int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                    int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                    Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                    //Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                    if (vertexIndexA >= 0) {
                        normals[vertexIndexA] += triangleNormal;
                    }
                    if (vertexIndexB >= 0) {
                        normals[vertexIndexB] += triangleNormal;
                    }
                    if (vertexIndexC >= 0) {
                        normals[vertexIndexC] += triangleNormal;
                    }
                }

                for (int i = 0; i < normals.Length; i++) {
                    normals[i].Normalize();
                }
            }

            // TODO: 这是一个冗余的函数，要改！
            public void RecaculateBorderNormal() {
                // TODO: 这个方法仅会重新计算边缘顶点的法线
                // TODO: 
                int triangleCount = triangles.Length / 3;
                for (int i = 0; i < triangleCount; i++) {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = triangles[normalTriangleIndex];
                    int vertexIndexB = triangles[normalTriangleIndex + 1];
                    int vertexIndexC = triangles[normalTriangleIndex + 2];

                    Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                    normals[vertexIndexA] += triangleNormal;
                    normals[vertexIndexB] += triangleNormal;
                    normals[vertexIndexC] += triangleNormal;
                }

                // border triangle, caculate their value to normal
                int borderTriangleCount = outOfMeshTriangles.Length / 3;
                for (int i = 0; i < borderTriangleCount; i++) {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                    int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                    int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                    Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                    if (vertexIndexA >= 0) {
                        normals[vertexIndexA] += triangleNormal;
                    }
                    if (vertexIndexB >= 0) {
                        normals[vertexIndexB] += triangleNormal;
                    }
                    if (vertexIndexC >= 0) {
                        normals[vertexIndexC] += triangleNormal;
                    }
                }

                for (int i = 0; i < normals.Length; i++) {
                    normals[i].Normalize();
                }
            }

            private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
                // 算三个点构成的三角型的叉乘
                Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : vertexs[indexA];
                Vector3 pointB = (indexB < 0) ? outofMeshVertexs[-indexB - 1] : vertexs[indexB];
                Vector3 pointC = (indexC < 0) ? outofMeshVertexs[-indexC - 1] : vertexs[indexC];

                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                return Vector3.Cross(sideAB, sideAC).normalized;
            }

            private Vector3 SurfaceNormalFromIndices_Fixed(int indexA, int indexB, int indexC) {
                Vector3 pointA = (indexA < 0) ? fixedOutMeshVertexs[-indexA - 1] : fixedVertexs[indexA];
                Vector3 pointB = (indexB < 0) ? fixedOutMeshVertexs[-indexB - 1] : fixedVertexs[indexB];
                Vector3 pointC = (indexC < 0) ? fixedOutMeshVertexs[-indexC - 1] : fixedVertexs[indexC];

                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                return Vector3.Cross(sideAB, sideAC).normalized;
            }

            #region mesh data get/set

            public Mesh GetMesh(int tileIdxX, int tileIdxY, int fixDirection) {
                Mesh mesh = new Mesh();
                mesh.name = string.Format("TerrainMesh_LOD{0}_Idx{1}_{2}", curLODLevel, tileIdxX, tileIdxY);

                // fix the lod seam
                fixedVertexs = vertexs;
                fixedOutMeshVertexs = outofMeshVertexs;
                bool fixLeft = ((fixDirection >> 0) & 1) == 1;
                bool fixRight = ((fixDirection >> 1) & 1) == 1;
                bool fixTop = ((fixDirection >> 2) & 1) == 1;
                bool fixBottom = ((fixDirection >> 3) & 1) == 1;
                if (fixLeft) {
                    // NOTE : 这块代码和外层 TileMeshData.SetMeshData 存在耦合，很重的耦合
                    FixLODEdgeSeam(true, 0, 1);
                }
                if (fixRight) {
                    FixLODEdgeSeam(true, vertexPerLineFixed - 1, vertexPerLineFixed - 2);
                }
                if (fixTop) {
                    FixLODEdgeSeam(false, vertexPerLineFixed - 1, vertexPerLineFixed - 2);
                }
                if (fixBottom) {
                    FixLODEdgeSeam(false, 0, 1);
                }

                //RecaculateNormal();
                RecaculateBorderNormal();

                mesh.vertices = fixedVertexs;
                mesh.normals = normals;
                mesh.triangles = triangles;
                mesh.uv = uvs;
                mesh.colors = colors;

                return mesh;
            }

            private void FixLODEdgeSeam(bool isVertical, int outIdx, int inIdx) {
                for (int i = 2; i < vertexPerLine + 1; i += 2) {
                    // traverse the indice map and reset the vertex position to neighbor's average

                    int outNgb1Idx, outNgb2Idx, outofMeshIdx;
                    int inNgb1Idx, inNgb2Idx, inMeshIdx;
                    //Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : vertexs[indexA];
                    if (isVertical) {
                        outNgb1Idx = vertexIndiceMap[outIdx, i - 1];
                        outNgb2Idx = vertexIndiceMap[outIdx, i + 1];
                        outofMeshIdx = vertexIndiceMap[outIdx, i];

                        inNgb1Idx = vertexIndiceMap[inIdx, i - 1];
                        inNgb2Idx = vertexIndiceMap[inIdx, i + 1];
                        inMeshIdx = vertexIndiceMap[inIdx, i];
                    } else {
                        outNgb1Idx = vertexIndiceMap[i - 1, outIdx];
                        outNgb2Idx = vertexIndiceMap[i + 1, outIdx];
                        outofMeshIdx = vertexIndiceMap[i, outIdx];

                        inNgb1Idx = vertexIndiceMap[i - 1, inIdx];
                        inNgb2Idx = vertexIndiceMap[i + 1, inIdx];
                        inMeshIdx = vertexIndiceMap[i, inIdx];
                    }
                    fixedOutMeshVertexs[-outofMeshIdx - 1] = (fixedOutMeshVertexs[-outNgb1Idx - 1] + fixedOutMeshVertexs[-outNgb2Idx - 1]) / 2;
                    fixedVertexs[inMeshIdx] = (fixedVertexs[inNgb1Idx] + fixedVertexs[inNgb2Idx]) / 2;
                }
                
            }

            public int GetIndiceInMap(int x, int y) {
                return vertexIndiceMap[x, y];
            }

            public void SetIndiceInMap(int x, int y, int idx) {
                vertexIndiceMap[x, y] = idx;
            }

            #endregion

        }

        public TerrainTile() { }

        public void InitTileMeshData(int idxX, int idxY, int longitude, int latitude, Vector3 startPoint, MeshFilter meshFilter,int[] lODLevels) {
            this.clusterStartPoint = startPoint;
            this.meshFilter = meshFilter;

            tileIdxX = idxX;
            tileIdxY = idxY;

            this.longitude = longitude;
            this.latitude = latitude;

            curLODLevel = -1;       // init as -1

            LODLevels = lODLevels;
            LODMeshes = new MeshData[lODLevels.Length];
        }

        public void SetMeshData(int curLODLevel, int tileSize, int vertexNumFix, Vector3Int terrainClusterSize, HeightDataManager heightDataManager) {
            this.tileSize = tileSize;
            this.heightDataManager = heightDataManager;

            // caculate tile's start point
            float startX = tileIdxX * tileSize;
            float startZ = tileIdxY * tileSize;
            Vector3 startPoint = new Vector3(startX, 0, startZ) + this.clusterStartPoint;       // 
            tileCenterPos = startPoint + new Vector3(tileSize / 2, 0, tileSize / 2);

            int gridNumPerLine = tileSize / vertexNumFix;
            int gridSize = tileSize / gridNumPerLine;
            int vertexPerLine = tileSize / vertexNumFix + 1;

            int gridNumPerLineFixed = gridNumPerLine + 2;
            int vertexPerLineFixed = vertexPerLine + 2;

            LODMeshes[curLODLevel] = new MeshData();
            MeshData meshData = LODMeshes[curLODLevel];
            meshData.InitMeshData(gridNumPerLine, gridNumPerLineFixed, vertexPerLine, vertexPerLineFixed);

            int curInVertIdx = 0;
            int curOutVertIdx = -1;
            //int[,] vertexIndiceMap = new int[vertexPerLineFixed, vertexPerLineFixed];

            // TODO: use job system
            Vector3 offsetInMeshVert = new Vector3(gridSize, 0, gridSize);
            for(int i = 0; i < vertexPerLineFixed; i++) {
                for (int j = 0; j < vertexPerLineFixed; j++) {
                    bool isVertOutOfMesh = (i == 0) || (i == vertexPerLineFixed - 1) || (j == 0) || (j == vertexPerLineFixed - 1);

                    Vector3 vert = new Vector3(gridSize * i, 0, gridSize * j) + startPoint - offsetInMeshVert;

                    //float height = SampleFromHeightData(terrainClusterSize, vert);
                    float height = heightDataManager.SampleFromHeightData(longitude, latitude, vert, clusterStartPoint);

                    //float height = 0;

                    vert.y = height;
                    Vector2 uv = new Vector2(vert.x / terrainClusterSize.x, vert.z / terrainClusterSize.z);

                    if (isVertOutOfMesh) {
                        meshData.AddVertex(vert, uv, curOutVertIdx);
                        meshData.SetIndiceInMap(i, j, curOutVertIdx);
                        //vertexIndiceMap[i, j] = curOutVertIdx;
                        curOutVertIdx --;
                    } else {
                        meshData.AddVertex(vert, uv, curInVertIdx);
                        meshData.SetIndiceInMap(i, j, curInVertIdx);
                        //vertexIndiceMap[i, j] = curInVertIdx;
                        curInVertIdx ++;
                    }
                }
            }

            int curGridIdx = 0;
            for (int i = 0; i < gridNumPerLineFixed; i++) {
                for (int j = 0; j < gridNumPerLineFixed; j++) {
                    // i, j 是当前遍历到的 grid 的 index
                    int cur_w = curGridIdx % gridNumPerLineFixed;
                    int cur_h = curGridIdx / gridNumPerLineFixed;
                    int next_w = cur_w + 1;
                    int next_h = cur_h + 1;

                    int a = meshData.GetIndiceInMap(cur_w, cur_h);
                    int b = meshData.GetIndiceInMap(cur_w, next_h);
                    int c = meshData.GetIndiceInMap(next_w, next_h);
                    int d = meshData.GetIndiceInMap(next_w, cur_h);

                    meshData.AddTriangle(a, b, c, i, j);
                    meshData.AddTriangle(a, c, d, i, j);

                    curGridIdx++;
                }
            }

            meshData.RecaculateNormal();
        }

        public int GetRefinedLODLevel(Vector3 cameraPos) {
            float distance = Vector3.Distance(cameraPos, tileCenterPos);
            
            int idx = LODLevels.Length - 1 ;
            int levelParam = tileSize / 2;

            while (true) {
                if(idx < 0) {
                    break;
                }
                if (levelParam >= distance) {
                    return LODLevels[idx];
                } else {
                    levelParam *= 2;
                    idx--;
                }
            }

            return 0;   // default level
        }

        // Move to HeightDataManager
        /*private float SampleFromHeightData(Vector3Int terrainClusterSize, Vector3 vertPos) {
            
            int srcWidth = heightDataManager.GetLength();
            int srcHeight = heightDataManager.GetLength();

            int terrainClusterWidth = terrainClusterSize.x;
            int terrainClusterHeight = terrainClusterSize.z;

            // fixed the vert, because exist cluster offset!
            vertPos.x -= clusterStartPoint.x;
            vertPos.z -= clusterStartPoint.z;
            // resample the size of height map
            float sx = vertPos.x / terrainClusterWidth * srcWidth;
            float sy = vertPos.z / terrainClusterHeight * srcHeight;

            int x0 = Mathf.Clamp(Mathf.FloorToInt(sx), 0, srcWidth - 1);
            int x1 = Mathf.Min(x0 + 1, srcWidth - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(sy), 0, srcHeight - 1); ;
            int y1 = Mathf.Min(y0 + 1, srcHeight - 1);

            float q00 = heightDataManager[x0, y0];
            float q01 = heightDataManager[x0, y1];
            float q10 = heightDataManager[x1, y0];
            float q11 = heightDataManager[x1, y1];

            float rx0 = Mathf.Lerp(q00, q10, sx - x0);
            float rx1 = Mathf.Lerp(q01, q11, sx - x0);

            // caculate the height by the data given
            float h = Mathf.Lerp(rx0, rx1, sy - y0) * terrainClusterSize.y;
            float fixed_h = Mathf.Clamp(h, 0, 50);

            return fixed_h;
        }
*/
        
        public Mesh GetMesh(int curLODLevel, int fixDirection) {
            this.curLODLevel = curLODLevel;
            if (curLODLevel < 0 || curLODLevel >= LODMeshes.Length) {
                Debug.LogError("wrong LOD level, can not get mesh");
                return null;
            }
            Mesh mesh = LODMeshes[curLODLevel].GetMesh(tileIdxX, tileIdxY, fixDirection);
            return mesh;
        }

        public void SetMesh(Mesh mesh) {
            meshFilter.mesh = mesh;
        }

    }


}
