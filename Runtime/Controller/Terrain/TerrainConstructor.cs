using LZ.WarGameCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    using GetSimplifierCall = Func<int, int, int, int, int, TerrainSimplifier>;

    public enum TerMeshGenMethod {
        TIFHeight,
        HexData,
    }

    /// <summary>
    /// 此类用于构建地形Mesh，基于高度图应用到 MeshRender 上
    /// </summary>
    //[ExecuteInEditMode]
    public class TerrainConstructor : MonoBehaviour
    {

        [SerializeField] Transform originPoint;
        [SerializeField] Transform heightClusterParent;
        [SerializeField] Transform riverMeshParent;
        [SerializeField] Transform signParent;

        // cluster and tile setting
        public int terrainWidth;
        public int terrainHeight;

        public MapRuntimeSetting mapSet;
        public TerrainSettingSO terSet;

        public Material terMaterial;

        public bool hasInit = false;
        // NOTE : will change to private

        // cluster list
        private TDList<TerrainCluster> clusterList;
        public TDList<TerrainCluster> ClusterList { get {
                if (clusterList == null) {
                    // TODO ; 要自动构建吗？
                }
                return clusterList; 
            }
        }


        // height data
        private HeightDataManager heightDataManager;

        private RiverDataManager riverDataManager;

        #region init terrain cons

        public void SetMapPrefab(Transform originPoint, Transform heightClusterParent, Transform riverMeshParent) {
            this.originPoint = originPoint;
            this.heightClusterParent = heightClusterParent;
            this.riverMeshParent = riverMeshParent;
        }

        public void InitTerrainCons(MapRuntimeSetting mapSet, TerrainSettingSO terSet, HexSettingSO hexSetting, List<HeightDataModel> heightDataModels, 
            HexMapSO rawHexMapSO, Material mat, MapRiverData mapRiverData) {
            heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, terSet, hexSetting, rawHexMapSO);
            //heightDataManager.InitHexSet(hexSetting, rawHexMapSO);

            riverDataManager = new RiverDataManager(mapRiverData, terSet.riverDownOffset, terSet.tileSize, terSet.clusterSize, terSet.terrainSize, riverMeshParent);

            this.mapSet = mapSet;
            this.terSet = terSet;
            this.terMaterial = mat;

            // 地图分块
            // TerrainCluster，cluster 对应单独的一个高度图 tif 文件
            // TerrainCluster 分为多个 TerrainTile，每个 Tile 拥有多个 LOD 层级的 mesh
            terrainWidth = terSet.terrainSize.x;
            terrainHeight = terSet.terrainSize.z;

            // clusterList 进行懒初始化，一次性分配太多内存会炸
            clusterList = new TDList<TerrainCluster>(terrainWidth, terrainHeight);
            hasInit = true;
            Debug.Log(string.Format($"successfully init terrain constructor!  create {terrainWidth}*{terrainHeight}"));
        }

        public void ClearClusterObj()
        {
            terrainWidth = 0;
            terrainHeight = 0;

            if (clusterList != null)
            {
                foreach (var cluster in clusterList)
                {
                    cluster.Dispose();
                }
            }

            heightClusterParent.ClearObjChildren();

            if (riverDataManager != null)
            {
                riverDataManager.Dispose();
            }
            riverMeshParent.ClearObjChildren();

            hasInit = false;    // clear 之后回归未初始化状态
        }

        private void OnDestroy()
        {
            riverDataManager.Dispose();
        }

        #endregion

        #region build terrain

        [Obsolete("现在应该分阶段地在外部调用各个步骤进行生成，逻辑放到了 TerrainGenTask 里头")]
        public void BuildCluster(int i, int j, bool shouldGenRiver, bool shouldGenLODBySimplify, int timerID)
        {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth)
            {
                Debug.LogError($"wrong index : {i}, {j}");
                return;
            }

            int longitude = this.terSet.startLL.x + i;
            int latitude = this.terSet.startLL.y + j;
            if (!clusterList[i, j].IsLoaded)
            {
                GameObject clusterGo = CreateTerrainCluster(i, j);
                //clusterList[i, j].InitTerrainCluster_Static(i, j, longitude, latitude, terSet, heightDataManager, clusterGo, terMaterial, shouldGenLODBySimplify);

                //ProgressManager.GetInstance().ProgressGoChildNextTask(timerID);

                if (shouldGenRiver)
                {
                    riverDataManager.BuildRiverData(i, j);
                    clusterList[i, j].ApplyRiverEffect(heightDataManager, riverDataManager);
                }

                //ProgressManager.GetInstance().ProgressGoChildNextTask(timerID);

                if (shouldGenLODBySimplify)
                {
                    clusterList[i, j].ExeTerrainSimplify_MT();
                }
            }

            //ProgressManager.GetInstance().ProgressGoNextTask(timerID);
            Debug.Log($"handle cluster successfully, use heightData : {longitude}, {latitude}");
        }

        public async Task BuildCluster_TerMesh(int i, int j, bool shouldGenLODBySimplify, CancellationToken token)
        {
            CheckClusterIdxValid(i, j);

            int longitude = this.terSet.startLL.x + i;
            int latitude = this.terSet.startLL.y + j;
            GameObject clusterGo = CreateTerrainCluster(i, j);
            clusterList[i, j].InitTerrainCluster_Static(i, j, longitude, latitude, terSet, clusterGo, terMaterial);

            Action<CancellationToken> exeGenTerMesh = (cancelToken) =>
            {
                clusterList[i, j].SetMeshData(heightDataManager, shouldGenLODBySimplify);
            };
            await ThreadManager.GetInstance().RunTaskAsync(exeGenTerMesh, null, token);
        }

        public async Task BuildCluster_River(int i, int j, CancellationToken token)
        {
            CheckClusterIdxValid(i, j);
            CheckClusterLoaded(i, j);
            
            Action<CancellationToken> exeSimplify = (cancelToken) =>
            {
                riverDataManager.BuildRiverData(i, j);
                clusterList[i, j].ApplyRiverEffect(heightDataManager, riverDataManager);
            };
            await ThreadManager.GetInstance().RunTaskAsync(exeSimplify, null, token);

            riverDataManager.BuildRiverMesh(i ,j);
        }

        public void BuildOriginMesh(int i, int j)
        {
            CheckClusterLoaded(i, j);
            clusterList[i, j].BuildOriginMesh();
        }

        public async Task ExeSimplify_MT(int i, int j, GetSimplifierCall getSimplifierCall, CancellationToken token)
        {
            CheckClusterIdxValid(i, j);
            CheckClusterLoaded(i, j);

            await clusterList[i, j].ExeSimplify_MT(getSimplifierCall, token);
        }

        public void ExeSimplify(int i, int j, int tileIdxX, int tileIdxY, float simplifyTarget)
        {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth)
            {
                Debug.LogError($"wrong index : {i}, {j}");
                return;
            }

            if (!clusterList[i, j].IsLoaded)
            {
                Debug.LogError($"cluster not valid, index : {i}, {j}");
                return;
            }

            TerrainSimplifier terrainSimplifier = new TerrainSimplifier();
            clusterList[i, j].ExeTerrainSimplify(tileIdxX, tileIdxY, terrainSimplifier, simplifyTarget);

            Debug.Log($"simplify over, cluster idx : {i}, {j}, target : {simplifyTarget}");
        }

        public int GetClusterCurVertNum(int i, int j)
        {
            return clusterList[i, j].GetClusterCurVertNum();
        }

        public int GetTargetSimplifyVertNum(int i, int j)
        {
            return clusterList[i, j].GetTargetSimplifyVertNum();
        }


        public void BuildCluster_Normal(int i, int j, Texture2D normalTex)
        {
            CheckClusterLoaded(i, j);

            clusterList[i, j].SampleMeshNormal(normalTex);
            Debug.Log("build mesh normal successfully!");
        }

        private void CheckClusterIdxValid(int i, int j)
        {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth)
            {
                throw new Exception($"wrong terrain cluster index : {i}, {j}");
            }
        }

        private void CheckClusterLoaded(int i, int j)
        {
            if (!clusterList[i, j].IsLoaded)
            {
                throw new Exception($"you should firstly build the cluster : {i}, {j}");
            }
        }

        private GameObject CreateTerrainCluster(int idxX, int idxY)
        {
            GameObject go = new GameObject();
            go.transform.parent = heightClusterParent;
            go.name = string.Format("heightCluster_{0}_{1}", idxX, idxY);
            return go;
        }

        #endregion


        #region 序列化/反序列化 terrain mesh 数据

        // TODO : 要大改了
        public void ExportClusterByBinary(int idxX, int idxY, int longitude, int latitude, BinaryReader reader) {
            if (!clusterList[idxX, idxY].IsLoaded) {
                GameObject clusterGo = CreateTerrainCluster(idxX, idxY);
                //clusterList[idxX, idxY].InitTerrainCluster_Static(idxX, idxY, longitude, latitude, terSet, heightDataManager, clusterGo, null, true);
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
            if (!clusterList[i, j].IsLoaded) {
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

        #endregion


        #region runtime updating

        public Vector3Int preCameraIdx = new Vector3Int(-100, 0, -100);   // NOTE : x z is idx, y is the curLODLevel

        public HashSet<Vector2Int> preClusterIdxSet = new HashSet<Vector2Int>();

        public void UpdateTerrain() {
            if(!hasInit) {
                //Debug.LogError("cons do not init!");
                return; 
            }
            if (clusterList == null) {
                //Debug.LogError("cluster list is null!");
                return;
            }

            Vector3 cameraPos = Camera.main.transform.position;
            int cameraIdxX = (int)(cameraPos.x / terSet.clusterSize);
            int cameraIdxY = (int)(cameraPos.z / terSet.clusterSize);
            // get the current LOD level
            float ratio = cameraPos.y / mapSet.MeshFadeDistance;
            int distanceLevel = (int)(terSet.LODLevel * ratio);
            int curLODLevel = terSet.LODLevel - distanceLevel - 1;

            Vector3Int curCameraIdx = new Vector3Int(cameraIdxX, curLODLevel, cameraIdxY);
            if (curCameraIdx == preCameraIdx && preClusterIdxSet != null && preClusterIdxSet.Count > 0) {
                // if camera-cluster do not change and pre cls is not null, then return
                return;
            }

            int aoiScope = mapSet.AOIScope;
            HashSet<Vector2Int> newScopeCls = new HashSet<Vector2Int>();

            for (int i = Mathf.Max(0, curCameraIdx.x - aoiScope); i <= Mathf.Min(terrainWidth - 1, curCameraIdx.x + aoiScope); i++) {
                for (int j = Mathf.Max(0, curCameraIdx.z - aoiScope); j <= Mathf.Min(terrainHeight - 1, curCameraIdx.z + aoiScope); j++) {
                    newScopeCls.Add(new Vector2Int(i, j));
                }
            }

            HashSet<Vector2Int> shouldHideIdxs = new HashSet<Vector2Int>();
            HashSet<Vector2Int> shouldShowIdxs = new HashSet<Vector2Int>();
            foreach (var idx in preClusterIdxSet) {
                if (!newScopeCls.Contains(idx)) {
                    shouldHideIdxs.Add(idx);
                }
            }
            foreach (var idx in newScopeCls) {
                if (!preClusterIdxSet.Contains(idx)) {
                    shouldShowIdxs.Add(idx);
                }
            }

            // hide all cluster in list
            foreach (var idx in shouldHideIdxs) {
                if (idx.x < 0 || idx.x >= terrainHeight || idx.y < 0 || idx.y >= terrainWidth) {
                    continue;
                }
                if (clusterList[idx.x, idx.y].IsShowing) {
                    clusterList[idx.x, idx.y].HideTerrainCluster();
                }
            }

            // show all cluster in list
            foreach (var idx in newScopeCls) {
                if (idx.x < 0 || idx.x >= terrainHeight || idx.y < 0 || idx.y >= terrainWidth) {
                    continue;
                }
                TerrainCluster cluster = clusterList[idx.x, idx.y];
                if (cluster.IsLoaded) {
                    if(mapSet.lodSwitchMethod == LODSwitchMethod.Height) {
                        UpdateTerrain_LODHeight(curLODLevel, cluster);
                    } else if (mapSet.lodSwitchMethod == LODSwitchMethod.Distance) {
                        UpdateTerrain_LODDistance(cameraPos, cluster);
                    }
                }
            }
            // do not delete!
            //DebugHashSet(newScopeCls, "new-scope");
            //DebugHashSet(preClusterIdxSet, "pre-scope");
            //DebugHashSet(shouldHideIdxs, "hide");
            //DebugHashSet(shouldShowIdxs, "show");

            preCameraIdx = new Vector3Int(cameraIdxX, curLODLevel, cameraIdxY);
            preClusterIdxSet = newScopeCls;
            DebugUtility.Log(string.Format("cur camera cls idx : {0}, cur LOD : {1}, hide cluster : {2}, show cluster {3}, load cluster {4}", curCameraIdx, curLODLevel, shouldHideIdxs.Count, shouldShowIdxs.Count, 0), DebugPriority.High);
        }

        private void DebugHashSet(HashSet<Vector2Int> sets, string name) {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var idx in sets)
            {
                stringBuilder.Append(idx.ToString());
            }
            DebugUtility.Log($"{name} hashSet content : {stringBuilder.ToString()}");
        }

        private int UpdateTerrain_LODHeight(int curLODLevel, TerrainCluster cluster) {
            int x = cluster.idxX;
            int y = cluster.idxY;
            return cluster.UpdateTerrainCluster_LODHeight(curLODLevel, terSet.LODLevel);
        }

        private void UpdateTerrain_LODDistance(Vector3 cameraPos, TerrainCluster cluster) {
            int x = cluster.idxX;
            int y = cluster.idxY;

            //int leftIdx = x - 1;
            //int rightIdx = x + 1;
            //int upIdx = y + 1;
            //int downIdx = y - 1;
            //int[,] direction = new int[4, 2]{
            //    {leftIdx, y},{rightIdx, y}, {x, upIdx}, {x, downIdx}
            //};
            //  find neighbours, the sequence is : left, right, up, down;
            //List<TerrainCluster> terrainClusters = new List<TerrainCluster>(4) { null, null, null, null };
            //for (int i = 0; i < 4; i++) {
                //int neighborX = direction[i, 0];
                //int neighborY = direction[i, 1];
                //if (clusterList.IsValidIndex(neighborX, neighborY) && clusterList[neighborX, neighborY].IsLoaded) {
                    //terrainClusters[i] = clusterList[neighborX, neighborY];
                //}
            //}
            // TODO : fullLodLevelMap 
            TDList<int> fullLodLevelMap = clusterList[x, y].GetFullLODLevelMap(cameraPos);
            // i dont know why there need clusters as params
            // , terrainClusters[0], terrainClusters[1], terrainClusters[2], terrainClusters[3]
            cluster.UpdateTerrainCluster_LODDistance(fullLodLevelMap);
        }

        #endregion

        public Mesh GetTerTileMesh(int lodLevel, int clusterX, int clusterY, int tileX, int tileY) {
            TerrainCluster cluster = GetTerrainCluster(clusterX, clusterY);
            if(cluster == null) {
                Debug.LogError("can not get cluster!");
                return null;
            }
            return cluster.GetTileMesh(lodLevel, tileX, tileY);
        }

        public TerrainCluster GetTerrainCluster(int i, int j) {
            if (i < 0 || i >= terrainHeight || j < 0 || j >= terrainWidth) {
                Debug.LogError($"wrong index when get cluster : {i}, {j}");
                return null;
            }
            return clusterList[i, j];
        }

        public int GetValidClusterNum() {
            int validClusterNum = 0;
            for (int i = 0; i < terrainWidth; i++) {
                for (int j = 0; j < terrainHeight; j++) {
                    if (clusterList[i, j].IsLoaded) {
                        validClusterNum++;
                    }
                }
            }
            return validClusterNum;
        }

        // quad tree NOTE : 不要删掉！！
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
