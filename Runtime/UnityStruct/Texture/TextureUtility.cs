using System.IO;
using Unity.Collections;
using Unity.Jobs;
using LZ.WarGameCommon;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace LZ.WarGameMap.Runtime
{

    //[BurstComp]
    internal struct FillPixelJob : IJobParallelFor {
        [WriteOnly]
        public NativeArray<Color> Pixels;

        public Color brushColor;

        public void Execute(int index) {
            Pixels[index] = brushColor;
        }
    }

    public class TextureUtility : Singleton <TextureUtility> {

        public TextureUtility() { }

        #region create/destroy/save/load texture

        private Texture2D CreateTexture2D(int width, int height, TextureFormat format = TextureFormat.RGBA32) {
            if (width == 0 || height == 0) {
                Debug.LogError("you can not create texture with 0 w/h");
                return null;
            }
            Texture2D texture2D = new Texture2D(width, height, format, false);

            NativeArray<Color> pixels = new NativeArray<Color>(width * height, Allocator.TempJob);
            FillPixelJob fillPixelsJob = new FillPixelJob {
                Pixels = pixels,
                brushColor = Color.white
            };
            JobHandle jobHandle = fillPixelsJob.Schedule(pixels.Length, 64);
            jobHandle.Complete();

            texture2D.SetPixels(pixels.ToArray());
            texture2D.Apply();

            return texture2D;
        }

        private void DestroyTexture(Texture2D texture) {
            GameObject.DestroyImmediate(texture);
        }

        public static void SaveTextureAsAsset(string outputPath, string fileName, Texture2D texture) {
            if (texture == null) {
                Debug.LogError($"the texture is null : {fileName}");
                return;
            }

            if (!outputPath.StartsWith("Assets/")) {
                Debug.LogError($"the output path should start with assets : {outputPath}");
                return;
            }

            string directoryPath = Path.Combine(Application.dataPath, outputPath.Substring(7));
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            // trans to png, and write to file
            byte[] pngData = texture.EncodeToPNG();
            if (pngData == null) {
                Debug.LogError("Texture2D 编码失败！");
                return;
            }
            string fullFilePath = Path.Combine(directoryPath, fileName + ".png");
            File.WriteAllBytes(fullFilePath, pngData);
            AssetDatabase.Refresh();

            // save as unity assets
            string assetPath = Path.Combine(outputPath, fileName + ".png");
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null) {
                importer.textureType = TextureImporterType.Default;
                importer.SaveAndReimport();
            }

            Debug.Log($"save texture to : {assetPath}");
        }

        private void SaveTexture(string path, string textureFileName, Texture2D texture) {
            if (texture == null) {
                return;
            }

            if (!path.StartsWith("Assets/")) {
                Debug.LogError($"the path should start with assets : {path}");
                return;
            }

            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            AssetsUtility.SaveAssets<Texture2D>(path, textureFileName, texture);
        }

        // TODO : test it
        public void SaveRenderTextureAsAsset(RenderTexture renderTexture, string outputPath) {
            RenderTexture.active = renderTexture;
            Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false);
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply();

            byte[] bytes = texture.EncodeToPNG();
            Object.Destroy(texture);
            File.WriteAllBytes(outputPath, bytes);

            Debug.Log($"save render texture sucessfully, path : {outputPath}");
            AssetDatabase.Refresh();
        }

        private Texture2D LoadTexture(string path, string textureName) {
            return AssetsUtility.LoadAssets<Texture2D>(path, textureName);
        }

        #endregion

        /// <summary>
        /// 对纹理进行双线性插值
        /// </summary>
        /// <param name="source"></param>
        /// <param name="newWidth"></param>
        /// <param name="newHeight"></param>
        /// <returns></returns>
        public Texture2D BilinearResize(Texture2D source) {
            int targetWidth = source.width;
            int targetHeight = source.height;

            Texture2D result = new Texture2D(targetWidth, targetHeight, source.format, false);
            result.filterMode = FilterMode.Bilinear;
            result.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[targetWidth * targetHeight];

            for (int y = 0; y < targetHeight; y++) {
                for (int x = 0; x < targetWidth; x++) {
                    float u = (x + 0.5f) / targetWidth;
                    float v = (y + 0.5f) / targetHeight;

                    float srcX = (source.width - 1) * u;
                    float srcY = (source.height - 1) * v;

                    int x0 = Mathf.FloorToInt(srcX);
                    int x1 = Mathf.Min(x0 + 1, source.width - 1);
                    int y0 = Mathf.FloorToInt(srcY);
                    int y1 = Mathf.Min(y0 + 1, source.height - 1);

                    Color c00 = source.GetPixel(x0, y0);
                    Color c10 = source.GetPixel(x1, y0);
                    Color c01 = source.GetPixel(x0, y1);
                    Color c11 = source.GetPixel(x1, y1);

                    float tx = srcX - x0;
                    float ty = srcY - y0;

                    Color c0 = Color.Lerp(c00, c10, tx);
                    Color c1 = Color.Lerp(c01, c11, tx);

                    pixels[y * targetWidth + x] = Color.Lerp(c0, c1, ty);
                }
            }

            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// 合并 texArrayPath 下的所有纹理到一张图集上
        /// 但必须按3*3 4*4的平方比例去布置文件夹下的贴图，否则会报错
        /// </summary>
        /// <param name="texArrayPath"></param>
        /// <param name="texSavePath"></param>
        /// <param name="texAltasResoution">新图集的分辨率</param>
        /// <param name="texAtlasSize">新图集合并的纹理的宽/高</param>
        private static void GenerateTextureArray(string texArrayPath, string texSavePath, int texAltasResoution = 2048, int texAtlasSize = 4) {

            if (!Directory.Exists(texArrayPath)) {
                Directory.CreateDirectory(texArrayPath);
            }
            if (!Directory.Exists(texSavePath)) {
                Directory.CreateDirectory(texSavePath);
            }

            string[] texturePaths = Directory.GetFiles(texArrayPath, "*.png", SearchOption.AllDirectories);

            // texAtlasSize must equal to texture asset size
            if (texturePaths.Length != texAtlasSize * texAtlasSize) {
                Debug.LogError(string.Format("wrong texture num, the texAtlasSize^2 should equal to texture num {0}", texturePaths.Length));
                return;
            }

            Texture2D[] rtArray = new Texture2D[texAtlasSize * texAtlasSize];

            // read the terrain textures asset
            int texSize = texAltasResoution / texAtlasSize;
            for (int i = 0; i < texturePaths.Length; i++) {
                string assetPath = texturePaths[i].Replace("\\", "/");
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                rtArray[i] = texture;
            }

            Texture2D exportAtlas = new Texture2D(texAltasResoution, texAltasResoution, TextureFormat.RGB24, false);
            for (int i = 0; i < texAtlasSize; i++) {
                for (int j = 0; j < texAtlasSize; j++) {
                    int curAtlaIdx = i * texAtlasSize + j;
                    int startWidth = i * texSize;
                    int startHeight = j * texSize;

                    for (int q = 0; q < texSize; q++) {
                        for (int p = 0; p < texSize; p++) {
                            // TODO: 这里可能会产生问题，要混合下边缘纹理
                            Color pixelColor = rtArray[curAtlaIdx].GetPixel(q, p);
                            exportAtlas.SetPixel(q + startWidth, p + startHeight, pixelColor);
                        }
                    }

                }
            }

            exportAtlas.Apply();
            byte[] bytes = exportAtlas.EncodeToPNG();
            string atlaName = string.Format("/TerrainTexArray_{0}x{0}.png", texAltasResoution);
            File.WriteAllBytes(texSavePath + atlaName, bytes);
            AssetDatabase.ImportAsset(texSavePath + atlaName);

            Debug.Log("generate texture altas, then you can generate the indexTex and blenderTex");
        }

        public static List<Color> GetRTColorList(RenderTexture rt)
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            Color[] colors = tex.GetPixels();
            List<Color> res = new List<Color>(colors);
            RenderTexture.active = prev;
#if UNITY_EDITOR
            GameObject.DestroyImmediate(tex);
#endif
            return res;
        }

    }
}
