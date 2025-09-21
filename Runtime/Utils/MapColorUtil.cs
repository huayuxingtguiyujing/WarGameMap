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
            new Color(1f, 0.5f, 0f),   // ��ɫ
            new Color(0.5f, 0f, 1f),   // ��ɫ
            new Color(0f, 0.7f, 0.3f), // ����
        };

        private static readonly System.Random rng = new System.Random();

        public static Color GetValidColor_Random()
        {
            int index = rng.Next(validColors.Length);
            return validColors[index];
        }

        // ��ɫԭ��������ͨͼ������
        // TODO : 
        // Ҫ�и����ݱ߽繹��������ͨͼ�ķ�����
    }
}
