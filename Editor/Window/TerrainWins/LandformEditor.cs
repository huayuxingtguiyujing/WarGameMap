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

        [FoldoutGroup("����scene", -1)]
        [Button("��ʼ����ò����")]
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

        [FoldoutGroup("������ò/��ò��������")]
        [LabelText("����ʱ��ת")]
        public bool ExportFlipVertically = true;

        [FoldoutGroup("������ò/��ò��������")]
        [LabelText("�����ֱ���")]
        public int ExportTexResolution = 1024;

        [FoldoutGroup("������ò/��ò��������")]
        [LabelText("ȱʧ���������ɫ")]
        public Color LostColor = Color.blue;

        [FoldoutGroup("������ò/��ò��������")]
        [LabelText("���߶ȶ�Ӧ��ɫ")]
        public Gradient heightGradient;

        [FoldoutGroup("������ò")]
        [LabelText("��ǰʹ�õĸ߶�ͼ����")]
        public List<HeightDataModel> heightDataModels;


        [FoldoutGroup("������ò")]
        [LabelText("ʹ�ð�����������")]
        public bool openPerlinNoise = true;

        [FoldoutGroup("������ò")]
        [LabelText("��ʼ��γ��")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        [FoldoutGroup("������ò")]
        [LabelText("��ǰ�����ĵ�ò����")]
        [Tooltip("ע�⣬���Texture���ܲ����ڴ������Ѿ��洢��")]
        public Texture2D curHandleTex;

        [FoldoutGroup("������ò")]
        [LabelText("��òͼ����λ��")]
        public string landformTexImportPath = MapStoreEnum.LandformTexOutputPath;

        [FoldoutGroup("������ò")]
        [LabelText("��ǰ�����ĵ�òͼλ��")]
        public string curLandformTexPath;
        
        [FoldoutGroup("������ò")]
        [Button("�����ò����ͼ", ButtonSizes.Medium)]
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

        [FoldoutGroup("������ò")]
        [Button("������ò����ͼ", ButtonSizes.Medium)]
        private void ExportLandFormTex() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            // use perlin noise
            PerlinGenerator perlinGenerator = new PerlinGenerator();
            perlinGenerator.GeneratePerlinNoise(ExportTexResolution, ExportTexResolution);

            // renderTexture, Ч�ʸ���
            //RenderTexture tmpTex = RenderTexture.GetTemporary(ExportTexResolution, ExportTexResolution, 24);
            //Graphics.Blit(terrainDataSplats[i].texture, rtArray[i], defaultMaterial, 0);
            //RenderTexture.active = tmpTex;
            //RenderTexture.ReleaseTemporary(tmpTex);
            //curHandleTex.ReadPixels(new Rect(0, 0, ExportTexResolution, ExportTexResolution), 0, 0);

            // TODO: ���� job system
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
                    // TODO : Ҫ���룺�¶ȡ�ʪ�� ��ζ�Ӧ����
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
                return new Color(0.04f, 0.38f, 0.66f); // ����ɫ #0A60A8
            } else if (height <= 0f) {
                return new Color(0.53f, 0.81f, 0.92f); // ǳ��ɫ #87CEEB
            } else if (height <= 10f) {
                return new Color(0.20f, 0.60f, 0.20f); // ǳ��ɫ #339933 (��ʾƽԭ)
            } else if (height <= 14f) {
                return new Color(0.42f, 0.69f, 0.30f); // ����ɫ #6AB04C
            } else if (height <= 21f) {
                return new Color(0.78f, 0.65f, 0.35f); // ����ɫ #C8A55A
            } else if (height <= 28f) {
                return new Color(0.62f, 0.49f, 0.34f); // ǳ��ɫ #9E7E57
            } else if (height <= 32f) {
                return new Color(0.85f, 0.85f, 0.85f); // ǳ��ɫ #D8D8D8
            } else {
                return new Color(1f, 1f, 1f);           // ����ɫ #FFFFFF
            }
        }

        [FoldoutGroup("������ò")]
        [Button("���浱ǰ����", ButtonSizes.Medium)]
        private void SaveLandFormTex() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("landform_{0}x{0}_{1}", ExportTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(landformTexImportPath, texName, curHandleTex);
        }

        [FoldoutGroup("������ò")]
        [Button("Ӧ�õ�ǰ����", ButtonSizes.Medium)]
        private void ApplyLandFormTex() { 
            // TODO : ��������
        }


        // TODO : �������µ�������ͼ

        [FoldoutGroup("��������ò����")]
        [LabelText("��ǰʹ�õĵ�ò����ͼ��")]
        public Texture2D curTerrainTexSplat;

        [FoldoutGroup("��������ò����")]
        [Button("����������ͼ", ButtonSizes.Medium)]
        private void ImportIdxTex() {
            // Ҳ����Ҫ����������ͼ + �����ͼ��ֱ��ʹ�� color ������
        }

        [FoldoutGroup("��������ò����")]
        [Button("���������ͼ", ButtonSizes.Medium)]
        private void ExportMixTex() {

        }

        [FoldoutGroup("��������ò����")]
        [Button("�������ջ�ϵ�ò", ButtonSizes.Medium)]
        private void ExportMixLandformTex() {

        }



        // TODO : ����Ĵ���Ҫ�쵽����
        // TODO : ��������Ĵ��룬+������ģ����ɻ�������ò
        // ��òģ�� �� ���� �߶� - ʪ�� - �¶� ��ȷ��һ������Ӧ�ò��õĵ�òͼ������

        // �ο� ��ŷ½����4

        [FoldoutGroup("ͼ������")]
        [LabelText("ͼ�����")]
        [Tooltip("����ֵΪ4,��ͼ���Ĺ����4x4")]
        public int texAtlasSize = 4;

        [FoldoutGroup("ͼ������")]
        [LabelText("�ֱ���")]
        public int texResoution = 2048;

        [FoldoutGroup("ͼ������")]
        [LabelText("Ĭ�ϲ���")]
        public Material defaultTerrainMat;

        public const string texArrayPath = MapStoreEnum.TerrainTexArrayPath;
        public const string texSavePath = MapStoreEnum.TerrainTexOutputPath;

        [FoldoutGroup("ͼ������")]
        [Button("��������ͼ��")]
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
                            // TODO: ������ܻ�������⣬Ҫ����±�Ե����
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

        [FoldoutGroup("ͼ������")]
        [Button("����������������")]
        private void GenerateIndexBlenderTex() {


            string atlaName = string.Format("/TerrainIndexArray_{0}x{0}.png", texResoution);
            //File.WriteAllBytes(texSavePath + atlaName, bytes);
            AssetDatabase.ImportAsset(texSavePath + atlaName);

            Debug.Log("generate index altas, then you can generate the blenderTex");
        }

        [FoldoutGroup("ͼ������")]
        [Button("���ɵ�������")]
        private void GenerateSplatTex() {

        }


    }
}
