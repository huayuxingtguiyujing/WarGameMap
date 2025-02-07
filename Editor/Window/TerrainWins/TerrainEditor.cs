
using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

namespace LZ.WarGameMap.MapEditor
{
    
    public class TerrainEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.TerrainEditor;

        GameObject mapRootObj;
        GameObject heightMeshParentObj;
        GameObject heightSignParentObj;

        TerrainConstructor TerrainCtor;

        [FoldoutGroup("配置scene", -1)]
        [AssetSelector(Filter = "t:Prefab")]
        public GameObject SignPrefab;

        [FoldoutGroup("配置scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: 没有初始化Scene")]
        public string warningMessage = "请点击按钮初始化!";

        [FoldoutGroup("配置scene", -1)]
        [Button("初始化地形配置")]
        protected override void InitEditor() {
            if (mapRootObj == null) {
                mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
                if (mapRootObj == null) {
                    mapRootObj = new GameObject(MapSceneEnum.MapRootName);
                }
            }
            if (heightMeshParentObj == null) {
                heightMeshParentObj = GameObject.Find(MapSceneEnum.HeightParentName);
                if (heightMeshParentObj == null) {
                    heightMeshParentObj = new GameObject(MapSceneEnum.HeightParentName);
                }
            }
            heightMeshParentObj.transform.SetParent(mapRootObj.transform);

            if (heightSignParentObj == null) {
                heightSignParentObj = GameObject.Find(MapSceneEnum.HeightSignParentName);
                if (heightSignParentObj == null) {
                    heightSignParentObj = new GameObject(MapSceneEnum.HeightSignParentName);
                }
            }
            heightSignParentObj.transform.SetParent(mapRootObj.transform);


            TerrainCtor = mapRootObj.GetComponent<TerrainConstructor>();
            if (TerrainCtor == null) {
                TerrainCtor = mapRootObj.AddComponent<TerrainConstructor>();
            }

            TerrainCtor.SetMapPrefab(mapRootObj.transform, heightMeshParentObj.transform, heightSignParentObj.transform, SignPrefab);
            notInitScene = false;
        }
        

        [FoldoutGroup("构建地形")]
        [LabelText("LOD总层级数")]
        public int LODLevel = 5;

        [FoldoutGroup("构建地形")]
        [LabelText("Terrain大小")]
        [Tooltip("大地图规模，表示共有多少个cluster，它不必是2的倍数")]
        public Vector3Int terrainSize = new Vector3Int(10, 0, 10);

        [FoldoutGroup("构建地形")]
        [LabelText("cluster大小")]
        [Tooltip("cluster规模，y轴代表对高度数据的放大操作")]
        public Vector3Int clusterSize = MapTerrainEnum.ClusterSize;

        [FoldoutGroup("构建地形")]
        [LabelText("地块大小")]
        public int tileSize = MapTerrainEnum.TileSize;

        [FoldoutGroup("构建地形")]
        [Button("初始化地形", ButtonSizes.Medium)]
        private void GenerateTerrain() { 

            int tileNumARow = clusterSize.x / tileSize;
            Debug.Log($"the map size : {terrainSize.x} * {terrainSize.z}");
            Debug.Log(string.Format("the cluster size : {0}x{1}, because the size of tile is {2}, so there are {3} tiles in a row", terrainSize.x, terrainSize.z, tileSize, tileNumARow));

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.InitHeightCons(terrainSize, clusterSize, tileSize, LODLevel, heightDataModels);
        }

        [FoldoutGroup("构建地形")]
        [Button("更新地形", ButtonSizes.Medium)]
        private void ShowTerrain() {

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            TerrainCtor.UpdateTerrain();

        }

        [FoldoutGroup("构建地形")]
        [Button("清空地形", ButtonSizes.Medium)]
        private void ClearHeightMesh() {
            if (TerrainCtor == null) {
                Debug.LogError("do not init height ctor!");
                return;
            }

            TerrainCtor.ClearHeightObj();
        }

        [FoldoutGroup("构建地形")]
        [Button("清空标记", ButtonSizes.Medium)]
        private void ClearSigns() {
            if (TerrainCtor == null) {
                Debug.LogError("do not init height ctor!");
                return;
            }

            TerrainCtor.ClearSignObj();
        }


