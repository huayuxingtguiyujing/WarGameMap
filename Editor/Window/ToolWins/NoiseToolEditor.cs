using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace LZ.WarGameMap.MapEditor
{
    public class NoiseToolEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.NoiseToolEditor;


        #region 噪声纹理生成

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("混合起点偏移")]
        public float startBlend = 20;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("混合终点偏移")]
        public float endBlend = 50;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("分辨率")]
        public int noiseTexResolution = 512;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("频率")]
        public int frequency = 16;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("噪声随机数")]
        public int randomSpeed = 1;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("FBM迭代次数")]
        public int fbmIteration = 8;

        [FoldoutGroup("噪声纹理生成/噪声设置")]
        [LabelText("噪声导出位置")]
        public string noiseExportPath = MapStoreEnum.NoiseTexOutputPath;

        [FoldoutGroup("噪声纹理生成")]
        [LabelText("当前的噪声纹理")]
        public Texture2D noiseTex;

        [FoldoutGroup("噪声纹理生成")]
        [ValueDropdown("noiseTypeOptions")]
        public string noiseType;

        private static IEnumerable<ValueDropdownItem<string>> noiseTypeOptions = new ValueDropdownList<string> {
            "Perlin", "Vonoro", "space",
        };

        [FoldoutGroup("噪声纹理生成")]
        [Button("生成噪声纹理", ButtonSizes.Medium)]
        private void GenerateNoiseTexture() {
            switch (noiseType) {
                case "Perlin":
                    GeneratePerlin();
                    break;
                case "Voronoi":
                    break;
                case "space":
                    break;
                default:
                    Debug.Log("you do not set the noise type");
                    break;
            }
        }

        struct GeneratePerlinJob : IJobParallelFor {
            [ReadOnly] public PerlinNoise perlinNoise;
            [ReadOnly] public int noiseTexResolution;

            [ReadOnly] public float startBlendX;
            [ReadOnly] public float startBlendY;
            [ReadOnly] public float endBlendX;
            [ReadOnly] public float endBlendY;

            [WriteOnly] public NativeArray<Color> colors;

            public void Execute(int index) {
                int i = index / noiseTexResolution;
                int j = index % noiseTexResolution;

                Vector3 vertPos = new Vector3(i, 0, j);
                Color color = Color.white;

                // NOTE : 目前在2个维度上进行修正，基本原理是通过 smoothstep对噪声进行一遍额外的处理
                // 以产生：噪声正在过渡的效果，或许可以用于地貌的过渡
                float tNorm1 = Mathf.InverseLerp(startBlendX, endBlendX, j);
                float tNorm2 = Mathf.InverseLerp(startBlendY, endBlendY, i);
                float blend = Mathf.SmoothStep(0, 2, (tNorm1 + tNorm2) / 2);

                colors[index] = color * perlinNoise.SampleNoise(vertPos) * blend;

            }

        }

        private void GeneratePerlin() {
            if (noiseTex != null) {
                UnityEngine.Object.DestroyImmediate(noiseTex);
            }

            noiseTex = new Texture2D(noiseTexResolution, noiseTexResolution, TextureFormat.RGB24, false);
            Color[] colors = noiseTex.GetPixels();
            NativeArray<Color> nativeColors = new NativeArray<Color>(colors, Allocator.TempJob);

            GeneratePerlinJob generatePerlinJob = new GeneratePerlinJob {
                perlinNoise = new PerlinNoise(noiseTexResolution, frequency, true, randomSpeed, new Vector2(1, 1), fbmIteration),
                noiseTexResolution = noiseTexResolution,

                startBlendX = startBlend,
                startBlendY = startBlend,
                endBlendX = endBlend,
                endBlendY = endBlend,
                colors = nativeColors,
            };
            JobHandle jobHandle1 = generatePerlinJob.Schedule(noiseTexResolution * noiseTexResolution, 64);
            jobHandle1.Complete();

            noiseTex.SetPixels(nativeColors.ToArray());
            noiseTex.Apply();
            nativeColors.Dispose();

            Debug.Log(string.Format("successfully generate noise texture, resolution : {0}, frequency : {1}, fbm : {2}", noiseTexResolution, frequency, fbmIteration));
        }

        private void GenerateVoronoi() {
            // TODO : voronoi noise!!
        }


        [FoldoutGroup("噪声纹理生成")]
        [Button("保存噪声纹理", ButtonSizes.Medium)]
        private void SaveNoiseTexture() {
            if (noiseTex == null) {
                Debug.LogError("noiseTex is null!");
                return;
            }

            TextureUtility.GetInstance().SaveTextureAsAsset(noiseExportPath, $"noiseTex_{noiseTexResolution}_{frequency}_{Random.Range(0, 1000)}.png", noiseTex);

        }

        #endregion

    }
}
