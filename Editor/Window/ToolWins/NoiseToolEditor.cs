using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static LZ.WarGameMap.Runtime.FastNoiseLite;

namespace LZ.WarGameMap.MapEditor
{
    public class NoiseToolEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.NoiseToolEditor;

        #region 噪声纹理生成

        [FoldoutGroup("噪声纹理生成/地形设置")]
        [LabelText("纹理分辨率")]
        public int noiseTexResolution = 1024;

        [FoldoutGroup("噪声纹理生成/地形设置")]
        [LabelText("基本高度"), Range(-2, 2)]
        public float baseHeight = 1;

        [FoldoutGroup("噪声纹理生成/地形设置")]
        [LabelText("高度倍率"), Range(1, 50)] 
        public float heightFix = 10;

        [FoldoutGroup("噪声纹理生成/地形设置")]
        [LabelText("FBM迭代次数"), Range(0.01f, 10)] 
        public float elevation = 1.0f;


        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("噪声种类")]
        public NoiseType noiseType = NoiseType.Perlin;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("噪声种子")]
        public int randomSeed = 1227;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("频率")]
        public float frequency = 0.010f;


        [FoldoutGroup("噪声纹理生成/噪声分形设置")]
        [LabelText("分形种类")]
        public FractalType fractalType = FractalType.FBm;

        [FoldoutGroup("噪声纹理生成/噪声分形设置")]
        [LabelText("分形迭代次数")]
        public int octaves = 3;

        [FoldoutGroup("噪声纹理生成/噪声分形设置")]
        [LabelText("")]
        public float lacunarity = 2.0f;

        [FoldoutGroup("噪声纹理生成/噪声分形设置")]
        [LabelText("")]
        public float gain = 0.5f;

        [FoldoutGroup("噪声纹理生成/噪声分形设置")]
        [LabelText("")]
        public float weightedStrength = 0;

        [FoldoutGroup("噪声纹理生成/噪声分形设置")]
        [LabelText("")]
        public float pingpongStrength = 0;


        [FoldoutGroup("噪声纹理生成")]
        [LabelText("噪声导出位置")]
        public string noiseExportPath = MapStoreEnum.NoiseTexOutputPath;

        [FoldoutGroup("噪声纹理生成")]
        [LabelText("当前的噪声纹理")]
        public Texture2D noiseTex;

        FastNoiseLite fastNoiseLite;


        [FoldoutGroup("噪声纹理生成")]
        [Button("生成噪声纹理", ButtonSizes.Medium)]
        private void GenerateNoiseTexture() {
            InitNoise();
            if (noiseTex != null)
            {
                UnityEngine.Object.DestroyImmediate(noiseTex);
            }
            noiseTex = new Texture2D(noiseTexResolution, noiseTexResolution, TextureFormat.RGB24, false);
            List<Color> nativeColors = new List<Color>(noiseTex.GetPixels());
            Parallel.ForEach(nativeColors, (color, state, index) =>
            {
                int idx = (int)index;
                int i = idx / noiseTexResolution;
                int j = idx % noiseTexResolution;

                float noise = fastNoiseLite.GetNoise(i, j) + baseHeight;
                noise = Mathf.Pow(noise, elevation);
                float height = noise * heightFix;
                nativeColors[idx] = new Color(height, height, height, 1f);
            });
            noiseTex.SetPixels(nativeColors.ToArray());
            noiseTex.Apply();
            Debug.Log(string.Format("successfully generate noise texture, resolution : {0}, frequency : {1}, fbm : {2}", noiseTexResolution, frequency, octaves));
        }

        private void InitNoise()
        {
            fastNoiseLite = new FastNoiseLite(randomSeed);
            fastNoiseLite.SetNoiseType(noiseType);
            fastNoiseLite.SetFrequency(frequency);

            fastNoiseLite.SetFractalType(fractalType);
            fastNoiseLite.SetFractalOctaves(octaves);
            fastNoiseLite.SetFractalLacunarity(lacunarity);
            fastNoiseLite.SetFractalGain(gain);
            fastNoiseLite.SetFractalWeightedStrength(weightedStrength);
            fastNoiseLite.SetFractalPingPongStrength(pingpongStrength);
        }

        [FoldoutGroup("噪声纹理生成")]
        [Button("保存噪声纹理", ButtonSizes.Medium)]
        private void SaveNoiseTexture() {
            if (noiseTex == null) {
                Debug.LogError("noiseTex is null!");
                return;
            }
            TextureUtility.SaveTextureAsAsset(noiseExportPath, $"noiseTex_{noiseTexResolution}_{frequency}_{UnityEngine.Random.Range(0, 1000)}.png", noiseTex);
        }

        #endregion


        public override void Destory() {
            if (noiseTex != null) {
                GameObject.DestroyImmediate(noiseTex);
                noiseTex = null;
            }
        }

    }
}
