using LZ.WarGameCommon;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    // NOTE : һ�� cluster ��Ӧһ��TIF�߶�ͼ�ļ��������˶�� TerrainTile
    public class TerrainCluster : IBinarySerializer {

        public int idxX { get; private set; }
        public int idxY { get; private set; }

        public int longitude { get; private set; }
        public int latitude { get; private set; }

        public bool IsValid { get; private set; }


        TerrainSetting terSet;
        Vector3 clusterStartPoint;

        private TDList<TerrainTile> tileList;
        
        public TDList<TerrainTile> TileList { get { return tileList; } }

        public GameObject clusterGo { get; private set; }

        public TerrainCluster() { IsValid = false; }

        public void InitTerrainCluster_Static(int idxX, int idxY, int longitude, int latitude, TerrainSetting terSet, HeightDataManager heightDataManager, GameObject clusterGo) {
            this.idxX = idxX;
            this.idxY = idxY;

            this.longitude = longitude;
            this.latitude = latitude;

            this.terSet = terSet;

            this.clusterGo = clusterGo;

            clusterStartPoint = new Vector3(terSet.clusterSize * idxX, 0, terSet.clusterSize * idxY);
            _InitTerrainCluster_Static(heightDataManager);

            IsValid = true;
        }

        private void _InitTerrainCluster_Static(HeightDataManager heightDataManager) {
            int tileNumPerLine = terSet.clusterSize / terSet.tileSize;

            //Debug.Log(string.Format("the cluster size : {0}x{1}, because the size of cluster is {2}, so there are {3} tiles in a row", 
            //    terrainSize.x, terrainSize.z, tileSize, tileNumPerLine));

            tileList = new TDList<TerrainTile>(tileNumPerLine, tileNumPerLine);
            int[] lodLevels = new int[terSet.LODLevel];
            for (int i = 0; i < terSet.LODLevel; i++) {
                lodLevels[i] = i;
            }

            for (int i = 0; i < tileNumPerLine; i++) {
                for (int j = 0; j < tileNumPerLine; j++) {
                    MeshFilter meshFilter = CreateTerrainTile(i, j);
                    tileList[i, j].InitTileMeshData(i, j, longitude, latitude, clusterStartPoint, meshFilter, lodLevels);
                }
            }

            // generate mesh data for every LOD level
            int curLODLevel = terSet.LODLevel - 1;
            while (curLODLevel >= 0) {
                // when num fix == 1, the vert num per line is equal to tileSize
                // int vertexNumFix = (int)Mathf.Pow(2, (terSet.LODLevel - curLODLevel - 1));

                // NOTE : ���ھ�̬����ʱ��ʹ��һ���� vertex ������128 ��ԭ���� LOD1 ���� ��������������� 2������ 2��
                int vertexNumFix = 2;

                if (vertexNumFix > terSet.tileSize) {
                    // wrong
                    break;
                }

                for (int i = 0; i < tileNumPerLine; i++) {
                    for (int j = 0; j < tileNumPerLine; j++) {
                        tileList[i, j].SetMeshData(curLODLevel, terSet, vertexNumFix, heightDataManager);
                    }
                }

                curLODLevel--;
            }

            Debug.Log(string.Format("successfully generate terrain tiles! "));
        }

        private MeshFilter CreateTerrainTile(int idxX, int idxY) {
            GameObject tileGo = new GameObject();
            tileGo.transform.parent = clusterGo.transform;
            tileGo.name = string.Format("heightTile_{0}_{1}", idxX, idxY);

            MeshFilter meshFilter = tileGo.AddComponent<MeshFilter>();
            tileGo.AddComponent<MeshRenderer>();
            return meshFilter;
        }

        public void ExeTerrainSimplify(int tileIdxX, int tileIdxY, TerrainSimplifier terrainSimplifier, float simplifyTarget) {

            // NOTE : ��ֻ��һ���ؿ��
            tileList[tileIdxX, tileIdxY].ExeTerrainSimplify(tileIdxX, tileIdxY, terrainSimplifier, simplifyTarget);;
            
            // TOOD : Ҫ��
            for (int i = 0; i < tileList.GetLength(1); i++) {
                for (int j = 0; j < tileList.GetLength(0); j++) {
                    //tileList[i, j].ExeTerrainSimplify(i, j, terrainSimplifier, simplifyTarget);
                }
            }
        }

        public void SampleMeshNormal(Texture2D normalTex) {

            for (int i = 0; i < tileList.GetLength(1); i++) {
                for (int j = 0; j < tileList.GetLength(0); j++) {
                    tileList[i, j].SampleMeshNormal();
                }
            }
        }

        #region update terrain ; fix LOD seam

        public void UpdateTerrainCluster(TDList<int> fullLodLevelMap) {

            foreach (var tileMeshData in tileList) {
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

                // ����LOD�㼶��û�з����ı䣬����������ˢ��mesh����ΪҪ����LOD�ӷ�
                Mesh mesh = tileMeshData.GetMesh(lodLevel, fixSeamDirection);
                tileMeshData.SetMesh(mesh);
            }
            //Debug.Log(string.Format("update successfully! handle tile num {0}", terrainTileList.GetLength(0)));
        }


        internal TDList<int> GetFullLODLevelMap(Vector3 cameraPos, TerrainCluster left, TerrainCluster right, TerrainCluster up, TerrainCluster down) {
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

            // copy egde's lodlevelmap to fulllodlevelmap. careful for the sequence
            //List<int> leftLODLevel = new List<int>() { -1, -1, -1, -1 };
            //if (left != null) {
            //    leftLODLevel = left.GetEdgeLODLevel(cameraPos, GetEdgeDir.Right);
            //}
            //for (int i = 0; i < tileHeight; i++) {
            //    fullLodLevelMap[0, i + 1] = leftLODLevel[i];
            //}

            //List<int> rightLODLevel = new List<int>() { -1, -1, -1, -1 };
            //if (right != null) {
            //    rightLODLevel = right.GetEdgeLODLevel(cameraPos, GetEdgeDir.Left);
            //}
            //for (int i = 0; i < tileHeight; i++) {
            //    fullLodLevelMap[tileHeight + 1, i + 1] = rightLODLevel[i];
            //}

            //List<int> upLODLevel = new List<int>() { -1, -1, -1, -1 };
            //if (up != null) {
            //    upLODLevel = up.GetEdgeLODLevel(cameraPos, GetEdgeDir.Down);
            //}
            //for (int i = 0; i < tileWidth; i++) {
            //    fullLodLevelMap[i + 1, tileWidth + 1] = upLODLevel[i];
            //}

            //List<int> downLODLevel = new List<int>() { -1, -1, -1, -1 };
            //if (down != null) {
            //    downLODLevel = down.GetEdgeLODLevel(cameraPos, GetEdgeDir.Up);
            //}
            //for (int i = 0; i < tileWidth; i++) {
            //    fullLodLevelMap[i + 1, 0] = downLODLevel[i];
            //}

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
                int lodLevel = tileMeshData.GetRefinedLODLevel(cameraPos);
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

            // һ����� tileWidth = tileHeight
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
                        int lodLevel = tileList[0, i].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Right:
                    for (int i = 0; i < tileHeight; i++) {
                        int lodLevel = tileList[tileWidth - 1, i].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Up:
                    for (int i = 0; i < tileWidth; i++) {
                        int lodLevel = tileList[i, tileHeight - 1].GetRefinedLODLevel(cameraPos);
                        lodLevels[i] = lodLevel;
                    }
                    break;
                case GetEdgeDir.Down:
                    for (int i = 0; i < tileWidth; i++) {
                        int lodLevel = tileList[i, 0].GetRefinedLODLevel(cameraPos);
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
    }


    public class TerrainTile : IBinarySerializer {

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

        public TerrainTile() { }

        public void InitTileMeshData(int idxX, int idxY, int longitude, int latitude, Vector3 startPoint, MeshFilter meshFilter, int[] lODLevels) {
            this.clusterStartPoint = startPoint;
            this.meshFilter = meshFilter;

            tileIdxX = idxX;
            tileIdxY = idxY;

            this.longitude = longitude;
            this.latitude = latitude;

            curLODLevel = -1;       // init as -1

            LODLevels = lODLevels;
            LODMeshes = new TerrainMeshData[lODLevels.Length];
        }

        // JobSystem
        // TODO : !


        public void SetMeshData(int curLODLevel, TerrainSetting terSet, int vertexNumFix, HeightDataManager heightDataManager) {
            this.heightDataManager = heightDataManager;
            this.clusterSize = terSet.clusterSize;
            this.tileSize = terSet.tileSize;

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

            LODMeshes[curLODLevel] = new TerrainMeshData();
            TerrainMeshData meshData = LODMeshes[curLODLevel];
            meshData.InitMeshData(gridNumPerLine, gridNumPerLineFixed, vertexPerLine, vertexPerLineFixed);

            int curInVertIdx = 0;
            int curOutVertIdx = -1;
            //int[,] vertexIndiceMap = new int[vertexPerLineFixed, vertexPerLineFixed];

            // TODO: use job system
            Vector3 offsetInMeshVert = new Vector3(gridSize, 0, gridSize);
            for (int i = 0; i < vertexPerLineFixed; i++) {
                for (int j = 0; j < vertexPerLineFixed; j++) {
                    bool isVertOutOfMesh = (i == 0) || (i == vertexPerLineFixed - 1) || (j == 0) || (j == vertexPerLineFixed - 1);

                    Vector3 vert = new Vector3(gridSize * i, 0, gridSize * j) + startPoint - offsetInMeshVert;

                    // NOTE : ����Ĵ��벻��ɾ��ǧ����ɾ��
                    //float height = SampleFromHeightData(terrainClusterSize, vert);
                    float height = heightDataManager.SampleFromHeightData(longitude, latitude, vert, clusterStartPoint);
                    //float height = heightDataManager.SampleFromHexMap(vert);
                    //float height = 0;

                    vert.y = height;
                    Vector2 uv = new Vector2(vert.x / terSet.clusterSize, vert.z / terSet.clusterSize);

                    if (isVertOutOfMesh) {
                        meshData.AddVertex(vert, uv, curOutVertIdx);
                        meshData.SetIndiceInMap(i, j, curOutVertIdx);
                        //vertexIndiceMap[i, j] = curOutVertIdx;
                        curOutVertIdx--;
                    } else {
                        meshData.AddVertex(vert, uv, curInVertIdx);
                        meshData.SetIndiceInMap(i, j, curInVertIdx);
                        //vertexIndiceMap[i, j] = curInVertIdx;
                        curInVertIdx++;
                    }
                }
            }

            int curGridIdx = 0;
            for (int i = 0; i < gridNumPerLineFixed; i++) {
                for (int j = 0; j < gridNumPerLineFixed; j++) {
                    // i, j �ǵ�ǰ�������� grid �� index
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
            SetMesh(curMesh);
        }

        public void ExeTerrainSimplify(int tileIdxX, int tileIdxY, TerrainSimplifier terrainSimplifier, float simplifyTarget) {
            int length = LODMeshes.Length;

            // TODO : Ϊ�˱�֤ʱ�䣬����ʱֻ����һ���ؿ�� ��idx : 1��
            List<int> edgeVerts = new List<int>();
            List<Vector3> edgeNormals = new List<Vector3>();
            LODMeshes[1].GetEdgeVertInfo(ref edgeVerts, ref edgeNormals);

            terrainSimplifier.InitSimplifyer(
                LODMeshes[1].GetMesh(tileIdxX, tileIdxY, 0),
                edgeVerts, edgeNormals, simplifyTarget, clusterStartPoint, clusterSize
            );

            terrainSimplifier.StartSimplify();
            SetMesh(terrainSimplifier.EndSimplify());


            // TODO : Ҫ��
            for (int i = 0; i < length; i++) {

                //terrainSimplifier.InitSimplifyer(
                //    LODMeshes[i].GetMesh(tileIdxX, tileIdxY, 0), 
                //    LODMeshes[i].GetCanNotDelVerts(), simplifyTarget
                //);

                //terrainSimplifier.StartSimplify();
                //terrainSimplifier.EndSimplify();
            }
        }

        // NOTE : ���ڷ���ԭ�ȵ� LOD ������
        public int GetRefinedLODLevel(Vector3 cameraPos) {
            return 1;

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
