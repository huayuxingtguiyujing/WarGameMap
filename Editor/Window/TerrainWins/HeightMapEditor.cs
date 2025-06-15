using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.BaseCommands.BranchExplorer.Layout.BrExLayout;
using static LZ.WarGameMap.MapEditor.TerrainEditor;
using static UnityEngine.Awaitable;
using Directory = System.IO.Directory;

namespace LZ.WarGameMap.MapEditor
{
    public class HeightMapEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.HeightMapEditor;


        struct TIFHeightMapInfo {
            public int latitude { get; private set; }
            public int longitude { get; private set; }

            public TIFHeightMapInfo(string fixedHeightFilePath) {
                latitude = 0; longitude = 0;

                // get tif file info from the name, such as "n33_e110_1arc_v3"
                string inputFileName = Path.GetFileName(fixedHeightFilePath);
                string[] tifFileInfo = inputFileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                if (tifFileInfo.Length < 3) {
                    Debug.LogError($"the tif error, name not correct : {fixedHeightFilePath}");
                    return;
                }

                // read the tif file, get longitude and latitude, write to this output file
                Match matchLatitude = Regex.Match(tifFileInfo[0], @"^[a-zA-Z]+(\d+)$");
                latitude = int.Parse(matchLatitude.Groups[1].Value);
                Match matchLongitude = Regex.Match(tifFileInfo[1], @"^[a-zA-Z]+(\d+)$");
                longitude = int.Parse(matchLongitude.Groups[1].Value);
            }
        }


        #region 高度图序列化

        [FoldoutGroup("高度图序列化/导入设置")]
        [LabelText("导入时翻转")]
        public bool flipVertically = true;

        [FoldoutGroup("高度图序列化")]
        [LabelText("压缩后高度图分辨率")]
        public int compressResultSize = 64;

        [FoldoutGroup("高度图序列化")]
        [LabelText("每次序列化的TIF文件数目")]
        public int batchTIFTileNum = 15;

        [FoldoutGroup("高度图序列化")]
        [LabelText("当前使用高度图集群")]
        [AssetSelector(Filter = "t:Texture")]
        public List<List<Texture>> heightMapLists;

        [FoldoutGroup("高度图序列化")]
        [LabelText("导入位置")]
        public string heightMapInputPath = MapStoreEnum.HeightMapInputPath;

        [FoldoutGroup("高度图序列化")]
        [LabelText("导出位置")]
        public string heightMapOutputPath = MapStoreEnum.HeightMapOutputPath;

        [FoldoutGroup("高度图序列化")]
        [Button("序列化高度图", ButtonSizes.Medium)]
        private void SerializeHeightMaps() {
            if (!Directory.Exists(heightMapInputPath)) {
                Directory.CreateDirectory(heightMapInputPath);
            }
            if (!Directory.Exists(heightMapOutputPath)) {
                Directory.CreateDirectory(heightMapOutputPath);
            }

            string[] heightMapPaths = Directory.GetFiles(heightMapInputPath, "*.tif", SearchOption.AllDirectories);
            int batches = heightMapPaths.Length / batchTIFTileNum + 1;
            for (int i = 0; i < batches; i++) {
                // get specify number of TIF file
                int start = i * batchTIFTileNum;
                int end = Mathf.Min((i + 1) * batchTIFTileNum, heightMapPaths.Length - 1);
                int length = end - start;
                string[] curHandleFilePaths = new string[length];
                Array.Copy(heightMapPaths, start, curHandleFilePaths, 0, length);

                // transfer them to a serialized file
                string outputName = GetOuputName(length, i);
                SerializeHeightMap(heightMapOutputPath, outputName, heightMapPaths, compressResultSize);
            }
        }


