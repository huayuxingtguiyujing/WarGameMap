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

        #region ��������

        [FoldoutGroup("��������")]
        [LabelText("����ʱ��ת")]
        public bool ExportFlipVertically = true;

        [FoldoutGroup("��������")]
        [LabelText("�����ֱ���")]
        public int ExportTexResolution = 1024;

        // TODO : ������
        [FoldoutGroup("��������")]
        [LabelText("�����ֱ���")]
        [Tooltip("��һ���Ӧ������ cluster �� num per line")]
        public int ExportTexSize = 4;


        [FoldoutGroup("��������")]
        [LabelText("��ǰʹ�õĸ߶�ͼ����")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("��������")]
        [LabelText("��ʼ��γ��")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        #endregion


        #region ������ò��ͼ
        // NOTE : �ÿ�Ĵ����������ɼ򵥰汾�ĵ�ò��������ͬ�ĸ߶ȶ�Ӧ��ͬ�Ĵ�ɫ��ò

        [FoldoutGroup("������ò��ͼ/��ò��������")]
        [LabelText("ȱʧ���������ɫ")]
        public Color LostColor = Color.blue;

        [FoldoutGroup("������ò��ͼ/��ò��������")]
        [LabelText("���߶ȶ�Ӧ��ɫ")]
        public Gradient heightGradient;

        [FoldoutGroup("������ò��ͼ/��ò��������")]
        [LabelText("ʹ�ð�����������")]
        public bool openPerlinNoise = true;

        [FoldoutGroup("������ò��ͼ")]
        [LabelText("��ǰ�����ĵ�ò����")]
        [Tooltip("ע�⣬���Texture���ܲ����ڴ������Ѿ��洢��")]
        public Texture2D curHandleLandformTex;

        [FoldoutGroup("������ò��ͼ")]
        [LabelText("��ǰ�����ĵ�òͼλ��")]
        public string curLandformTexPath;

        [FoldoutGroup("������ò��ͼ")]
        [LabelText("��ͼ����λ��")]
        public string landformTexImportPath = MapStoreEnum.LandformTexOutputPath;

        [FoldoutGroup("������ò��ͼ")]
        [Button("�����ò����ͼ", ButtonSizes.Medium)]
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

        [FoldoutGroup("������ò��ͼ")]
        [Button("������ò����ͼ", ButtonSizes.Medium)]
        private void ExportLandFormTex() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            // use perlin noise
            //PerlinGenerator perlinGenerator = new PerlinGenerator();
            //perlinGenerator.GeneratePerlinNoise(ExportTexResolution / 4, ExportTexResolution / 4);
            PerlinNoise perlinNoise = new PerlinNoise(ExportTexResolution, 16, true, 4, new Vector2(1, 1), 8);

            // renderTexture, Ч�ʸ���
            //RenderTexture tmpTex = RenderTexture.GetTemporary(ExportTexResolution, ExportTexResolution, 24);
            //Graphics.Blit(terrainDataSplats[i].texture, rtArray[i], defaultMaterial, 0);
            //RenderTexture.active = tmpTex;
            //RenderTexture.ReleaseTemporary(tmpTex);
            //curHandleTex.ReadPixels(new Rect(0, 0, ExportTexResolution, ExportTexResolution), 0, 0);

            // TODO: ���� job system
            curHandleLandformTex = new Texture2D(ExportTexResolution, ExportTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleLandformTex.GetPixels();

            // TODO : ��������������ɶ��ŵ�òͼ����ÿ��clusterҪ��Ӧһ����ͼ�����滹���������� VT��
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
                    // TODO : Ҫ���룺�¶ȡ�ʪ�� ��ζ�Ӧ����
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

        [FoldoutGroup("������ò��ͼ")]
        [Button("���浱ǰ��ò����", ButtonSizes.Medium)]
        private void SaveLandFormTex() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("landform_{0}x{0}_{1}", ExportTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(landformTexImportPath, texName, curHandleLandformTex);
        }

        #endregion


        #region ���������ε�ò��ͼ
        // NOTE : 

        #endregion


        #region ����������ͼ


        [FoldoutGroup("����������ͼ")]
        [LabelText("��ǰ�����ķ�������")]
        public Texture2D curHandleNormalTex;

        [FoldoutGroup("����������ͼ")]
        [LabelText("��ǰ�����ķ���ͼλ��")]
        public string curNormalTexPath;

        [FoldoutGroup("����������ͼ")]
        [LabelText("��ͼ����λ��")]
        public string normalTexImportPath = MapStoreEnum.NormalTexOutputPath;

        [FoldoutGroup("����������ͼ")]
        [Button("������η���ͼ", ButtonSizes.Medium)]
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

        [FoldoutGroup("����������ͼ")]
        [Button("�������η���ͼ", ButtonSizes.Medium)]
        private void ExportNormalTexture() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            curHandleNormalTex = new Texture2D(ExportTexResolution, ExportTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleNormalTex.GetPixels();

            // ERROR Ϊʲôȫ����ɫ��

            // TODO : ��������������ɶ��ŷ���ͼ����ÿ��clusterҪ��Ӧһ����ͼ��
            // TODO : ���� cluster �Ŀ�� ���ɷ���ͼ
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

        [FoldoutGroup("����������ͼ")]
        [Button("���浱ǰ��������", ButtonSizes.Medium)]
        private void SaveNormalTexture() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("normal_{0}x{0}_{1}", ExportTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(normalTexImportPath, texName, curHandleNormalTex);
        }

        #endregion


        #region ��������/�����ͼ

        // TODO : ĿǰҪ��ô�㣬�ǲ���Ӧ��ŪЩ�߼������������ֹ��perlin��
        // ˵������Ҫ��Щ�㷨

        // step1 : �ҿ��ܿ����õ�����/�Զ��������㷨������Ҫ�������ģ�������GPT
        // step2 : Ӧ������㷨������һ��������Ȼ��������������ȥ�������ϣ��Ȳ�����������ͼ��
        // step3 : ˼����ô��������ͼ��˻......�ǲ���ҲӦ��ȥ�Զ�������һ���أ�Ŀǰû��ͷ����

        // ���⣬����ƪ��������ò������ʵ������������������ֻ�ǰ��˴���

        #endregion

        #region ��������ò

        [FoldoutGroup("��������ò����", order: 99)]
        [LabelText("NOTE")]  // NOTE : ��ʵ������ܲ�ֹ���õ�16�ŵ�ò����Ҫ���Ǻ�������
        public string blenderTexLandformStr = "�˲�����������ϵ�ò���������Ը��ݲ����Զ������޷�Ļ������/��������";

        // NOTE : �����Ǵ�������������ò����
        [FoldoutGroup("��������ò����", order: 99)]
        [LabelText("��ǰʹ�õĵ�ò����ͼ��")]  // NOTE : ��ʵ������ܲ�ֹ���õ�16�ŵ�ò����Ҫ���Ǻ�������
        public List<Texture2D> curTerrainTexSplats;

        [FoldoutGroup("��������ò����")]
        [Button("����������ͼ", ButtonSizes.Medium)]
        private void ExportIdxTex() {
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


        #endregion



    }
}
