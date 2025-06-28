using LZ.WarGameCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace LZ.WarGameMap.Runtime
{

    // NOTE : 一个 cluster 对应一个TIF高度图文件，包含了多个 TerrainTile
    public class TerrainCluster : IBinarySerializer, IDisposable {

        public int idxX { get; private set; }
        public int idxY { get; private set; }

        public int longitude { get; private set; }
        public int latitude { get; private set; }

        public bool IsLoaded { get; private set; }
        public bool IsShowing { get; private set; }     // TODO : 有bug！


        TerrainSetting terSet;
        Vector3 clusterStartPoint;

        Material mat;   // TODO : 要记录在这吗？

        private TDList<TerrainTile> tileList;
        
        public TDList<TerrainTile> TileList { get { return tileList; } }

        public GameObject clusterGo { get; private set; }

        public TerrainCluster() { IsLoaded = false; IsShowing = false; }


        #region init cluster

        public void InitTerrainCluster_Static(int idxX, int idxY, int longitude, int latitude, TerrainSetting terSet, HeightDataManager heightDataManager, GameObject clusterGo, Material mat) {
            this.idxX = idxX;
            this.idxY = idxY;

            this.longitude = longitude;
            this.latitude = latitude;

            this.terSet = terSet;

            this.clusterGo = clusterGo;

            clusterStartPoint = new Vector3(terSet.clusterSize * idxX, 0, terSet.clusterSize * idxY);
            _InitTerrainCluster_Static(heightDataManager, mat);

            IsLoaded = true;
        }

        private void _InitTerrainCluster_Static(HeightDataManager heightDataManager, Material mat) {
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;

            this.mat = mat;

            //Debug.Log(string.Format("the cluster size : {0}x{1}, because the size of cluster is {2}, so there are {3} tiles in a row", 
            //    terrainSize.x, terrainSize.z, tileSize, tileNumPerLine));

            tileList = new TDList<TerrainTile>(tileNumPerLine, tileNumPerLine);
            int[] lodLevels = new int[terSet.LODLevel];
            for (int i = 0; i < terSet.LODLevel; i++) {
                lodLevels[i] = i;
            }

            for (int i = 0; i < tileNumPerLine; i++) {
                for (int j = 0; j < tileNumPerLine; j++) {
                    GameObject tileGo = CreateTerrainTile(i, j, mat);
                    MeshFilter meshFilter = tileGo.GetComponent<MeshFilter>();
                    MeshRenderer meshRenderer = tileGo.GetComponent<MeshRenderer>();
                    tileList[i, j].InitTileMeshData(i, j, longitude, latitude, clusterStartPoint, meshFilter, meshRenderer, lodLevels);
                }
            }

            // generate mesh data for every LOD level
            int curLODLevel = terSet.LODLevel - 1;
            while (curLODLevel >= 0) {
                // vertexNumFix == 1, 生成的 tile 每行顶点数等于 tileSize
                // vertextNumFix == 2, 即当前生成的这个 tile 有 terSet.tileSize / 2 个顶点
                // 按目前计算 : LOD = maxLODLevel 时 顶点树等于 tileSize
                int vertexNumFix = (int)Mathf.Pow(2, (terSet.LODLevel - curLODLevel - 1));

                if (vertexNumFix > terSet.tileSize) {   // wrong
                    break;
                }

                for (int i = 0; i < tileNumPerLine; i++) {
                    for (int j = 0; j < tileNumPerLine; j++) {
                        //tileList[i, j].SetMeshData_Origin(curLODLevel, terSet, vertexNumFix, heightDataManager);
                        //tileList[i, j].SetMeshData_NoCoroutine(curLODLevel, terSet, vertexNumFix, heightDataManager);
                        tileList[i, j].SetMeshData_Coroutine(curLODLevel, terSet, vertexNumFix, heightDataManager);
                    }
                }

                curLODLevel--;
            }

            //Debug.Log(string.Format($"successfully generate terrain cluster {longitude}_{latitude} and tiles! "));
        }

        private GameObject CreateTerrainTile(int idxX, int idxY, Material mat) {
            GameObject tileGo = new GameObject();
            tileGo.transform.parent = clusterGo.transform;
            tileGo.name = string.Format("heightTile_{0}_{1}", idxX, idxY);

            MeshFilter meshFilter = tileGo.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = tileGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = mat;
            return tileGo;
        }

        public void SampleMeshNormal(Texture2D normalTex) {

            for (int i = 0; i < tileList.GetLength(1); i++) {
                for (int j = 0; j < tileList.GetLength(0); j++) {
                    tileList[i, j].SampleMeshNormal();
                }
            }
        }

        public void Dispose() {
            if (tileList != null) {
                foreach (var tile in tileList) {
                    tile.Dispose();
                }
            }
        }

        #endregion


        public void ExeTerrainSimplify(int tileIdxX, int tileIdxY, TerrainSimplifier terrainSimplifier, float simplifyTarget) {

            // NOTE : 先只做一个地块的
            tileList[tileIdxX, tileIdxY].ExeTerrainSimplify_Min(terrainSimplifier, simplifyTarget);;
            
            // TOOD : 要改
            for (int i = 0; i < tileList.GetLength(1); i++) {
                for (int j = 0; j < tileList.GetLength(0); j++) {
                    //tileList[i, j].ExeTerrainSimplify(i, j, terrainSimplifier, simplifyTarget);
                }
            }
        }


        #region runtime update terrain, fix LOD seam

        // NOTE : LODHeight 根据摄像机距离地面的高度决定地块的 lod level
        public int UpdateTerrainCluster_LODHeight(int curLODLevel, int LODLevels) {
            // if we switch LOD by height, then we do not need to fix the seam
            //  float FadeDistance, int LODLevels
            if(tileList == null || tileList.Count <= 0) {
                return -100;
            }

            if (tileList[0, 0].curLODLevel == curLODLevel && IsShowing == true) {
                // tile's lod do not change and is showing, so return (only if its in height switch method
                return curLODLevel;     
            }

            foreach (var tile in tileList) {
                tile.SetMesh(tile.GetMesh(curLODLevel, 0, LODSwitchMethod.Height), mat);
            }

            // 当调用这个方法的时候，默认show statu发生了改变，除非LOD级别越界了
            if (curLODLevel >= 0 && curLODLevel < LODLevels) {
                IsShowing = true;
            }
            return curLODLevel;
        }

        public void UpdateTerrainCluster_LODDistance(TDList<int> fullLodLevelMap) {
            int showTileNum = 0;
            foreach (var tile in tileList) {
                // use full lod map, so index offset is 1
                int x = tile.tileIdxX + 1;
                int y = tile.tileIdxY + 1;
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

                if (fixSeamDirection == 0 && tile.curLODLevel == lodLevel) {
                    // no need to fix seam, and no change LOD level, so continue
                    continue;
                }

                // 不管LOD层级有没有发生改变，都必须重新刷新mesh，因为要处理LOD接缝
                Mesh mesh = tile.GetMesh(lodLevel, fixSeamDirection, LODSwitchMethod.Distance);
                tile.SetMesh(mesh, mat);
                if(mesh != null) {
                    showTileNum++;
                }
            }

            //IsShowing = (showTileNum > 0);
            //Debug.Log(string.Format("update successfully! handle tile num {0}", terrainTileList.GetLength(0)));
        }

        public void HideTerrainCluster() {
            foreach (var tileMeshData in tileList) {
                tileMeshData.SetMesh(null, mat);
            }
            IsShowing = false;
        }

        internal TDList<int> GetFullLODLevelMap(Vector3 cameraPos) {
            int tileWidth = tileList.GetLength(1);
            int tileHeight = tileList.GetLength(0);

            TDList<int> lodLevelMap = GetLODLevelMap(cameraPos);
            TDList<int> fullLodLevelMap = new TDList<int>(tileWidth + 2, tileHeight + 2);

            // copy cluster's lodlevelmap to fulllodlevelmap
            for (int i = 1; i < tileWidth + 1; i++) {
                for (int j = 1; j < tileHeight + 1; j++) {
                    fullLodLevelMap[i, j] = lodLevelMap[i - 1, j - 1];
                }
            }
            return fullLodLevelMap;
        }

        private TDList<int> GetLODLevelMap(Vector3 cameraPos) {
            int tileWidth = tileList.GetLength(1);
            int tileHeight = tileList.GetLength(0);
            TDList<int> lodLevelMap = new TDList<int>(tileWidth, tileHeight);

            // get every tile's LOD level
            foreach (var tileMeshData in tileList) {
                int x = tileMeshData.tileIdxX;
                int y = tileMeshData.tileIdxY;
                int lodLevel = tileMeshData.GetLODLevel_Distance(cameraPos);
                lodLevelMap[x, y] = lodLevel;
            }
            return lodLevelMap;
        }

        private enum GetEdgeDir {
            Left, Right, Up, Down
        }

        private List<int> GetEdgeLODLevel(Vector3 cameraPos, GetEdgeDir edgeDir) {
            int tileWidth = tileList.GetLength(1);
            int tileHeight = tileList.GetLength(0);

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
                        int lodLevel = tileList[0, i].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Right:
                    for (int i = 0; i < tileHeight; i++) {
                        int lodLevel = tileList[tileWidth - 1, i].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Up:
                    for (int i = 0; i < tileWidth; i++) {
                        int lodLevel = tileList[i, tileHeight - 1].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Down:
                    for (int i = 0; i < tileWidth; i++) {
                        int lodLevel = tileList[i, 0].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
            }
            return lodLevels;
        }

        #endregion


        #region serialize

        public string GetClusterInfo() {
            return $"{idxX},{idxY},{longitude},{latitude}";
        }

        public void WriteToBinary(BinaryWriter writer) {
            writer.Write(idxX);
            writer.Write(idxY);
            writer.Write(longitude);
            writer.Write(latitude);
        }

        public void ReadFromBinary(BinaryReader reader) {
            idxX = reader.ReadInt32();
            idxY = reader.ReadInt32();
            longitude = reader.ReadInt32();
            latitude = reader.ReadInt32();
        }

        #endregion
    
        public Vector2Int GetClusterLL() {
            return new Vector2Int(longitude, latitude);
        }

        public Mesh GetTileMesh(int lodLevel, int tileIdxX, int tileIdxY) {
            if (tileIdxX < 0 || tileIdxX >= tileList.GetLength(0) || tileIdxY < 0 || tileIdxY >= tileList.GetLength(1)) {
                Debug.LogError($"wrong index when get cluster : {tileIdxX}, {tileIdxY}");
                return null;
            }
            return tileList[tileIdxX, tileIdxY].GetMesh(lodLevel, 0, LODSwitchMethod.Height);
        }

    }


    public class TerrainTile : IBinarySerializer, IDisposable {

        public int tileIdxX { get; private set; }
        public int tileIdxY { get; private set; }

        public int longitude { get; private set; }
        public int latitude { get; private set; }

        public int curLODLevel { get; private set; }

        Vector3 clusterStartPoint;

        int clusterSize;
        int tileSize;
        int[] LODLevels;
        TerrainMeshData[] LODMeshes;

        public TerrainMeshData[] GetLODMeshes() { return LODMeshes; }


        HeightDataManager heightDataManager;

        public Vector3 tileCenterPos { get; private set; }

        private MeshFilter meshFilter;
        private MeshRenderer renderer;

        public TerrainTile() { }


        #region init tile data

        public void InitTileMeshData(int idxX, int idxY, int longitude, int latitude, Vector3 startPoint, MeshFilter meshFilter, MeshRenderer renderer, int[] lODLevels) {
            this.clusterStartPoint = startPoint;
            this.meshFilter = meshFilter;
            this.renderer = renderer;

            tileIdxX = idxX;
            tileIdxY = idxY;

            this.longitude = longitude;
            this.latitude = latitude;

            curLODLevel = -1;       // init as -1

            LODLevels = lODLevels;
            LODMeshes = new TerrainMeshData[lODLevels.Length];
        }

        public void Dispose() {
            foreach (var mesh in LODMeshes) {
                if (mesh != null) {
                    mesh.Dispose();
                }
            }
        }

        public void SetMeshData_Coroutine(int curLODLevel, TerrainSetting terSet, int vertexNumFix, HeightDataManager heightDataManager) {
            this.heightDataManager = heightDataManager;
            this.clusterSize = terSet.clusterSize;
            this.tileSize = terSet.tileSize;

            CoroutineManager.GetInstance().RunCoroutine(_SetMeshData(curLODLevel, terSet, vertexNumFix, 50000));
        }

        private IEnumerator _SetMeshData(int curLODLevel, TerrainSetting terSet, int vertexNumFix, int maxTickOneFrame) {
            // caculate tile's start point
            float startX = tileIdxX * tileSize;
            float startZ = tileIdxY * tileSize;
            Vector3 startPoint = new Vector3(startX, 0, startZ) + this.clusterStartPoint;       // 
            tileCenterPos = startPoint + new Vector3(tileSize / 2, 0, tileSize / 2);

            // v : vertex, g : grid
            //v———-v
            //| g | g |
            //|————
            //| g | g |
            //v———-v
            // LOD4(max) :  gridSize : 256, vertexPerLine : 257, vertexPerLineFixed : 259, gridSize : 1
            int gridNumPerLine = tileSize / vertexNumFix;
            int gridSize = tileSize / gridNumPerLine;
            int vertexPerLine = tileSize / vertexNumFix + 1;

            int gridNumPerLineFixed = gridNumPerLine + 2;
            int vertexPerLineFixed = vertexPerLine + 2;

            LODMeshes[curLODLevel] = new TerrainMeshData();
            TerrainMeshData meshData = LODMeshes[curLODLevel];
            meshData.InitMeshData(tileIdxX, tileIdxY, curLODLevel, gridNumPerLine, gridNumPerLineFixed, vertexPerLine, vertexPerLineFixed);

            int curInVertIdx = 0;
            int curOutVertIdx = -1;

            int tickCnt = 0;

            Vector3 offsetInMeshVert = new Vector3(gridSize, 0, gridSize);
            //Vector3 offsetInMeshVert = new Vector3(0, 0, 0);

            // multithreading, but slow ?
            /*Action<int> exeHeightSet = (idx) => {
                int i = idx / vertexPerLineFixed;
                int j = idx % vertexPerLineFixed;

                bool isVertOutOfMesh = (i == 0) || (i == vertexPerLineFixed - 1) || (j == 0) || (j == vertexPerLineFixed - 1);
                Vector3 vert = new Vector3(gridSize * i, 0, gridSize * j) + startPoint - offsetInMeshVert;

                float height = heightDataManager.SampleFromHeightData(longitude, latitude, vert, clusterStartPoint);

                vert.y = height;
                Vector2 uv = new Vector2(vert.x / terSet.clusterSize, vert.z / terSet.clusterSize);

                if (isVertOutOfMesh) {
                    meshData.AddVertex(vert, uv, curOutVertIdx);
                    meshData.SetIndiceInMap(i, j, curOutVertIdx);
                    curOutVertIdx--;
                } else {
                    meshData.AddVertex(vert, uv, curInVertIdx);
                    meshData.SetIndiceInMap(i, j, curInVertIdx);
                    curInVertIdx++;
                }
            };
            Parallel.For(0, vertexPerLineFixed * vertexPerLineFixed, exeHeightSet);*/

            for (int i = 0; i < vertexPerLineFixed; i++) {
                for (int j = 0; j < vertexPerLineFixed; j++) {
                    tickCnt++;
                    if (tickCnt > maxTickOneFrame) {
                        tickCnt = 0;
                        yield return null;
                    }

                    bool isVertOutOfMesh = (i == 0) || (i == vertexPerLineFixed - 1) || (j == 0) || (j == vertexPerLineFixed - 1);
                    Vector3 vert = new Vector3(gridSize * i, 0, gridSize * j) + startPoint - offsetInMeshVert;

                    // NOTE : 这里的代码不能删！千万不能删啊
                    //float height = SampleFromHeightData(terrainClusterSize, vert);
                    float height = heightDataManager.SampleFromHeightData(longitude, latitude, vert, clusterStartPoint);
                    //float height = heightDataManager.SampleFromHexMap(vert);
                    //float height = 0;

                    vert.y = height;
                    Vector2 uv = new Vector2(vert.x / terSet.clusterSize, vert.z / terSet.clusterSize);

                    if (isVertOutOfMesh) {
                        meshData.AddVertex(vert, uv, curOutVertIdx);
                        meshData.SetIndiceInMap(i, j, curOutVertIdx);
                        curOutVertIdx--;
                    } else {
                        meshData.AddVertex(vert, uv, curInVertIdx);
                        meshData.SetIndiceInMap(i, j, curInVertIdx);
                        curInVertIdx++;
                    }
                }
            }

            // TODO : 有问题！！！！
            // LOD4(max) :  gridSize : 256, vertexPerLine : 257, vertexPerLineFixed : 259, gridSize : 1
            // 需要计算一遍
            int curGridIdx = 0;
            for (int i = 0; i < gridNumPerLineFixed; i++) {
                for (int j = 0; j < gridNumPerLineFixed; j++) {
                    tickCnt++;
                    if (tickCnt > maxTickOneFrame) {
                        tickCnt = 0;
                        yield return null;
                    }

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
            meshData.BuildOriginMesh();
        }

        public void SetMeshData_Origin(int curLODLevel, TerrainSetting terSet, int vertexNumFix, HeightDataManager heightDataManager) {
            this.heightDataManager = heightDataManager;
            this.clusterSize = terSet.clusterSize;
            this.tileSize = terSet.tileSize;

            // caculate tile's start point
            float startX = tileIdxX * tileSize;
            float startZ = tileIdxY * tileSize;
            Vector3 startPoint = new Vector3(startX, 0, startZ) + this.clusterStartPoint;       // 
            tileCenterPos = startPoint + new Vector3(tileSize / 2, 0, tileSize / 2);

            // v : vertex, g : grid
            //v———-v
            //| g | g |
            //|————
            //| g | g |
            //v———-v
            // LOD4(max) :  gridSize : 256, vertexPerLine : 257, vertexPerLineFixed : 259, gridSize : 1
            int gridNumPerLine = tileSize / vertexNumFix;
            int gridSize = tileSize / gridNumPerLine;
            int vertexPerLine = tileSize / vertexNumFix + 1;

            int gridNumPerLineFixed = gridNumPerLine + 2;
            int vertexPerLineFixed = vertexPerLine + 2;

            LODMeshes[curLODLevel] = new TerrainMeshData();
            TerrainMeshData meshData = LODMeshes[curLODLevel];
            meshData.InitMeshData(tileIdxX, tileIdxY, curLODLevel, gridNumPerLine, gridNumPerLineFixed, vertexPerLine, vertexPerLineFixed);

            int curInVertIdx = 0;
            int curOutVertIdx = -1;

            Vector3 offsetInMeshVert = new Vector3(gridSize, 0, gridSize);
            //Vector3 offsetInMeshVert = new Vector3(0, 0, 0);

            for (int i = 0; i < vertexPerLineFixed; i++) {
                for (int j = 0; j < vertexPerLineFixed; j++) {

                    if (i >= 254 && j <= 3) {
                        int test03 = 1;
                    }

                    bool isVertOutOfMesh = (i == 0) || (i == vertexPerLineFixed - 1) || (j == 0) || (j == vertexPerLineFixed - 1);
                    Vector3 vert = new Vector3(gridSize * i, 0, gridSize * j) + startPoint - offsetInMeshVert;

                    // NOTE : 这里的代码不能删！千万不能删啊
                    //float height = SampleFromHeightData(terrainClusterSize, vert);
                    //float height = heightDataManager.SampleFromHeightData(longitude, latitude, vert, clusterStartPoint);
                    //float height = heightDataManager.SampleFromHexMap(vert);
                    float height = 0;

                    vert.y = height;
                    Vector2 uv = new Vector2(vert.x / terSet.clusterSize, vert.z / terSet.clusterSize);

                    if (isVertOutOfMesh) {
                        meshData.AddVertex(vert, uv, curOutVertIdx);
                        meshData.SetIndiceInMap(i, j, curOutVertIdx);
                        curOutVertIdx--;
                    } else {
                        meshData.AddVertex(vert, uv, curInVertIdx);
                        meshData.SetIndiceInMap(i, j, curInVertIdx);
                        curInVertIdx++;
                    }
                }
            }

            // TODO : 有问题！！！！
            // LOD4(max) :  gridSize : 256, vertexPerLine : 257, vertexPerLineFixed : 259, gridSize : 1
            // 需要计算一遍
            int curGridIdx = 0;
            for (int i = 0; i < gridNumPerLineFixed; i++) {
                for (int j = 0; j < gridNumPerLineFixed; j++) {

                    // i, j 是当前遍历到的 grid 的 index
                    int cur_w = curGridIdx % gridNumPerLineFixed;
                    int cur_h = curGridIdx / gridNumPerLineFixed;
                    int next_w = cur_w + 1;
                    int next_h = cur_h + 1;

                    if (cur_w == 256 && cur_h == 1) {
                        int tt = 1;
                    }
                    if (next_w == 256 && next_h == 1) {
                        int tt = 1;
                    }

                    int a = meshData.GetIndiceInMap(cur_w, cur_h);
                    int b = meshData.GetIndiceInMap(cur_w, next_h);
                    int c = meshData.GetIndiceInMap(next_w, next_h);
                    int d = meshData.GetIndiceInMap(next_w, cur_h);

                    if (a == 65278 || b == 65278 || c == 65278 || d == 65278) {
                        int test2 = 2;
                    }
                    meshData.AddTriangle(a, b, c, i, j);
                    meshData.AddTriangle(a, c, d, i, j);

                    curGridIdx++;
                }
            }

            meshData.RecaculateNormal();
            meshData.BuildOriginMesh();
        }

        #endregion


        public void SampleMeshNormal() {
            Mesh curMesh = meshFilter.sharedMesh;    // GetMesh(curLODLevel, 0000)
            int vertNum = curMesh.normals.Length;
            Vector3[] vertex = curMesh.vertices;
            Vector3[] normals = new Vector3[vertNum];
            for(int i = 0; i < vertNum; i++) {
                Vector3 pos = vertex[i];
                normals[i] = heightDataManager.SampleNormalFromData(longitude, latitude, pos, clusterStartPoint);
            }

            curMesh.normals = normals;
            SetMesh(curMesh, null);
        }

        public void ExeTerrainSimplify(TerrainSimplifier terrainSimplifier, float simplifyTarget) {
            int length = LODMeshes.Length;

            // exe simplify to all lod level tile
            for (int i = 0; i < length; i++) {
                ExeTerrainSimplify(i, terrainSimplifier, simplifyTarget);
            }
        }

        public void ExeTerrainSimplify_Min(TerrainSimplifier terrainSimplifier, float simplifyTarget) {
            ExeTerrainSimplify(curLODLevel, terrainSimplifier, simplifyTarget);
        }

        private void ExeTerrainSimplify(int curLODLevel, TerrainSimplifier terrainSimplifier, float simplifyTarget, int iterLimit = 5) {
            if (curLODLevel >= LODLevels.Length || curLODLevel < 0) {
                Debug.LogError("wrong cur lod level");
                return;
            }

            // only update once time
            LODMeshes[curLODLevel].UpdateEdgeVertInfoByOrigin(null);
            // LODMeshes[curLODLevel].GetMesh_LODDistance(tileIdxX, tileIdxY, 0)
            Mesh mesh = LODMeshes[curLODLevel].GetMesh_LODHeight();
            int targetVertCnt = (int)(mesh.vertexCount * simplifyTarget);

            int iterCnt = 0;
            while(mesh.vertexCount > targetVertCnt && iterCnt ++ < iterLimit) {
                List<int> edgeVerts = LODMeshes[curLODLevel].GetEdgeVertInfo();
                terrainSimplifier.InitSimplifyer(mesh, edgeVerts, null, targetVertCnt, clusterStartPoint, clusterSize);

                terrainSimplifier.StartSimplify();

                // get new mesh and new edge verts
                List<int> newEdgeVerts = new List<int>();
                mesh = terrainSimplifier.EndSimplify(ref newEdgeVerts);
                ReplaceMesh(mesh, null);
                LODMeshes[curLODLevel].SetEdgeVertInfo(newEdgeVerts);
            }
        }


        public int GetLODLevel_Distance(Vector3 cameraPos) {
            float distance = Vector3.Distance(cameraPos, tileCenterPos);

            int idx = LODLevels.Length - 1;
            int levelParam = tileSize / 2;

            while (true) {
                if (idx < 0) {
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

        public Mesh GetMesh(int curLODLevel, int fixDirection, LODSwitchMethod switchMethod) {
            this.curLODLevel = curLODLevel;
            if (curLODLevel < 0 || curLODLevel >= LODMeshes.Length) {
                Debug.LogError("wrong LOD level, can not get mesh");
                return null;
            }

            if(switchMethod == LODSwitchMethod.Height) {
                return LODMeshes[curLODLevel].GetMesh_LODHeight();
            } else if(switchMethod == LODSwitchMethod.Distance) {
                return LODMeshes[curLODLevel].GetMesh_LODDistance(tileIdxX, tileIdxY, fixDirection);
            }
            return null;
        }

        public void SetMesh(Mesh mesh, Material mat) {
#if UNITY_EDITOR
            meshFilter.mesh = mesh;
#else
            meshFilter.mesh = mesh;
#endif
            renderer.sharedMaterial = mat;
        }

        public void ReplaceMesh(Mesh mesh, Material mat) {
            UnityEngine.Object.DestroyImmediate(meshFilter.sharedMesh);
            LODMeshes[curLODLevel].SetMesh(mesh);
            SetMesh(mesh, mat);
        }
        
        
        #region serialize

        public string GetTileInfo() {
            return $"{tileIdxX},{tileIdxY},{longitude},{latitude},{curLODLevel},{clusterStartPoint.ToStringFixed()}";
        }

        public void WriteToBinary(BinaryWriter writer) {
            writer.Write(tileIdxX);
            writer.Write(tileIdxY);
            writer.Write(longitude);
            writer.Write(latitude);
            writer.Write(curLODLevel);

            writer.Write(clusterStartPoint.x);
            writer.Write(clusterStartPoint.y);
            writer.Write(clusterStartPoint.z);
        }

        public void ReadFromBinary(BinaryReader reader) {
            tileIdxX = reader.ReadInt32();
            tileIdxY = reader.ReadInt32();
            longitude = reader.ReadInt32();
            latitude = reader.ReadInt32();
            curLODLevel = reader.ReadInt32();

            clusterStartPoint = new Vector3();
            clusterStartPoint.x = reader.ReadSingle();
            clusterStartPoint.y = reader.ReadSingle();
            clusterStartPoint.z = reader.ReadSingle();
        }

        #endregion

    }

}
