using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Directory = System.IO.Directory;

namespace LZ.WarGameMap.MapEditor
{

    public enum HeightMapWorkFlow
    {
        TIF,            // TIF工作流：          使用地理信息网站下载的TIF文件，生成 HeightDataModel，用于地形生成
        GridTerrain     // GridTerrain工作流：  类似文明的六边形格子地图，编辑格子地形，格子地形导出纹理，用于生成 HeightDataModel
    }

    public struct SerializedHeightMapInfo
    {
        public int latitude { get; private set; }
        public int longitude { get; private set; }

        public Vector2Int GetFixedLongitudeAndLatitude()
        {
            return new Vector2Int(longitude, latitude);
        }

        public SerializedHeightMapInfo(string fixedHeightFilePath, HeightMapWorkFlow WorkFlow)
        {
            latitude = 0; longitude = 0;

            string inputFileName = Path.GetFileName(fixedHeightFilePath);
            switch (WorkFlow)
            {
                case HeightMapWorkFlow.TIF:
                    SetTIFHeightInfo(inputFileName);
                    break;
                case HeightMapWorkFlow.GridTerrain:
                    SetGridTerrainHeightInfo(inputFileName);
                    break;
            }
        }

        private void SetTIFHeightInfo(string inputFileName)
        {
            // Get tif file info from the name, such as "n33_e110_1arc_v3"
            string[] tifFileInfo = inputFileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (tifFileInfo.Length < 3)
            {
                Debug.LogError($"the tif error, name not correct : {inputFileName}");
                return;
            }

            // Read the tif file, get longitude and latitude, write to this output file
            Match matchLatitude = Regex.Match(tifFileInfo[0], @"^[a-zA-Z]+(\d+)$");
            latitude = int.Parse(matchLatitude.Groups[1].Value);
            Match matchLongitude = Regex.Match(tifFileInfo[1], @"^[a-zA-Z]+(\d+)$");
            longitude = int.Parse(matchLongitude.Groups[1].Value);
        }

        private void SetGridTerrainHeightInfo(string inputFileName)
        {
            // Get tif file info from the name, such as "GridTerrain_x0_y0_512x512_Batch0_638965990576634345"
            string[] gridTerrainTexFileInfo = inputFileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (gridTerrainTexFileInfo.Length < 3)
            {
                Debug.LogError($"the tif error, name not correct : {inputFileName}");
                return;
            }
            longitude = int.Parse(gridTerrainTexFileInfo[1].Substring(1));
            latitude = int.Parse(gridTerrainTexFileInfo[2].Substring(1));
        }
    }

    public class HeightMapEditor : BaseMapEditor {

        // TODO : 后续要从 EditorSceneManager 中获取
        protected HexSettingSO hexSet;

        protected TerrainSettingSO terSet;

        // TODO : 后续要从 EditorSceneManager 中获取
        protected GridTerrainSO gridTerrainSO;

        public override string EditorName => MapEditorEnum.HeightMapEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();

            terSet = EditorSceneManager.TerSet;
            //FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");

            hexSet = EditorSceneManager.HexSet;
            //FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");

            gridTerrainSO = EditorSceneManager.GridTerrainSO;
            //FindOrCreateSO<GridTerrainSO>(ref gridTerrainSO, MapStoreEnum.GamePlayGridTerrainDataPath, "GridTerrainSO_Default.asset");
            gridTerrainSO.UpdateTerSO(hexSet.mapWidth, hexSet.mapHeight);
            Debug.Log("Init HeightMap Editor over!");
        }

        //
        // ----二进制文件格式----
        // int 文件数量 （4B）
        // int 单个cluster的尺寸 （4B）
        // 对于每个地块文件：
        //      int 地块x坐标
        //      int 地块y坐标
        //      float[clusterSize, clusterSize] 高度图数据
        //
        // ----对于地块经纬度的解释----
        // 在 scene 视图中
        // x 为横轴，为经度 longitutde
        // y 为纵轴，为维度 latitude
        // 但是在计算偏移时，如果使用简单的 longitudeAndLatitude * terrainSize
        // 却会导致错误的结果
        // 例如 longitudeAndLatitude = (0, 1)
        // 那么 offset 值为 (0, 512)，在坐标中的计算是错误的，正确的应该是 (512, 0)
        // 所以每次获取经纬度计算 offset 时，都需要注意是否反转 
        //

