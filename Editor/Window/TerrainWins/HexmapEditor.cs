using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

//using 

namespace LZ.WarGameMap.MapEditor
{
    [Serializable]
    public class HexmapEditor : BaseMapEditor 
    {
        public override string EditorName => MapEditorEnum.HexMapEditor;

        public static class TerrainColorEnum {
            public static Color RiverColor = Color.white;
            public static Color SeaColor = Color.white;

            public static Color PlainColor = Color.white;
            public static Color HillColor = Color.white;
            public static Color MountainColor = Color.white;

        }

        HexmapConstructor HexCtor;

        protected override void InitEditor() {
            HexCtor = EditorSceneManager.HexCtor;
            InitMapSetting();
            base.InitEditor();
        }
        
        #region init map

        [FoldoutGroup("初始化地图")]
        [LabelText("地图高度")]
        public float hexHeight = 30;

        [FoldoutGroup("初始化地图")]
        [LabelText("地图材质")]
        public Material hexMaterial;

        [FoldoutGroup("初始化地图")]
        [Button("初始化Hex地图", ButtonSizes.Medium)]
        private void DrawRectangleGrid() {
            HexCtor.InitHexConsRectangle(null);
        }

        [FoldoutGroup("初始化地图")]
        [Button("刷新地图", ButtonSizes.Medium)]
        private void UpdateHexMap() {
            // set the height
            
            // update the material
        }

        [FoldoutGroup("初始化地图")]
        [Button("清空地图", ButtonSizes.Medium)]
        private void ClearHexMap() {

            EditorSceneManager.mapScene.hexClusterParentObj.ClearObjChildren();
        }

        #endregion

        #region hexTexture Construct
        // NOTE : 此处输出六边形网格纹理，是为了构建 类似 文明那样的 六边形 Terrain 地表

