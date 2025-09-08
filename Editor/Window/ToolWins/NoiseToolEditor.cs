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

        #region ������������

        [FoldoutGroup("������������/��������")]
        [LabelText("����ֱ���")]
        public int noiseTexResolution = 1024;

        [FoldoutGroup("������������/��������")]
        [LabelText("�����߶�"), Range(-2, 2)]
        public float baseHeight = 1;

        [FoldoutGroup("������������/��������")]
        [LabelText("�߶ȱ���"), Range(1, 50)] 
        public float heightFix = 10;

        [FoldoutGroup("������������/��������")]
        [LabelText("FBM��������"), Range(0.01f, 10)] 
        public float elevation = 1.0f;


        [FoldoutGroup("������������/��������")]
        [LabelText("��������")]
        public NoiseType noiseType = NoiseType.Perlin;

        [FoldoutGroup("������������/��������")]
        [LabelText("��������")]
        public int randomSeed = 1227;

        [FoldoutGroup("������������/��������")]
        [LabelText("Ƶ��")]
        public float frequency = 0.010f;


        [FoldoutGroup("������������/������������")]
        [LabelText("��������")]
        public FractalType fractalType = FractalType.FBm;

        [FoldoutGroup("������������/������������")]
        [LabelText("���ε�������")]
        public int octaves = 3;

        [FoldoutGroup("������������/������������")]
        [LabelText("")]
        public float lacunarity = 2.0f;

        [FoldoutGroup("������������/������������")]
        [LabelText("")]
        public float gain = 0.5f;

        [FoldoutGroup("������������/������������")]
        [LabelText("")]
        public float weightedStrength = 0;

        [FoldoutGroup("������������/������������")]
        [LabelText("")]
        public float pingpongStrength = 0;


        [FoldoutGroup("������������")]
        [LabelText("��������λ��")]
        public string noiseExportPath = MapStoreEnum.NoiseTexOutputPath;

        [FoldoutGroup("������������")]
        [LabelText("��ǰ����������")]
        public Texture2D noiseTex;

        FastNoiseLite fastNoiseLite;


        [FoldoutGroup("������������")]
        [Button("������������", ButtonSizes.Medium)]
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

        [FoldoutGroup("������������")]
        [Button("������������", ButtonSizes.Medium)]
        private void SaveNoiseTexture() {
            if (noiseTex == null) {
                Debug.LogError("noiseTex is null!");
                return;
            }
            TextureUtility.GetInstance().SaveTextureAsAsset(noiseExportPath, $"noiseTex_{noiseTexResolution}_{frequency}_{UnityEngine.Random.Range(0, 1000)}.png", noiseTex);
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