        #region 高度图 转 二进制文件

        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("当前工作流")]
        public HeightMapWorkFlow WorkFlow = HeightMapWorkFlow.GridTerrain;

        bool IsInTIFWorkFlow => (WorkFlow == HeightMapWorkFlow.TIF);
        bool IsInGridTerrainWorkFlow => (WorkFlow == HeightMapWorkFlow.GridTerrain);

        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("转化后高度图分辨率")]
        public int compressResultSize = 64;

        //[ShowIf("IsInTIFWorkFlow")]
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("导入时翻转"), ReadOnly]
        public bool flipVertically = true;

        [ShowIf("IsInTIFWorkFlow")]
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("每次序列化的TIF文件数目"), ReadOnly]
        public int batchTIFTileNum = 15;

        [ShowIf("IsInTIFWorkFlow")]
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("TIF导入位置"), ReadOnly]
        public string tifInputPath = MapStoreEnum.HeightMapInputPath;

        [ShowIf("IsInGridTerrainWorkFlow")]
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("每次序列化的GridTerrain纹理数目"), ReadOnly]
        public int batchGridTerrainTileNum = 15;

        [ShowIf("IsInGridTerrainWorkFlow")]
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("转化的GridTerrainTex"), ReadOnly]
        public List<Texture2D> gridTerrainTexs = new List<Texture2D>();

        [ShowIf("IsInGridTerrainWorkFlow")]
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("GridTerrainTex导入位置"), ReadOnly]
        public string gridTerrainTexInputPath = MapStoreEnum.GamePlayGridTerrainTexDataPath;

        // You can go to {heightMapOutputPath} to delete output files
        [FoldoutGroup("高度图转二进制文件")]
        [LabelText("导出位置"), ReadOnly]
        public string heightMapOutputPath = MapStoreEnum.HeightMapOutputPath;

        [FoldoutGroup("高度图转二进制文件")]
        [Button("生成二进制文件", ButtonSizes.Medium)]
        private void GenSerializeFiles() {
            
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!Directory.Exists(heightMapOutputPath)) {
                Directory.CreateDirectory(heightMapOutputPath);
            }

            gridTerrainTexs.Clear();
            switch (WorkFlow)
            {
                case HeightMapWorkFlow.TIF:
                    StartSerialize(batchTIFTileNum, "*.tif", tifInputPath);
                    break;
                case HeightMapWorkFlow.GridTerrain:
                    StartSerialize(batchGridTerrainTileNum, "*.png", gridTerrainTexInputPath);
                    break;
            }
            stopwatch.Stop();
            Debug.Log($"Gen serialized file over! cost : {stopwatch.ElapsedMilliseconds}ms");
        }
        
        private void StartSerialize(int batchTileNum, string filterFileSuffix, string fileInputPath)
        {
            if (!Directory.Exists(fileInputPath))
            {
                Directory.CreateDirectory(fileInputPath);
            }

            string[] heightMapPaths = Directory.GetFiles(fileInputPath, filterFileSuffix, SearchOption.AllDirectories);
            int batches = heightMapPaths.Length / batchTileNum + 1;
            for (int i = 0; i < batches; i++)
            {
                // Get specify number of TIF file
                int start = i * batchTileNum;
                int end = Mathf.Min((i + 1) * batchTileNum, heightMapPaths.Length);
                int length = end - start;
                string[] curHandleFilePaths = new string[length];
                Array.Copy(heightMapPaths, start, curHandleFilePaths, 0, length);

                // Transfer them to a serialized file
                string outputName = GetOuputName(length, i);
                SerializeHeightMap(heightMapOutputPath, outputName, curHandleFilePaths, compressResultSize);
            }
        }
        
        private void SerializeHeightMap(string outputPath, string outputName, string[] inputFilePaths, int size) 
        {
            string outputFile = AssetsUtility.CombinedPath(outputPath, outputName);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fs)) {
                    // File header : fileNum, single fileSize  // Int32
                    writer.Write(inputFilePaths.Length);
                    writer.Write(size);

                    foreach (var inputFilePath in inputFilePaths) {
                        string fixedFilePath = AssetsUtility.FixFilePath(inputFilePath);

                        SerializedHeightMapInfo heightMapInfo = new SerializedHeightMapInfo(fixedFilePath, WorkFlow);
                        writer.Write(heightMapInfo.latitude + terSet.startLL.y);
                        writer.Write(heightMapInfo.longitude + terSet.startLL.x);

                        CompressAndWirteHeight(fixedFilePath, writer, heightMapInfo);
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"height map data has been set to: {outputFile}");
        }

        private string GetOuputName(int fileNum, int batch) {
            DateTime dateTime = DateTime.Now;
            return string.Format("heightData_{0}files_{1}batch_{2}", fileNum, batch, dateTime.Ticks);
        }

        private void CompressAndWirteHeight(string heightMapPath, BinaryWriter writer, SerializedHeightMapInfo heightMapInfo) 
        {
            //float[,] heights = new float[1,1];\
            TDList<float> heights = new TDList<float>();
            switch (WorkFlow)
            {
                case HeightMapWorkFlow.TIF:
                    heights = ReadTifData(heightMapPath);
                    break;
                case HeightMapWorkFlow.GridTerrain:
                    heights = ReadGridTerrainTex(heightMapPath, heightMapInfo);
                    break;
            }
            Debug.Log(string.Format("read height data over, length: {0}, path: {1}", heights.Count, heightMapPath));

            //float[,] compressedHeights = new float[compressResultSize, compressResultSize];   // float[,]
            if (heights == null) {
                Debug.Log("error, do not read height data successfully!");
                return;
            }

            int srcWidth = heights.GetLength(1);
            int dstHeight = heights.GetLength(0);

            // Resample the size of height map
            for (int i = 0; i < compressResultSize; i++) {
                for (int j = 0; j < compressResultSize; j++) {

                    float sx = i * (float)(srcWidth - 1) / compressResultSize;
                    float sy = j * (float)(dstHeight - 1) / compressResultSize;

                    int x0 = Mathf.FloorToInt(sx);
                    int x1 = Mathf.Min(x0 + 1, srcWidth - 1);
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
                    float fixed_h = Mathf.Clamp(h, 0, 50);        // 不建议在这里修改原数据
                    //compressedHeights[i, j] = fixed_h;

                    writer.Write(fixed_h);
                }
            }

            //Return compressedHeights;
        }

        private TDList<float> ReadTifData(string path) {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(path);
            int width = bitmap.Width;
            int height = bitmap.Height;

            TDList<float> heights = new TDList<float>(width, height);

            //float[,] heights = new float[width, height];
            for (int x = 0; x < height; x++) {
                for (int y = 0; y < width; y++) {
                    int destY = flipVertically ? width - 1 - y : y;
                    int destX = flipVertically ? height - 1 - x : x;
                    System.Drawing.Color pixelColor = bitmap.GetPixel(y, destX);

                    // GrapScale = 0.299 * R + 0.587 * G + 0.114 * B
                    float grayscale = 0.299f * pixelColor.R + 0.587f * pixelColor.G + 0.114f * pixelColor.B;
                    heights[x, y] = grayscale / 255.0f;
                }
            }

            Debug.Log(string.Format("read tif file over! length:{0}, width:{1}, height:{2}", heights.Count, width, height));
            bitmap.Dispose();
            return heights;
        }

        // Cache of gridTerrain gen
        MountainNoiseData CurMountainNoise;

        FastNoiseLite CurMountainNoiseLite;

        FastNoiseLite CurInteruptNoiseLite;

        // TODO : 后续要对山脉外的地形也加入 height （例如丘陵、平原甚至也可以）
        private TDList<float> ReadGridTerrainTex(string path, SerializedHeightMapInfo heightMapInfo)
        {
            Texture2D gridTerrainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int terrainSize = terSet.clusterSize;
            int fixedTerSize = terSet.fixedClusterSize;
            gridTerrainTexs.Add(gridTerrainTex);
            List<Color> gridTerrainTexColors = gridTerrainTex.GetPixels().ToList();
            TDList<float> heights = new TDList<float>(terrainSize, terrainSize);

            Vector2Int longitudeAndLatitude = heightMapInfo.GetFixedLongitudeAndLatitude();
            for (int i = 0; i < terrainSize; i++)
            {
                for (int j = 0; j < terrainSize; j++)
                {
                    // Cache mountain sample data and Get IsMountain
                    bool IsMountain = CacheMountainSample(i, j, terrainSize, fixedTerSize, longitudeAndLatitude);
                    float height;
                    if (IsMountain)
                    {
                        height = InterpretWithMountain(i, j, terrainSize, fixedTerSize, longitudeAndLatitude, gridTerrainTexColors);
                    }
                    else
                    {
                        height = 1; // Plain
                    }

                    // 需要进行中心旋转
                    if (flipVertically)
                    {
                        heights[j, i] = height;
                    }
                    else
                    {
                        heights[i, j] = height;
                    }
                }
            }
            return heights;
        }
        
        private bool CacheMountainSample(int i, int j, int terrainSize, int fixedTerSize, Vector2Int longitudeAndLatitude)
        {
            Vector2Int sampleFix = new Vector2Int((fixedTerSize - terrainSize) / 2, (fixedTerSize - terrainSize) / 2);
            Vector2Int pos = new Vector2Int(i, j) + longitudeAndLatitude * terrainSize + sampleFix;

            Hexagon hex = HexHelper.PixelToAxialHex(pos, hexSet.hexGridSize, true);
            Vector2Int offsetHex = HexHelper.AxialToOffset(hex);

            // TODO : 用下方的逻辑，为丘陵等地形也生成起伏
            //byte idx = gridTerrainSO.GetGridTerrainDataIdx(offsetHex);
            //Color color = gridTerrainSO.GetGridTerrainTypeColorByIdx(idx);

            // Get mountainNoiseData by cur point
            MountainData mountainData;
            MountainNoiseData noiseData = CurMountainNoise;
            bool IsMountain = gridTerrainSO.GetGridIsMountain(offsetHex);
            if (IsMountain)
            {
                int mountainID = gridTerrainSO.GetGridMountainID(offsetHex);
                mountainData = gridTerrainSO.GetMountainData(mountainID);
                if (mountainData == null)
                {
                    noiseData = MountainNoiseData.DefaultNoise;
                }
                else
                {
                    noiseData = mountainData.MountainNoiseData;
                }
            }
            
            // Set interuptNoiseLite and mountainNoiseLite by mountainNoiseData
            if (noiseData != CurMountainNoise)
            {
                CurMountainNoise = noiseData;
                CurMountainNoiseLite = CurMountainNoise.GetNoiseDataLite();
                CurInteruptNoiseLite = CurMountainNoise.GetSampleNoiseData();
            }
            return IsMountain;
        }

        private float InterpretWithMountain(int i, int j, int terrainSize, int fixedTerSize, Vector2Int longitudeAndLatitude, List<Color> gridTerrainTexColors)
        {
            // Fix texture sample (terrain is 532, but we need 512)
            int clusterSampleFix = (fixedTerSize - terrainSize) / 2;
            Vector2Int sampleFix = new Vector2Int(clusterSampleFix, clusterSampleFix);
            Vector2Int texPos = new Vector2Int(i, j) + sampleFix;
            //texPos = FixInterpretCoord(texPos, fixedTerSize);

            //if (texPos.y * terrainSize + texPos.x > gridTerrainTexColors.Count)
            //{
            //    Debug.Log(111);
            //}
            //if (texPos.y * terrainSize + texPos.x < 0)
            //{
            //    Debug.Log(111);
            //}

            Color color = gridTerrainTexColors[texPos.y * fixedTerSize + texPos.x];    // TODO : 会越界，想想办法... 让输出的texture额外采样周围的部分点...

            // Get true position in this cluster （DONT DEL）
            Vector2Int pos = new Vector2Int(i, j) + longitudeAndLatitude * terrainSize;
            float sampleNoise = CurInteruptNoiseLite.GetNoise(pos.x, pos.y);
            int clusterStartX = longitudeAndLatitude.x * terrainSize - clusterSampleFix;
            int clusterEndX = (longitudeAndLatitude.x + 1) * terrainSize - 1 + clusterSampleFix;
            int clusterStartY = longitudeAndLatitude.y * terrainSize - clusterSampleFix;
            int clusterEndY = (longitudeAndLatitude.y + 1) * terrainSize - 1 + clusterSampleFix;
            //pos.x = Mathf.Clamp(pos.x + (int)(sampleNoise * CurMountainNoise.interuptInstence), clusterStartX, clusterEndX);  // 生成 地块纹理时 要额外采样周围的点
            //pos.y = Mathf.Clamp(pos.y + (int)(sampleNoise * CurMountainNoise.interuptInstence), clusterStartY, clusterEndY);

            pos.x += (int)(sampleNoise * CurMountainNoise.interuptInstence);
            pos.y += (int)(sampleNoise * CurMountainNoise.interuptInstence);

            // Use noise to interrupt height 
            float ratio = MathUtil.ColorInverseLerp(BaseGridTerrain.GetPlainColor(), BaseGridTerrain.GetMountainColor(), color);
            float noise = Mathf.Abs(CurMountainNoiseLite.GetNoise(pos.x, pos.y)) * ratio * 10 + CurMountainNoise.baseHeight;

            if (noise > 1)
            {
                noise = Mathf.Pow(noise, CurMountainNoise.elevation);
            }
            else
            {
                noise = Mathf.Pow(noise, 1 / CurMountainNoise.elevation);
            }
            float height = noise * CurMountainNoise.heightFix;
            return height;
        }

        private Vector2Int FixInterpretCoord(Vector2Int pos, int fixedTerSize)
        {
            if (pos.x >= fixedTerSize)
            {
                pos.x = fixedTerSize - 2;
            }
            else if (pos.x < 0)
            {
                pos.x = 0;
            }

            if (pos.y >= fixedTerSize)
            {
                pos.y = fixedTerSize - 2;
            }
            else if (pos.y < 0)
            {
                pos.y = 0;
            }
            return pos;
        }

        #endregion


        #region 高度图反序列化

        [FoldoutGroup("生成HeightDataModel")]
        [LabelText("当前操作的序列化文件")]
        [Tooltip("不要直接使用该字段导入，点击下方的按钮进行导入")]
        public List<UnityEngine.Object> heightMapSerilzedFile;

        [FoldoutGroup("生成HeightDataModel")]
        [LabelText("导入位置"), ReadOnly]
        public string serlDataOutputPath = MapStoreEnum.HeightMapOutputPath;

        [FoldoutGroup("生成HeightDataModel")]
        [LabelText("导出位置"), ReadOnly]
        public string deserlDataOutputPath = MapStoreEnum.HeightMapScriptableObjPath;

        [FoldoutGroup("生成HeightDataModel")]
        [Button("导入序列化文件", ButtonSizes.Medium)]
        private void ImportSerilizedFile() 
        {
            //string heightMapSerlizedPath = EditorUtility.OpenFilePanel("Import Raw Heightmap", "", "");
            if (string.IsNullOrEmpty(serlDataOutputPath) || string.IsNullOrEmpty(deserlDataOutputPath)) {
                Debug.LogError("input / output path is null!");
                return;
            }

            string fullOutpath = AssetsUtility.AssetToFullPath(serlDataOutputPath);
            Debug.Log(fullOutpath);
            string[] heightMapPaths = Directory.GetFiles(fullOutpath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta")).ToArray();
            heightMapSerilzedFile = new List<UnityEngine.Object>(heightMapPaths.Length);
            for (int i = 0; i < heightMapPaths.Length; i++) {
                Debug.Log(heightMapPaths[i]);
                string fileRelativePath = AssetsUtility.TransToAssetPath(heightMapPaths[i]);
                UnityEngine.Object fileObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fileRelativePath);
                if (fileObj == null) {
                    Debug.LogError(string.Format("can not load height serialized resource from this path: {0}", fileRelativePath));
                }
                heightMapSerilzedFile.Add(fileObj);
            }
        }

        [FoldoutGroup("生成HeightDataModel")]
        [Button("生成HeightDataModel（用于游戏中）", ButtonSizes.Medium)]
        private void DeserializeHeightMaps() 
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!Directory.Exists(heightMapOutputPath)) {
                Directory.CreateDirectory(heightMapOutputPath);
            }
            if (!Directory.Exists(deserlDataOutputPath)) {
                Directory.CreateDirectory(deserlDataOutputPath);
            }

            for(int i = 0; i < heightMapSerilzedFile.Count; i++) {
                string path = AssetDatabase.GetAssetPath(heightMapSerilzedFile[i]);
                DeserializeHeightMaps(i, path, deserlDataOutputPath);
            }
            stopwatch.Stop();
            Debug.Log($"Gen HeightDataModel over! cost : {stopwatch.ElapsedMilliseconds}ms");
        }

        private void DeserializeHeightMaps(int batch, string fileRelativePath, string deserlOutputFilePath) 
        {

            fileRelativePath = AssetsUtility.FixFilePath(fileRelativePath);
            //Debug.Log(deserlOutputFilePath);

            using (FileStream fs = new FileStream(fileRelativePath, FileMode.Open, FileAccess.Read)) 
            {
                using (BinaryReader reader = new BinaryReader(fs)) {
                    int fileNum = reader.ReadInt32();
                    int singleFileWidth = reader.ReadInt32();

                    HeightDataModel heightDataModel = ScriptableObject.CreateInstance<HeightDataModel>();
                    heightDataModel.InitHeightModel(fileNum, singleFileWidth);
                    DateTime dateTime = DateTime.Now;
                    string modelName = string.Format("HeightModel_{0}files_{1}batch_{2}.asset", fileNum, batch, dateTime.Ticks);
                    heightDataModel.name = modelName;

                    for (int i = 0; i < fileNum; i++)
                    {
                        int latitude = reader.ReadInt32();
                        int longitude = reader.ReadInt32();

                        float[,] heightDatas = new float[singleFileWidth, singleFileWidth];
                        // read the height data then add to the heightDataModel
                        for (int q = 0; q < singleFileWidth; q++) {
                            for (int p = 0; p < singleFileWidth; p++) {
                                heightDatas[q, p] = reader.ReadSingle();
                            }
                        }
                        //Debug.Log($"now add a height data n{latitude}, e{longitude}, file width {singleFileWidth}");
                        heightDataModel.AddHeightData(longitude, latitude, singleFileWidth, heightDatas);
                    }

                    Debug.Log($"header info, num of files : {fileNum}, single SO file size : {singleFileWidth}");
                    string assetFullPath = AssetsUtility.CombinedPath(deserlOutputFilePath, modelName);
                    AssetDatabase.CreateAsset(heightDataModel, assetFullPath);
                    AssetDatabase.SaveAssets();
                }
            }

            AssetDatabase.Refresh();
        }

        #endregion

        // TODO : 没有完成
        // TODO : 后续需要在生成高度图的同时，生成法线贴图，或者动态地生成法线贴图
        #region 根据高度图生成法线贴图

        [FoldoutGroup("根据高度图生成法线图")]
        [LabelText("法线分辨率")]
        public int normalMapSize = 512;

        [FoldoutGroup("根据高度图生成法线图")]
        [LabelText("源高度图")]
        public Texture2D originTexture;

        [FoldoutGroup("根据高度图生成法线图")]
        [LabelText("导出位置"), ReadOnly]
        public string normalTexOutputPath = MapStoreEnum.HeightMapNormalTexOutputPath;

        [FoldoutGroup("根据高度图生成法线图")]
        [Button("生成法线贴图", ButtonSizes.Medium)]

        private void GenerateNormalMap() {
            if (!Directory.Exists(tifInputPath)) {
                Debug.LogError("do not exist input heightmap path");
                return;
            }
            if (!Directory.Exists(heightMapOutputPath)) {
                Directory.CreateDirectory(heightMapOutputPath);
            }

            if (originTexture == null) {
                Debug.LogError("do not use origin texture, so we can not generate normal!");
                return;
            }

            string[] heightMapPaths = Directory.GetFiles(tifInputPath, "*.tif", SearchOption.AllDirectories);
            GenerateNormalMap(normalTexOutputPath, heightMapPaths);
        }

        //ERROR : 目前生成结果有问题！
        private void GenerateNormalMap(string outputPath, string[] inputFilePaths) {
            
            int generateBatch = UnityEngine.Random.Range(1, 255);

            int count = 1;
            //foreach (var inputFilePath in inputFilePaths) {
                //if (count <= 0) {
                //    break;
                //}
                //count--;

                //string fixedFilePath = AssetsUtility.GetInstance().FixFilePath(inputFilePath);

                //TIFHeightMapInfo info = new TIFHeightMapInfo(fixedFilePath);

                //float[,] heights = ReadTifData(fixedFilePath);
                int srcWidth = originTexture.width;
                int srcHeight = originTexture.height;

                Texture2D normalTex = new Texture2D(normalMapSize, normalMapSize, TextureFormat.RGB24, false);

                // write the data to normalTex
                for (int i = 0; i < normalMapSize; i++) {
                    for (int j = 0; j < normalMapSize; j++) {
                        float sx = i * (float)(srcWidth - 1) / normalMapSize;
                        float sy = j * (float)(srcHeight - 1) / normalMapSize;

                        int int_sx = Mathf.FloorToInt(sx);
                        int int_sy = Mathf.FloorToInt(sy);

                        // sobel x
                        int[,] kernel_sobel_x = {
                            { -1,  0,  1 },
                            { -2,  0,  2 },
                            { -1,  0,  1 }
                        };
                        float sum_x = 0f;
                        for (int ky = -1; ky <= 1; ky++) {
                            for (int kx = -1; kx <= 1; kx++) {
                                int px = Mathf.Clamp(int_sx + kx, 0, srcWidth - 1);
                                int py = Mathf.Clamp(int_sy + ky, 0, srcHeight - 1);
                                float gray = originTexture.GetPixel(px, py).grayscale;
                                sum_x += gray * kernel_sobel_x[ky + 1, kx + 1];
                            }
                        }

                        // sobel y
                        int[,] kernel_sobel_y = {
                            { -1, -2, -1 },
                            {  0,  0,  0 },
                            {  1,  2,  1 }
                        };
                        float sum_y = 0f;
                        for (int ky = -1; ky <= 1; ky++) {
                            for (int kx = -1; kx <= 1; kx++) {
                                int px = Mathf.Clamp(int_sx + kx, 0, srcWidth - 1);
                                int py = Mathf.Clamp(int_sy + ky, 0, srcHeight - 1);
                                float gray = originTexture.GetPixel(px, py).grayscale;
                                sum_y += gray * kernel_sobel_y[ky + 1, kx + 1];
                            }
                        }


                        float strength = 5f;
                        Vector3 normal = new Vector3(-sum_x * strength, -sum_y * strength, 1.0f);
                        normal.Normalize();

                        // 映射到 [0,1] 区间
                        Color nColor = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f, 1.0f);
                        normalTex.SetPixel(i, j, nColor);



                        //// TODO : caculate normal
                        //Vector3 left = new Vector3(x0, q00, int_sy);
                        //Vector3 right = new Vector3(x1, q00, int_sy);
                        //Vector3 up = new Vector3(int_sx, q00, y1);
                        //Vector3 down = new Vector3(int_sx, q01, y0);

                        ////Vector3 normal = Vector3.Cross(up - down, right - left).normalized;
                        //Vector3 normal = new Vector3((left - right).magnitude, (up - down).magnitude, 1 / 100).normalized;
                        //Color color = new Color((normal.x + 1) / 2, (normal.y + 1) / 2 * 255, (normal.z + 1) / 2 * 255);
                        ////Color color = new Color(normal.x, normal.y, normal.z);
                        //normalTex.SetPixel(i, j, color);
                    }
                }

                // save normalTex as unity asset
                string normalName = GetNormalOutputName(200, 200, normalMapSize, -1);
                TextureUtility.SaveTextureAsAsset(normalTexOutputPath, normalName, normalTex);
            GameObject.DestroyImmediate(normalTex);
            //}

            Debug.Log($"generate normal map, path : {normalTexOutputPath}");

        }

        private string GetNormalOutputName(int latitude, int longitude, int size, int generateBatch) {
            return string.Format("normalMap_{0}_{1}_{2}x{2}_{3}",  longitude, latitude, size, generateBatch);
        }

        #endregion
    }

}
