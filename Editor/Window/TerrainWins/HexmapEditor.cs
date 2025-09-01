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

        [FoldoutGroup("��ʼ����ͼ")]
        [LabelText("��ͼ�߶�")]
        public float hexHeight = 30;

        [FoldoutGroup("��ʼ����ͼ")]
        [LabelText("��ͼ����")]
        public Material hexMaterial;

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("��ʼ��Hex��ͼ", ButtonSizes.Medium)]
        private void DrawRectangleGrid() {
            HexCtor.InitHexConsRectangle(null);
        }

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("ˢ�µ�ͼ", ButtonSizes.Medium)]
        private void UpdateHexMap() {
            // set the height
            
            // update the material
        }

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("��յ�ͼ", ButtonSizes.Medium)]
        private void ClearHexMap() {

            EditorSceneManager.mapScene.hexClusterParentObj.ClearObjChildren();
        }

        #endregion

        #region hexTexture Construct
        // NOTE : �˴��������������������Ϊ�˹��� ���� ���������� ������ Terrain �ر�

        public enum BlendMethod {
            Catlike, NoCorner, NoInner
        }

        [FoldoutGroup("������������/��������")]
        [LabelText("��Ϸ�ʽ")]
        public BlendMethod blendMethod = BlendMethod.Catlike;

        [FoldoutGroup("������������/��������")]
        [LabelText("��������Ȧ����")]
        public float innerHexRatio = 0.8f;

        [FoldoutGroup("������������/��������")]
        [LabelText("�ֱ���")]
        public int OuputTexResolution = 1024;

        [FoldoutGroup("������������/��������")]
        [LabelText("��ɫ")]
        public Color hexEdgeColor = new Color(1, 1, 1, 0.5f);

        [FoldoutGroup("������������/������ƽ��")]
        [LabelText("����Ƶ��")]
        public float frequency = 16;

        [FoldoutGroup("������������/������ƽ��")]
        [LabelText("�����")]
        public float randomSpeed = 1;

        [FoldoutGroup("������������/������ƽ��")]
        [LabelText("��������")]
        public int fbmIteration = 8;

        [FoldoutGroup("������������/������ƽ��")]
        [LabelText("����ǿ��")]
        public float noiseIntense = 30;

        [FoldoutGroup("������������/������ƽ��")]
        [LabelText("����ƫ��")]
        public float noiseOffset = 0.5f;

        [FoldoutGroup("������������")]
        [LabelText("��òͼ��")]
        public Texture2D curHexTerrainSplat;    //��ǰû����

        [FoldoutGroup("������������")]
        [LabelText("��ò��������")]
        public Texture2D curHexLandformIdxTex;  //��ǰû����

        [FoldoutGroup("������������")]
        [LabelText("��ò���Ȩ��")]
        public Texture2D curHexLandformBlendTex;    //��ǰû����

        [FoldoutGroup("������������")]
        [LabelText("���ɵĵ�ò����")]
        public Texture2D curHexLandformResult;


        [FoldoutGroup("������������")]
        [LabelText("��ͼ����λ��")]
        public string hexLandformTexImportPath = MapStoreEnum.HexLandformTexOutputPath;

        // TODO : ��Ŀ�꣺��������6�ĵ�ò��ʽ���ɻ�ϵ�ò�������������£�
        // step1 : ͨ����������ӵõ�ÿ��Hex���ӵĵ�������
        // step2 : ׼����Ӧ���ε���������16�ţ���Щ�����������ʵ����һһ��Ӧ�ģ����ܻ���е��ӣ�
        // ��--Sloved--��step3 : ����catlike�ķ���������� hex ��ϵ�ò������Ե��ֵ����Χ���ӽ��л�ϣ����Ĳ������
        // step4 : ͨ�� renderdoc �� ����6 ����Ŀ��������
        // step4 : ��Ҫ���У�ͨ�������ȷ�ʽ�Ա�Ե��ϵش����д����õ���Ϊ��Ȼ�����۵ĵ�ò
        // step5 : ��Ҫ���У�������ĵ�òЧ�����˽�����ԭ��дshader......
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
                    // TODO : ��ô�� idx color �� base color ��ӳ�䣿   // use Texture2DArray
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
                        // ��������,���ʱ : cur + 1, neighbor + 2, next + 4 
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
                        // ��������,���ʱ : neighbor - 2, next - 4 
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
                // TODO : ����ΪĿǰ�Ļ�Ͻ����������ģ����Ҳ�֪���������������

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
                // NOTE : �������Ŀǰ��ռλ��, ֮��Ҫ�ӵ�ò��������ȡ��ɫ
                switch (type) {
                    case 0:
                        return new Color(0.20f, 0.60f, 0.20f); // ǳ��ɫ #339933
                    case 1:
                        return new Color(0.78f, 0.65f, 0.35f); // ����ɫ #C8A55A
                    case 2:
                        return new Color(0.62f, 0.49f, 0.34f); // ǳ��ɫ #9E7E57
                    case 3:
                        return new Color(0.85f, 0.85f, 0.85f); // ǳ��ɫ #D8D8D8;
                    case 4:
                        return new Color(0.85f, 0.85f, 0.85f); // ǳ��ɫ #D8D8D8;
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

        [FoldoutGroup("������������")]
        [Button("���������λ�ϵ�ò����", ButtonSizes.Medium)]
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

            // ��ʱʹ�� ���hashmap ��¼���� hex ʡ�ݵĵ�������
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

            // TODO : ���ڵĵ�ò��Ϸ����������²���
            // step1 : ���������ε����ݣ����������ص���ɫʱҪ��ȡ��Ӧhex���ݣ���������һ�����ν�����hex������
            // step2 : �ڶ���batch�����г�����ϣ�������catlike�ģ�Ҳ�����������Ļ�Ϸ�ʽ����֮Ҫ�ﵽcv6��Ч����
            // step3 : ������batch����2֮�϶Ա�Ե���л�ϣ���������������Ʊ�Ե�Ĳ�ֵ����֮Ҫ��cv6��
            // step4 : ���ĸ�batch������ĵ��λ�ϣ������Ѿ�����˵�һ���ò��ϣ����ǻ���Ҫ�����Ч����cv6һЩ���λ�����������������棬������
            // ��Ҫ�� job ���в�֣��� bacth ��

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


        [FoldoutGroup("������������")]
        [Button("���������λ������/Ȩ��ͼ", ButtonSizes.Medium)]
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

        [FoldoutGroup("������������")]
        [Button("���������ε�ò����ͼ", ButtonSizes.Medium)]
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
            if (abSquared == 0f) {  // A B�غ�
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
