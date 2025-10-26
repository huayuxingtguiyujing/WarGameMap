using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
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
        public int GetLODMeshVertNum(int lodLevel) { return LODMeshes[lodLevel].GetVertNum(); }


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

        public void SetMeshData_Coroutine(int curLODLevel, TerrainSettingSO terSet, int vertexNumFix, HeightDataManager heightDataManager) {
            this.heightDataManager = heightDataManager;
            this.clusterSize = terSet.clusterSize;
            this.tileSize = terSet.tileSize;

            // can not work with multiThread
            CoroutineManager.GetInstance().RunCoroutine(_SetMeshData(curLODLevel, terSet, vertexNumFix, 65535));
        }

        private IEnumerator _SetMeshData(int curLODLevel, TerrainSettingSO terSet, int vertexNumFix, int maxTickOneFrame) {
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

            // gen triangle
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

            meshData.SetInited();
        }

        public void SetMeshData_Origin(int curLODLevel, TerrainSettingSO terSet, int vertexNumFix, HeightDataManager heightDataManager)
        {
            this.heightDataManager = heightDataManager;
            this.clusterSize = terSet.clusterSize;
            this.tileSize = terSet.tileSize;

            var setCoroutine = _SetMeshData(curLODLevel, terSet, vertexNumFix, 65535);
            while (setCoroutine.MoveNext()) { }
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

                    int a = meshData.GetIndiceInMap(cur_w, cur_h);
                    int b = meshData.GetIndiceInMap(cur_w, next_h);
                    int c = meshData.GetIndiceInMap(next_w, next_h);
                    int d = meshData.GetIndiceInMap(next_w, cur_h);

                    meshData.AddTriangle(a, b, c, i, j);
                    meshData.AddTriangle(a, c, d, i, j);

                    curGridIdx++;
                }
            }

        }


        public void SetMeshData_Copy(int dstLODLevel, TerrainSimplifier terrainSimplifier, float simplifyTarget)
        {
            // we will setMeshData of LOD max, and use it as simplify src
            int srcLODLevel = LODMeshes.Length - 1;

            // copy LOD max's raw data to dstLODLevel
            LODMeshes[dstLODLevel] = new TerrainMeshData();
            TerrainMeshData meshData = LODMeshes[dstLODLevel];
            meshData.CopyMeshData(dstLODLevel, LODMeshes[srcLODLevel]);
            meshData.SetInited();
        }


        public void ApplyRiverEffect(HeightDataManager heightDataManager, RiverDataManager riverDataManager)
        {
            //for (int i = 0; i < LODMeshes.Length; i ++)
            //{
                LODMeshes[LODMeshes.Length - 1].ApplyRiverEffect(heightDataManager, riverDataManager);
            //}
        }

        public void BuildOriginMeshWrapper()
        {
            for (int i = 0; i < LODMeshes.Length; i++)
            {
                LODMeshes[i].BuildOriginMeshWrapper();
            }
        }

        public void BuildOriginMesh()
        {
            for (int i = 0; i < LODMeshes.Length; i++)
            {
                LODMeshes[i].BuildOriginMesh();
            }
        }

        public void RecaculateNormal_Mesh()
        {
            // NOTE : 建议地块使用 normal 贴图，自动生成的normal 边界老是有问题...很不适用于减面后的地表 
            for (int i = 0; i < LODMeshes.Length; i++)
            {
                LODMeshes[i].RecaculateNormal_Mesh();
            }
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

        // This Function must be thread safe
        public void ExeTerrainSimplify(int lodLevel, TerrainSimplifier terrainSimplifier, float simplifyTarget, int iterLimit = 5) {
            
            if (lodLevel >= LODMeshes.Length || lodLevel < 0)
            {
                DebugUtility.LogError($"wrong lod level : {lodLevel}", DebugPriority.Medium);
                return;
            }

            // only update once time
            LODMeshes[lodLevel].UpdateEdgeVertInfoByOrigin();
            // LODMeshes[curLODLevel].GetMesh_LODDistance(tileIdxX, tileIdxY, 0)
            MeshWrapper meshWrapper = LODMeshes[lodLevel].GetMeshWrapper();
            int targetVertCnt = (int)(meshWrapper.GetVertNum() * simplifyTarget);

            int iterCnt = 0;
            while (meshWrapper.GetVertNum() > targetVertCnt && iterCnt ++ < iterLimit) {

                List<int> edgeVerts = LODMeshes[lodLevel].GetEdgeVertInfo();
                terrainSimplifier.InitSimplifyer(meshWrapper, edgeVerts, null, targetVertCnt, clusterStartPoint, clusterSize);

                terrainSimplifier.StartSimplify();

                // get new mesh and new edge verts
                List<int> newEdgeVerts = new List<int>();
                List<int> newOutOfMeshTris = LODMeshes[lodLevel].GetOutOfMeshTris();
                meshWrapper = terrainSimplifier.EndSimplify(ref newEdgeVerts, ref newOutOfMeshTris);

                SetMeshWrapper(lodLevel, meshWrapper, null);
                LODMeshes[lodLevel].SetEdgeVertInfo(newEdgeVerts);
                LODMeshes[lodLevel].SetOutOfMeshTris(newOutOfMeshTris);
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
                DebugUtility.LogError("wrong LOD level, can not get mesh", DebugPriority.Medium);
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
            meshFilter.sharedMesh = mesh;
#else
            meshFilter.sharedMesh = mesh;
#endif
            renderer.sharedMaterial = mat;
        }

        public void SetMeshWrapper(int lodLevel, MeshWrapper meshWrapper, Material mat) {
            LODMeshes[lodLevel].SetMeshWrapper(meshWrapper);
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