        public enum BlendMethod {
            Catlike, NoCorner, NoInner
        }

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("混合方式")]
        public BlendMethod blendMethod = BlendMethod.Catlike;

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("六边形内圈比例")]
        public float innerHexRatio = 0.8f;

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("分辨率")]
        public int OuputTexResolution = 1024;

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("颜色")]
        public Color hexEdgeColor = new Color(1, 1, 1, 0.5f);

        [FoldoutGroup("六边形纹理构建/噪声与平滑")]
        [LabelText("噪声频率")]
        public float frequency = 16;

        [FoldoutGroup("六边形纹理构建/噪声与平滑")]
        [LabelText("随机数")]
        public float randomSpeed = 1;

        [FoldoutGroup("六边形纹理构建/噪声与平滑")]
        [LabelText("分形噪声")]
        public int fbmIteration = 8;

        [FoldoutGroup("六边形纹理构建/噪声与平滑")]
        [LabelText("噪声强度")]
        public float noiseIntense = 30;

        [FoldoutGroup("六边形纹理构建/噪声与平滑")]
        [LabelText("噪声偏移")]
        public float noiseOffset = 0.5f;

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("地貌图集")]
        public Texture2D curHexTerrainSplat;    //当前没有用

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("地貌索引纹理")]
        public Texture2D curHexLandformIdxTex;  //当前没有用

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("地貌混合权重")]
        public Texture2D curHexLandformBlendTex;    //当前没有用

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("生成的地貌纹理")]
        public Texture2D curHexLandformResult;


        [FoldoutGroup("六边形纹理构建")]
        [LabelText("贴图导出位置")]
        public string hexLandformTexImportPath = MapStoreEnum.HexLandformTexOutputPath;

        // TODO : 总目标：按照文明6的地貌方式生成混合地貌，基本步骤如下：
        // step1 : 通过随机数种子得到每个Hex格子的地形数据
        // step2 : 准备对应地形的纹理（至少16张，这些纹理与地形其实不是一一对应的，可能会进行叠加）
        // （--Sloved--）step3 : 根据catlike的方案做初版的 hex 混合地貌，即边缘插值与周围格子进行混合，中心不做混合
        // step4 : 通过 renderdoc 看 文明6 等项目的纹理混合
        // step4 : （要调研）通过噪声等方式对边缘混合地带进行处理，得到更为自然和美观的地貌
        // step5 : （要调研）做更多的地貌效果，了解贴花原理，写shader......
        struct HexLandFormDataJob : IJobParallelFor {
            // this job prepare hex landform data
            public void Execute(int index) {

            }
        }

        struct ExportHexLandFormJob : IJobParallelFor {
            [ReadOnly] public BlendMethod blendMethod;
            [ReadOnly] public int OuputTextureResolution;
            [ReadOnly] public int hexGridSize;
            [ReadOnly] public float innerHexRatio;
            [ReadOnly] public Layout layout;

            [ReadOnly] public NativeHashMap<Vector2Int, int> tempTerrainType;
            //[ReadOnly] public NativeArray<Color> baseColors;    // terrain texture colors

            [WriteOnly] public NativeArray<Color> landformColors;

            public void Execute(int index) {
                int j = index / OuputTextureResolution;
                int i = index % OuputTextureResolution;

                Vector2 pos = new Vector2(i, j);
                Hexagon hex = HexHelper.PixelToAxialHex(pos, hexGridSize, true);

                switch (blendMethod) {
                    case BlendMethod.Catlike:
                        HexAreaPointData hexData = HexHelper.GetPointHexArea(pos, hex, layout, innerHexRatio);
                        HandleHexPointData(index, ref hexData, ref hex);
                        break;
                    case BlendMethod.NoCorner:
                        HexAreaPointData hexData_NoCorner = HexHelper.GetPointHexArea(pos, hex, layout, innerHexRatio);
                        HandleHexPointData_NoCorner(index, ref hexData_NoCorner, ref hex);
                        break;
                    case BlendMethod.NoInner:
                        HexAreaPointData hexData_NoInner = HexHelper.GetPointHexArea_NoInner(pos, hex, layout);
                        HandleHexPointData_NoInner(index, ref hexData_NoInner, ref hex);
                        break;
                }
            }

            private void HandleHexPointData(int idx, ref HexAreaPointData hexData, ref Hexagon hex) {
                // get this hexGrid's type type
                Vector2Int OffsetHex = HexHelper.AxialToOffset(hex);
                int terrainType = GetTerrainType(OffsetHex);
                
                if (hexData.insideInnerHex) {
                    landformColors[idx] = GetColorByType_Test(terrainType);
                    // TODO : 怎么做 idx color 到 base color 的映射？   // use Texture2DArray
                } else {
                    BlendWithNeighborHex(terrainType, idx, ref hexData, ref hex);
                    /*switch (hexData.hexAreaDir) {
                        case HexDirection.NW:
                            break;
                        case HexDirection.NE:
                            break;
                        case HexDirection.E:
                            break;
                        case HexDirection.SE:
                            break;
                        case HexDirection.SW:
                            break;
                        case HexDirection.W:
                            break;
                        case HexDirection.None:
                            break;
                    }*/
                }
            }

            private void BlendWithNeighborHex(int terrainType, int idx, ref HexAreaPointData hexData, ref Hexagon hex) {

                Hexagon neighborHex = hex.Hex_Neighbor(hexData.hexAreaDir);

                switch (hexData.outHexAreaEnum) {
                    case OutHexAreaEnum.Edge:
                        Vector2Int neighbor_offsetHex = HexHelper.AxialToOffset(neighborHex);
                        int neighborType = GetTerrainType(neighbor_offsetHex);
                        Color neighbor_Color = GetColorByType_Test(neighborType);
                        Color hex_Color = GetColorByType_Test(terrainType);
                        landformColors[idx] = MathUtil.LinearLerp(hex_Color, neighbor_Color, hexData.ratioBetweenInnerAndOutter);

                        break;
                    case OutHexAreaEnum.LeftCorner:
                        // 根据推演,混合时 : cur + 1, neighbor + 2, next + 4 
                        Hexagon nextHex = hex.Hex_Neighbor(hexData.hexAreaDir + 1);
                        Vector2 l_A = hex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 1, innerHexRatio);
                        Vector2 l_B = neighborHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 3, innerHexRatio);
                        Vector2 l_C = nextHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 5, innerHexRatio);
                        Color l_a = GetColorByType_Test(GetTerrainType(HexHelper.AxialToOffset(hex)));
                        Color l_b = GetColorByType_Test(GetTerrainType(HexHelper.AxialToOffset(neighborHex)));
                        Color l_c = GetColorByType_Test(GetTerrainType(HexHelper.AxialToOffset(nextHex)));
                        landformColors[idx] = MathUtil.TriangleLerp(l_a, l_b, l_c, l_A, l_B, l_C, hexData.worldPos);
                        break;
                    case OutHexAreaEnum.RightCorner:
                        // 根据推演,混合时 : neighbor - 2, next - 4 
                        Hexagon preHex = hex.Hex_Neighbor(hexData.hexAreaDir - 1);
                        Vector2 r_A = hex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir, innerHexRatio);
                        Vector2 r_B = neighborHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir - 2, innerHexRatio);
                        Vector2 r_C = preHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir - 4, innerHexRatio);
                        Color r_a = GetColorByType_Test(GetTerrainType(HexHelper.AxialToOffset(hex)));
                        Color r_b = GetColorByType_Test(GetTerrainType(HexHelper.AxialToOffset(neighborHex)));
                        Color r_c = GetColorByType_Test(GetTerrainType(HexHelper.AxialToOffset(preHex)));
                        landformColors[idx] = MathUtil.TriangleLerp(r_a, r_b, r_c, r_A, r_B, r_C, hexData.worldPos);
                        break;
                }
            }

            private void HandleHexPointData_NoCorner(int idx, ref HexAreaPointData hexData, ref Hexagon hex) {
                Vector2Int OffsetHex = HexHelper.AxialToOffset(hex);
                int terrainType = GetTerrainType(OffsetHex);    // perlinNoise

                if (hexData.insideInnerHex) {
                    landformColors[idx] = GetColorByType_Test(terrainType);
                } else {
                    // edge directly blend with neighbor, and do not divide into left/right corner
                    Hexagon neighborHex = hex.Hex_Neighbor(hexData.hexAreaDir);

                    Vector2Int neighbor_offsetHex = HexHelper.AxialToOffset(neighborHex);
                    int neighborType = GetTerrainType(neighbor_offsetHex);
                    Color neighbor_Color = GetColorByType_Test(neighborType);
                    Color hex_Color = GetColorByType_Test(terrainType);
                    landformColors[idx] = MathUtil.LinearLerp(hex_Color, neighbor_Color, hexData.ratioBetweenInnerAndOutter);
                }
            }

            private void HandleHexPointData_NoInner(int idx, ref HexAreaPointData hexData, ref Hexagon hex) {
                // TODO : 我认为目前的混合结果是有问题的，但我不知道是哪里出了问题

                Vector2Int OffsetHex = HexHelper.AxialToOffset(hex);
                int terType = GetTerrainType(OffsetHex);
                Color hex_Color = GetColorByType_Test(terType);
                Vector2 hexCenter = hex.Get_Hex_Center(layout);

                Hexagon neighborHex = hex.Hex_Neighbor(hexData.hexAreaDir);
                Vector2Int neighborOffsetHex = HexHelper.AxialToOffset(neighborHex);
                int neighborTerType = GetTerrainType(neighborOffsetHex);
                Color neighbor_Color = GetColorByType_Test(neighborTerType);
                Vector2 neighborCenter = neighborHex.Get_Hex_Center(layout);

                if (hexData.outHexAreaEnum == OutHexAreaEnum.LeftCorner) {
                    Hexagon nextHex = hex.Hex_Neighbor(hexData.hexAreaDir + 1);
                    Vector2Int nextOffsetHex = HexHelper.AxialToOffset(nextHex);
                    int nextTerType = GetTerrainType(nextOffsetHex);
                    Color next_Color = GetColorByType_Test(nextTerType);
                    Vector2 nextCenter = nextHex.Get_Hex_Center(layout);

                    landformColors[idx] = MathUtil.TriangleLerp(hex_Color, neighbor_Color, next_Color,
                        hexCenter, neighborCenter, nextCenter, hexData.worldPos); ;

                } else if (hexData.outHexAreaEnum == OutHexAreaEnum.RightCorner) {
                    Hexagon preHex = hex.Hex_Neighbor(hexData.hexAreaDir - 1);
                    Vector2Int preOffsetHex = HexHelper.AxialToOffset(preHex);
                    int preTerType = GetTerrainType(preOffsetHex);
                    Color pre_Color = GetColorByType_Test(preTerType);
                    Vector2 preCenter = preHex.Get_Hex_Center(layout);

                    landformColors[idx] = MathUtil.TriangleLerp(hex_Color, neighbor_Color, pre_Color,
                        hexCenter, neighborCenter, preCenter, hexData.worldPos);

                }
            }


            private int GetTerrainType(Vector2Int offsetHex) {
                int terrainType = -1;
                if (tempTerrainType.ContainsKey(offsetHex)) {
                    terrainType = tempTerrainType[offsetHex];
                }
                return terrainType;
            }

            private void BlendHexEdge(Vector2 worldPos, HexDirection hexAreaDir, ref Hexagon hex) {
                switch (hexAreaDir) {
                    case HexDirection.NE:
                        break;
                        //return Color.red;
                    case HexDirection.NW:
                        //return Color.green;
                        break;
                    case HexDirection.W:
                        //return Color.blue;
                        break;
                    case HexDirection.SW:
                        //return Color.gray;
                        break;
                    case HexDirection.SE:
                        //return Color.cyan;
                        break;
                    case HexDirection.E:
                        //return Color.yellow;
                        break;
                    default:
                        //return Color.white;
                        break;
                }
            }

            // test method
            private Color GetColorByType_Test(int type) {
                //Debug.Log("trriger it!");
                // NOTE : 这个方法目前是占位符, 之后要从地貌纹理上提取颜色
                switch (type) {
                    case 0:
                        return new Color(0.20f, 0.60f, 0.20f); // 浅绿色 #339933
                    case 1:
                        return new Color(0.78f, 0.65f, 0.35f); // 黄绿色 #C8A55A
                    case 2:
                        return new Color(0.62f, 0.49f, 0.34f); // 浅棕色 #9E7E57
                    case 3:
                        return new Color(0.85f, 0.85f, 0.85f); // 浅灰色 #D8D8D8;
                    case 4:
                        return new Color(0.85f, 0.85f, 0.85f); // 浅灰色 #D8D8D8;
                    default:
                        return Color.white;
                }
            }

        
        }

        struct NoiseHexLandFormJob : IJobParallelFor {
            [ReadOnly] public int OuputTexResolution;
            [ReadOnly] public float noiseIntense;
            [ReadOnly] public float noiseOffset;

            [ReadOnly] public PerlinNoise perlinNoise;
            [ReadOnly] public NativeArray<Color> originColors;
            [WriteOnly] public NativeArray<Color> targetColors;

            public void Execute(int index) {
                int j = index / OuputTexResolution;
                int i = index % OuputTexResolution;

                Vector3Int pos = new Vector3Int(i, 0, j);

                float noise = (perlinNoise.SampleNoise(pos) - noiseOffset);
                pos = new Vector3Int(i, 0, j);
                pos.x = Mathf.Clamp(pos.x + (int)(noise * noiseIntense), 0, OuputTexResolution * OuputTexResolution - 1);
                pos.z = Mathf.Clamp(pos.z + (int)(noise * noiseIntense), 0, OuputTexResolution * OuputTexResolution - 1);

                int originIdx = pos.z * OuputTexResolution + pos.x;
                int targetIdx = j * OuputTexResolution + i;

                targetColors[targetIdx] = originColors[originIdx];
            }

        }

        [FoldoutGroup("六边形纹理构建")]
        [Button("导出六边形混合地貌纹理", ButtonSizes.Medium)]
        private void ExportHexLandFormTex() {
            //if (curHexTerrainSplat == null) {
            //    Debug.Log("curHexTerrainSplat is null, no base color, can not blend");
            //    return;
            //}

            if(curHexLandformIdxTex != null) {
                DestroyImmediate(curHexLandformIdxTex);
            }
            if (curHexLandformBlendTex != null) {
                DestroyImmediate(curHexLandformBlendTex);
            }
            if (curHexLandformResult != null) {
                DestroyImmediate(curHexLandformResult);
            }
            curHexLandformIdxTex = new Texture2D(OuputTexResolution, OuputTexResolution, TextureFormat.RGBA32, false);
            curHexLandformBlendTex = new Texture2D(OuputTexResolution, OuputTexResolution, TextureFormat.RGBA32, false);
            curHexLandformResult = new Texture2D(OuputTexResolution, OuputTexResolution, TextureFormat.RGBA32, false);

            // 暂时使用 这个hashmap 记录所有 hex 省份的地形类型
            Layout layout = EditorSceneManager.hexSet.GetScreenLayout();

            Color[] colors = curHexLandformResult.GetPixels();
            //Color[] idxColor = curHexLandformIdxTex.GetPixels();
            //Color[] blendColor = curHexLandformBlendTex.GetPixels();

            NativeArray<Color> nativeColors = new NativeArray<Color>(colors, Allocator.TempJob);
            NativeArray<Color> targetColors = new NativeArray<Color>(OuputTexResolution * OuputTexResolution, Allocator.TempJob);
            //NativeArray<Color> idxColors = new NativeArray<Color>(idxColor, Allocator.TempJob);
            //NativeArray<Color> blendColors = new NativeArray<Color>(blendColor, Allocator.TempJob);

            HexGenerator hexGenerator = new HexGenerator();
            hexGenerator.GenerateRectangle(0, 50, 0, 50);
            NativeHashMap<Vector2Int, int> tempTerrainType = new NativeHashMap<Vector2Int, int>(
                hexGenerator.HexagonIdxDic.Count, Allocator.TempJob
            );
            foreach (var pair in hexGenerator.HexagonIdxDic) {
                int type = Random.Range(0, 3);
                tempTerrainType.Add(pair.Key, type);
            }

            // TODO : 现在的地貌混合方案，有如下步骤
            // step1 : 基于六边形的数据，对纹理像素点着色时要获取对应hex数据，建立先用一个批次建立好hex的数据
            // step2 : 第二个batch，进行初步混合，可以是catlike的，也可以用其他的混合方式（总之要达到cv6的效果）
            // step3 : 第三个batch，在2之上对边缘进行混合，例如加入噪声控制边缘的插值（总之要像cv6）
            // step4 : 第四个batch，更多的地形混合，上面已经完成了第一层地貌混合，但是还需要更多的效果（cv6一些地形会叠加在其他地形上面，如沼泽）
            // 故要对 job 进行拆分，按 bacth 来

            ExportHexLandFormJob exportJob = new ExportHexLandFormJob {
                innerHexRatio = innerHexRatio,
                OuputTextureResolution = OuputTexResolution,
                hexGridSize = EditorSceneManager.hexSet.hexGridSize,
                blendMethod = blendMethod,
                layout = layout, 
                
                tempTerrainType = tempTerrainType,
                landformColors = nativeColors,
            };
            JobHandle jobHandle1 = exportJob.Schedule(OuputTexResolution * OuputTexResolution, 128);
            jobHandle1.Complete();

            // noise handle
            NoiseHexLandFormJob noiseHexLandForm = new NoiseHexLandFormJob() {
                OuputTexResolution = OuputTexResolution,
                noiseIntense = noiseIntense,
                noiseOffset = noiseOffset,

                perlinNoise = new PerlinNoise(OuputTexResolution, frequency, true, randomSpeed, new Vector2(1, 1), fbmIteration),
                originColors = nativeColors,
                targetColors = targetColors,
            };
            JobHandle jobHandle2 = noiseHexLandForm.Schedule(OuputTexResolution * OuputTexResolution, 128);
            jobHandle2.Complete();

            // TODO : lerp handle

            curHexLandformResult.SetPixels(targetColors.ToArray());
            //curHexLandformResult.SetPixels(nativeColors.ToArray());
            curHexLandformResult.Apply();

            nativeColors.Dispose();
            targetColors.Dispose();
            //idxColors.Dispose();
            //blendColors.Dispose();
            tempTerrainType.Dispose();

            Debug.Log($"blend over, use method : {blendMethod}, resolution : {OuputTexResolution}");
        }// C#, python, matlab, C++, Java, R, 

        private Color SampleFromTex(int i, int j, Texture2D tex) {
            return tex.GetPixel(i, j);
        }


        [FoldoutGroup("六边形纹理构建")]
        [Button("保存六边形混合索引/权重图", ButtonSizes.Medium)]
        private void ExportHexBlendWeightTex() {
            if (curHexLandformIdxTex == null) {
                Debug.Log("cur idx texture is null, so we can not save");
                return;
            }
            if (curHexLandformBlendTex == null) {
                Debug.Log("cur blend weight texture is null, so we can not save");
                return;
            }

            string texName1 = string.Format("idx_landform_{0}x{0}_{1}", OuputTexResolution, DateTime.Now.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(hexLandformTexImportPath, texName1, curHexLandformIdxTex);
            string texName2 = string.Format("blendweight_landform_{0}x{0}_{1}", OuputTexResolution, DateTime.Now.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(hexLandformTexImportPath, texName2, curHexLandformBlendTex);

        }

        [FoldoutGroup("六边形纹理构建")]
        [Button("保存六边形地貌纹理图", ButtonSizes.Medium)]
        private void SaveHexLandFormTex() {
            string texName = string.Format("landform_{0}x{0}_{1}", OuputTexResolution, DateTime.Now.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(hexLandformTexImportPath, texName, curHexLandformResult);
        }

        public List<Vector2Int> GetLinePoints(Vector2Int A, Vector2Int B) {
            List<Vector2Int> points = new List<Vector2Int>();

            int dx = Mathf.Abs(B.x - A.x);
            int dy = Mathf.Abs(B.y - A.y);
            int sx = A.x < B.x ? 1 : -1;
            int sy = A.y < B.y ? 1 : -1;
            int err = dx - dy;

            int x = A.x, y = A.y;

            while (true) {
                points.Add(new Vector2Int(x, y));
                if (x == B.x && y == B.y) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }

            return points;
        }

        private Color GetColorByLineDistance(Vector2 point, Vector2 A, Vector2 B, float maxDistance = 2) {
            float distance = DistanceToSegment(point, A, B);
            if (distance > maxDistance)
                return Color.white;

            float intensity = 1f - (distance / maxDistance);
            return new Color(1f, intensity, intensity);
        }

        private float DistanceToSegment(Vector2 P, Vector2 A, Vector2 B) {
            Vector2 AB = B - A;
            Vector2 AP = P - A;

            float abSquared = AB.sqrMagnitude;
            if (abSquared == 0f) {  // A B重合
                return AP.magnitude;
            } 

            float t = Mathf.Clamp01(Vector2.Dot(AP, AB) / abSquared);
            Vector2 closest = A + t * AB;
            return Vector2.Distance(P, closest);
        }

        #endregion


        private MapGrid curMapGrid;

        private List<MapGrid> curGridsInScope;


        public override void Enable() {
            base.Enable();
            //GizmosCtrl.GetInstance().RegisterGizmoEvent(ShowCurChooseGrid);
        }

        public override void Disable() {
            //GizmosCtrl.GetInstance().UnregisterGizmoEvent(ShowCurChooseGrid);
            base.Disable();
        }

        public override void Destory() {
            base.Destory();
            if (curHexLandformIdxTex != null) {
                GameObject.DestroyImmediate(curHexLandformIdxTex);
                curHexLandformIdxTex = null;
            }
            if (curHexLandformBlendTex != null) {
                GameObject.DestroyImmediate(curHexLandformBlendTex);
                curHexLandformBlendTex = null;
            }
            if (curHexLandformResult != null) {
                GameObject.DestroyImmediate(curHexLandformResult);
                curHexLandformResult = null;
            }
        }

        protected override void OnMouseDrag(Event e) {

            if (curGridsInScope == null) {
                return;
            }
            DetectCurChooseGrid(e);

            //HexCtor.SetMapGridColor(curGridsInScope, BrushColor);
        }

        protected override void OnMouseMove(Event e) {
            base.OnMouseMove(e);
            
            if(HexCtor == null) {
                return;
            }

            DetectCurChooseGrid(e);
        }

        private void DetectCurChooseGrid(Event e) {
            //Vector2 CurPos = GetMousePos(e);
            //MapGrid grid = HexCtor.GetClosestMapGrid(CurPos);
            //if (grid != null) {
            //    //if (curMapGrid != grid) {
            //    //    curMapGrid = grid;
            //    //    curGridsInScope = HexCtor.GetMapGrid_HexScope(curMapGrid.mapIdx.x, curMapGrid.mapIdx.y, BrushScope);
            //    //}
            //}
        }

    }
}
