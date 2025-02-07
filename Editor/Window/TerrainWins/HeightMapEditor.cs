using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using static LZ.WarGameMap.MapEditor.TerrainEditor;

namespace LZ.WarGameMap.MapEditor
{
    public class HeightMapEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.HeightMapEditor;

        protected override void InitEditor() { }

        [FoldoutGroup("�߶�ͼ���л�/��������")]
        [LabelText("����ʱ��ת")]
        public bool flipVertically = true;

        [FoldoutGroup("�߶�ͼ���л�")]
        [LabelText("ѹ����߶�ͼ�ֱ���")]
        public int compressResultSize = 64;

        [FoldoutGroup("�߶�ͼ���л�")]
        [LabelText("��ǰʹ�ø߶�ͼ��Ⱥ")]
        [AssetSelector(Filter = "t:Texture")]
        public List<List<Texture>> heightMapLists;

        [FoldoutGroup("�߶�ͼ���л�")]
        [LabelText("����λ��")]
        public string heightMapInputPath = MapStoreEnum.HeightMapInputPath;

        [FoldoutGroup("�߶�ͼ���л�")]
        [LabelText("����λ��")]
        public string heightMapOutputPath = MapStoreEnum.HeightMapOutputPath;

        [FoldoutGroup("�߶�ͼ���л�")]
        [Button("���л��߶�ͼ", ButtonSizes.Medium)]
        private void SerializeHeightMaps() {
            if (!Directory.Exists(heightMapInputPath)) {
                Directory.CreateDirectory(heightMapInputPath);
            }
            if (!Directory.Exists(heightMapOutputPath)) {
                Directory.CreateDirectory(heightMapOutputPath);
            }

            string[] heightMapPaths = Directory.GetFiles(heightMapInputPath, "*.tif", SearchOption.AllDirectories);
            string outputName = GetOuputName(heightMapPaths.Length);
            SerializeHeightMaps(heightMapOutputPath, outputName, heightMapPaths, compressResultSize);
        }

        private void SerializeHeightMaps(string outputPath, string outputName, string[] inputFilePaths, int size) {
            
            string outputFile = AssetsUtility.GetInstance().GetCombinedPath(outputPath, outputName);

            using (FileStream fs = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write)) {
                using (BinaryWriter writer = new BinaryWriter(fs)) {
                    // file header : fileNum, single fileSize  // Int32
                    writer.Write(inputFilePaths.Length);
                    writer.Write(size);

                    foreach (var inputFilePath in inputFilePaths) {
                        string fixedFilePath = AssetsUtility.GetInstance().FixFilePath(inputFilePath);

                        // get tif file info from the name, such as "n33_e110_1arc_v3"
                        string inputFileName = Path.GetFileName(fixedFilePath);
                        string[] tifFileInfo = inputFileName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                        if (tifFileInfo.Length < 3) {
                            Debug.LogError($"the tif error, name not correct : {fixedFilePath}");
                            break;
                        }

                        // read the tif file, get longitude and latitude, write to this output file
                        Match matchLatitude = Regex.Match(tifFileInfo[0], @"^[a-zA-Z]+(\d+)$");
                        int latitude = int.Parse(matchLatitude.Groups[1].Value);
                        Match matchLongitude = Regex.Match(tifFileInfo[1], @"^[a-zA-Z]+(\d+)$");
                        int longitude = int.Parse(matchLongitude.Groups[1].Value);

                        writer.Write(latitude);
                        writer.Write(longitude);

                        // get height data and write to file
                        float[,] heightMap = CompressHeightData(fixedFilePath);
                        for (int i = 0; i < size; i++) {
                            for (int j = 0; j < size; j++) {
                                writer.Write(heightMap[i, j]);
                            }
                        }
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"height map data has been set to: {outputFile}");
        }

        private string GetOuputName(int fileNum) {
            DateTime dateTime = DateTime.Now;
            return string.Format("heightData_{0}files_{1}", fileNum, dateTime.Ticks);
        }

        private float[,] CompressHeightData(string heightMapPath) {

            float[,] heights = ReadHeightMapData(heightMapPath);
            float[,] compressedHeights = new float[compressResultSize, compressResultSize];
            if (heights == null) {
                Debug.Log("error, do not read height data successfully!");
                return null;
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
                    compressedHeights[i, j] = fixed_h;
                }
            }

            return compressedHeights;
        }

