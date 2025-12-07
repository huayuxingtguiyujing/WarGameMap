using LZ.WarGameCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace LZ.WarGameMap.Runtime
{
    using GetSimplifierCall = Func<int, int, int, int, int, TerrainSimplifier>;
    // NOTE : 一个 cluster 对应一个TIF高度图文件，包含了多个 TerrainTile
    public class TerrainCluster : IBinarySerializer, IDisposable
    {

        public int idxX { get; private set; }
        public int idxY { get; private set; }

        public int longitude { get; private set; }
        public int latitude { get; private set; }

        public bool IsInited { get; private set; }
        public bool IsLoaded { get; private set; }
        public bool IsShowing { get; private set; }     // TODO : 有bug！


        TerrainSettingSO terSet;
        Vector3 clusterStartPoint;

        Material mat;   // TODO : 要记录在这吗？

        private TDList<TerrainTile> tileList;

        public TDList<TerrainTile> TileList { get { return tileList; } }

        public GameObject clusterGo { get; private set; }

        public TerrainCluster() { IsInited = false; IsLoaded = false; IsShowing = false; }


        #region Init cluster

        public void InitTerrainCluster_Static(int idxX, int idxY, int longitude, int latitude, TerrainSettingSO terSet, GameObject clusterGo, Material mat)
        {
            this.idxX = idxX;
            this.idxY = idxY;

            this.longitude = longitude;
            this.latitude = latitude;

            this.terSet = terSet;

            this.clusterGo = clusterGo;

            clusterStartPoint = new Vector3(terSet.clusterSize * idxX, 0, terSet.clusterSize * idxY);
            _InitTerrainCluster_Static(mat);

            IsInited = true;
        }

        private void _InitTerrainCluster_Static(Material mat)
        {
            int tileNumPerLine = terSet.GetTileNumClsPerLine();
            this.mat = mat;
            //Debug.Log(string.Format("the cluster size : {0}x{1}, because the size of cluster is {2}, so there are {3} tiles in a row", 
            //    terrainSize.x, terrainSize.z, tileSize, tileNumPerLine));

            tileList = new TDList<TerrainTile>(tileNumPerLine, tileNumPerLine);
            int[] lodLevels = new int[terSet.LODLevel];
            for (int i = 0; i < terSet.LODLevel; i++)
            {
                lodLevels[i] = i;
            }

            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    GameObject tileGo = CreateTerrainTile(i, j, mat);
                    MeshFilter meshFilter = tileGo.GetComponent<MeshFilter>();
                    MeshRenderer meshRenderer = tileGo.GetComponent<MeshRenderer>();
                    tileList[i, j].InitTileMeshData(i, j, longitude, latitude, clusterStartPoint, meshFilter, meshRenderer, lodLevels);
                }
            }

        }

        private GameObject CreateTerrainTile(int idxX, int idxY, Material mat)
        {
            GameObject tileGo = new GameObject();
            tileGo.transform.parent = clusterGo.transform;
            tileGo.name = string.Format("heightTile_{0}_{1}", idxX, idxY);

            MeshFilter meshFilter = tileGo.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = tileGo.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = mat;
            return tileGo;
        }


        public void SetMeshData(HeightDataManager heightDataManager)
        {
            int tileNumPerLine = terSet.GetTileNumClsPerLine();
            // Now we use TerrainSimplifyer; if LOD 4, vertexNumFix is 1, so there are width's num of vertexs
            // Firstly gen max lodlevel's mesh
            int curLODLevel = terSet.LODLevel - 1;

            // Gen LOD by TerrainSimplify, we will set LOD max firstly, and then use it to simplify other LOD
            int vertexNumFix = 1;
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    tileList[i, j].SetMeshData_Origin(curLODLevel, terSet, vertexNumFix, heightDataManager);
                }
            }
            curLODLevel--;
            TerrainSimplifier terrainSimplifier = new TerrainSimplifier();
            while (curLODLevel >= 0)
            {
                for (int i = 0; i < tileNumPerLine; i++)
                {
                    for (int j = 0; j < tileNumPerLine; j++)
                    {
                        tileList[i, j].SetMeshData_Copy(curLODLevel, terrainSimplifier, terSet.GetSimplifyTarget(curLODLevel));
                    }
                }
                curLODLevel--;
            }
            IsLoaded = true;
            //Debug.Log(string.Format($"successfully generate terrain cluster {longitude}_{latitude} and tiles! "));
        }

        // Recommand use SetMeshData
        // _CutHalf : Generate diff LOD mesh by cut them to half 
        public void SetMeshData_CutHalf(HeightDataManager heightDataManager)
        {
            int tileNumPerLine = terSet.GetTileNumClsPerLine();
            // Now we use TerrainSimplifyer; if LOD 4, vertexNumFix is 1, so there are width's num of vertexs
            // Firstly gen max lodlevel's mesh
            int curLODLevel = terSet.LODLevel - 1;
            // generate mesh data for every LOD level
            int vertexNumFix = 1;
            while (curLODLevel >= 0)
            {
                // vertexNumFix == 1, 生成的 tile 每行顶点数等于 tileSize
                // vertextNumFix == 2, 即当前生成的这个 tile 有 terSet.tileSize / 2 个顶点
                // 按目前计算 : LOD = maxLODLevel 时 顶点数等于 tileSize
                //int vertexNumFix = (int)Mathf.Pow(2, (terSet.LODLevel - curLODLevel - 1));
                if (vertexNumFix >= terSet.tileSize)
                {   // wrong
                    break;
                }
                for (int i = 0; i < tileNumPerLine; i++)
                {
                    for (int j = 0; j < tileNumPerLine; j++)
                    {
                        //tileList[i, j].SetMeshData_Origin(curLODLevel, terSet, vertexNumFix, heightDataManager);
                        //tileList[i, j].SetMeshData_NoCoroutine(curLODLevel, terSet, vertexNumFix, heightDataManager);
                        tileList[i, j].SetMeshData_Coroutine(curLODLevel, terSet, vertexNumFix, heightDataManager);
                    }
                }
                curLODLevel--;
                vertexNumFix *= 2;
            }
            IsLoaded = true;
        }

        // OnlyMaxLOD : Only gen max lod layer, usually used in editor scene
        public void SetMeshData_OnlyMaxLOD(HeightDataManager heightDataManager)
        {
            int tileNumPerLine = terSet.GetTileNumClsPerLine();
            int maxLODLevel = terSet.LODLevel - 1;
            int vertexNumFix = 1;
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    tileList[i, j].SetMeshData_Coroutine(maxLODLevel, terSet, vertexNumFix, heightDataManager);
                }
            }
            IsLoaded = true;
        }

        public void ApplyRiverEffect(HeightDataManager heightDataManager, RiverDataManager riverDataManager)
        {
            if (riverDataManager.IsValid == false)
            {
                DebugUtility.LogError("un valid river data manager, so can not init river!", DebugPriority.Medium);
                return;
            }
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    tileList[i, j].ApplyRiverEffect(heightDataManager, riverDataManager);
                }
            }
        }

        public void BuildOriginMeshWrapper()
        {
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    tileList[i, j].BuildOriginMeshWrapper();
                }
            }
        }

        public void BuildOriginMesh()
        {
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    BuildOriginMesh(i, j);
                }
            }
        }

        public void BuildOriginMesh(int tileX, int tileY)
        {
            tileList[tileX, tileY].BuildOriginMesh();
        }

        public void SampleMeshNormal(Texture2D normalTex)
        {

            for (int i = 0; i < tileList.GetLength(1); i++)
            {
                for (int j = 0; j < tileList.GetLength(0); j++)
                {
                    tileList[i, j].SampleMeshNormal();
                }
            }
        }

        public void Dispose()
        {
            if (tileList != null)
            {
                foreach (var tile in tileList)
                {
                    tile.Dispose();
                }
            }
        }

        #endregion
        
        #region Simplify

        public async Task ExeSimplify_MT(GetSimplifierCall getSimplifierCall, CancellationToken token)
        {
            List<Task> taskList = new List<Task>();
            int tileNumPerLine = terSet.GetTileNumClsPerLine();
            for (int tileX = 0; tileX < tileNumPerLine; tileX++)
            {
                for (int tileY = 0; tileY < tileNumPerLine; tileY++)
                {
                    for (int lodLevel = 0; lodLevel < terSet.LODLevel - 1; lodLevel++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            // exe cancel when the circle start
                            return;
                        }

                        int _tileX = tileX, _tileY = tileY, lodLevelRec = lodLevel;

                        // TODO : add cacel Token 要能中止，不然只能干等
                        /*//Func<CancellationToken, int> exeSimplifyFunc = (token) =>
                        //{
                        //    TerrainSimplifier terrainSimplifier = new TerrainSimplifier();
                        //    tileList[tileX, tileY].ExeTerrainSimplify(lodLevelRec, terrainSimplifier, terSet.GetSimplifyTarget(lodLevelRec));
                        //    return 0;
                        //};*/

                        Action<CancellationToken> exeSimplify = (cancelToken) =>
                        {
                            TerrainSimplifier terrainSimplifier = getSimplifierCall(idxX, idxY, _tileX, _tileY, lodLevelRec);
                            tileList[_tileX, _tileY].BuildOriginMeshWrapper();
                            tileList[_tileX, _tileY].ExeTerrainSimplify(lodLevelRec, terrainSimplifier, terSet.GetSimplifyTarget(lodLevelRec));
                            tileList[_tileX, _tileY].RecaculateNormal_Mesh();
                        };
                        Action<int> onComplete = (taskId) =>
                        {
                            DebugUtility.Log($"task over : {taskId}, cluster : {idxX},{idxY}, tile : {_tileX},{_tileY}, lod level : {lodLevelRec}, complete simplify");
                        };

                        var task = ThreadManager.GetInstance().RunTaskAsync(exeSimplify, onComplete, token);
                        taskList.Add(task);
                    }
                }
            }
            await Task.WhenAll(taskList);
        }

        [Obsolete]
        public void ExeTerrainSimplify_MT()
        {
            object lockObj = new object();
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;

            int totalSimplifyTargets = tileNumPerLine * tileNumPerLine * (terSet.LODLevel - 1);
            int startTotalVertNum = 0;
            int targetVertNum = GetTargetSimplifyVertNum();
            //SimplifyProcess.StartRecSimplifyProcess(startTotalVertNum, targetVertNum);

            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    for (int lodLevel = 0; lodLevel < terSet.LODLevel - 1; lodLevel++)
                    {
                        int tileX = i, tileY = j, lodLevelRec = lodLevel;

                        //Func<CancellationToken, int> exeSimplifyFunc = (token) =>
                        //{
                        //    TerrainSimplifier terrainSimplifier = new TerrainSimplifier();
                        //    tileList[tileX, tileY].ExeTerrainSimplify(lodLevelRec, terrainSimplifier, terSet.GetSimplifyTarget(lodLevelRec));
                        //    return 0;
                        //};

                        Action exeSimplify = () =>
                        {
                            TerrainSimplifier terrainSimplifier = new TerrainSimplifier();
                            tileList[tileX, tileY].BuildOriginMeshWrapper();
                            tileList[tileX, tileY].ExeTerrainSimplify(lodLevelRec, terrainSimplifier, terSet.GetSimplifyTarget(lodLevelRec));
                            tileList[tileX, tileY].RecaculateNormal_Mesh();
                        };
                        Action onComplete = () =>
                        {
                            DebugUtility.Log($"cluster : {idxX},{idxY}, tile : {tileX},{tileY}, lod level : {lodLevelRec}, complete simplify");

                            lock (lockObj)
                            {
                                totalSimplifyTargets--;
                                if (totalSimplifyTargets <= 0)
                                {
                                    DebugUtility.Log("total simplify over, start build Mesh.");

                                    // TODO : 也许应该放到 TerGenTask ?
                                    BuildOriginMesh();
                                    //ProgressManager.GetInstance().ProgressGoNextTask(timerID);
                                }
                            }
                        };
                        // NOTE : !!! 怎么处理cacel Token？
                        //ThreadManager.GetInstance().RunAsync(exeSimplify, onComplete);
                    }
                }
            }
        }

        public void ExeTerrainSimplify(int tileIdxX, int tileIdxY, TerrainSimplifier terrainSimplifier, float simplifyTarget)
        {
            int curLOD = tileList[tileIdxX, tileIdxY].curLODLevel;
            tileList[tileIdxX, tileIdxY].ExeTerrainSimplify(curLOD, terrainSimplifier, simplifyTarget);
        }

        public int GetClusterCurVertNum()
        {
            int vertNum = 0;
            int maxLODLevel = terSet.LODLevel - 1;
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    for (int lodLevel = 0; lodLevel < terSet.LODLevel - 1; lodLevel++)
                    {
                        vertNum += tileList[i, j].GetLODMeshVertNum(lodLevel);
                    }
                }
            }
            return vertNum;
        }

        public int GetTargetSimplifyVertNum()
        {
            int targetVertNum = 0;
            int maxLODLevel = terSet.LODLevel - 1;
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;

            int vertNumPerTile = tileList[0, 0].GetLODMeshVertNum(maxLODLevel);
            for (int i = 0; i < tileNumPerLine; i++)
            {
                for (int j = 0; j < tileNumPerLine; j++)
                {
                    for (int lodLevel = 0; lodLevel < terSet.LODLevel - 1; lodLevel++)
                    {
                        targetVertNum += (int)(vertNumPerTile * terSet.GetSimplifyTarget(lodLevel));
                    }
                }
            }
            return targetVertNum;
        }

        #endregion

        #region Runtime update terrain, fix LOD seam

        // NOTE : LODHeight 根据摄像机距离地面的高度决定地块的 lod level
        public int UpdateTerrainCluster_LODHeight(int curLODLevel, int LODLevels)
        {
            // if we switch LOD by height, then we do not need to fix the seam
            //  float FadeDistance, int LODLevels
            if (tileList == null || tileList.Count <= 0)
            {
                return -100;
            }

            if (!IsLoaded)
            {
                return -100;
            }

            if (tileList[0, 0].curLODLevel == curLODLevel && IsShowing == true)
            {
                // tile's lod do not change and is showing, so return (only if its in height switch method
                return curLODLevel;
            }

            foreach (var tile in tileList)
            {
                tile.SetMesh(tile.GetMesh(curLODLevel, 0, LODSwitchMethod.Height), mat);
            }

            // 当调用这个方法的时候，默认show statu发生了改变，除非LOD级别越界了
            if (curLODLevel >= 0 && curLODLevel < LODLevels)
            {
                IsShowing = true;
            }
            return curLODLevel;
        }

        public void UpdateTerrainCluster_LODDistance(TDList<int> fullLodLevelMap)
        {
            if (!IsLoaded)
            {
                return;
            }

            int showTileNum = 0;
            foreach (var tile in tileList)
            {
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

                for (int i = 0; i < 4; i++)
                {
                    int idxX = direction[i, 0];
                    int idxY = direction[i, 1];
                    if (fullLodLevelMap.IsValidIndex(idxX, idxY) && lodLevel > fullLodLevelMap[idxX, idxY])
                    {
                        fixSeamDirection |= (1 << i);
                    }
                }

                if (fixSeamDirection == 0 && tile.curLODLevel == lodLevel)
                {
                    // no need to fix seam, and no change LOD level, so continue
                    continue;
                }

                // 不管LOD层级有没有发生改变，都必须重新刷新mesh，因为要处理LOD接缝
                Mesh mesh = tile.GetMesh(lodLevel, fixSeamDirection, LODSwitchMethod.Distance);
                tile.SetMesh(mesh, mat);
                if (mesh != null)
                {
                    showTileNum++;
                }
            }

            //IsShowing = (showTileNum > 0);
            //DebugUtility.Log(string.Format("update successfully! handle tile num {0}", terrainTileList.GetLength(0)));
        }

        public void HideTerrainCluster()
        {
            foreach (var tileMeshData in tileList)
            {
                tileMeshData.SetMesh(null, mat);
            }
            IsShowing = false;
        }

        internal TDList<int> GetFullLODLevelMap(Vector3 cameraPos)
        {
            int tileWidth = tileList.GetLength(1);
            int tileHeight = tileList.GetLength(0);

            TDList<int> lodLevelMap = GetLODLevelMap(cameraPos);
            TDList<int> fullLodLevelMap = new TDList<int>(tileWidth + 2, tileHeight + 2);

            // copy cluster's lodlevelmap to fulllodlevelmap
            for (int i = 1; i < tileWidth + 1; i++)
            {
                for (int j = 1; j < tileHeight + 1; j++)
                {
                    fullLodLevelMap[i, j] = lodLevelMap[i - 1, j - 1];
                }
            }
            return fullLodLevelMap;
        }

        private TDList<int> GetLODLevelMap(Vector3 cameraPos)
        {
            int tileWidth = tileList.GetLength(1);
            int tileHeight = tileList.GetLength(0);
            TDList<int> lodLevelMap = new TDList<int>(tileWidth, tileHeight);

            // get every tile's LOD level
            foreach (var tileMeshData in tileList)
            {
                int x = tileMeshData.tileIdxX;
                int y = tileMeshData.tileIdxY;
                int lodLevel = tileMeshData.GetLODLevel_Distance(cameraPos);
                lodLevelMap[x, y] = lodLevel;
            }
            return lodLevelMap;
        }

        private enum GetEdgeDir
        {
            Left, Right, Up, Down
        }

        private List<int> GetEdgeLODLevel(Vector3 cameraPos, GetEdgeDir edgeDir)
        {
            int tileWidth = tileList.GetLength(1);
            int tileHeight = tileList.GetLength(0);

            // 一般而言 tileWidth = tileHeight
            List<int> lodLevels = new List<int>(4);

            if (edgeDir == GetEdgeDir.Left || edgeDir == GetEdgeDir.Right)
            {
                for (int i = 0; i < tileHeight; i++)
                {
                    lodLevels.Add(-1);
                }
            }
            else
            {
                for (int i = 0; i < tileWidth; i++)
                {
                    lodLevels.Add(-1);
                }
            }


            switch (edgeDir)
            {
                case GetEdgeDir.Left:
                    for (int i = 0; i < tileHeight; i++)
                    {
                        int lodLevel = tileList[0, i].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Right:
                    for (int i = 0; i < tileHeight; i++)
                    {
                        int lodLevel = tileList[tileWidth - 1, i].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Up:
                    for (int i = 0; i < tileWidth; i++)
                    {
                        int lodLevel = tileList[i, tileHeight - 1].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Down:
                    for (int i = 0; i < tileWidth; i++)
                    {
                        int lodLevel = tileList[i, 0].GetLODLevel_Distance(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
            }
            return lodLevels;
        }

        #endregion

        #region Serialize

        public string GetClusterInfo()
        {
            return $"{idxX},{idxY},{longitude},{latitude}";
        }

        public void WriteToBinary(BinaryWriter writer)
        {
            writer.Write(idxX);
            writer.Write(idxY);
            writer.Write(longitude);
            writer.Write(latitude);
        }

        public void ReadFromBinary(BinaryReader reader)
        {
            idxX = reader.ReadInt32();
            idxY = reader.ReadInt32();
            longitude = reader.ReadInt32();
            latitude = reader.ReadInt32();
        }

        #endregion

        #region Paint In Editor

        public List<Vector3> GetPointsInScope(int tileX, int tileY, Vector3 pos, float scope)
        {
            return tileList[tileX, tileY].GetPointsInScope(pos, scope);
        }

        public void UpdatePaintPoints(int tileX, int tileY, List<Vector3> newPoints, List<Vector2Int> pointInTileIdx)
        {
            tileList[tileX, tileY].UpdatePaintPoints(newPoints, pointInTileIdx);
        }

        #endregion

        public Vector2Int GetClusterLL()
        {
            return new Vector2Int(longitude, latitude);
        }

        public Mesh GetTileMesh(int lodLevel, int tileIdxX, int tileIdxY)
        {
            if (tileIdxX < 0 || tileIdxX >= tileList.GetLength(0) || tileIdxY < 0 || tileIdxY >= tileList.GetLength(1))
            {
                DebugUtility.LogError($"wrong index when get cluster : {tileIdxX}, {tileIdxY}", DebugPriority.Medium);
                return null;
            }
            return tileList[tileIdxX, tileIdxY].GetMesh(lodLevel, 0, LODSwitchMethod.Height);
        }

    }

}
