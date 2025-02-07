
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using LZ.WarGameCommon;
using UnityEditor;
using UnityEngine;

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

        public void SaveTextureAsAsset(string outputPath, string fileName, Texture2D texture) {
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
                Debug.LogError("Texture2D ±‡¬Î ß∞‹£°");
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

            AssetsUtility.GetInstance().SaveAssets<Texture2D>(path, textureFileName, texture);
        }


        private Texture2D LoadTexture(string path, string textureName) {
            return AssetsUtility.GetInstance().LoadAssets<Texture2D>(path, textureName);
        }

    }
}
