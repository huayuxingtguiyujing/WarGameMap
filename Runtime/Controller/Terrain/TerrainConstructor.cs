using LZ.WarGameCommon;
using System.Collections.Generic;
using System.IO;
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

        // cluster and tile setting
        int terrainWidth;
        int terrainHeight;

        TerrainSetting terSet;

        // cluster list
        private TDList<TerrainCluster> clusterList;

        public TDList<TerrainCluster> ClusterList { get { return clusterList; } }


        // height data
        private HeightDataManager heightDataManager;

        private bool hasInit = false;


        #region init height cons

        public void SetMapPrefab(Transform originPoint, Transform heightClusterParent) {
            this.originPoint = originPoint;
            this.heightClusterParent = heightClusterParent;
        }

        public void InitHeightCons(TerrainSetting terSet, List<HeightDataModel> heightDataModels) {
            heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, terSet.clusterSize);

            this.terSet = terSet;

            // 地图分块
            // TerrainCluster，cluster 对应单独的一个高度图 tif 文件
            // TerrainCluster 分为多个 TerrainTile，每个 Tile 拥有多个 LOD 层级的 mesh
            terrainWidth = terSet.terrainSize.x;
            terrainHeight = terSet.terrainSize.z;

            // clusterList 懒初始化，一次性分配太多内存会炸
            clusterList = new TDList<TerrainCluster>(terrainWidth, terrainHeight);

            hasInit = true;
            Debug.Log(string.Format($"successfully generate total terrain!  create {terrainWidth}*{terrainHeight}"));
        }

        public void InitHexCons(HexSettingSO hexSetting, RawHexMapSO rawHexMapSO) {
            if (heightDataManager == null) {
                Debug.LogError("you should init height Data Manager!");
                return;
            }
            heightDataManager.InitHexSet(hexSetting, rawHexMapSO);
        }

        // NOTE : 由于现在在开发Hex功能，所以这个函数目前不能用于构建通用地形的 TerrainMesh
        // 现在只能用来搞 hex 版本的地形
        public void BuildCluster(int i, int j, int longitude, int latitude) {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth) {
                Debug.LogError($"wrong index : {i}, {j}");
                return;
            }

            if (!clusterList[i, j].IsValid) {
                GameObject clusterGo = CreateTerrainCluster(i, j);
                clusterList[i, j].InitTerrainCluster_Static(i, j, longitude, latitude, terSet, heightDataManager, clusterGo);
            }

            Debug.Log($"handle cluster successfully, use heightData : {longitude}, {latitude}");
        }

        public void BuildClusterNormal(int i, int j, Texture2D normalTex) {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth) {
                Debug.LogError($"wrong index : {i}, {j}");
                return;
            }

            if (!clusterList[i, j].IsValid) {
                Debug.LogError($"firstly you should build the cluster : {i}, {j}");
                return;
            }

            clusterList[i, j].SampleMeshNormal(normalTex);

            Debug.Log("build mesh normal successfully!");
        }

        public void ExeSimplify(int i, int j, int tileIdxX, int tileIdxY, float simplifyTarget) {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth) {
                Debug.LogError($"wrong index : {i}, {j}");
                return;
            }

            if (!clusterList[i, j].IsValid) {
                Debug.LogError($"cluster not valid, index : {i}, {j}");
                return;
            }

            TerrainSimplifier terrainSimplifier = new TerrainSimplifier();
            clusterList[i, j].ExeTerrainSimplify(tileIdxX, tileIdxY, terrainSimplifier, simplifyTarget);

            Debug.Log($"simplify over, cluster idx : {i}, {j}, target : {simplifyTarget}");
        }

        public void ExportClusterByBinary(int idxX, int idxY, int longitude, int latitude, BinaryReader reader) {
            if (!clusterList[idxX, idxY].IsValid) {
                GameObject clusterGo = CreateTerrainCluster(idxX, idxY);
                clusterList[idxX, idxY].InitTerrainCluster_Static(idxX, idxY, longitude, latitude, terSet, heightDataManager, clusterGo);
            }
            //clusterList[idxX, idxY].SetTerrainCluster(reader);

            TDList<TerrainTile> tiles = clusterList[idxX, idxY].TileList;
            foreach (var tile in tiles) {
                tile.ReadFromBinary(reader);
                TerrainMeshData[] meshDatas = tile.GetLODMeshes();
                foreach (var terrainMesh in meshDatas) {
                    terrainMesh.ReadFromBinary(reader);
                }
            }

        }

        public void ImportClusterToBinary(int i, int j, BinaryWriter writer) {
            if (!clusterList[i, j].IsValid) {
                return;
            }

            TDList<TerrainTile> tiles = clusterList[i, j].TileList;
            foreach (var tile in tiles) {
                // write tile setting to file
                tile.WriteToBinary(writer);

                // write every mesh to file
                TerrainMeshData[] meshDatas = tile.GetLODMeshes();
                foreach (var terrainMesh in meshDatas) {
                    terrainMesh.WriteToBinary(writer);
                }
            }
        }

        private GameObject CreateTerrainCluster(int idxX, int idxY) {
            GameObject go = new GameObject();
            go.transform.parent = heightClusterParent;
            go.name = string.Format("heightCluster_{0}_{1}", idxX, idxY);
            return go;
        }

        public void ClearClusterObj() {
            terrainWidth = 0;
            terrainHeight = 0;
            heightClusterParent.ClearObjChildren();
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

        #endregion

        public TerrainCluster GetTerrainCluster(int i, int j) {
            return clusterList[i, j];
        }

        public int GetValidClusterNum() {
            int validClusterNum = 0;
            for (int i = 0; i < terrainWidth; i++) {
                for (int j = 0; j < terrainHeight; j++) {
                    if (clusterList[i, j].IsValid) {
                        validClusterNum++;
                    }
                }
            }
            return validClusterNum;
        }

        // NOTE : 不要删掉！！
        /*#region use quad tree LOD init and dynamic switching

        //*List<GameObject> heightClusters;
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
        }
        #endregion*/

    }


}