        private float[,] ReadHeightMapData(string heightMapPath) {
            float[,] heights = ReadTifData(heightMapPath);
            Debug.Log(string.Format("read height data over, length: {0}, path: {1}", heights.Length, heightMapPath));
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

        [FoldoutGroup("�߶�ͼ���л�")]
        [Button("�����������ļ�", ButtonSizes.Medium)]
        private void ClearSerializedData() {
            Debug.Log($"please go to {heightMapOutputPath} to delete output files, delete by code is dangerous");
        }



        [FoldoutGroup("�߶�ͼ�����л�")]
        [LabelText("��ǰ���������л��ļ�")]
        [Tooltip("��Ҫֱ��ʹ������ֶε��룬����·��İ�ť���е���")]
        public UnityEngine.Object heightMapSerilzedFile;

        [FoldoutGroup("�߶�ͼ�����л�")]
        [LabelText("����λ��")]
        public string deserlDataOutputPath = MapStoreEnum.HeightMapScriptableObjPath;

        [FoldoutGroup("�߶�ͼ�����л�")]
        [LabelText("��ǰ�����ļ�λ��")]
        public string fileRelativePath = "";

        [FoldoutGroup("�߶�ͼ�����л�")]
        [Button("�������л�����ļ�", ButtonSizes.Medium)]
        private void ImportSerilizedFile() {
            string heightMapSerlizedPath = EditorUtility.OpenFilePanel("Import Raw Heightmap", "", "");
            if (heightMapSerlizedPath == "") {
                Debug.LogError("you do not get the height map");
                return;
            }

            fileRelativePath = AssetsUtility.GetInstance().TransToUnityAssetPath(heightMapSerlizedPath);
            heightMapSerilzedFile = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fileRelativePath);
            if (heightMapSerilzedFile == null) {
                Debug.LogError(string.Format("can not load height serialized resource from this path: {0}", fileRelativePath));
                return;
            }
        }

        [FoldoutGroup("�߶�ͼ�����л�")]
        [Button("�����л��߶�ͼ", ButtonSizes.Medium)]
        private void DeserializeHeightMaps() {
            if (!Directory.Exists(heightMapOutputPath)) {
                Directory.CreateDirectory(heightMapOutputPath);
            }
            if (!Directory.Exists(deserlDataOutputPath)) {
                Directory.CreateDirectory(deserlDataOutputPath);
            }

            DeserializeHeightMaps(fileRelativePath, deserlDataOutputPath);
        }

        private void DeserializeHeightMaps(string fileRelativePath, string deserlOutputFilePath) {

            fileRelativePath = AssetsUtility.GetInstance().FixFilePath(fileRelativePath);
            //Debug.Log(deserlOutputFilePath);

            using (FileStream fs = new FileStream(fileRelativePath, FileMode.Open, FileAccess.Read)) {
                using (BinaryReader reader = new BinaryReader(fs)) {
                    int fileNum = reader.ReadInt32();
                    int singleFileWidth = reader.ReadInt32();
                    Debug.Log($"header info, num of files : {fileNum}, single file size : {singleFileWidth}");

                    HeightDataModel heightDataModel = ScriptableObject.CreateInstance<HeightDataModel>();
                    heightDataModel.InitHeightModel(fileNum, singleFileWidth);
                    DateTime dateTime = DateTime.Now;
                    string modelName = string.Format("HeightModel_{0}files_{1}.asset", fileNum, dateTime.Ticks);
                    heightDataModel.name = modelName;

                    for (int i = 0; i < fileNum; i++) {
                        int latitude = reader.ReadInt32();
                        int longitude = reader.ReadInt32();

                        float[,] heightDatas = new float[singleFileWidth, singleFileWidth];
                        // read the height data then add to the heightDataModel
                        for (int q = 0; q < singleFileWidth; q++) {
                            for(int p  = 0; p < singleFileWidth;  p++) {
                                heightDatas[q, p] = reader.ReadSingle();
                            }
                        }
                        Debug.Log($"now add a height data n{latitude}, e{longitude}, file width {singleFileWidth}");
                        heightDataModel.AddHeightData(longitude, latitude, singleFileWidth, heightDatas);
                    }

                    string assetFullPath = AssetsUtility.GetInstance().GetCombinedPath(deserlOutputFilePath, modelName);
                    AssetDatabase.CreateAsset(heightDataModel, assetFullPath);
                    AssetDatabase.SaveAssets();
                }
            }

            AssetDatabase.Refresh();
        }

    }

}