        [FoldoutGroup("懒构建地形")]
        [LabelText("当前操作的cluster索引")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前操作的cluster索引")]
        public Vector2Int clusterIdx;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前使用的经度")]
        public int longitude;

        [FoldoutGroup("懒构建地形")]
        [LabelText("当前使用的纬度")]
        public int latitude;

        [FoldoutGroup("懒构建地形")]
        [Button("构建对应的clusterMesh", ButtonSizes.Medium)]
        private void BuildCluster() {
            if (heightDataModels == null) {
                Debug.LogError("you do not set the heightDataModel");
                return;
            }

            if (TerrainCtor == null) {
                Debug.LogError("terrian ctor is null!");
                return;
            }

            foreach (var model in heightDataModels)
            {
                if (model.ExistHeightData(longitude, latitude)) {
                    //HeightData heightData = model.GetHeightData(longitude, latitude);
                    TerrainCtor.BuildCluster(clusterIdx.x, clusterIdx.y, longitude, latitude);
                    return;
                }
            }

            Debug.LogError($"unable to find heightdata, longitude : {longitude}, latitude : {latitude}");

        }


        // 高度图导入，不要删除
        /*public enum Depth { Bit8 = 1, Bit16 = 2 }
        public enum ByteOrder { Mac = 1, Windows = 2 }
        public enum HeightMapType {
            TIF, RAW
        }
        [FoldoutGroup("高度图导入/导入设置")]
        [LabelText("高度图格式")]
        public HeightMapType heightMapType;
        [FoldoutGroup("高度图导入/导入设置")]
        [LabelText("DepthMode")]
        public Depth depthMode = Depth.Bit16;
        [FoldoutGroup("高度图导入/导入设置")]
        [LabelText("ByteOrder")]
        public ByteOrder byteOrder = ByteOrder.Windows;
        [FoldoutGroup("高度图导入/导入设置")]
        [LabelText("分辨率")]
        public int resolution = 1;
        [FoldoutGroup("高度图导入/导入设置")]
        [LabelText("导入时翻转")]
        public bool flipVertically = false;
        [FoldoutGroup("高度图导入/导入设置")]
        [AssetSelector(Filter = "t:Material")]
        public Material material;
        [FoldoutGroup("高度图导入")]
        [LabelText("当前使用的高度图")]
        [AssetSelector(Filter = "t:Texture")]
        public UnityEngine.Object heightMap;
        
        [FoldoutGroup("高度图导入")]
        [LabelText("当前操作的地块索引")]
        public Vector2Int curHandleTileIdx = new Vector2Int(0, 0);

        [FoldoutGroup("高度图导入")]
        [LabelText("压缩后高度图分辨率")]
        public int compressResultSize = 64;

        private string heightMapPath;
        private float[,] heightMapDatas;

        [FoldoutGroup("高度图导入")]
        [Button("导入高度图")]
        private void ImportHeightMapData() {
            
            heightMapPath = EditorUtility.OpenFilePanel("Import Raw Heightmap", "", "raw,tif");
            if (heightMapPath == "") {
                Debug.LogError("you do not get the height map");
                return;
            }

            string relativePath = "Assets" + heightMapPath.Substring(Application.dataPath.Length);
            heightMap = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
            if (heightMap == null) {
                Debug.LogError(string.Format("can not load texture resource from this path: {0}", relativePath));
                return;
            }
            string fileExtension = Path.GetExtension(relativePath).ToLower();
            switch (fileExtension) {
                case ".raw":
                    heightMapType = HeightMapType.RAW;
                    break;
                case ".tif":
                    heightMapType = HeightMapType.TIF;
                    break;
                default: 
                    break;
            }

            // set cfg by the file path (NOTE: only set for .raw file)
            FileStream file = File.Open(heightMapPath, FileMode.Open, FileAccess.Read);
            int fileSize = (int)file.Length;
            file.Close();
            
            depthMode = Depth.Bit16;
            int pixels = fileSize / (int)depthMode;
            int res = Mathf.RoundToInt(Mathf.Sqrt(pixels));
            if ((res * res * (int)depthMode) == fileSize) {
                resolution = res;
                return;
            }
            depthMode = Depth.Bit8;
            pixels = fileSize / (int)depthMode;
            res = Mathf.RoundToInt(Mathf.Sqrt(pixels));
            if ((res * res * (int)depthMode) == fileSize) {
                resolution = res;
                return;
            }
            depthMode = Depth.Bit16;
        }

        [FoldoutGroup("高度图导入")]
        [Button("压缩高度图")]
        private void CompressHeightData() {

            float[,] heights = ReadHeightMapData();
            if(heights == null ) {
                Debug.Log("error, do not read height data successfully!");
                return;
            }

            int srcWsidth = heights.GetLength(0);
            int dstHeight = heights.GetLength(1);
            heightMapDatas = new float[compressResultSize, compressResultSize];

            // resample the size of height map
            for (int i = 0; i < compressResultSize; i++) {
                for (int j = 0; j < compressResultSize; j++) {

                    float sx = i * (float)(srcWsidth - 1) / compressResultSize;
                    float sy = j * (float)(dstHeight - 1) / compressResultSize;

                    int x0 = Mathf.FloorToInt(sx);
                    int x1 = Mathf.Min(x0 + 1, srcWsidth - 1);
                    int y0 = Mathf.FloorToInt(sy);
                    int y1 = Mathf.Min(y0 + 1, dstHeight - 1);

                    float q00 = heights[x0, y0];
                    float q01 = heights[x0, y1];
                    float q10 = heights[x1, y0];
                    float q11 = heights[x1, y1];

                    float rx0 = Mathf.Lerp(q00, q10, sx - x0);
                    float rx1 = Mathf.Lerp(q01, q11, sx - x0);

                    // caculate the height by the data given
                    float h = Mathf.Lerp(rx0, rx1, sy - y0);
                    float fixed_h = Mathf.Clamp(h, 0, 50);
                    heightMapDatas[i, j] = fixed_h;
                }
            }

            Debug.Log("compress over, now you can use the data to generate terrain!");
        }

        private float[,] ReadHeightMapData() {
            if (heightMapPath == "") {
                Debug.LogError("you do not get the height map");
                return null;
            }

            float[,] heights;
            switch (heightMapType) {
                case HeightMapType.TIF:
                    heights = ReadTifData(heightMapPath);
                    break;
                case HeightMapType.RAW:
                    heights = ReadRawData(heightMapPath);
                    break;
                default:
                    heights = new float[1, 1];
                    break;
            }

            Debug.Log(string.Format("read height data over, length: {0}, path: {1}", heights.Length, heightMapPath));
            //TerrainCtor.SetHeights(curHandleTileIdx.x, curHandleTileIdx.y, heights);

            return heights;
        }

        private float[,] ReadRawData(string path) {
            // Read data
            byte[] data;
            using (BinaryReader br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))) {
                data = br.ReadBytes(resolution * resolution * (int)depthMode);
                br.Close();
            }

            float[,] heights = new float[resolution, resolution];
            if (depthMode == Depth.Bit16) {
                float normalize = 1.0F / (1 << 16);
                for (int y = 0; y < resolution; ++y) {
                    for (int x = 0; x < resolution; ++x) {
                        int index = Mathf.Clamp(x, 0, resolution - 1) + Mathf.Clamp(y, 0, resolution - 1) * resolution;
                        ushort compressedHeight = System.BitConverter.ToUInt16(data, index * 2);

                        float height = compressedHeight * normalize;
                        int destY = flipVertically ? resolution - 1 - y : y;
                        heights[destY, x] = height;
                    }
                }
            } else {
                float normalize = 1.0F / (1 << 8);
                for (int y = 0; y < resolution; ++y) {
                    for (int x = 0; x < resolution; ++x) {
                        int index = Mathf.Clamp(x, 0, resolution - 1) + Mathf.Clamp(y, 0, resolution - 1) * resolution;
                        byte compressedHeight = data[index];

                        float height = compressedHeight * normalize;
                        int destY = flipVertically ? resolution - 1 - y : y;
                        heights[destY, x] = height;
                    }
                }
            }

            //Debug.Log("read the height file over: " + heights.Length);
            return heights;
        }

        private float[,] ReadTifData(string path) {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(path);
            int width = bitmap.Width;
            int height = bitmap.Height;

            float[,] heights = new float[width, height];
            for (int x = 0; x < height; x++) {
                for (int y = 0; y < width; y++) {
                    int destY = flipVertically ? width - 1 - y : y;
                    int destX = flipVertically ? height - 1 - x : x;
                    System.Drawing.Color pixelColor = bitmap.GetPixel(y, destX);

                    // grapScale = 0.299 * R + 0.587 * G + 0.114 * B
                    float grayscale = 0.299f * pixelColor.R + 0.587f * pixelColor.G + 0.114f * pixelColor.B;
                    heights[x, y] = grayscale / 255.0f;
                }
            }

            Debug.Log(string.Format("read tif file over! length:{0}, width:{1}, height:{2}", heights.Length, width, height));
            bitmap.Dispose();
            return heights;
        }
*/



        /*[FoldoutGroup("构建地形")]
        [LabelText("地块数目宽度")]
        public int tileNumWidth = 10;

        [FoldoutGroup("构建地形")]
        [LabelText("地块数目高度")]
        public int tileNumHeight = 10;

        [FoldoutGroup("构建地形")]
        [LabelText("地块宽度")]
        public int tileWidth = 100;

        [FoldoutGroup("构建地形")]
        [LabelText("地块高度")]
        public int tileHeight = 100;

        [FoldoutGroup("构建地形")]
        [LabelText("网格大小")]
        public int gridSize = 1;

        [FoldoutGroup("构建地形")]
        [Button("生成地形网格")]
        private void CreateHeightMesh() {
            if(TerrainCtor == null) {
                Debug.LogError("do not init height ctor!");
                return;
            }

            //TerrainCtor.InitHeightCons(tileNumWidth, tileNumHeight, tileWidth, tileHeight, gridSize);
        }*/
        /*
                [FoldoutGroup("构建地形")]
                [Button("清空地形")]
                private void ClearHeightMesh() {
                    if (TerrainCtor == null) {
                        Debug.LogError("do not init height ctor!");
                        return;
                    }

                    TerrainCtor.ClearHeightObj();
                }

                [FoldoutGroup("构建地形")]
                [Button("清空标记")]
                private void ClearSigns() {
                    if (TerrainCtor == null) {
                        Debug.LogError("do not init height ctor!");
                        return;
                    }

                    TerrainCtor.ClearSignObj();
                }
        */
        /*[FoldoutGroup("Terrain数据预构建")]
        [LabelText("terrain尺寸")]
        public int terrainSize = 512;

        [FoldoutGroup("Terrain数据预构建")]
        [LabelText("terrain尺寸单位")]
        public int terrainGridSize = 1;

        [FoldoutGroup("Terrain数据预构建")]
        [LabelText("四叉树深度")]
        [Tooltip("四叉树深度决定了构建地形时的划分状况，最大的深度 = sqrt(size, 4)")]
        public int quadTreeLevel = 4;

        [FoldoutGroup("Terrain数据预构建")]
        [AssetSelector(Filter = "t:Material")]
        [LabelText("地形材质")]
        public Material terrainMaterial;

        //[FoldoutGroup("Terrain数据预构建")]
        //[LabelText("当前查看Mesh的Level")]
        //public int showMeshLevel = 1;

        [FoldoutGroup("Terrain数据预构建")]
        [LabelText("mesh存储路径")]
        public const string meshStorePath = MapStoreEnum.TerrainMeshPath;

        private Mesh[] meshes;

        private const int meshVertSize = 8;

        [FoldoutGroup("Terrain数据预构建")]
        [Button("构建地形网格数据", ButtonSizes.Medium)]
        private void BakeTerrainMesh() {
            BakeTerrainMesh(terrainSize, terrainSize, quadTreeLevel, terrainGridSize);
        }

        private void BakeTerrainMesh(int terrainWidth, int terrainHeight, int quadTreeLevel, int gridSize) {
            Func<int, bool> isPowerOfTwo = (x) => {
                return (x > 0) && (x & (x - 1)) == 0;
            };
            if (!isPowerOfTwo(terrainWidth) || !isPowerOfTwo(terrainHeight)) {
                Debug.LogError("wrong width or height, you should input power of 2");
                return;
            }
            if (terrainWidth != terrainHeight) {
                Debug.LogError("unsuggest tile width and height");
                return;
            }

            // caculate how much meshes should we generate
            int meshNums = 0;
            int levelMeshNums = 1;
            int rec = quadTreeLevel;
            while (rec > 0) {
                meshNums += levelMeshNums;
                levelMeshNums *= 4;
                rec--;
            }
            meshes = new Mesh[meshNums];

            // tile's size in current LOD level
            // NOTE: LOD0 tile size: 8m * 8m; the LOD0 has 81 vertexs
            int tileSize = terrainWidth;
            int tileGridSize = tileSize / 8;

            int curTileIdx = 0;
            int curLevel = quadTreeLevel - 1;
            int curLevelNodeNum = 1;
            while (curLevel >= 0) {
                int curLevelWH = (int)Mathf.Sqrt(curLevelNodeNum);
                for (int i = 0; i < curLevelWH; i++) {
                    for (int j = 0; j < curLevelWH; j++) {
                        Mesh mesh = CreateTerrainCluster(i, j, curTileIdx, tileSize, tileGridSize, curLevel, terrainWidth);
                        meshes[curTileIdx] = mesh;
                        curTileIdx++;
                    }
                }

                tileGridSize /= 2;
                tileSize /= 2;
                curLevelNodeNum *= 4;
                curLevel--;
            }

            Debug.Log(string.Format("bake mesh successfully! quad level: {0}, mesh nums: {1}", quadTreeLevel, meshNums));

        }

        private Mesh CreateTerrainCluster(int i, int j, int curTileIdx, int tileSize, int tileGridSize, int curLevel, int terrainSize) {

            // this code will build a basic vertex mesh
            Vector3 tileStartOffset = new Vector3(tileSize * i, 0, tileSize * j);

            Mesh mesh = new Mesh();
            mesh.name = GetMeshName(curLevel, curTileIdx);
            Vector3[] vertexs = new Vector3[81];
            Vector3[] normals = new Vector3[81];
            Vector2[] uvs = new Vector2[81];
            int[] triangles = new int[128 * 3];

            // set vertexs
            for (int q = 0; q <= meshVertSize; q++) {
                for (int p = 0; p <= meshVertSize; p++) {
                    int vertIdx = q * 9 + p;
                    vertexs[vertIdx] = new Vector3(q * tileGridSize, 0, p * tileGridSize) + tileStartOffset;
                    normals[vertIdx] = new Vector3(0, 1, 0);
                    uvs[vertIdx] = new Vector2(
                        (q * tileGridSize + tileStartOffset.x) / terrainSize,
                        (p * tileGridSize + tileStartOffset.z) / terrainSize
                    );

                    //CreateSignObj(vertexs[vertIdx]);
                }
            }

            //MapHeightMapToMesh(ref vertexs, terrainSize, );

            // set triangles
            int curTriGridIdx = 0;
            for (int idx = 0; idx < triangles.Length; idx += 24) {
                int cur_w = curTriGridIdx % 4 * 2;
                int cur_h = curTriGridIdx / 4 * 2;
                int second_h = cur_h + 1;
                int third_h = cur_h + 2;

                int leftDown = cur_h * 9 + cur_w;
                int down = cur_h * 9 + cur_w + 1;
                int rightDown = cur_h * 9 + cur_w + 2;

                int left = second_h * 9 + cur_w;
                int center = second_h * 9 + cur_w + 1;
                int right = second_h * 9 + cur_w + 2;

                int leftUp = third_h * 9 + cur_w;
                int up = third_h * 9 + cur_w + 1;
                int rightUp = third_h * 9 + cur_w + 2;

                triangles[idx] = center; triangles[idx + 1] = left; triangles[idx + 2] =  leftDown;
                triangles[idx + 3] = center; triangles[idx + 4] = leftDown; triangles[idx + 5] =  down;
                triangles[idx + 6] = center; triangles[idx + 7] = down; triangles[idx + 8] =  rightDown;
                triangles[idx + 9] = center; triangles[idx + 10] = rightDown; triangles[idx + 11] =  right;

                triangles[idx + 12] = center; triangles[idx + 13] = leftUp; triangles[idx + 14] =  left;
                triangles[idx + 15] = center; triangles[idx + 16] = up; triangles[idx + 17] =  leftUp;
                triangles[idx + 18] = center; triangles[idx + 19] = rightUp; triangles[idx + 20] =  up;
                triangles[idx + 21] = center; triangles[idx + 22] = right; triangles[idx + 23] =  rightUp;

                curTriGridIdx++;
            }

            mesh.vertices = vertexs;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.uv2 = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        [FoldoutGroup("Terrain数据预构建")]
        [Button("导入高度数据到网格", ButtonSizes.Medium)]
        private void ImportHeightDataToMesh() {
            if (heightMapPath == "") {
                Debug.LogError("you do not get the height map");
                return;
            }

            if (meshes.Length <= 0) {
                Debug.LogError("you do not establish the terrain mesh");
                return;
            }

            float[,] heights;
            switch (heightMapType) {
                case HeightMapType.TIF:
                    heights = ReadTifData(heightMapPath);
                    break;
                case HeightMapType.RAW:
                    heights = ReadRawData(heightMapPath);
                    break;
                default:
                    heights = new float[1, 1];
                    break;
            }

            foreach (var mesh in meshes) {
                Vector3[] vertexs = mesh.vertices;
                MapHeightMapToMesh(ref vertexs, terrainSize, heights);
                SetClusterMesh(mesh, vertexs);
            }

            Debug.Log(string.Format("successfully set height data to the terrain meshs, mesh num: {0}, height length: {1}", meshes.Length, heights.Length));

            //Debug.Log(string.Format("read height data over, length: {0}, path: {1}", heights.Length, heightMapPath));
            //HeightCtor.SetHeights(curHandleTileIdx.x, curHandleTileIdx.y, heights);
        }

        public void MapHeightMapToMesh(ref Vector3[] vertices, float terrainSize, float[,] heightMap) {
            
            // this function will mapping height map data to the terrain mesh data
            int heightMapWidth = heightMap.GetLength(0);
            int heightMapHeight = heightMap.GetLength(1);

            for (int i = 0; i < vertices.Length; i++) {
                Vector3 vertex = vertices[i];
                // normalize the coord and covert to pixel coord in height map
                float normalizedX = vertex.x / terrainSize;
                float normalizedZ = vertex.z / terrainSize;
                int heightMapX = Mathf.Clamp(Mathf.RoundToInt(normalizedX * (heightMapWidth - 1)), 0, heightMapWidth - 1);
                int heightMapY = Mathf.Clamp(Mathf.RoundToInt(normalizedZ * (heightMapHeight - 1)), 0, heightMapHeight - 1);

                float height = heightMap[heightMapX, heightMapY] * 2000;
                vertices[i] = new Vector3(vertex.x, height, vertex.z);
            }
        }

        public void SetClusterMesh(Mesh mesh, Vector3[] vertexs) {
            mesh.vertices = vertexs;
            mesh.RecalculateNormals();
            
        }

        [FoldoutGroup("Terrain数据预构建")]
        [Button("展示地形网格数据", ButtonSizes.Medium)]
        private void ShowTerrainMesh() {
            //showMeshLevel;
            if (meshes.Length <= 1) {
                Debug.LogError("you do not create the mesh");
                return;
            }

            //TerrainCtor.InitHeightCons(meshes, quadTreeLevel, terrainMaterial);
        }
        [FoldoutGroup("Terrain数据预构建")]
        [Button("序列化网格资产")]
        private void SerializeMesh() { }
        [FoldoutGroup("Terrain数据预构建")]
        [Button("保存网格资产")]
        private void SaveAllMesh() {
            // TODO: unsuggested save way, you should serialize the mesh data to one file
            for (int i = 0; i < meshes.Length; i++) {
                SaveTileMesh(meshes[i]);
            }

            Debug.Log(string.Format("save over!, path: {0}, mesh nums: {1}", MapStoreEnum.TerrainMeshPath, meshes.Length));
        }
        private void LoadTileMesh(string meshName) {
            string path = MapStoreEnum.TerrainMeshPath + meshName;
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshName);
        }
        private void SaveTileMesh(Mesh mesh) {
            string path = MapStoreEnum.TerrainMeshPath + mesh.name + ".asset";
            AssetDatabase.CreateAsset(mesh, path);
        }
        private string GetMeshName(int curLevel, int curTileIdx) {
            return string.Format("TerrainMesh_LOD{0}_Idx{1}", curLevel, curTileIdx);
        }
        private void CreateSignObj(Vector3 pos) {
            GameObject go = Instantiate(SignPrefab, heightSignParentObj.transform);
            go.transform.position = pos;
        }
*/


    }

}
