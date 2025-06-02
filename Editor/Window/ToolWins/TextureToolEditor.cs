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

        [FoldoutGroup("配置scene", -1)]
        [Button("初始化地貌配置")]
        protected override void InitEditor() {

        }

        #region 纹理图集生成

        [FoldoutGroup("地貌图集生成")]
        [LabelText("地貌纹理数目")]
        public int texListSize = 16;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("纹理图集分辨率")]
        public int baseResolution = 2048;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("地貌纹理集合")]
        public List<Texture2D> curTerrainTexList;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("生成的纹理图集")]
        public Texture2D terrainTexSplat;

        [FoldoutGroup("地貌图集生成")]
        [LabelText("图集导出位置")]
        public string exportPath = MapStoreEnum.TerrainTexOutputPath;

        [FoldoutGroup("地貌图集生成")]
        [Button("纹理数据准备", ButtonSizes.Medium)]
        private void HandleTexData() {
            curTerrainTexList = new List<Texture2D>();
            for (int i = 0; i < texListSize; i++) {
                curTerrainTexList.Add(null);
            }
        }

        [FoldoutGroup("地貌图集生成")]
        [Button("生成地貌图集", ButtonSizes.Medium)]
        private void GenerateTexSplat() {
            if (terrainTexSplat != null) {
                Debug.LogError("already exist texture splat，please clean before generate");
                return;
            }
            
            int texSize = baseResolution / 4;
            terrainTexSplat = new Texture2D(baseResolution, baseResolution, TextureFormat.RGB24, false);

            // 4 * 4 的图集，可以以此取到每行/列的 index
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
            Debug.LogError("texture splat generate successfully！");
        }

        private Color GetPixelColor(int rowNum, int columnNum, Texture2D oriTex) {
            //拉边几个像素
            Color oriColor;
            int minNum = baseResolution / 128 - 1;
            int maxNum = baseResolution / 4 - minNum + 1;

            //四个角
            if (rowNum <= minNum && columnNum <= minNum) {
                oriColor = oriTex.GetPixel(minNum, minNum);
            } else if (rowNum <= minNum && columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(minNum, maxNum);
            } else if (rowNum >= maxNum && columnNum <= minNum) {
                oriColor = oriTex.GetPixel(maxNum, minNum);
            } else if (rowNum >= maxNum && columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(maxNum, maxNum);
            }
            //四条边
            else if (rowNum <= minNum) {
                oriColor = oriTex.GetPixel(minNum, columnNum);
            } else if (rowNum >= maxNum) {
                oriColor = oriTex.GetPixel(maxNum, columnNum);
            } else if (columnNum <= minNum) {
                oriColor = oriTex.GetPixel(rowNum, minNum);
            } else if (columnNum >= maxNum) {
                oriColor = oriTex.GetPixel(rowNum, maxNum);
            }
            else {        //正常采样
                oriColor = oriTex.GetPixel(rowNum, columnNum);
            }
            return oriColor;
        }

        [FoldoutGroup("地貌图集生成")]
        [Button("保存地貌图集", ButtonSizes.Medium)]
        private void SaveSplatTexture() {
            if (terrainTexSplat == null) {
                Debug.LogError("terrainTexSplat is null!");
                return;
            }

            byte[] bytes = terrainTexSplat.EncodeToPNG();
            TextureUtility.GetInstance().SaveTextureAsAsset(exportPath, "TerrainSplat_AAA.png", terrainTexSplat);

        }

        #endregion


        #region 噪声纹理生成

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


        #region 纹理混合测试

        [FoldoutGroup("纹理混合测试")]
        [LabelText("混合权重纹理")]
        public Texture2D blenderTex;

        [FoldoutGroup("纹理混合测试")]
        [LabelText("混合纹理图集")]
        public Texture2D blenderTexSplat;

        [FoldoutGroup("纹理混合测试")]
        [LabelText("生成的混合地貌纹理")]
        public Texture2D targetTerrainTex;

        [FoldoutGroup("纹理混合测试")]
        [Button("生成混合地貌纹理", ButtonSizes.Medium)]
        private void GenerateBlendTerrain() {
            // 生成多大的尺寸？ 和 混合纹理一样大吗？


        }

        [FoldoutGroup("地貌图集生成")]
        [Button("保存混合地貌纹理", ButtonSizes.Medium)]
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
