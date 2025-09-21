using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class MapColorUtil
    {
        private static readonly Color[] validColors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.yellow,
            new Color(1f, 0.5f, 0f),   // 橙色
            new Color(0.5f, 0f, 1f),   // 紫色
            new Color(0f, 0.7f, 0.3f), // 青绿
        };

        private static readonly System.Random rng = new System.Random();

        public static Color GetValidColor_Random()
        {
            int index = rng.Next(validColors.Length);
            return validColors[index];
        }

        // 四色原理，区域连通图！！！
        // TODO : 
        // 要有个根据边界构建区域连通图的方法！
    }
}
