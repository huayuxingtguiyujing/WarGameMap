using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace LZ.WarGameMap.MapEditor
{
    public class LandformEditor : BaseMapEditor {

        public override string EditorName => MapEditorEnum.LandformEditor;

        TerrainConstructor TerrainCtor;

        protected override void InitEditor() {
            GameObject mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
            if (mapRootObj == null) {
                mapRootObj = new GameObject(MapSceneEnum.MapRootName);
            }
            TerrainCtor = EditorSceneManager.TerrainCtor;
            if (TerrainCtor == null) {
                TerrainCtor = mapRootObj.GetComponent<TerrainConstructor>();
                if(TerrainCtor == null) {
                    Debug.LogError("unable to find TerrainCtor, please goto terrainWindow to config it");
                }
            }
            base.InitEditor();
        }

        #region ��������

        [FoldoutGroup("��������")]
        [LabelText("����ʱ��ת")]
        public bool ExportFlipVertically = true;

        [FoldoutGroup("��������")]
        [LabelText("�����ֱ���")]
        public int ExpTexResolution = 1024;

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

        [FoldoutGroup("������ò��ͼ/������ƽ��")]
        [LabelText("������������")]
        public bool openNoise = true;

        [FoldoutGroup("������ò��ͼ/������ƽ��")]
        [LabelText("����ƫ������")]
        public float noiseOffset = 1.0f;

        [FoldoutGroup("������ò��ͼ/������ƽ��")]
        [LabelText("����ǿ��")]
        public float noiseIntense = 100;

        [FoldoutGroup("������ò��ͼ/������ƽ��")]
        [LabelText("����ƽ������")]
        public bool openLerp = false;

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

            curLandformTexPath = AssetsUtility.TransToAssetPath(landformImportPath);
            curHandleLandformTex = AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(curLandformTexPath);
            if (curHandleLandformTex == null) {
                curLandformTexPath = "";
                Debug.LogError(string.Format("can not load landform texture from this path: {0}", curLandformTexPath));
                return;
            }
        }


        struct LerpLandformJob : IJobParallelFor {
            [ReadOnly] public int lerpScope;
            [ReadOnly] public int resolution;

            [ReadOnly] public NativeArray<Color> originColors;
            [WriteOnly] public NativeArray<Color> targetColors;

            public void Execute(int index) {
                int i = index / resolution;
                int j = index % resolution;

                Color targetColor = Color.black;
                int lerpWH = (lerpScope * 2 + 1) * (lerpScope * 2 + 1);
                for(int q = Mathf.Max(0, i - lerpScope); q <= Mathf.Min(i + lerpScope, resolution - 1); q++) {
                    for (int p = Mathf.Max(0, j - lerpScope); p <= Mathf.Min(j + lerpScope, resolution - 1); p++) {
                        int targetIdx = q * resolution + p;
                        targetColor += originColors[targetIdx] / lerpWH;
                    }
                }
                targetColors[index] = targetColor;
            }

        }

        [FoldoutGroup("������ò��ͼ")]
        [Button("������ò����ͼ_���ٰ汾", ButtonSizes.Medium)]
        private void ExportLandFormTex() {
            if(curHandleLandformTex != null) {
                UnityEngine.Object.DestroyImmediate(curHandleLandformTex);
            }

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Debug.Log("now start generate landform texture");

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize, null, null);

            // use perlin noise
            PerlinNoise perlinNoise = new PerlinNoise(ExpTexResolution, 16, true, 4, new Vector2(1, 1), 8);

            // TODO: ���� job system
            curHandleLandformTex = new Texture2D(ExpTexResolution, ExpTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleLandformTex.GetPixels();

            Action<int> exeLanformPlant = (idx) => {
                int i;
                int j;
                if (ExportFlipVertically) {
                    i = idx % ExpTexResolution;
                    j = idx / ExpTexResolution;
                } else {
                    j = idx % ExpTexResolution;
                    i = idx / ExpTexResolution;
                }

                Vector3 vertPos = new Vector3(i, 0, j);

                if (openNoise) {
                    float noise = (perlinNoise.SampleNoise(vertPos) - noiseOffset);
                    vertPos = new Vector3(vertPos.x, 0, vertPos.z);
                    vertPos.x += (int)(noise * noiseIntense);
                    vertPos.z += (int)(noise * noiseIntense);
                }

                vertPos.y = heightDataManager.SampleFromHeightData(startLongitudeLatitude, vertPos);

                Color color = GetColorByHeight(vertPos.y);
                idx = Mathf.Clamp(idx, 0, ExpTexResolution * ExpTexResolution - 1);
                colors[idx] = color;
            };

            Parallel.For(0, ExpTexResolution * ExpTexResolution, exeLanformPlant);

            NativeArray<Color> originColors = new NativeArray<Color>(colors, Allocator.TempJob);
            NativeArray<Color> targetColors = new NativeArray<Color>(ExpTexResolution * ExpTexResolution, Allocator.TempJob);
            if (openLerp) {
                // �����ƽ������
                LerpLandformJob lerpLandformJob = new LerpLandformJob() {
                    resolution = ExpTexResolution,
                    lerpScope = 1,
                    originColors = originColors,
                    targetColors = targetColors
                };
                JobHandle jobHandle1 = lerpLandformJob.Schedule(ExpTexResolution * ExpTexResolution, 64);
                jobHandle1.Complete();
                curHandleLandformTex.SetPixels(targetColors.ToArray());
            } else {
                curHandleLandformTex.SetPixels(originColors.ToArray());
            }
            curHandleLandformTex.Apply();

            originColors.Dispose();
            targetColors.Dispose();

            stopwatch.Stop();
            Debug.Log(string.Format("successfully generate texture, resolution : {0}x{0}, cost {1} ms", ExpTexResolution, stopwatch.ElapsedMilliseconds));
            
            // it cant work!
            //LightThreadPool.ScheduleTask(16, ExpTexResolution * ExpTexResolution, exeLanformPlant, exelerp);
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
            string texName = string.Format("landform_{0}x{0}_{1}", ExpTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(landformTexImportPath, texName, curHandleLandformTex);
        }



        #endregion


        #region ���������ε�ò��ͼ
        // NOTE : 

        #endregion


        #region ����������ͼ

        [FoldoutGroup("����������ͼ")]
        [LabelText("�������")]
        public Vector2Int buildStartPos;

        [FoldoutGroup("����������ͼ")]
        [LabelText("������Χ")]
        public Vector2Int buildScope;

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

            curNormalTexPath = AssetsUtility.TransToAssetPath(normalImportPath);
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
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize, null, null);

            curHandleNormalTex = new Texture2D(ExpTexResolution, ExpTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleNormalTex.GetPixels();

            // ERROR Ϊʲôȫ����ɫ��

            // TODO : ��������������ɶ��ŷ���ͼ����ÿ��clusterҪ��Ӧһ����ͼ��
            // TODO : ���� cluster �Ŀ�� ���ɷ���ͼ
            for (int i = 0; i < ExpTexResolution; i++) {
                for (int j = 0; j < ExpTexResolution; j++) {
                    Vector3 vertPos = new Vector3(i, 0, j);
                    Vector3 normal = heightDataManager.SampleNormalFromData(startLongitudeLatitude, vertPos);
                    Color normalColor = new Color(normal.x * 0.5f + 0.5f, normal.y * 0.5f + 0.5f, normal.z * 0.5f + 0.5f);
                    int idx;
                    if (ExportFlipVertically) {
                        idx = j * ExpTexResolution + i;
                    } else {
                        idx = i * ExpTexResolution + j;
                    }
                    colors[idx] = normalColor;
                }
            }
            curHandleNormalTex.SetPixels(colors);
            curHandleNormalTex.Apply();

            curHandleNormalTex = TextureUtility.GetInstance().BilinearResize(curHandleNormalTex);

            Debug.Log(string.Format("successfully generate texture, resolution : {0}x{0}", ExpTexResolution));
        }


        // TODO : move to landform editor
        [FoldoutGroup("����������ͼ")]
        [Button("ʹ�� TerCtor ����������ͼ", ButtonSizes.Medium)]
        private void BuildClusterNormal() {

            if (curHandleNormalTex == null) {
                Debug.LogError("no normal texture, so you can not build the mesh");
                return;
            }

            for(int i = 0; i < buildScope.x; i++) {
                for(int j = 0;  j < buildScope.y; j++) {
                    TerrainCtor.BuildClusterNormal(buildStartPos.x + i, j, curHandleNormalTex);
                }
            }

        }

        [FoldoutGroup("����������ͼ")]
        [Button("���浱ǰ��������", ButtonSizes.Medium)]
        private void SaveNormalTexture() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("normal_{0}x{0}_{1}", ExpTexResolution, dateTime.Ticks);
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

        //#region ��������ò
        //[FoldoutGroup("��������ò����", order: 99)]
        //[LabelText("NOTE")]  // NOTE : ��ʵ������ܲ�ֹ���õ�16�ŵ�ò����Ҫ���Ǻ�������
        //public string blenderTexLandformStr = "�˲�����������ϵ�ò���������Ը��ݲ����Զ������޷�Ļ������/��������";
        //// NOTE : �����Ǵ�������������ò����
        //[FoldoutGroup("��������ò����", order: 99)]
        //[LabelText("��ǰʹ�õĵ�ò����ͼ��")]  // NOTE : ��ʵ������ܲ�ֹ���õ�16�ŵ�ò����Ҫ���Ǻ�������
        //public List<Texture2D> curTerrainTexSplats;
        //[FoldoutGroup("��������ò����")]
        //[Button("����������ͼ", ButtonSizes.Medium)]
        //private void ExportIdxTex() {
        //    // Ҳ����Ҫ����������ͼ + �����ͼ��ֱ��ʹ�� color ������
        //}
        //[FoldoutGroup("��������ò����")]
        //[Button("���������ͼ", ButtonSizes.Medium)]
        //private void ExportMixTex() { }
        //[FoldoutGroup("��������ò����")]
        //[Button("�������ջ�ϵ�ò", ButtonSizes.Medium)]
        //private void ExportMixLandformTex() { }
        //#endregion


        public override void Destory() {
            if(curHandleLandformTex != null) {
                GameObject.DestroyImmediate(curHandleLandformTex);
                curHandleLandformTex = null;
            }
            if (curHandleNormalTex != null) {
                GameObject.DestroyImmediate(curHandleNormalTex);
                curHandleNormalTex = null;
            }
        }


    }
}
