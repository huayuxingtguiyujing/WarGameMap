
using LZ.WarGameMap.Runtime.HexStruct;
using LZ.WarGameMap.Runtime.QuadTree;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LZ.WarGameCommon;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    /// <summary>
    /// 封装 六边形网格地图 的生成过程，地图管理不在此处，此类不会工作在运行时
    /// - 负责把 hexagon 数据转为 monobehaviour 的地图块（MapCluster）
    /// - 负责 地图效果的制作和调整
    /// </summary>
    public class HexmapConstructor : MonoBehaviour
    {
        [Header("hex map config")]
        //[SerializeField] Transform originPoint;
        [SerializeField] Vector2 Size = new Vector2(1, 1);
        [SerializeField] GameObject SignPrefab;

        Transform clusterParentTrans;
        Transform signParentTrans;

        HexGenerator hexGenerator;
        Layout layout;

        int width;
        int height;

        [Header("hex map object")]
        [SerializeField] TDList<HexCluster> hexClusters;

        QuadTree<MapGrid> mapGridQuadTree;

        #region hex map init

        public void SetMapPrefab(Transform clusterParentObj, Transform signParentObj) {
            this.clusterParentTrans = clusterParentObj;
            this.signParentTrans = signParentObj;
        }


        private void InitHexGenerator() {
            if (hexGenerator == null) {
                hexGenerator = new HexGenerator();
            } else {
                hexGenerator.ClearHexagon();
            }
        }

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

        public void InitHexConsRectangle(int bottom, int top, int left, int right) {
            InitHexGenerator();
            hexGenerator.GenerateRectangle(bottom, top, left, right);
            // NOTE: 其他地图形状需要不一样的cluster构建，过于复杂，此处只写四边形的生成
            _InitCons(right - left + 1, top - bottom + 1);
        }

        private void _InitCons(int width, int height) {

            layout = GetScreenLayout();
            StartCoroutine(LoadMapGrid(width, height, 100000));

            // TODO: init river etc
            // TODO: add noise disturb
            // TODO: add more map detail (should do it in MapController)
        }

        IEnumerator LoadMapGrid(int width, int height, int maxLoadGridNum) {
            this.width = width;
            this.height = height;

            // divide map into cluster
            int clusterSize = MapEnum.ClusterSize;
            int cls_num_width = width / clusterSize;
            if (width % clusterSize > 0) cls_num_width++;
            int cls_num_height = height / clusterSize;
            if (height % clusterSize > 0) cls_num_height++;

            hexClusters = new TDList<HexCluster>(cls_num_width, cls_num_height);
            
            // create and init map cluster
            int count = 0;
            foreach (var pair in hexGenerator.HexagonIdxDic) {
                Vector2Int mapIdx = pair.Key;

                // caculate map grid'index inside cluster
                Vector2Int clusterIdx = GetGridClusterIdx(mapIdx.x, mapIdx.y);
                Vector2Int inClusterIdx = GetGridInClusterIdx(mapIdx.x, mapIdx.y);

                count++;
                if(count > maxLoadGridNum) {
                    count = 0;
                    Debug.Log(string.Format("load {0} grids in one frame ", maxLoadGridNum));
                    yield return null;
                }

                if (!hexClusters[clusterIdx.x, clusterIdx.y].hasInit) {
                    CreateHexCluster(clusterIdx.x, clusterIdx.y, clusterSize);
                }

                hexClusters[clusterIdx.x, clusterIdx.y].AddMapGrid(mapIdx, inClusterIdx, pair.Value, layout);
            }

            for (int i = 0; i < cls_num_width; i++) {
                for (int j = 0; j < cls_num_height; j++) {
                    hexClusters[i,j].SetClusterMesh();
                }
            }

            // build the quad tree
            BuildQuadTreeMap(cls_num_width, cls_num_height);

            yield return null;
        }

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
                    foreach (var mapGrid in hexClusters[i, j])
                    {
                        totalGrids.Add(mapGrid);
                        poss.Add(mapGrid.Position);
                    }

                }
            }
            //Debug.Log("the last cluster pos is: " + poss.GetLastVal());
            if (mapGridQuadTree != null) {
                GizmosCtrl.GetInstance().UnregisterGizmoEvent(mapGridQuadTree.DrawScopeInGizmos);
            }
            // caculate map size
            Vector3 leftDown = GetMapLeftDown();
            Vector3 rightUp = GetMapRightUp();
            mapGridQuadTree = new QuadTree<MapGrid>();
            mapGridQuadTree.BuildTree(
                leftDown, rightUp, 3,
                totalGrids, poss
            );
            GizmosCtrl.GetInstance().RegisterGizmoEvent(mapGridQuadTree.DrawScopeInGizmos);
        }

        /*public void ShowQuadTreeGizmos(bool status) {
            if (mapQuadTree == null) {
                return;
            }
            Debug.Log(111);
            if (status) {
                GizmosCtrl.GetInstance().UnregisterGizmoEvent(mapQuadTree.DrawScopeInGizmos);
            } else {
                GizmosCtrl.GetInstance().RegisterGizmoEvent(mapQuadTree.DrawScopeInGizmos);
            }
        }*/

        public Layout GetScreenLayout() {
            //Vector2 startPoint = new Vector2(transform.position.x, transform.position.y);
            //Layout layout = new Layout(
            //    Orientation.Layout_Pointy, new Point(Size.x, Size.y), new Point(startPoint.x, startPoint.y)
            //);
            //return layout;
            return GetScreenLayout(Size);
        }

        public Layout GetScreenLayout(Vector2 size) {
            Vector2 startPoint = new Vector2(transform.position.x, transform.position.y);
            Layout layout = new Layout(
                Orientation.Layout_Pointy, new Point(size.x, size.y), new Point(startPoint.x, startPoint.y)
            );
            return layout;
        }

        private void CreateHexCluster(int i, int j, int clusterSize) {

            GameObject clusterObj = new GameObject();
            clusterObj.name = string.Format("HexmapCluster_{0}_{1}", i, j);
            clusterObj.transform.parent = clusterParentTrans.transform;
            
            MeshFilter meshFilter = clusterObj.AddComponent<MeshFilter>();
            clusterObj.AddComponent<MeshRenderer>();

            HexCluster hexCluster = new HexCluster();
            hexClusters[i, j] = hexCluster;
            hexCluster.InitMapCluster(meshFilter, clusterSize);
        }

        #endregion


        private Vector3 GetMapRightUp() {
            float x = width * Mathf.Sqrt(3);
            float y = (height - 1) * 1.5f + 1;
            return new Vector3(x, 0, y);
        }

        private Vector3 GetMapLeftDown() {
            return new Vector3(-Mathf.Sqrt(3) / 2, 0, -1);
        }


        #region method to help you get MapGrid

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

            
            HashSet<Vector2Int> gridRec = new HashSet<Vector2Int>() { };
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
            int clusterSize = MapEnum.ClusterSize;
            Vector2Int clusterIdx = new Vector2Int();
            clusterIdx.x = mapIdxX / clusterSize;
            clusterIdx.y = mapIdxY / clusterSize;
            return clusterIdx;
        }

        private Vector2Int GetGridInClusterIdx(int mapIdxX, int mapIdxY) {
            int clusterSize = MapEnum.ClusterSize;

            // caculate map grid'index inside cluster
            Vector2Int inClusterIdx = new Vector2Int();
            inClusterIdx.x = mapIdxX % clusterSize;
            inClusterIdx.y = mapIdxY % clusterSize;
            return inClusterIdx;
        }

        #endregion


        // TODO : 需要重写
        public void SetMapGridColor(List<MapGrid> grids, Color color32) {
            if (grids == null || grids.Count == 0) {
                return;
            }

            Dictionary<Vector2Int, List<Vector2Int>> clusterGridsDict = new Dictionary<Vector2Int, List<Vector2Int>>();
            foreach (var grid in grids) {
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


    public class HexCluster : IEnumerable {

        public int clusterSize { get; private set; }

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color> colors = new List<Color>();

        TDList<MapGrid> mapGridList;
        public Dictionary<Vector2, MapGrid> mapPosGridDict {  get; private set; }

        private Mesh hexMesh;
        private MeshFilter meshFilter;

        public bool hasInit { get; private set; }


        public IEnumerator<MapGrid> GetEnumerator() {
            return mapGridList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public HexCluster() { hasInit = false; }

        public void InitMapCluster(MeshFilter meshFilter, int clusterSize) {
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

            hasInit = true;
        }

        internal void AddMapGrid(Vector2Int mapIdx, Vector2Int clusterIdx, Hexagon hex, Layout layout) {
            Point center = hex.Hex_To_Pixel(layout, hex).ConvertToXZ();
            Vector3 hexPos = new Vector3((float)center.x, 0, (float)center.z);
            //Debug.Log(hexPos);
            // construct the map grid, and add it
            MapGrid mapGrid = new MapGrid(mapIdx, clusterIdx, hexPos);
            mapGridList[clusterIdx.x, clusterIdx.y] = mapGrid;
            if (mapPosGridDict.ContainsKey(hexPos)) {
                mapPosGridDict[hexPos] = mapGrid;
            } else {
                mapPosGridDict.Add(hexPos, mapGrid);
            }

            BuildGridMesh(hex, layout, MapEnum.DefaultGridColor);
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
            Point center = hex.Hex_To_Pixel(layout, hex).ConvertToXZ();
            List<Point> vertexs = hex.Polygon_Corners(layout, hex);

            float radius = Vector3.Distance(center, (Vector3)vertexs[0]);

            float innerRatio = MapEnum.GridInnerRatio;

            List<Point> innerVertexs = new List<Point>();
            List<Point> outterVertexs = new List<Point>();

            for (int i = 0; i < 6; i++) {
                Point offset = hex.Hex_Corner_Offset(layout, i);
                Point innerVertex = center + new Point(offset.x * innerRatio, 0, offset.y * innerRatio);
                Point outterVertex = center + new Point(offset.x, 0, offset.y);
                innerVertexs.Add(innerVertex);
                outterVertexs.Add(outterVertex);
            }

            for (int i = 0; i < 6; i++) {
                int j = i + 1 >= 6 ? 0 : i + 1;

                AddTriangle(center, innerVertexs[i], innerVertexs[j]);
                AddTriangle(center, outterVertexs[i], outterVertexs[j]);
                AddTriangleColor(color, color, color);
                AddTriangleColor(color, color, color);
            }
        }

        internal void ClearClusterMesh() {
            hexMesh.Clear();
            vertices.Clear();
            triangles.Clear();
            colors.Clear();
            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            hexMesh.colors = colors.ToArray();
            hexMesh.RecalculateNormals();
        }

        internal void SetClusterMesh() {
            hexMesh.vertices = vertices.ToArray();
            hexMesh.triangles = triangles.ToArray();
            hexMesh.colors = colors.ToArray();
            hexMesh.RecalculateNormals();
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
        public Hexagon hexagon { get; private set; }


        public Vector3 Position { get; private set; }

        public Color32 GridColor { get; private set; }


        [Header("neighbor")]
        public Vector2Int[] neighborGrids = new Vector2Int[6];          // idx与邻居方位对应

        public MapGrid() {
            IsValidGrid = false;
        }

        internal MapGrid(Vector2Int mapIdx, Vector2Int clsIdx, Vector3 position) {
            this.mapIdx = mapIdx;
            this.clusterIdx = clsIdx;
            this.hexagon = hexagon;
            Position = position;

            // set neighbor
            if (mapIdx.y % 2 == 1) {
                // 奇数行
                neighborGrids[0] = new Vector2Int(mapIdx.x - 1, mapIdx.y);
                neighborGrids[1] = new Vector2Int(mapIdx.x, mapIdx.y + 1);
                neighborGrids[2] = new Vector2Int(mapIdx.x + 1, mapIdx.y + 1);
                neighborGrids[3] = new Vector2Int(mapIdx.x + 1, mapIdx.y);
                neighborGrids[4] = new Vector2Int(mapIdx.x + 1, mapIdx.y - 1);
                neighborGrids[5] = new Vector2Int(mapIdx.x, mapIdx.y - 1);

            } else {
                // 偶数行
                neighborGrids[0] = new Vector2Int(mapIdx.x - 1, mapIdx.y);
                neighborGrids[1] = new Vector2Int(mapIdx.x - 1, mapIdx.y + 1);
                neighborGrids[2] = new Vector2Int(mapIdx.x, mapIdx.y + 1);
                neighborGrids[3] = new Vector2Int(mapIdx.x + 1, mapIdx.y);
                neighborGrids[4] = new Vector2Int(mapIdx.x, mapIdx.y - 1);
                neighborGrids[5] = new Vector2Int(mapIdx.x - 1, mapIdx.y - 1);

            }

            SetGridColor(MapEnum.DefaultGridColor);

            IsValidGrid = true;
        }


        public Vector2Int GetNeighborIdx(HexDirection direction) {
            int idx = (int)direction;
            return neighborGrids[idx];
        }

        public Vector2Int GetNeighborIdx(int direction) {
            return neighborGrids[direction];
        }


        internal void SetGridColor(Color32 color) {
            GridColor = color;
        }

    }

}