        private void SerializeHeightMap(string outputPath, string outputName, string[] inputFilePaths, int size) {

            string outputFile = AssetsUtility.GetInstance().CombinedPath(outputPath, outputName);
            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fs)) {
                    // file header : fileNum, single fileSize  // Int32
                    writer.Write(inputFilePaths.Length);
                    writer.Write(size);

                    foreach (var inputFilePath in inputFilePaths) {
                        string fixedFilePath = AssetsUtility.FixFilePath(inputFilePath);

                        //// get tif file info from the name, such as "n33_e110_1arc_v3"
                        //string inputFileName = Path.GetFileName(fixedFilePath);
                        //string[] tifFileInfo = inputFileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        //if (tifFileInfo.Length < 3) {
                        //    Debug.LogError($"the tif error, name not correct : {fixedFilePath}");
                        //    break;
                        //}

                        //// read the tif file, get longitude and latitude, write to this output file
                        //Match matchLatitude = Regex.Match(tifFileInfo[0], @"^[a-zA-Z]+(\d+)$");
                        //int latitude = int.Parse(matchLatitude.Groups[1].Value);
                        //Match matchLongitude = Regex.Match(tifFileInfo[1], @"^[a-zA-Z]+(\d+)$");
                        //int longitude = int.Parse(matchLongitude.Groups[1].Value);

                        TIFHeightMapInfo heightMapInfo = new TIFHeightMapInfo(fixedFilePath);

                        writer.Write(heightMapInfo.latitude);
                        writer.Write(heightMapInfo.longitude);

                        // get height data and write to file
                        CompressAndWirteHeight(fixedFilePath, writer);
                        //for (int i = 0; i < size; i++) {
                        //    for (int j = 0; j < size; j++) {
                        //        writer.Write(heightMap[i, j]);
                        //    }
                        //}
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

        private void CompressAndWirteHeight(string heightMapPath, BinaryWriter writer) {

            float[,] heights = ReadTifData(heightMapPath);
            Debug.Log(string.Format("read height data over, length: {0}, path: {1}", heights.Length, heightMapPath));

            //float[,] compressedHeights = new float[compressResultSize, compressResultSize];   // float[,]
            if (heights == null) {
                Debug.Log("error, do not read height data successfully!");
                return;
            }

            int srcWsidth = heights.GetLength(0);
            int dstHeight = heights.GetLength(1);

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
                    //compressedHeights[i, j] = fixed_h;

                    writer.Write(fixed_h);
                }
            }

            //return compressedHeights;
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

        [FoldoutGroup("高度图序列化")]
        [Button("清空输出数据文件", ButtonSizes.Medium)]
        private void ClearSerializedData() {
            Debug.Log($"please go to {heightMapOutputPath} to delete output files, delete by code is dangerous");
        }

        #endregion


        #region 高度图反序列化

        [FoldoutGroup("高度图反序列化")]
        [LabelText("当前操作的序列化文件")]
        [Tooltip("不要直接使用这个字段导入，点击下方的按钮进行导入")]
        public List<UnityEngine.Object> heightMapSerilzedFile;

        [FoldoutGroup("高度图反序列化")]
        [LabelText("导入位置")]
        public string serlDataOutputPath = MapStoreEnum.HeightMapOutputPath;

        [FoldoutGroup("高度图反序列化")]
        [LabelText("导出位置")]
        public string deserlDataOutputPath = MapStoreEnum.HeightMapScriptableObjPath;

        [FoldoutGroup("高度图反序列化")]
        [Button("导入序列化后文件", ButtonSizes.Medium)]
        private void ImportSerilizedFile() {
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

        [FoldoutGroup("高度图反序列化")]
        [Button("反序列化高度图", ButtonSizes.Medium)]
        private void DeserializeHeightMaps() {
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

        }

        private void DeserializeHeightMaps(int batch, string fileRelativePath, string deserlOutputFilePath) {

            fileRelativePath = AssetsUtility.FixFilePath(fileRelativePath);
            //Debug.Log(deserlOutputFilePath);

            using (FileStream fs = new FileStream(fileRelativePath, FileMode.Open, FileAccess.Read)) {
                using (BinaryReader reader = new BinaryReader(fs)) {
                    int fileNum = reader.ReadInt32();
                    int singleFileWidth = reader.ReadInt32();

                    HeightDataModel heightDataModel = ScriptableObject.CreateInstance<HeightDataModel>();
                    heightDataModel.InitHeightModel(fileNum, singleFileWidth);
                    DateTime dateTime = DateTime.Now;
                    string modelName = string.Format("HeightModel_{0}files_{1}batch_{2}.asset", fileNum, batch, dateTime.Ticks);
                    heightDataModel.name = modelName;

                    for (int i = 0; i < fileNum; i++) {
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
                    string assetFullPath = AssetsUtility.GetInstance().CombinedPath(deserlOutputFilePath, modelName);
                    AssetDatabase.CreateAsset(heightDataModel, assetFullPath);
                    AssetDatabase.SaveAssets();
                }
            }

            AssetDatabase.Refresh();
        }

        #endregion

        // TODO : UNCOMPLETE
        #region 根据高度图生成法线贴图

        [FoldoutGroup("根据高度图生成法线图")]
        [LabelText("法线分辨率")]
        public int normalMapSize = 512;

        [FoldoutGroup("根据高度图生成法线图")]
        [LabelText("源高度图")]
        public Texture2D originTexture;

        [FoldoutGroup("根据高度图生成法线图")]
        [LabelText("导出位置")]
        public string normalTexOutputPath = MapStoreEnum.HeightMapNormalTexOutputPath;

        [FoldoutGroup("根据高度图生成法线图")]
        [Button("生成法线贴图", ButtonSizes.Medium)]

        private void GenerateNormalMap() {
            if (!Directory.Exists(heightMapInputPath)) {
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

            string[] heightMapPaths = Directory.GetFiles(heightMapInputPath, "*.tif", SearchOption.AllDirectories);
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
                TextureUtility.GetInstance().SaveTextureAsAsset(normalTexOutputPath, normalName, normalTex);
            //}

            Debug.Log($"generate normal map, path : {normalTexOutputPath}");

        }

        private string GetNormalOutputName(int latitude, int longitude, int size, int generateBatch) {
            return string.Format("normalMap_{0}_{1}_{2}x{2}_{3}",  longitude, latitude, size, generateBatch);
        }

        #endregion
    }

}
