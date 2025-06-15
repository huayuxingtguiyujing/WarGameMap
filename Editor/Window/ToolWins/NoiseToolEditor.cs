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


        #region ������������

        [FoldoutGroup("������������/��������")]
        [LabelText("������ƫ��")]
        public float startBlend = 20;

        [FoldoutGroup("������������/��������")]
        [LabelText("����յ�ƫ��")]
        public float endBlend = 50;

        [FoldoutGroup("������������/��������")]
        [LabelText("�ֱ���")]
        public int noiseTexResolution = 512;

        [FoldoutGroup("������������/��������")]
        [LabelText("Ƶ��")]
        public int frequency = 16;

        [FoldoutGroup("������������/��������")]
        [LabelText("���������")]
        public int randomSpeed = 1;

        [FoldoutGroup("������������/��������")]
        [LabelText("FBM��������")]
        public int fbmIteration = 8;

        [FoldoutGroup("������������/��������")]
        [LabelText("��������λ��")]
        public string noiseExportPath = MapStoreEnum.NoiseTexOutputPath;

        [FoldoutGroup("������������")]
        [LabelText("��ǰ����������")]
        public Texture2D noiseTex;

        [FoldoutGroup("������������")]
        [ValueDropdown("noiseTypeOptions")]
        public string noiseType;

        private static IEnumerable<ValueDropdownItem<string>> noiseTypeOptions = new ValueDropdownList<string> {
            "Perlin", "Vonoro", "space",
        };

        [FoldoutGroup("������������")]
        [Button("������������", ButtonSizes.Medium)]
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

                // NOTE : Ŀǰ��2��ά���Ͻ�������������ԭ����ͨ�� smoothstep����������һ�����Ĵ���
                // �Բ������������ڹ��ɵ�Ч��������������ڵ�ò�Ĺ���
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


        [FoldoutGroup("������������")]
        [Button("������������", ButtonSizes.Medium)]
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
