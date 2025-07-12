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

        #region 构建设置

        [FoldoutGroup("构建设置")]
        [LabelText("导出时翻转")]
        public bool ExportFlipVertically = true;

        [FoldoutGroup("构建设置")]
        [LabelText("导出分辨率")]
        public int ExpTexResolution = 1024;

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

        [FoldoutGroup("构建地貌贴图/噪声与平滑")]
        [LabelText("开启噪声处理")]
        public bool openNoise = true;

        [FoldoutGroup("构建地貌贴图/噪声与平滑")]
        [LabelText("噪声偏移修正")]
        public float noiseOffset = 1.0f;

        [FoldoutGroup("构建地貌贴图/噪声与平滑")]
        [LabelText("噪声强度")]
        public float noiseIntense = 100;

        [FoldoutGroup("构建地貌贴图/噪声与平滑")]
        [LabelText("开启平滑处理")]
        public bool openLerp = false;

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

        [FoldoutGroup("构建地貌贴图")]
        [Button("导出地貌纹理图_加速版本", ButtonSizes.Medium)]
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

            // TODO: 改用 job system
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
                // 额外的平滑处理
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
            string texName = string.Format("landform_{0}x{0}_{1}", ExpTexResolution, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(landformTexImportPath, texName, curHandleLandformTex);
        }



        #endregion


        #region 构建六边形地貌贴图
        // NOTE : 

        #endregion


        #region 构建法线贴图

        [FoldoutGroup("构建法线贴图")]
        [LabelText("构建起点")]
        public Vector2Int buildStartPos;

        [FoldoutGroup("构建法线贴图")]
        [LabelText("构建范围")]
        public Vector2Int buildScope;

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

            curNormalTexPath = AssetsUtility.TransToAssetPath(normalImportPath);
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
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize, null, null);

            curHandleNormalTex = new Texture2D(ExpTexResolution, ExpTexResolution, TextureFormat.RGB24, false);
            Color[] colors = curHandleNormalTex.GetPixels();

            // ERROR 为什么全是绿色？

            // TODO : 如果超出，就生成多张法线图！（每个cluster要对应一张贴图）
            // TODO : 根据 cluster 的宽度 生成法线图
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
        [FoldoutGroup("构建法线贴图")]
        [Button("使用 TerCtor 导出法线贴图", ButtonSizes.Medium)]
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

        [FoldoutGroup("构建法线贴图")]
        [Button("保存当前法线纹理", ButtonSizes.Medium)]
        private void SaveNormalTexture() {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("normal_{0}x{0}_{1}", ExpTexResolution, dateTime.Ticks);
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

        //#region 混合纹理地貌
        //[FoldoutGroup("混合纹理地貌生成", order: 99)]
        //[LabelText("NOTE")]  // NOTE : 其实后面可能不止会用到16张地貌纹理，要考虑后续扩充
        //public string blenderTexLandformStr = "此部分用于做混合地貌方案，可以根据参数自动生成无缝的混合纹理/索引纹理";
        //// NOTE : 以下是打算做混合纹理地貌方案
        //[FoldoutGroup("混合纹理地貌生成", order: 99)]
        //[LabelText("当前使用的地貌纹理图集")]  // NOTE : 其实后面可能不止会用到16张地貌纹理，要考虑后续扩充
        //public List<Texture2D> curTerrainTexSplats;
        //[FoldoutGroup("混合纹理地貌生成")]
        //[Button("导出索引贴图", ButtonSizes.Medium)]
        //private void ExportIdxTex() {
        //    // 也许不需要导出索引贴图 + 混合贴图，直接使用 color 制作？
        //}
        //[FoldoutGroup("混合纹理地貌生成")]
        //[Button("导出混合贴图", ButtonSizes.Medium)]
        //private void ExportMixTex() { }
        //[FoldoutGroup("混合纹理地貌生成")]
        //[Button("生成最终混合地貌", ButtonSizes.Medium)]
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
