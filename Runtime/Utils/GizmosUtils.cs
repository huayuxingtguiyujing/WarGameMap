using Codice.Client.BaseCommands.BranchExplorer;
using log4net.Util;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace LZ.WarGameMap.MapEditor
{
    public static class GizmosUtils
    {
        public static void DrawCube(Vector3 position, Color32 color, float size = 0.5f) {
            Gizmos.color = color;
            Gizmos.DrawCube(position, new Vector3(size, size, size));
        }

        public static void DrawRect(Vector3 leftDown, Vector3 rightUp, Color32 color) {
            Gizmos.color = color;

            Vector3 leftUp = new Vector3(leftDown.x, 0, rightUp.z);
            Vector3 rightDown = new Vector3(rightUp.x, 0, leftDown.z);

            Gizmos.DrawLine(leftDown, leftUp);
            Gizmos.DrawLine(leftUp, rightUp);
            Gizmos.DrawLine(rightUp, rightDown);
            Gizmos.DrawLine(rightDown, leftDown);
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
            if (index >= 0 && index < colors.Length) {
                return colors[index];
            } else {
                Debug.LogWarning("Index out of bounds, returning default color (Red).");
                return colors[0]; // 默认返回红色
            }
        }

    }
}
