
using LZ.WarGameMap.Runtime;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    [CustomEditor(typeof(HexmapConstructor))]
    public class HexConsEditor : Editor {


        int mapSize;

        Vector2 Q1AndQ2;
        Vector2 Q3AndQ4;

        Vector2 LeftAndRight = new Vector2(0, 10);
        Vector2 TopAndBottom = new Vector2(0, 10);

        int mapSizeHex;

        Vector2Int noiseWH = new Vector2Int(20, 20);
        float noiseScale = 1.0f;

        Vector2Int gridIdx = new Vector2Int(0, 0);


        public override void OnInspectorGUI() {
            DrawDefaultInspector();

            HexmapConstructor hexConstructor = (HexmapConstructor)target;

            // 空白区域
            GUILayout.Space(15);

            // 创建加粗的文本样式
            GUIStyle boldTextStyle = new GUIStyle() {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };
            EditorGUILayout.LabelField("编辑器界面功能:", boldTextStyle);

            GUILayout.Space(6);

            if (GUILayout.Button("清空网格")) {
                //hexConstructor.ClearHexObject();
                //HexGenerator.GetInstance().ClearHexagon();
            }
            if (GUILayout.Button("绘制河流")) {
                //hexConstructor.GenerateRiver();
            }
            if (GUILayout.Button("刷新网格")) {
                //hexConstructor.RefreshHexagons();
            }

            GUILayout.Space(6);

            GUILayout.Space(6);

            //inspector界面 四种绘制方式，提供按钮接口
            GUILayout.Label("三角形网格大小");
            mapSize = EditorGUILayout.IntField(mapSize, GUILayout.Width(80));
            if (GUILayout.Button("绘制三角形网格")) {
                hexConstructor.InitHexConsTriangle(mapSize);
            }

            Q1AndQ2 = EditorGUILayout.Vector2Field("Left-Right", Q1AndQ2);
            Q3AndQ4 = EditorGUILayout.Vector2Field("Top-Bottom", Q3AndQ4);
            if (GUILayout.Button("绘制平行四边形网格")) {
                hexConstructor.InitHexConsParallelogram(
                    (int)Q1AndQ2.x, (int)Q1AndQ2.y,
                    (int)Q3AndQ4.x, (int)Q3AndQ4.y
                );
            }

            LeftAndRight = EditorGUILayout.Vector2Field("Left-Right", LeftAndRight);
            TopAndBottom = EditorGUILayout.Vector2Field("Top-Bottom", TopAndBottom);
            if (GUILayout.Button("绘制矩形网格")) {
                //hexConstructor.InitHexConsRectangle(
                //    (int)TopAndBottom.x, (int)TopAndBottom.y,
                //    (int)LeftAndRight.x, (int)LeftAndRight.y
                //);
            }

            GUILayout.Label("六边形网格大小");
            mapSizeHex = EditorGUILayout.IntField(mapSizeHex, GUILayout.Width(80));
            if (GUILayout.Button("绘制六边形网格")) {
                hexConstructor.InitHexConsHexagon(mapSize);
            }

            GUILayout.Space(12);
            GUILayout.Label("地图随机扰动");
            GUILayout.Space(6);
            noiseWH = EditorGUILayout.Vector2IntField("Noise Width Height", noiseWH);
            GUILayout.Space(2);
            GUILayout.Label("噪声规模");
            noiseScale = EditorGUILayout.FloatField(noiseScale, GUILayout.Width(80));
            if (GUILayout.Button("生成柏林噪声")) {
                //hexConstructor.GeneratePerlinTexture(noiseWH.x, noiseWH.y, noiseScale);
            }

            if (GUILayout.Button("应用柏林噪声")) {
                //hexConstructor.DisturbGridMap();
            }

            GUILayout.Space(2);
            gridIdx = EditorGUILayout.Vector2IntField("Grid Index", gridIdx);
            if (GUILayout.Button("测试获取邻居")) {
                hexConstructor.GetMapGridNeighbor(gridIdx.x, gridIdx.y);
            }
            if (GUILayout.Button("测试坐标")) {
                hexConstructor.GetMapGridTest(gridIdx.x, gridIdx.y);
            }

        }

    }
}
