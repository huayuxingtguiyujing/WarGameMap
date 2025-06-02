using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
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

        #region 构建设置

        [FoldoutGroup("构建设置")]
        [LabelText("导出时翻转")]
        public bool ExportFlipVertically = true;

        [FoldoutGroup("构建设置")]
        [LabelText("导出分辨率")]
        public int ExportTexResolution = 1024;

        // TODO : 用上它
        [FoldoutGroup("构建设置")]
        [LabelText("导出分辨率")]
        [Tooltip("这一项对应地形中 cluster 的 num per line")]
        public int ExportTexSize = 4;


        [FoldoutGroup("构建设置")]
        [LabelText("当前使用的高度图数据")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("构建设置")]
        [LabelText("起始经纬度")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        #endregion


        #region 构建地貌贴图
        // NOTE : 该块的代码用于生成简单版本的地貌，即：不同的高度对应不同的纯色地貌

        [FoldoutGroup("构建地貌贴图/地貌纹理设置")]
        [LabelText("缺失部分填充颜色")]
        public Color LostColor = Color.blue;

        [FoldoutGroup("构建地貌贴图/地貌纹理设置")]
        [LabelText("各高度对应颜色")]
        public Gradient heightGradient;

        [FoldoutGroup("构建地貌贴图/地貌纹理设置")]
        [LabelText("使用柏林噪声处理")]
        public bool openPerlinNoise = true;

        [FoldoutGroup("构建地貌贴图")]
        [LabelText("当前操作的地貌纹理")]
        [Tooltip("注意，这个Texture可能不是在磁盘中已经存储的")]
        public Texture2D curHandleLandformTex;

        [FoldoutGroup("构建地貌贴图")]
        [LabelText("当前操作的地貌图位置")]
        public string curLandformTexPath;

        [FoldoutGroup("构建地貌贴图")]
        [LabelText("贴图导出位置")]
        public string landformTexImportPath = MapStoreEnum.LandformTexOutputPath;

        [FoldoutGroup("构建地貌贴图")]
        [Button("导入地貌纹理图", ButtonSizes.Medium)]
        private void ImportLandFormTex() {
            string landformImportPath = EditorUtility.OpenFilePanel("Import Landform Texture", "", "");
            if (landformImportPath == "") {
                Debug.LogError("you do not get the landform texture");
                return;
            }

            curLandformTexPath = AssetsUtility.GetInstance().TransToAssetPath(landformImportPath);
            curHandleLandformTex = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(curLandformTexPath);
            if (curHandleLandformTex == null) {
                curLandformTexPath = "";
                Debug.LogError(string.Format("can not load landform texture from this path: {0}", curLandformTexPath));
                return;
            }
        }

        [FoldoutGroup("构建地貌贴图")]
        [Button("导出地貌纹理图", ButtonSizes.Medium)]
        private void ExportLandFormTex() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            // use perlin noise
            //PerlinGenerator perlinGenerator = new PerlinGenerator();
            //perlinGenerator.GeneratePerlinNoise(ExportTexResolution / 4, ExportTexResolution / 4);
            PerlinNoise perlinNoise = new PerlinNoise(ExportTexResolution, 16, true, 4, new Vector2(1, 1), 8);

            // renderTexture, 效率更高
            //RenderTexture tmpTex = RenderTexture.GetTemporary(ExportTexResolution, ExportTexResolution, 24);
            //Graphics.Blit(terrainDataSplats[i].texture, rtArray[i], defaultMaterial, 0);
            //RenderTexture.active = tmpTex;
            //RenderTexture.ReleaseTemporary(tmpTex);
            //curHandleTex.ReadPixels(new Rect(0, 0, ExportTexResolution, ExportTexResolution), 0, 0);

            // TODO: 改用 job system
            curHandleLandformTex = new Texture2D(ExportTexResolution, ExportTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleLandformTex.GetPixels();

            // TODO : 如果超出，就生成多张地貌图！（每个cluster要对应一张贴图，后面还可以用来做 VT）
            for (int i = 0; i < ExportTexResolution; i++) {
                for (int j = 0; j < ExportTexResolution; j++) {
                    Vector3 vertPos = new Vector3(i, 0, j);
                    vertPos.y = heightDataManager.SampleFromHeightData(startLongitudeLatitude, vertPos);

                    int idx;
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
                    color = Color.white;
                    if (openPerlinNoise) {
                        //colors[idx] = perlinGenerator.SampleNoise(vertPos) * color;
                        colors[idx] = perlinNoise.SampleNoise(vertPos) * color;
                    } else {
                        colors[idx] = color;
                    }
                }
            }
            curHandleLandformTex.SetPixels(colors);
            curHandleLandformTex.Apply();

            perlinNoise.Dispose();

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

        [FoldoutGroup("构建地貌贴图")]
        [Button("保存当前地貌纹理", ButtonSizes.Medium)]
        private void SaveLandFormTex() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("landform_{0}x{0}_{1}", ExportTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(landformTexImportPath, texName, curHandleLandformTex);
        }

        #endregion


        #region 构建六边形地貌贴图
        // NOTE : 

        #endregion


        #region 构建法线贴图


        [FoldoutGroup("构建法线贴图")]
        [LabelText("当前操作的法线纹理")]
        public Texture2D curHandleNormalTex;

        [FoldoutGroup("构建法线贴图")]
        [LabelText("当前操作的法线图位置")]
        public string curNormalTexPath;

        [FoldoutGroup("构建法线贴图")]
        [LabelText("贴图导出位置")]
        public string normalTexImportPath = MapStoreEnum.NormalTexOutputPath;

        [FoldoutGroup("构建法线贴图")]
        [Button("导入地形法线图", ButtonSizes.Medium)]
        private void ImportNormalTexture() {
            string normalImportPath = EditorUtility.OpenFilePanel("Import Normal Texture", "", "");
            if (normalImportPath == "") {
                Debug.LogError("you do not get the normal texture");
                return;
            }

            curNormalTexPath = AssetsUtility.GetInstance().TransToAssetPath(normalImportPath);
            curHandleNormalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(curNormalTexPath);
            if (curHandleNormalTex == null) {
                curNormalTexPath = "";
                Debug.LogError(string.Format("can not load normal texture from this path: {0}", curNormalTexPath));
                return;
            }
        }

        [FoldoutGroup("构建法线贴图")]
        [Button("导出地形法线图", ButtonSizes.Medium)]
        private void ExportNormalTexture() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            curHandleNormalTex = new Texture2D(ExportTexResolution, ExportTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleNormalTex.GetPixels();

            // ERROR 为什么全是绿色？

            // TODO : 如果超出，就生成多张法线图！（每个cluster要对应一张贴图）
            // TODO : 根据 cluster 的宽度 生成法线图
            for (int i = 0; i < ExportTexResolution; i++) {
                for (int j = 0; j < ExportTexResolution; j++) {
                    Vector3 vertPos = new Vector3(i, 0, j);
                    Vector3 normal = heightDataManager.SampleNormalFromData(startLongitudeLatitude, vertPos);
                    Color normalColor = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f);
                    int idx;
                    if (ExportFlipVertically) {
                        idx = j * ExportTexResolution + i;
                    } else {
                        idx = i * ExportTexResolution + j;
                    }
                    colors[idx] = normalColor;
                }
            }
            curHandleNormalTex.SetPixels(colors);
            curHandleNormalTex.Apply();

            curHandleNormalTex = TextureUtility.GetInstance().BilinearResize(curHandleNormalTex);

            Debug.Log(string.Format("successfully generate texture, resolution : {0}x{0}", ExportTexResolution));
        }

        [FoldoutGroup("构建法线贴图")]
        [Button("保存当前法线纹理", ButtonSizes.Medium)]
        private void SaveNormalTexture() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("normal_{0}x{0}_{1}", ExportTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(normalTexImportPath, texName, curHandleNormalTex);
        }

        #endregion


        #region 构建索引/混合贴图

        // TODO : 目前要怎么搞，是不是应该弄些高级点的噪声，不止是perlin呢
        // 说不定还要看些算法

        // step1 : 找可能可以用的噪声/自动化生成算法，可能要读读论文，多问问GPT
        // step2 : 应用这个算法，生成一版混合纹理，然后用这个混合纹理去做纹理混合（先不考虑索引贴图）
        // step3 : 思考怎么搞索引贴图，嘶......是不是也应该去自动化生成一下呢（目前没有头绪）

        // 另外，把那篇混合纹理地貌的文章实践出来！！！（反正只是搬运代码

        #endregion

        #region 混合纹理地貌

        [FoldoutGroup("混合纹理地貌生成", order: 99)]
        [LabelText("NOTE")]  // NOTE : 其实后面可能不止会用到16张地貌纹理，要考虑后续扩充
        public string blenderTexLandformStr = "此部分用于做混合地貌方案，可以根据参数自动生成无缝的混合纹理/索引纹理";

        // NOTE : 以下是打算做混合纹理地貌方案
        [FoldoutGroup("混合纹理地貌生成", order: 99)]
        [LabelText("当前使用的地貌纹理图集")]  // NOTE : 其实后面可能不止会用到16张地貌纹理，要考虑后续扩充
        public List<Texture2D> curTerrainTexSplats;

        [FoldoutGroup("混合纹理地貌生成")]
        [Button("导出索引贴图", ButtonSizes.Medium)]
        private void ExportIdxTex() {
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


        #endregion



    }
}
