using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime.HexStruct;
using LZ.WarGameMap.Runtime.Model;
using LZ.WarGameMap.Runtime.QuadTree;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {
    /// <summary>
    /// 封装 六边形网格地图 的生成过程，地图管理不在此处，此类不会工作在运行时
    /// - 负责把 hexagon 数据转为 monobehaviour 的地图块（MapCluster）
    /// - 负责 地图效果的制作和调整
    /// </summary>
    public class HexmapConstructor : MonoBehaviour 
    {
        [Header("hex map config")]
        [SerializeField] HexSettingSO hexSet;
        [SerializeField] public GameObject SignPrefab;
        [SerializeField] Material hexMat;

        Transform clusterParentTrans;
        Transform signParentTrans;

        HexGenerator hexGenerator;
        Layout layout;

        int width;
        int height;
        int clusterWidth;
        int clusterHeight;

        public bool hasInit = false;

        [Header("hex map object")]
        [SerializeField] TDList<HexCluster> hexClusters;

        int curLoadedClusterNum = 0;

        QuadTree<MapGrid> mapGridQuadTree;

        // GamePlay managers
        CountryManager countryManager = new CountryManager();

        // runtime update
        Vector2Int lastIdx = new Vector2Int(-1, -1);

        HashSet<Vector2Int> lastShowClsSet = new HashSet<Vector2Int>();

        HashSet<Vector2Int> hasInitClsSet = new HashSet<Vector2Int>();

        bool initOnce = false;

        // TODO : 要参考隔壁的 TerrainConstructor，用Task机制来控制生成（就是建一个 xxxxTask，然后用Task控制Hexmap生成的全过程

        // hexmap 格子数据，常驻
        // NOTE : 分块（HexCluster）仅负责 LOD 和显示；卸载分块时不要卸载这里的格子数据
        private MapGrid[] mapGrids;
        private int gridMapWidth;
        private int gridMapHeight;
        public bool IsGridDataInitialized => mapGrids != null;

        #region hex map init

        public void SetHexSetting(HexSettingSO hexSettingSO, Transform clusterParentObj, Material hexMat) {
            this.hexSet = hexSettingSO;
            this.clusterParentTrans = clusterParentObj;
            this.hexMat = hexMat;
        }

        private void InitHexGenerator() {
            if (hexGenerator == null) {
                hexGenerator = new HexGenerator();
            } else {
                hexGenerator.ClearHexagon();
            }
        }


        /// <summary>
        /// 初始化 hexmap 格子
        ///     - 仅生成数据，并不生成 hex 格子实体
        /// </summary>
        public void InitHexMapData(HexSettingSO settings, HexMapSO data, GridTerrainSO terrainData = null)
        {
            if (settings == null || data == null)
            {
                throw new ArgumentNullException("Hex 地图配置或数据为空。");
            }
            int mapWidth = settings.mapWidth;
            int mapHeight = settings.mapHeight;
            if (mapWidth <= 0 || mapHeight <= 0 || settings.clusterSize <= 0)
            {
                throw new InvalidOperationException("Hex 地图尺寸或分块尺寸无效。");
            }

            List<Vector2Int> generatedCoordinates = null;
            if (terrainData != null)
            {
                if (!terrainData.IsHexmapInit || !terrainData.isTerTypeInit
                    || terrainData.mapWidth != mapWidth || terrainData.mapHeight != mapHeight)
                {
                    throw new InvalidOperationException("GridTerrain 尚未初始化或与 Hex 配置尺寸不一致。");
                }
                // TODO ：GridTerrain 的 x * mapWidth + y 索引约定，别乱改！
                if (mapWidth != mapHeight)
                {
                    throw new InvalidOperationException("本次生成仅支持正方形 GridTerrain，避免改变原有索引约定。");
                }
                int expectedCount = checked(mapWidth * mapHeight);
                if (terrainData.HexmapGridTerTypeList == null || terrainData.HexmapGridTerTypeList.Count != expectedCount)
                {
                    throw new InvalidOperationException("GridTerrain 格子数量不完整。");
                }
                generatedCoordinates = new List<Vector2Int>();
                for (int x = 0; x < mapWidth; x++)
                {
                    for (int y = 0; y < mapHeight; y++)
                    {
                        var coordinate = new Vector2Int(x, y);
                        // 获取到 offset 坐标，去 terrainData 中查找基础地形类型
                        uint typeIndex = terrainData.HexmapGridTerTypeList[x * mapWidth + y][TerrainLayerIdxs.BaseLayer];
                        if (typeIndex >= terrainData.GridTerrainTypeList.Count)
                        {
                            throw new InvalidOperationException("基础地形类型索引无效：" + coordinate);
                        }
                        var type = terrainData.GridTerrainTypeList[(int)typeIndex];
                        if (type == null)
                        {
                            throw new InvalidOperationException("基础地形类型为空：" + coordinate);
                        }
                        string typeName = type.terrainTypeName;

                        // 海洋和山地不会生成 hex 格子
                        if (BaseGridTerrain.IsSea(typeName) || BaseGridTerrain.IsMountain(typeName))
                        {
                            continue;
                        }
                        generatedCoordinates.Add(coordinate);
                    }
                }
            }
            else if (!data.HasGenerated || data.MapWidth != mapWidth || data.MapHeight != mapHeight)
            {
                throw new InvalidOperationException("请先生成 HexMapSO，且确保尺寸与 Hex 配置一致。");
            }

            // 更新完毕 coordinates（offset坐标）
            IReadOnlyList<Vector2Int> coordinates = data.GridCoordinates;
            if (generatedCoordinates != null)
            {
                coordinates = generatedCoordinates;
            }

            var grids = new MapGrid[checked(mapWidth * mapHeight)];
            var gridLayout = settings.GetScreenLayout();
            foreach (var coordinate in coordinates)
            {
                if (coordinate.x < 0 || coordinate.y < 0 || coordinate.x >= mapWidth || coordinate.y >= mapHeight)
                {
                    throw new InvalidOperationException("格子坐标越界：" + coordinate);
                }
                int index = coordinate.y * mapWidth + coordinate.x;
                if (grids[index] != null)
                {
                    throw new InvalidOperationException("格子坐标重复：" + coordinate);
                }

                // 这里是初始化 hexmap 格子操作
                var localIndex = new Vector2Int(coordinate.x % settings.clusterSize, coordinate.y % settings.clusterSize);
                Vector3 position = HexHelper.OffsetToWorld(gridLayout, coordinate);
                grids[index] = new MapGrid(coordinate, localIndex, position);
            }
            if (generatedCoordinates != null)
            {
                data.ReplaceData(mapWidth, mapHeight, generatedCoordinates);
            }
            hexSet = settings;
            gridMapWidth = mapWidth;
            gridMapHeight = mapHeight;
            mapGrids = grids;

            // 刷新已存在的分块引用，避免显示持有旧实体。
            if (hexClusters != null)
            {
                foreach (var cluster in hexClusters)
                {
                    if (cluster.hasInit)
                    {
                        cluster.RefreshGridReferences(this);
                    }
                }
            }
        }

        // init 初始化格子实体
        public void InitHexConsParallelogram(int q1, int q2, int r1, int r2) {
            InitHexGenerator();
            hexGenerator.GenerateParallelogram(q1 + 1, q2, r1, r2);
            //_InitCons();
        }

        public void InitHexConsTriangle(int mapSize) {
            InitHexGenerator();
            hexGenerator.GenerateTriangle(mapSize);
            //_InitCons();
        }

        public void InitHexConsHexagon(int mapSize) {
            InitHexGenerator();
            hexGenerator.GenerateHexagon(mapSize);
            //_InitCons();
        }

        public void InitHexConsRectangle(Material mat) {
            hexMat = mat;

            // NOTE: 其他地图形状需要不一样的cluster构建，过于复杂，此处只做四边形版Hex的生成
            InitHexGenerator();
            _InitHexCons();
            hasInit = true;

            //hexGenerator.GenerateRectangle(0, hexSet.mapHeight, 0, hexSet.mapWidth);
            //_InitCons();
        }

        private void _InitHexCons() {
            layout = hexSet.GetScreenLayout();

            this.width = hexSet.mapWidth;
            this.height = hexSet.mapHeight;

            // divide map into cluster
            int clusterSize = hexSet.clusterSize;
            int cls_num_width = width / clusterSize;
            if (width % clusterSize > 0) cls_num_width++;
            int cls_num_height = height / clusterSize;
            if (height % clusterSize > 0) cls_num_height++;

            curLoadedClusterNum = 0;
            this.clusterWidth = cls_num_width;
            this.clusterHeight = cls_num_height;
            hexClusters = new TDList<HexCluster>(cls_num_width, cls_num_height);
            ClusterSize.InitClusterSizeInfo(hexSet.hexGridSize, clusterSize, hexSet.originOffset);

            //Debug.Log($"Now we init hex cluster num : {cls_num_width * cls_num_height}");
        }

        public void InitHexConsRectangle_Once(Material mat)
        {
            hexMat = mat;
            initOnce = true;

            InitHexGenerator();
            _InitHexCons();
            for (int i = 0; i < clusterWidth; i++)
            {
                for(int j = 0; j < clusterHeight; j++)
                {
                    if (!hexClusters[i, j].hasInit)
                    {
                        InitHexCluster(i, j, hexSet.clusterSize);
                    }
                }
            }

            hasInit = true;
        }

        public void UpdateHex() {
            // 根据摄像机位置 和 展示 范围
            //动态地去加载 hexcluster
            if (hasInit == false) {
                Debug.LogError("hex cons not init!");
                return;
            }

            if (hexClusters == null) {
                Debug.LogError("hex clsuter list is null!");
                return;
            }

            if (initOnce)
            {
                return;
            }

            Vector3 cameraPos = Camera.main.transform.position;
            // make sure which cls camera in, and if not a valid cluster, skip it
            Vector2Int clsIdx = ClusterSize.GetClusterIdxByPos(cameraPos);

            //Debug.Log($"hex update  clsIdx : {clsIdx}, lastIdx : {lastIdx}");

            if (clsIdx.x < 0 || clsIdx.x > clusterWidth - 1 || clsIdx.y < 0 || clsIdx.y > clusterHeight - 1) {
                return;
            }

            if(clsIdx == lastIdx) {
                return;
            }
            lastIdx = clsIdx;

            HashSet<Vector2Int> shouldShowClsSet = new HashSet<Vector2Int>();

            int hexScope = hexSet.hexAOIScope / 2;
            for (int i = -hexScope + clsIdx.x; i <= hexScope + clsIdx.x; i++) {
                for (int j = -hexScope + clsIdx.y; j <= hexScope + clsIdx.y; j++) {
                    // not valid hex cluster index
                    if (i < 0 || i > clusterWidth - 1 || j < 0 || j > clusterHeight - 1) {
                        continue;
                    }

                    shouldShowClsSet.Add(new Vector2Int(i, j));
                    if (!hexClusters[i, j].hasInit) {
                        // if this cluster is not loaded, loaded it
                        InitHexCluster(i, j, hexSet.clusterSize);
                    }
                }
            }

            foreach (var cluster in lastShowClsSet) {
                hexClusters[cluster.x, cluster.y].HideMapCluster();
            }

            foreach (var cluster in shouldShowClsSet) {
                hexClusters[cluster.x, cluster.y].ShowMapCluster();
            }

            lastShowClsSet = shouldShowClsSet;

            // if cluster is too much, we will unload some of them
            int beforeUnloadClsNum = curLoadedClusterNum;
            UnloadClusterObj(clsIdx);
            int afterUnloadClsNum = curLoadedClusterNum;

            Debug.Log($"new idx : {lastIdx}, show cluster num {shouldShowClsSet.Count}, unload cluster num {beforeUnloadClsNum - afterUnloadClsNum}, has init num {hasInitClsSet.Count}");
        }

        private void InitHexCluster(int i, int j, int clusterSize) {

            GameObject clusterObj = new GameObject();
            clusterObj.name = string.Format("HexmapCluster_{0}_{1}", i, j);
            clusterObj.transform.parent = clusterParentTrans.transform;

            MeshFilter meshFilter = clusterObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = clusterObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = hexMat;

            hexClusters[i, j].InitMapCluster(i, j, meshFilter, clusterSize);

            Vector2Int startMapGridIdx = GetStartMapGridByClusterIdx(i, j);

            // The index(num) of map grid
            int left = i * clusterSize;
            int right = (i + 1) * clusterSize;
            int down = j * clusterSize;
            int up = (j + 1) * clusterSize;
            Dictionary<Vector2Int, Hexagon> hexDatas = HexGenerator.GenerateRectangleHexData(down, up, left, right);
            
            foreach (var hex in hexDatas) {
                Vector2Int inClusterIdx = hex.Key;
                Vector2Int mapIdx = startMapGridIdx + inClusterIdx;

                MapGrid residentGrid = null;
                if (IsGridDataInitialized)
                    residentGrid = GetHexMapGrid(mapIdx);
                hexClusters[i, j].AddMapGrid(mapIdx, inClusterIdx, hex.Value, layout, residentGrid);
            }

            // Build this cluster
            hexClusters[i, j].SetClusterMesh();

            hasInitClsSet.Add(new Vector2Int(i, j));
            curLoadedClusterNum++;

            //Debug.Log($"cluster {i}, {j} has inited, and create map grids");
        }

        private struct ClusterDistanceStruct : IComparable<ClusterDistanceStruct> {
            // it is used to measure the distance of this cluster to camera
            public Vector2Int clsIdx {  get; private set; }
            public float distance {  get; private set; }

            public ClusterDistanceStruct(Vector2Int clsIdx, Vector2Int curClsIdx) {
                this.clsIdx = clsIdx;
                this.distance = Vector2Int.Distance(clsIdx, curClsIdx);
            }

            public int CompareTo(ClusterDistanceStruct other) {
                return distance.CompareTo(other.distance);
            }
        }

        private void UnloadClusterObj(Vector2Int curClsIdx) {
            if (curLoadedClusterNum <= hexSet.mapHexClusterNumLimit) {
                return;
            }

            // at last, it should more than the scope
            int itCnt = 0, maxIterCnt = 20;
            int limit = Mathf.Max(hexSet.hexAOIScope * hexSet.hexAOIScope, hexSet.mapHexClusterNumLimit);

            SimpleHeapStruct<ClusterDistanceStruct> heap = new SimpleHeapStruct<ClusterDistanceStruct>(false);
            foreach (var clusterIdx in hasInitClsSet)
            {
                heap.Push(new ClusterDistanceStruct(clusterIdx, curClsIdx));
            }

            while (curLoadedClusterNum > limit && itCnt < maxIterCnt && !heap.Empty()) {
                // find the farest and dispose it
                ClusterDistanceStruct clsStruct = heap.Pop();
                Vector2Int clsIdx = clsStruct.clsIdx;

                hexClusters[clsIdx.x, clsIdx.y].Dispose();
                hasInitClsSet.Remove(clsIdx);

                curLoadedClusterNum--;
                itCnt++;
            }

        }

        public void ClearClusterObj() {
            width = 0;
            height = 0;
            clusterWidth = 0;
            clusterHeight = 0;

            if (hexClusters != null) {
                foreach (var cluster in hexClusters) {
                    cluster.Dispose();
                }
            }
            hexClusters = null;
            clusterParentTrans.ClearObjChildren();
        }


        [Obsolete]
        private void _InitCons() {
            layout = hexSet.GetScreenLayout();
            StartCoroutine(LoadMapGrid(hexSet.mapWidth, hexSet.mapHeight, 100000));
        }

        [Obsolete]
        IEnumerator LoadMapGrid(int width, int height, int maxLoadGridNum) {
            this.width = width;
            this.height = height;

            // divide map into cluster
            int clusterSize = hexSet.clusterSize;
            int cls_num_width = width / clusterSize;
            if (width % clusterSize > 0) cls_num_width++;
            int cls_num_height = height / clusterSize;
            if (height % clusterSize > 0) cls_num_height++;

            hexClusters = new TDList<HexCluster>(cls_num_width, cls_num_height);
            Debug.Log($"num : {hexGenerator.HexagonIdxDic.Count}");
            // create and init map cluster
            int count = 0;
            foreach (var pair in hexGenerator.HexagonIdxDic) {
                // NOTE : HexagonIdxDic中的 key 是 offset hex，value 是 axial hex
                // use them discreetly
                Vector2Int mapIdx = pair.Key;

                // caculate map grid'index inside cluster
                Vector2Int clusterIdx = GetGridClusterIdx(mapIdx.x, mapIdx.y);
                Vector2Int inClusterIdx = GetGridInClusterIdx(mapIdx.x, mapIdx.y);

                count++;
                if (count > maxLoadGridNum) {
                    count = 0;
                    Debug.Log(string.Format("load {0} grids in one frame ", maxLoadGridNum));
                    yield return null;
                }

                if (!hexClusters[clusterIdx.x, clusterIdx.y].hasInit) {
                    InitHexCluster(clusterIdx.x, clusterIdx.y, clusterSize);
                }

                hexClusters[clusterIdx.x, clusterIdx.y].AddMapGrid(mapIdx, inClusterIdx, pair.Value, layout);
            }

            for (int i = 0; i < cls_num_width; i++) {
                for (int j = 0; j < cls_num_height; j++) {
                    //Debug.Log($"{i}, {j}");
                    hexClusters[i, j].SetClusterMesh();
                }
            }

            // build the quad tree
            //BuildQuadTreeMap(cls_num_width, cls_num_height);

            yield return null;
        }

        [Obsolete]
        public void BuildQuadTreeMap(int cls_num_width = 0, int cls_num_height = 0) {
            if (cls_num_width == 0) {
                cls_num_width = hexClusters.GetLength(0);
            }
            if (cls_num_height == 0) {
                cls_num_height = hexClusters.GetLength(1);
            }

            List<MapGrid> totalGrids = new List<MapGrid>();
            List<Vector3> poss = new List<Vector3>();
            for (int i = 0; i < cls_num_width; i++) {
                for (int j = 0; j < cls_num_height; j++) {
                    foreach (var mapGrid in hexClusters[i, j]) {
                        totalGrids.Add(mapGrid);
                        poss.Add(mapGrid.Position);
                    }

                }
            }
            //Debug.Log("the last cluster pos is: " + poss.GetLastVal());
            if (mapGridQuadTree != null) {
                //GizmosCtrl.GetInstance().UnregisterGizmoEvent(mapGridQuadTree.DrawScopeInGizmos);
            }
            // caculate map size
            Vector3 leftDown = GetMapLeftDown();
            Vector3 rightUp = GetMapRightUp();
            mapGridQuadTree = new QuadTree<MapGrid>();
            mapGridQuadTree.BuildTree(
                leftDown, rightUp, 3,
                totalGrids, poss
            );
            //GizmosCtrl.GetInstance().RegisterGizmoEvent(mapGridQuadTree.DrawScopeInGizmos);
        }

        #endregion


        #region gameplay 相关

        // If Country info changes, call it
        public void InitCountry(CountrySO countrySO)
        {
            countryManager.InitCountryManager(countrySO);
        }

        /// <summary>
        /// 按 offset 坐标取得常驻格子
        /// </summary>
        public MapGrid GetHexMapGridByWorldPosition(Vector3 worldPosition)
        {
            if (!IsGridDataInitialized)
            {
                return null;
            }

            Vector2 worldXZ = new Vector2(worldPosition.x, worldPosition.z);
            Hexagon hexagon = HexHelper.PixelToAxialHex(worldXZ, hexSet.hexGridSize);
            Vector2Int coordinate = HexHelper.AxialToOffset(hexagon);
            return GetHexMapGrid(coordinate);
        }

        public MapGrid GetHexMapGrid(Vector2Int coordinate)
        {
            if (!IsGridDataInitialized)
            {
                throw new InvalidOperationException("Hex 格子数据尚未初始化。");
            }
            if (coordinate.x < 0 || coordinate.y < 0 || coordinate.x >= gridMapWidth || coordinate.y >= gridMapHeight)
            {
                return null;
            }
            int index = coordinate.y * gridMapWidth + coordinate.x;
            return mapGrids[index];
        }

        #endregion

        // TODO : 还没做完
        #region country, gridtype functions

        public void UpdateCountryColor()
        {
            countryManager.UpdateCountryColor();
        }

        public CountryBoundData GetBoundDataByIndex(int layer, int indexInLayer)
        {
            return countryManager.GetBoundDataByIndex(layer, indexInLayer);
        }

        public Vector2Int GetBoundCenterByIndex(int layer, int indexInLayer)
        {
            return countryManager.GetBoundCenterByIndex(layer, indexInLayer);
        }

        // Country rendering
        public void UpdateCountryTex()
        {
            // TODO : 对一个地块生成 SDF 纹理，转为 Vector2Int[]，传入到 地块的 Mesh 数据里
        }

        #endregion


        #region generate hex message by height info

        public void GenerateRawHexMap(Vector2Int startLongitudeLatitude, HexMapSO rawHexMapSO, HeightDataManager heightDataManager) {
            layout = hexSet.GetScreenLayout();
            if (hexGenerator == null || hexGenerator.HexagonIdxDic == null) {
                Debug.LogError("do not set hexGenerator!");
                return;
            }

            foreach (var pair in hexGenerator.HexagonIdxDic) {
                Vector2Int mapIdx = pair.Key;
                // trans hex center position to terrain position
                Hexagon hex = pair.Value;
                Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();

                // get height datas, then use them to generate hex grid...
                //TDList<float> heights = heightDataManager.SampleScopeFromHeightData(startLongitudeLatitude, center, hexSet.hexCalcuVertScope);
                //rawHexMapSO.AddGridTerrainData(mapIdx, hex, center);
            }
            //rawHexMapSO.UpdateGridTerrainData();

            Debug.Log($"generate over, RawHexMapSO can be use, grid : {hexGenerator.HexagonIdxDic.Count}");
        }

        public void BuildByRawHexMap() {
            // TODO : 
        }

        #endregion


        private Vector3 GetMapRightUp() {
            float x = width * Mathf.Sqrt(3);
            float y = (height - 1) * 1.5f + 1;
            return new Vector3(x, 0, y) + hexSet.originOffset;
        }

        private Vector3 GetMapLeftDown() {
            return new Vector3(-Mathf.Sqrt(3) / 2, 0, -1) + hexSet.originOffset;
        }


        #region method to help you get MapGrid / MapCluster info

        // input a scene position, return the closest grid
        public MapGrid GetClosestMapGrid(Vector2 pos) {
            //if (mapQuadTree == null) {
            //    return null;
            //}
            //MapCluster cluster = null;
            //mapQuadTree.SearchObjByPos(pos, ref cluster);
            //if (cluster != null) {
            //    //Debug.Log("find cluster!");
            //    return cluster.GetClosestMapGrid(pos);
            //}
            return null;
        }

        // get map grid by map index / grid position
        public MapGrid GetMapGrid(int mapIdxX, int mapIdxY) {
            if (IsGridDataInitialized)
                return GetHexMapGrid(new Vector2Int(mapIdxX, mapIdxY));
            if (hexClusters == null)
                return null;
            // 兼容尚未生成 HexMapSO 的旧编辑器显示链路。

            Vector2Int clusterIdx = GetGridClusterIdx(mapIdxX, mapIdxY);
            Vector2Int inClusterIdx = GetGridInClusterIdx(mapIdxX, mapIdxY);

            // exe outside bounds
            if (clusterIdx.x < 0 || clusterIdx.x >= hexClusters.GetLength(0)
                || clusterIdx.y < 0 || clusterIdx.y >= hexClusters.GetLength(1)) {
                return null;
            }

            HexCluster hexCluster = hexClusters[clusterIdx.x, clusterIdx.y];
            if (hexCluster != null) {
                return hexCluster.GetMapGrid(inClusterIdx);
            }
            Debug.Log(string.Format("Can not find map grid : {0}, {1}", mapIdxX, mapIdxY));
            return null;
        }

        public List<MapGrid> GetMapGrid_HexScope(int mapIdxX, int mapIdxY, int scope) {
            MapGrid centerGrid = GetMapGrid(mapIdxX, mapIdxY);
            if (centerGrid == null) {
                return null;
            }

            
            HashSet<Vector2Int> gridRec = new HashSet<Vector2Int>() { centerGrid.mapIdx };
            List<MapGrid> resGrids = new List<MapGrid>() { centerGrid };
            Queue<MapGrid> curGrids = new Queue<MapGrid>();
            curGrids.Enqueue(centerGrid);

            int count = 1;
            while (curGrids.Count > 0 && scope > 0) {

                int newCount = 0;
                while(count > 0) {
                    MapGrid cur = curGrids.Dequeue();

                    for (int i = 0; i < 6; i++) {
                        Vector2Int neighborIdx = cur.GetNeighborIdx(i);
                        if (!gridRec.Contains(neighborIdx)) {
                            MapGrid neighbor = GetMapGrid(neighborIdx.x, neighborIdx.y);
                            if (neighbor != null) {
                                gridRec.Add(neighborIdx);
                                resGrids.Add(neighbor);
                                curGrids.Enqueue(neighbor);
                                // record this layer's grid num
                                newCount++;
                            }
                        }
                    }
                    count--;
                }
                count = newCount;
                scope--;
            }

            //Debug.Log("result grids num is: " + curGrids.Count + ", " + gridRec.Count);
            return resGrids;
        }

        public void GetMapGridNeighbor(int mapIdxX, int mapIdxY) {
            MapGrid mapGrid = GetMapGrid(mapIdxX, mapIdxY);
            if (mapGrid != null) {

                for (int i = 0; i < 6; i++) {
                    Vector2Int neighborIdx = mapGrid.GetNeighborIdx(i);
                    MapGrid neighbor = GetMapGrid(neighborIdx.x, neighborIdx.y);
                    if (neighbor != null) {
                        CreateSignObj(neighbor);
                    }
                }
            }
        }

        private Vector2Int GetGridClusterIdx(int mapIdxX, int mapIdxY) {
            int clusterSize = hexSet.clusterSize;
            Vector2Int clusterIdx = new Vector2Int();
            clusterIdx.x = mapIdxX / clusterSize;
            clusterIdx.y = mapIdxY / clusterSize;
            return clusterIdx;
        }

        private Vector2Int GetGridInClusterIdx(int mapIdxX, int mapIdxY) {
            int clusterSize = hexSet.clusterSize;

            // caculate map grid'index inside cluster
            Vector2Int inClusterIdx = new Vector2Int();
            inClusterIdx.x = mapIdxX % clusterSize;
            inClusterIdx.y = mapIdxY % clusterSize;
            return inClusterIdx;
        }

        private Vector2Int GetStartMapGridByClusterIdx(int clsIdxX, int clsIdxY) {
            int clusterSize = hexSet.clusterSize;
            Vector2Int startMapIdx = new Vector2Int();
            startMapIdx.x = clsIdxX * clusterSize;
            startMapIdx.y = clsIdxY * clusterSize;
            return startMapIdx;
        }

        #endregion


        // TODO : 需要重写
        public void SetMapGridColor(List<MapGrid> grids, Color color32) {
            if (grids == null || grids.Count == 0) {
                return;
            }

            Dictionary<Vector2Int, List<Vector2Int>> clusterGridsDict = new Dictionary<Vector2Int, List<Vector2Int>>();
            foreach (var grid in grids) {
                if (grid == null)
                    continue;
                grid.SetGridColor(color32);
                if (hexClusters == null)
                    continue;
                Vector2Int clusterIdx = GetGridClusterIdx(grid.mapIdx.x, grid.mapIdx.y);
                Vector2Int inClusterIdx = GetGridInClusterIdx(grid.mapIdx.x, grid.mapIdx.y);

                if (hexClusters[clusterIdx.x, clusterIdx.y] == null) {
                    continue;
                }

                if (!clusterGridsDict.ContainsKey(clusterIdx)) {
                    clusterGridsDict.Add(clusterIdx, new List<Vector2Int>() { inClusterIdx });
                } else {
                    clusterGridsDict[clusterIdx].Add(inClusterIdx);
                }
            }

            foreach (var pair in clusterGridsDict) {
                if (hexClusters[pair.Key.x, pair.Key.y] == null) {
                    continue;
                }

                HexCluster cluster = hexClusters[pair.Key.x, pair.Key.y];
                cluster.SetGridColor(pair.Value, color32, layout);
            }

        }

        public void GetMapGridTest(int mapIdxX, int mapIdxY) {
            MapGrid grid = GetMapGrid(mapIdxX, mapIdxY);
            CreateSignObj(grid);
        }

        private GameObject CreateSignObj(MapGrid grid) {
            GameObject go = Instantiate(SignPrefab, signParentTrans.transform);
            //go.transform.position = grid.GetHexPos(layout);
            go.transform.position = grid.Position;
            return go;
        }

    }


    // data about a hex cluster's width height
    // this class can help to get hex Cluster's Info
    public static class ClusterSize {

        public static float Width { get; private set; }
        public static float Height { get; private set; }

        public static Vector3 OriginOffset { get; private set; }

        public static void InitClusterSizeInfo(int hexSize, int clusterSize, Vector3 originOffset) {
            Width = Mathf.Sqrt(3) * hexSize * clusterSize;
            if (clusterSize % 2 == 0) {
                Height = hexSize * 3 * clusterSize / 2;
            } else {
                // cluster size can not be odd
                Height = hexSize * 3 * clusterSize / 2 + hexSize;
            }
            OriginOffset = originOffset;
        }

        public static Vector2Int GetClusterIdxByPos(Vector3 pos) {
            int i = (int)((pos.x - OriginOffset.x) / ClusterSize.Width);
            int j = (int)((pos.z - OriginOffset.z) / ClusterSize.Height);
            return new Vector2Int(i, j);
        }
    }

    public class HexCluster : IEnumerable , IDisposable{

        public int clusterIdxX {  get; private set; }
        public int clusterIdxY {  get; private set; }


        public int clusterSize { get; private set; }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();
        List<Vector2> uvs = new List<Vector2>();

        TDList<MapGrid> mapGridList;
        public Dictionary<Vector2, MapGrid> mapPosGridDict {  get; private set; }

        private Mesh hexMesh;
        private MeshFilter meshFilter;
        private Layout layout;

        public bool hasInit { get; private set; }
        public bool hasShow { get; private set; }

        public IEnumerator<MapGrid> GetEnumerator() {
            return mapGridList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public HexCluster() { 
            hasInit = false;
            hasShow = true;
        }

        public void InitMapCluster(int clusterIdxX, int clusterIdxY, MeshFilter meshFilter, int clusterSize) {
            this.clusterIdxX = clusterIdxX;
            this.clusterIdxY = clusterIdxY;

            this.meshFilter = meshFilter;
            this.clusterSize = clusterSize;

            hexMesh = new Mesh();
            if (meshFilter != null) {
                meshFilter.mesh = hexMesh;
            }
            hexMesh.name = "cluster_mesh";
            hexMesh.Clear();

            mapGridList = new TDList<MapGrid>(clusterSize, clusterSize);
            mapPosGridDict = new Dictionary<Vector2, MapGrid>();

            // create all map grid
            // TODO : create them!

            hasInit = true;
            hasShow = true;
        }

        internal void AddMapGrid(Vector2Int mapIdx, Vector2Int inClusterIdx, Hexagon hex, Layout layout, MapGrid residentGrid = null) {
            this.layout = layout;

            Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();
            Vector3 hexPos = new Vector3((float)center.x, 0, (float)center.z);
            
            // construct the map grid, and add it
            // 常驻实体复用引用。海洋、山脉和旧显示模式的临时格子仅用于 Mesh，不进入数据查询链路。
            MapGrid mapGrid = residentGrid;
            if (mapGrid == null)
                mapGrid = new MapGrid(mapIdx, inClusterIdx, hexPos);
            mapGridList[inClusterIdx.x, inClusterIdx.y] = mapGrid;
            if (mapPosGridDict.ContainsKey(hexPos)) {
                mapPosGridDict[hexPos] = mapGrid;
            } else {
                mapPosGridDict.Add(hexPos, mapGrid);
            }

            BuildGridMesh(hex, layout, mapGrid.GridColor);
        }

        internal void RefreshGridReferences(HexmapConstructor constructor) {
            for (int x = 0; x < clusterSize; x++) {
                for (int y = 0; y < clusterSize; y++) {
                    var grid = mapGridList[x, y];
                    if (grid == null || !grid.IsValidGrid)
                        continue;
                    var resident = constructor.GetHexMapGrid(grid.mapIdx);
                    if (resident == null)
                        resident = new MapGrid(grid.mapIdx, grid.clusterIdx, grid.Position);
                    mapGridList[x, y] = resident;
                    mapPosGridDict[resident.Position] = resident;
                }
            }
            RebuildClusterMesh(layout);
        }

        private void RebuildClusterMesh(Layout layout) {
            ClearClusterMesh();
            foreach (var grid in mapGridList) {
                if (!grid.IsValidGrid) {
                    continue;
                }
                BuildGridMesh(grid.hexagon, layout, grid.GridColor);
            }
            SetClusterMesh();
        }

        private void BuildGridMesh(Hexagon hex, Layout layout, Color32 color) {
            Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();
            List<Point> vertexs = hex.Polygon_Corners(layout);

            float radius = Vector3.Distance(center, (Vector3)vertexs[0]);

            float innerRatio = 0.9f;

            List<Point> innerVertexs = new List<Point>();
            List<Point> outterVertexs = new List<Point>();

            for (int i = 0; i < 6; i++) {
                // Do not delete
                //if (i == 0 || i == 1 || i == 4) {
                //    Hexagon neighbor = hex.Hex_Neighbor(  (HexDirection)i );
                //    Point neighbor_center = neighbor.Hex_To_Pixel(layout).ConvertToXZ();
                //    GameObject go = GameObject.Instantiate(HexmapConstructor.Instance.SignPrefab, HexmapConstructor.Instance.clusterParentTrans);
                //    go.transform.position = new Vector3((float)neighbor_center.x, 0, (float)neighbor_center.z);
                //}

                Point offset = hex.Hex_Corner_Offset(layout, i);
                //Point innerVertex = center + new Point(offset.x, 0, offset.y) * innerRatio;
                Point outterVertex = center + new Point(offset.x, 0, offset.y);

                // Do not delete
                //Point _center = hex.Hex_To_Pixel(layout).ConvertToXZ();
                //Point curOffset = hex.Hex_Corner_Offset(layout, i);
                //Point curVertex = center + new Point(curOffset.x, 0, curOffset.y);
                //Vector2 cornerVec = new Vector2((float)curVertex.x, (float)curVertex.z);

                //Point innerVertex = new Point(cornerVec.x, 0, cornerVec.y) * innerRatio;
                //Point outterVertex = new Point(cornerVec.x, 0, cornerVec.y);

                //innerVertexs.Add(innerVertex);
                outterVertexs.Add(outterVertex);
            }

            for (int i = 0; i < 6; i++) {
                int j = i + 1 >= 6 ? 0 : i + 1;

                // Do not delete
                //AddTriangle(center, innerVertexs[i], innerVertexs[j]);
                AddTriangle(center, outterVertexs[i], outterVertexs[j]);
                //AddTriangleColor(color, color, color);
                //AddTriangleColor(color, color, color);
            }
        }

        internal void ClearClusterMesh() {
            hexMesh.Clear();
            vertices.Clear();
            triangles.Clear();
            colors.Clear();
            uvs.Clear();
            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            hexMesh.colors = colors.ToArray();
            hexMesh.RecalculateNormals();
        }

        internal void SetClusterMesh() {
            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            //hexMesh.colors = colors.ToArray();
            hexMesh.uv = uvs.ToArray();
            hexMesh.RecalculateNormals();
        }

        public void HideMapCluster() {
            if (!hasInit) {
                return;
            }
            if (!hasShow) {
                return;
            }
            meshFilter.sharedMesh = null;
            hasShow = false;
        }

        public void ShowMapCluster() {
            if (!hasInit) {
                return;
            }
            if (hasShow) {
                return;
            }
            meshFilter.sharedMesh = hexMesh;
            hasShow = false;
        }

        public void Dispose() {
            // 仅释放显示资源，分块中引用的常驻格子及外部状态不受影响。
            if(hexMesh != null) {
                GameObject.DestroyImmediate(hexMesh);
                hexMesh = null;
            }
            hasShow = false;
            hasInit = false;
        }


        #region draw mesh base method

        // add triangle to mesh
        private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
            int vertexIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            // 方向很怪，反正是右手定则的反方向
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);

            uvs.Add(new Vector2(v1.x / layout.Width, v1.z / layout.Height));
            uvs.Add(new Vector2(v2.x / layout.Width, v2.z / layout.Height));
            uvs.Add(new Vector2(v3.x / layout.Width, v3.z / layout.Height));
        }

        private void AddTriangleColor(Color c1, Color c2, Color c3) {
            // 要在 AddTriangle 之后立刻调用
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);

            //Vector3 types;
            //types.x = types.y = types.z = 5;
            //AddTriangleTerrainTypes(types);
        }

        // add quad to mesh
        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
            int vertexIndex = vertices.Count;
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);
            // 要产生正确的法向量 则加入顺序为: v1 v3 v2, v1 v4 v3
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex + 2);
        }

        private void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
            // 要在 AddQuad 之后立刻调用
            colors.Add(c1);
            colors.Add(c2);
            colors.Add(c3);
            colors.Add(c4);

            //Vector3 types;
            //types.x = types.y = types.z = 5;
            //AddQuadTerrainTypes(types);
        }

        #endregion


        internal void SetGridColor(List<Vector2Int> clusterIdxs, Color color32, Layout layout) {
            if (clusterIdxs.Count == 0) {
                return;
            }

            foreach (var clusterIdx in clusterIdxs) {
                MapGrid grid = GetMapGrid(clusterIdx);
                if (grid != null) {
                    grid.SetGridColor(color32);
                }
            }

            // set color will trigger ClusterRebuild
            RebuildClusterMesh(layout);
        }

        public MapGrid GetMapGrid(Vector2Int clusterIdx) {
            if (clusterIdx.x < 0 || clusterIdx.x >= mapGridList.GetLength(0)
                || clusterIdx.y < 0 || clusterIdx.y >= mapGridList.GetLength(1)) {
                return null;
            }
            MapGrid grid = mapGridList[clusterIdx.x, clusterIdx.y];
            if (grid == null || !grid.IsValidGrid) {
                return null;
            }
            return grid;
        }

    }

    public class MapGrid {

        public bool IsValidGrid { get; private set; }

        public Vector2Int mapIdx { get; private set; }
        public Vector2Int clusterIdx { get; private set; }
        public Hexagon hexagon => HexHelper.OffsetToAxial(mapIdx);


        public Vector3 Position { get; private set; }

        public Color32 GridColor { get; private set; }


        // 外部状态通常约三个，按职责拆分，不设置硬上限；未挂载时不分配容器。
        private List<MapGridStatusInterface> statuses;

        /// <summary>挂载运行时状态；同一具体类型替换原引用，不接受 null。</summary>
        public void SetStatus<T>(T status) where T : class, MapGridStatusInterface {
            if (status == null)
                throw new ArgumentNullException(nameof(status));
            if (statuses == null)
                statuses = new List<MapGridStatusInterface>(3);
            Type statusType = status.GetType();
            for (int i = 0; i < statuses.Count; i++) {
                var current = statuses[i];
                if (current.GetType() != statusType)
                    continue;
                statuses[i] = status;
                return;
            }
            statuses.Add(status);
        }

        /// <summary>按精确具体类型取得状态；未挂载返回 null，不匹配公共基类或接口。</summary>
        public T GetStatus<T>() where T : class, MapGridStatusInterface {
            if (statuses == null)
                return null;
            foreach (var status in statuses) {
                if (status.GetType() == typeof(T))
                    return (T)status;
            }
            return null;
        }

        /// <summary>移除指定具体类型的引用；不负责释放外部对象。</summary>
        public bool RemoveStatus<T>() where T : class, MapGridStatusInterface {
            if (statuses == null)
                return false;
            for (int i = 0; i < statuses.Count; i++) {
                if (statuses[i].GetType() != typeof(T))
                    continue;
                statuses.RemoveAt(i);
                return true;
            }
            return false;
        }

        public MapGrid() {
            IsValidGrid = false;
        }

        internal MapGrid(Vector2Int mapIdx, Vector2Int clsIdx, Vector3 position) {
            this.mapIdx = mapIdx;
            this.clusterIdx = clsIdx;
            Position = position;

            SetGridColor(MapEnum.DefaultGridColor);

            IsValidGrid = true;
        }


        /// <summary>按方向名称即时计算邻居 offset 坐标，不保证该坐标存在有效实体。</summary>
        public Vector2Int GetNeighborIdx(HexDirection direction) {
            switch (direction) {
                case HexDirection.W: return GetNeighborIdx(0);
                case HexDirection.NW: return GetNeighborIdx(1);
                case HexDirection.NE: return GetNeighborIdx(2);
                case HexDirection.E: return GetNeighborIdx(3);
                case HexDirection.SE: return GetNeighborIdx(4);
                case HexDirection.SW: return GetNeighborIdx(5);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        /// <summary>保持旧整数顺序：W、NW、NE、E、SE、SW；即时计算，不分配数组。</summary>
        public Vector2Int GetNeighborIdx(int direction) {
            int x = mapIdx.x;
            int y = mapIdx.y;
            bool oddRow = (y & 1) != 0;
            switch (direction) {
                case 0: return new Vector2Int(x - 1, y);
                case 1:
                    if (oddRow) return new Vector2Int(x, y + 1);
                    return new Vector2Int(x - 1, y + 1);
                case 2:
                    if (oddRow) return new Vector2Int(x + 1, y + 1);
                    return new Vector2Int(x, y + 1);
                case 3: return new Vector2Int(x + 1, y);
                case 4:
                    if (oddRow) return new Vector2Int(x + 1, y - 1);
                    return new Vector2Int(x, y - 1);
                case 5:
                    if (oddRow) return new Vector2Int(x, y - 1);
                    return new Vector2Int(x - 1, y - 1);
                default: throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        internal void SetGridColor(Color32 color) {
            GridColor = color;
        }

    }

}
