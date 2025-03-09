using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class LandformEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.LandformEditor;

        TerrainConstructor TerrainCtor;

        [FoldoutGroup("配置scene", -1)]
        [Button("初始化地貌配置")]
        protected override void InitEditor() {
            GameObject mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
            if (mapRootObj == null) {
                mapRootObj = new GameObject(MapSceneEnum.MapRootName);
            }
            TerrainCtor = mapRootObj.GetComponent<TerrainConstructor>();
            if (TerrainCtor == null) {
                Debug.LogError("unable to find TerrainCtor, please goto terrainWindow to config it");
                return;
            }
        }

        [FoldoutGroup("构建地貌/地貌纹理设置")]
        [LabelText("导出时翻转")]
        public bool ExportFlipVertically = true;

        [FoldoutGroup("构建地貌/地貌纹理设置")]
        [LabelText("导出分辨率")]
        public int ExportTexResolution = 1024;

        [FoldoutGroup("构建地貌/地貌纹理设置")]
        [LabelText("缺失部分填充颜色")]
        public Color LostColor = Color.blue;

        [FoldoutGroup("构建地貌/地貌纹理设置")]
        [LabelText("各高度对应颜色")]
        public Gradient heightGradient;

        [FoldoutGroup("构建地貌")]
        [LabelText("当前使用的高度图数据")]
        public List<HeightDataModel> heightDataModels;


        [FoldoutGroup("构建地貌")]
        [LabelText("使用柏林噪声处理")]
        public bool openPerlinNoise = true;

        [FoldoutGroup("构建地貌")]
        [LabelText("起始经纬度")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        [FoldoutGroup("构建地貌")]
        [LabelText("当前操作的地貌纹理")]
        [Tooltip("注意，这个Texture可能不是在磁盘中已经存储的")]
        public Texture2D curHandleTex;

        [FoldoutGroup("构建地貌")]
        [LabelText("地貌图导出位置")]
        public string landformTexImportPath = MapStoreEnum.LandformTexOutputPath;

        [FoldoutGroup("构建地貌")]
        [LabelText("当前操作的地貌图位置")]
        public string curLandformTexPath;
        
        [FoldoutGroup("构建地貌")]
        [Button("导入地貌纹理图", ButtonSizes.Medium)]
        private void ImportLandFormTex() {
            string landformImportPath = EditorUtility.OpenFilePanel("Import Raw Heightmap", "", "");
            if (landformImportPath == "") {
                Debug.LogError("you do not get the height map");
                return;
            }

            curLandformTexPath = AssetsUtility.GetInstance().TransToAssetPath(landformImportPath);
            curHandleTex = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(curLandformTexPath);
            if (curHandleTex == null) {
                curLandformTexPath = "";
                Debug.LogError(string.Format("can not load landform texture from this path: {0}", curLandformTexPath));
                return;
            }
        }

        [FoldoutGroup("构建地貌")]
        [Button("导出地貌纹理图", ButtonSizes.Medium)]
        private void ExportLandFormTex() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            // use perlin noise
            PerlinGenerator perlinGenerator = new PerlinGenerator();
            perlinGenerator.GeneratePerlinNoise(ExportTexResolution, ExportTexResolution);

            // renderTexture, 效率更高
            //RenderTexture tmpTex = RenderTexture.GetTemporary(ExportTexResolution, ExportTexResolution, 24);
            //Graphics.Blit(terrainDataSplats[i].texture, rtArray[i], defaultMaterial, 0);
            //RenderTexture.active = tmpTex;
            //RenderTexture.ReleaseTemporary(tmpTex);
            //curHandleTex.ReadPixels(new Rect(0, 0, ExportTexResolution, ExportTexResolution), 0, 0);

            // TODO: 改用 job system
            curHandleTex = new Texture2D(ExportTexResolution, ExportTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleTex.GetPixels();
            for (int i = 0; i < ExportTexResolution; i++) {
                for (int j = 0; j < ExportTexResolution; j++) {
                    Vector3 vertPos = new Vector3(i, 0, j);
                    vertPos.y = heightDataManager.SampleFromHeightData(startLongitudeLatitude, vertPos);

                    int idx = 0;
                    if (ExportFlipVertically) {
                        idx = j * ExportTexResolution + i;
                    } else {
                        idx = i * ExportTexResolution + j;
                    }
                    // TODO : 要加入：温度、湿度 如何对应纹理？
                    //float humidity = LandformDataModel.GetHumidity(vertPos, startLongitudeLatitude);
                    //float temperature = LandformDataModel.GetTemperature(vertPos, startLongitudeLatitude);
                    //Color color = LandformDataModel.SampleColor(humidity, temperature);
                    Color color = GetColorByHeight(vertPos.y);
                    if (openPerlinNoise) {
                        colors[idx] = perlinGenerator.SampleNosie(vertPos) * color;
                    } else {
                        colors[idx] = color;
                    }
                }
            }
            curHandleTex.SetPixels(colors);
            curHandleTex.Apply();

            Debug.Log(string.Format("successfully generate texture, resolution : {0}x{0}", ExportTexResolution));
        }

        private Color GetColorByHeight(float height) {
            if (height <= -4f) {
                return new Color(0.04f, 0.38f, 0.66f); // 深蓝色 #0A60A8
            } else if (height <= 0f) {
                return new Color(0.53f, 0.81f, 0.92f); // 浅蓝色 #87CEEB
            } else if (height <= 10f) {
                return new Color(0.20f, 0.60f, 0.20f); // 浅绿色 #339933 (表示平原)
            } else if (height <= 14f) {
                return new Color(0.42f, 0.69f, 0.30f); // 草绿色 #6AB04C
            } else if (height <= 21f) {
                return new Color(0.78f, 0.65f, 0.35f); // 黄绿色 #C8A55A
            } else if (height <= 28f) {
                return new Color(0.62f, 0.49f, 0.34f); // 浅棕色 #9E7E57
            } else if (height <= 32f) {
                return new Color(0.85f, 0.85f, 0.85f); // 浅灰色 #D8D8D8
            } else {
                return new Color(1f, 1f, 1f);           // 纯白色 #FFFFFF
            }
        }

        [FoldoutGroup("构建地貌")]
        [Button("保存当前纹理", ButtonSizes.Medium)]
        private void SaveLandFormTex() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("landform_{0}x{0}_{1}", ExportTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(landformTexImportPath, texName, curHandleTex);
        }

        [FoldoutGroup("构建地貌")]
        [Button("应用当前纹理", ButtonSizes.Medium)]
        private void ApplyLandFormTex() { 
            // TODO : 好像不用做
        }


        // TODO : 导出以下的所有贴图

        [FoldoutGroup("混合纹理地貌生成")]
        [LabelText("当前使用的地貌纹理图集")]
        public Texture2D curTerrainTexSplat;

        [FoldoutGroup("混合纹理地貌生成")]
        [Button("导出索引贴图", ButtonSizes.Medium)]
        private void ImportIdxTex() {
            // 也许不需要导出索引贴图 + 混合贴图，直接使用 color 制作？
        }

        [FoldoutGroup("混合纹理地貌生成")]
        [Button("导出混合贴图", ButtonSizes.Medium)]
        private void ExportMixTex() {

        }

        [FoldoutGroup("混合纹理地貌生成")]
        [Button("生成最终混合地貌", ButtonSizes.Medium)]
        private void ExportMixLandformTex() {

        }



        // TODO : 下面的代码要缝到上面
        // TODO : 利用下面的代码，+上上面的，生成混合纹理地貌
        // 地貌模型 ： 基于 高度 - 湿度 - 温度 来确定一个地区应该采用的地貌图（纹理）

        // 参考 ：欧陆风云4

        [FoldoutGroup("图集生成")]
        [LabelText("图集规格")]
        [Tooltip("例如值为4,则图集的规格是4x4")]
        public int texAtlasSize = 4;

        [FoldoutGroup("图集生成")]
        [LabelText("分辨率")]
        public int texResoution = 2048;

        [FoldoutGroup("图集生成")]
        [LabelText("默认材质")]
        public Material defaultTerrainMat;

        public const string texArrayPath = MapStoreEnum.TerrainTexArrayPath;
        public const string texSavePath = MapStoreEnum.TerrainTexOutputPath;

        [FoldoutGroup("图集生成")]
        [Button("生成纹理图集")]
        private void GenerateTextureArray() {

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
            int texSize = texResoution / texAtlasSize;
            for (int i = 0; i < texturePaths.Length; i++) {
                string assetPath = texturePaths[i].Replace("\\", "/");
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                rtArray[i] = texture;
            }

            Texture2D exportAtlas = new Texture2D(texResoution, texResoution, TextureFormat.RGB24, false);
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
            string atlaName = string.Format("/TerrainTexArray_{0}x{0}.png", texResoution);
            File.WriteAllBytes(texSavePath + atlaName, bytes);
            AssetDatabase.ImportAsset(texSavePath + atlaName);

            Debug.Log("generate texture altas, then you can generate the indexTex and blenderTex");
        }

        [FoldoutGroup("图集生成")]
        [Button("生成索引与混合纹理")]
        private void GenerateIndexBlenderTex() {


            string atlaName = string.Format("/TerrainIndexArray_{0}x{0}.png", texResoution);
            //File.WriteAllBytes(texSavePath + atlaName, bytes);
            AssetDatabase.ImportAsset(texSavePath + atlaName);

            Debug.Log("generate index altas, then you can generate the blenderTex");
        }

        [FoldoutGroup("图集生成")]
        [Button("生成地形纹理")]
        private void GenerateSplatTex() {

        }


    }
}
