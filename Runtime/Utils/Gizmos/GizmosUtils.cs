
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public static class GizmosUtils
    {

        static float maxDrawTxtDistance = 5000f;

        public static void DrawCube(Vector3 position, Color32 color, float size = 0.5f) {
            Gizmos.color = color;
            Gizmos.DrawCube(position, new Vector3(size, size, size));
        }

        public static void DrawRect(Vector2 leftDown, Vector2 rightUp, Color32 color) {
            Vector3 ld = new Vector3(leftDown.x, 0, rightUp.y);
            Vector3 ru = new Vector3(rightUp.x, 0, leftDown.y);
            DrawRect(ld, ru, color);
        }

        public static void DrawRect(Vector3 leftDown, Vector3 rightUp, Color32 color) {
            Gizmos.color = color;

            Vector3 leftUp = new Vector3(leftDown.x, 0, rightUp.z);
            Vector3 rightDown = new Vector3(rightUp.x, 0, leftDown.z);

            Gizmos.DrawLine(leftDown, leftUp);
            Gizmos.DrawLine(leftUp, rightUp);
            Gizmos.DrawLine(rightUp, rightDown);
            Gizmos.DrawLine(rightDown, leftDown);

            //Gizmos.Dr
        }

        public static void DrawText(Vector3 worldPos, string text, int fontSize, Color color, bool fade = true, int width = 20, int height = 20) {
            Camera cam = SceneView.lastActiveSceneView.camera;
            float dist = Vector3.Distance(cam.transform.position, worldPos);
            float sizeFactor = Mathf.Clamp(2f / dist, 1f, 2f); // 根据距离调节字体大小
            if (dist > maxDrawTxtDistance && fade) {
                return;
            }

            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = Mathf.RoundToInt(sizeFactor * fontSize);
            style.alignment = TextAnchor.MiddleCenter;

            Handles.Label(worldPos, text, style);
        }


        private static readonly Color[] colors = new Color[]
        {
            new Color(1f, 0f, 0f), // 红色
            new Color(0f, 1f, 0f), // 绿色
            new Color(0f, 0f, 1f), // 蓝色
            new Color(1f, 1f, 0f), // 黄色
            new Color(1f, 0f, 1f), // 品红色
            new Color(0f, 1f, 1f)  // 青色
        };

        public static Color32 GetRandomColor(int index) {
            if(index < 0) {
                return colors[0];
            } else {
                index = index % 6;
                return colors[index];
            }
        }

        public static Color32 GetRandomColor() {
            int idx = Random.Range(0, colors.Length);
            return colors[idx];
        }

    }
}
