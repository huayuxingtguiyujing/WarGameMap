using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace LZ.WarGameMap.MapEditor
{
    public class TextureToolEditor : BaseMapEditor {
        public override string EditorName => MapEditorEnum.TextureToolEditor;

        [FoldoutGroup("����scene", -1)]
        [Button("��ʼ����ò����")]
        protected override void InitEditor() {

        }

        #region ����ͼ������

        [FoldoutGroup("��òͼ������")]
        [LabelText("��ò������Ŀ")]
        public int texListSize = 16;

        [FoldoutGroup("��òͼ������")]
        [LabelText("����ͼ���ֱ���")]
        public int baseResolution = 2048;

        [FoldoutGroup("��òͼ������")]
        [LabelText("��ò������")]
        public List<Texture2D> curTerrainTexList;

        [FoldoutGroup("��òͼ������")]
        [LabelText("���ɵ�����ͼ��")]
        public Texture2D terrainTexSplat;

        [FoldoutGroup("��òͼ������")]
        [LabelText("ͼ������λ��")]
        public string exportPath = MapStoreEnum.TerrainTexOutputPath;

        [FoldoutGroup("��òͼ������")]
        [Button("��������׼��", ButtonSizes.Medium)]
        private void HandleTexData() {
            curTerrainTexList = new List<Texture2D>();
            for (int i = 0; i < texListSize; i++) {
                curTerrainTexList.Add(null);
            }
        }

        [FoldoutGroup("��òͼ������")]
        [Button("���ɵ�òͼ��", ButtonSizes.Medium)]
        private void GenerateTexSplat() {
            if (terrainTexSplat != null) {
                Debug.LogError("already exist texture splat��please clean before generate");
                return;
            }
            
            int texSize = baseResolution / 4;
            terrainTexSplat = new Texture2D(baseResolution, baseResolution, TextureFormat.RGB24, false);

            // 4 * 4 ��ͼ���������Դ�ȡ��ÿ��/�е� index
            for (int i = 0; i < texListSize; i++) {
                int controlColumn = i % 4;
                int controlRow = (i % 16) / 4;
                int startWidth = controlColumn * texSize;
                int startHeight = controlRow * texSize;

                for (int j = 0; j < texSize; j++) {
                    for (int k = 0; k < texSize; k++) {
                        Color pixelColor = Color.blue;
                        if (curTerrainTexList[i] != null) {
                            pixelColor = GetPixelColor(j, k, curTerrainTexList[i]);
                        }
                        terrainTexSplat.SetPixel(j + startWidth, k + startHeight, pixelColor);
                        //Color normalColor = (terrainDataSplats[i].normalMap == null)?new Color(0.5f,0.5f,1): GetPixelColor(j, k, normalArray[i]);
                        //normalAtlas.SetPixel(j + startWidth, k + startHeight, normalColor);
                    }
                }
                terrainTexSplat.Apply();
            }
            Debug.LogError("texture splat generate successfully��");
        }

        private Color GetPixelColor(int rowNum, int columnNum, Texture2D oriTex) {
            //���߼�������
            Color oriColor;
            int minNum = baseResolution / 128 - 1;
            int maxNum = baseResolution / 4 - minNum + 1;

            //�ĸ���
            if (rowNum <= minNum && columnNum <= minNum) {
                oriColor = oriTex.GetPixel(minNum, minNum);
            } else if (rowNum <= minNum && columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(minNum, maxNum);
            } else if (rowNum >= maxNum && columnNum <= minNum) {
                oriColor = oriTex.GetPixel(maxNum, minNum);
            } else if (rowNum >= maxNum && columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(maxNum, maxNum);
            }
            //������
            else if (rowNum <= minNum) {
                oriColor = oriTex.GetPixel(minNum, columnNum);
            } else if (rowNum >= maxNum) {
                oriColor = oriTex.GetPixel(maxNum, columnNum);
            } else if (columnNum <= minNum) {
                oriColor = oriTex.GetPixel(rowNum, minNum);
            } else if (columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(rowNum, maxNum);
            }
            else {        //��������
                oriColor = oriTex.GetPixel(rowNum, columnNum);
            }
            return oriColor;
        }

        [FoldoutGroup("��òͼ������")]
        [Button("�����òͼ��", ButtonSizes.Medium)]
        private void SaveSplatTexture() {
            if (terrainTexSplat == null) {
                Debug.LogError("terrainTexSplat is null!");
                return;
            }

            byte[] bytes = terrainTexSplat.EncodeToPNG();
            TextureUtility.GetInstance().SaveTextureAsAsset(exportPath, "TerrainSplat_AAA.png", terrainTexSplat);

        }

        #endregion


        #region ������������

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

        private void GeneratePerlin() {
            PerlinNoise perlinNoise = new PerlinNoise(noiseTexResolution, frequency, true, randomSpeed, new Vector2(1, 1), fbmIteration);

            noiseTex = new Texture2D(noiseTexResolution, noiseTexResolution, TextureFormat.RGB24, false);
            Color[] colors = noiseTex.GetPixels();

            for (int i = 0; i < noiseTexResolution; i++) {
                for (int j = 0; j < noiseTexResolution; j++) {
                    Vector3 vertPos = new Vector3(i, 0, j);
                    int idx;
                    idx = j * noiseTexResolution + i;
                    Color color = Color.white; 
                    colors[idx] = perlinNoise.SampleNoise(vertPos) * color; 
                }
            }
            noiseTex.SetPixels(colors);
            noiseTex.Apply();
            perlinNoise.Dispose();
            Debug.Log(string.Format("successfully generate noise texture, resolution : {0}, frequency : {1}, fbm : {2}", noiseTexResolution, frequency, fbmIteration));
        }

        private void GenerateVoronoi() {
            // TODO : 
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


        #region �����ϲ���

        [FoldoutGroup("�����ϲ���")]
        [LabelText("���Ȩ������")]
        public Texture2D blenderTex;

        [FoldoutGroup("�����ϲ���")]
        [LabelText("�������ͼ��")]
        public Texture2D blenderTexSplat;

        [FoldoutGroup("�����ϲ���")]
        [LabelText("���ɵĻ�ϵ�ò����")]
        public Texture2D targetTerrainTex;

        [FoldoutGroup("�����ϲ���")]
        [Button("���ɻ�ϵ�ò����", ButtonSizes.Medium)]
        private void GenerateBlendTerrain() {
            // ���ɶ��ĳߴ磿 �� �������һ������


        }

        [FoldoutGroup("��òͼ������")]
        [Button("�����ϵ�ò����", ButtonSizes.Medium)]
        private void SaveBlendTerrainTexture() {
            if (targetTerrainTex == null) {
                Debug.Log("you do not generate blend terrain texture");
                return;
            }
            // TODO : save to asset path
        }

        #endregion

    }
}
