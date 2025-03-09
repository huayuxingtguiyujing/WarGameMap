using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;
//using 

namespace LZ.WarGameMap.MapEditor
{

    // NOTE: WARNING: fileName 必须和 EditorName 一样
    //[CreateAssetMenu(fileName = "网格编辑", menuName = "地图/编辑器/网格编辑器")]
    [Serializable]
    public class HexmapEditor : BaseMapEditor {


        public override string EditorName => MapEditorEnum.HexMapEditor;


        public static class TerrainColorEnum {
            public static Color RiverColor = Color.white;
            public static Color SeaColor = Color.white;

            public static Color PlainColor = Color.white;
            public static Color HillColor = Color.white;
            public static Color MountainColor = Color.white;

        }

        GameObject mapRootObj;
        GameObject hexClusterParentObj;
        GameObject hexSignParentObj;

        HexmapConstructor HexCtor;

        [FoldoutGroup("配置地图Scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: 没有初始化Scene")]
        public string warningMessage = "请点击按钮初始化!";

        [FoldoutGroup("配置地图Scene", -1)]
        [Button("初始化地图 Scene")]
        protected override void InitEditor() {
            if (mapRootObj == null) {
                mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
                if (mapRootObj == null) {
                    mapRootObj = new GameObject(MapSceneEnum.MapRootName);
                }
            }
            if (hexClusterParentObj == null) {
                hexClusterParentObj = GameObject.Find(MapSceneEnum.HexClusterParentName);
                if (hexClusterParentObj == null) {
                    hexClusterParentObj = new GameObject(MapSceneEnum.HexClusterParentName);
                }
            }
            hexClusterParentObj.transform.parent = mapRootObj.transform;

            if (hexSignParentObj == null) {
                hexSignParentObj = GameObject.Find(MapSceneEnum.HexSignParentName);
                if (hexSignParentObj == null) {
                    hexSignParentObj = new GameObject(MapSceneEnum.HexSignParentName);
                }
            }
            hexSignParentObj.transform.parent = mapRootObj.transform;

            // TODO: 以后其他界面也需要用到 HexConstructor, 需要放到其他地方（开个单例）
            HexCtor = mapRootObj.GetComponent<HexmapConstructor>();
            if (HexCtor == null) {
                HexCtor = mapRootObj.AddComponent<HexmapConstructor>();
            }
            HexCtor.SetMapPrefab(hexClusterParentObj.transform, hexSignParentObj.transform);
            notInitScene = false;
        }


        #region init map

        [FoldoutGroup("初始化地图")]
        [LabelText("矩形宽度")]
        public Vector2 LeftAndRight = new Vector2(0, 10);
        [FoldoutGroup("初始化地图")]
        [LabelText("矩形高度")]
        public Vector2 TopAndBottom = new Vector2(0, 10);

        [FoldoutGroup("初始化地图")]
        [Button("绘制矩形网格")]
        private void DrawRectangleGrid() {
            HexCtor.InitHexConsRectangle(
                (int)TopAndBottom.x, (int)TopAndBottom.y,
                (int)LeftAndRight.x, (int)LeftAndRight.y
            );
        }

        [FoldoutGroup("初始化地图")]
        [LabelText("平行四边形宽度")]
        public Vector2 Q1AndQ2 = new Vector2(0, 10);
        [FoldoutGroup("初始化地图")]
        [LabelText("平行四边形高度")]
        public Vector2 Q3AndQ4 = new Vector2(0, 10);

        [FoldoutGroup("初始化地图")]
        [Button("绘制平行四边形网格")]
        private void DrawParallelogramGrid() {
            HexCtor.InitHexConsParallelogram(
                (int)Q1AndQ2.x, (int)Q1AndQ2.y,
                (int)Q3AndQ4.x, (int)Q3AndQ4.y
            );
        }

        [FoldoutGroup("初始化地图")]
        [Button("清空地图")]
        private void ClearHexMap() {
            hexClusterParentObj.ClearObjChildren();
        }

        [FoldoutGroup("初始化地图")]
        [Button("清空标记")]
        private void ClearHexSigns() {
            hexSignParentObj.ClearObjChildren();
        }

        //[FoldoutGroup("初始化地图")]
        //[Button("构建地图四叉树")]
        //private void BuildQuadTree() {
            //HexCtor.BuildQuadTreeMap();
        //}
        //[FoldoutGroup("初始化地图")]
        //[OnValueChanged("ShowHidQuadTree")]
        //public bool ShowQuadTree = true;
        //private void ShowHidQuadTree() {
            //HexCtor.ShowQuadTreeGizmos(ShowQuadTree);
        //}

        #endregion


        #region test function

        [FoldoutGroup("测试")]
        [LabelText("网格坐标")]
        public Vector2Int gridIndex = new Vector2Int(0, 0);

        [FoldoutGroup("测试")]
        [Button("获取邻居")]
        private void TestGetNeighbors() {
            HexCtor.GetMapGridNeighbor(gridIndex.x, gridIndex.y);
        }

        [FoldoutGroup("测试")]
        [Button("获取坐标")]
        private void TestGetCoordinates() {
            HexCtor.GetMapGridTest(gridIndex.x, gridIndex.y);
        }

        #endregion

        #region draw map

        [FoldoutGroup("绘制")]
        public bool OpenBrush = false;

        [FoldoutGroup("绘制")]
        public int BrushScope = 3;

        [FoldoutGroup("绘制")]
        public Color32 BrushColor = TerrainColorEnum.RiverColor;

        [FoldoutGroup("绘制")]
        public Color32 SignColor = Color.green;

        #endregion


        #region hexTexture Construct
        // NOTE : 此处输出六边形网格纹理，是为了构建 类似 文明那样的 六边形 Terrain 地表
        
        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("分辨率")]
        public int OuputTextureResolution = 1024;

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("六边形宽度")]
        public int hexGridSize = 10;

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("六边形起点")]
        public Vector2 hexGridStart = new Vector2(0, 0);

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("颜色")]
        public Color hexEdgeColor = new Color(1, 1, 1, 0.5f);

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("输出位置")]
        public string OuputHexTexturePath = MapStoreEnum.HexLandformTexOutputPath;

        [FoldoutGroup("六边形纹理构建")]
        [Button("输出六边形纹理", ButtonSizes.Medium)]
        private void ConsHexTexture() {

            Texture2D hexTexture = new Texture2D(OuputTextureResolution, OuputTextureResolution);

            // TODO : 构建六边形的纹理
            // 1.先用 hexgenerator 得到 hex 的数据
            // 2.遍历所有的 hex，计算哪些格子在六条线段上，然后设置它们的颜色（给点 A 和 B 然后得出线段关系...（用bresenham画线算法））
            // over

            HexGenerator hexGenerator = new HexGenerator();
            hexGenerator.GenerateRectangle(0, 50, 0, 50);

            Layout layout = HexCtor.GetScreenLayout(new Vector2(20, 20));
            foreach (var pair in hexGenerator.HexagonIdxDic)
            {
                Vector2Int idx = pair.Key;
                Hexagon hexagon = pair.Value;

                Point center = hexagon.Hex_To_Pixel(layout, hexagon).ConvertToXZ();
                for (int i = 0; i < 6; i++) {
                    Point curOffset = hexagon.Hex_Corner_Offset(layout, i);
                    Point curVertex = center + new Point(curOffset.x, 0, curOffset.y);

                    Point nextOffset = hexagon.Hex_Corner_Offset(layout, i + 1);
                    Point nextVertex = center + new Point(nextOffset.x, 0, nextOffset.y);

                    Vector2Int curPoint = new Vector2Int((int)curVertex.x, (int)curVertex.z);
                    Vector2Int nextPoint = new Vector2Int((int)nextVertex.x, (int)nextVertex.z);
                    List<Vector2Int> texPointsInLine = GetLinePoints(curPoint, nextPoint);
                    foreach (var point in texPointsInLine)
                    {
                        hexTexture.SetPixel(point.x, point.y, Color.red);
                    }
                }
            }

            hexTexture.Apply();

            string hexTextureName = string.Format("hexTexture{0}x{0}_{1}", OuputTextureResolution, DateTime.Now.Ticks.ToString());
            TextureUtility.GetInstance().SaveTextureAsAsset(OuputHexTexturePath, hexTextureName, hexTexture);

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

        protected override void OnMouseDrag(Event e) {

            if (curGridsInScope == null) {
                return;
            }
            DetectCurChooseGrid(e);

            HexCtor.SetMapGridColor(curGridsInScope, BrushColor);
        }

        protected override void OnMouseMove(Event e) {
            base.OnMouseMove(e);
            
            if(HexCtor == null) {
                return;
            }

            DetectCurChooseGrid(e);
        }

        private void DetectCurChooseGrid(Event e) {
            Vector2 CurPos = GetMousePos(e);
            MapGrid grid = HexCtor.GetClosestMapGrid(CurPos);
            if (grid != null) {
                if (curMapGrid != grid) {
                    curMapGrid = grid;
                    curGridsInScope = HexCtor.GetMapGrid_HexScope(curMapGrid.mapIdx.x, curMapGrid.mapIdx.y, BrushScope);
                }
            }
        }


    }
}
