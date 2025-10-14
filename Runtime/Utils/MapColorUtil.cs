using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class MapColorUtil
    {
        public static readonly Color NotValidColor = Color.black;

        public static readonly Color[] Colors = new Color[]
        {
            new Color(0.91f, 0.12f, 0.39f), // ���޷ۺ�
            new Color(0.20f, 0.60f, 1.00f), // ����
            new Color(0.00f, 0.75f, 0.45f), // ��ɫ
            new Color(1.00f, 0.65f, 0.00f), // ��ɫ
            new Color(0.60f, 0.20f, 0.80f), // ��ɫ
            new Color(1.00f, 0.30f, 0.30f), // ��ɫ
            new Color(0.10f, 0.80f, 0.80f), // ��ɫ
            new Color(0.90f, 0.80f, 0.10f), // ��ɫ
            new Color(0.30f, 0.90f, 0.40f), // ����ɫ
            new Color(0.95f, 0.55f, 0.75f), // ����
            new Color(0.50f, 0.60f, 0.95f), // ����
            new Color(0.85f, 0.50f, 0.20f), // �س�
            new Color(0.20f, 0.85f, 0.55f), // ������
            new Color(0.80f, 0.25f, 0.55f), // õ��
            new Color(0.55f, 0.75f, 0.20f), // �����
            new Color(0.70f, 0.30f, 0.95f)  // ����
        };

        private static readonly System.Random rng = new System.Random();

        public static Color GetValidColor_Random()
        {
            int index = rng.Next(Colors.Length);
            return Colors[index];
        }

        public static Color GetDifferentColor(HashSet<Color> disableColors)
        {
            int curIndex = rng.Next(Colors.Length);
            int maxIter = Colors.Length;
            int curIter = 0;
            while(curIter < maxIter)
            {
                if (curIndex >= Colors.Length)
                {
                    curIndex = 0;
                }
                if (!disableColors.Contains(Colors[curIndex]))
                {
                    return Colors[curIndex];
                }

                curIndex++;
                curIter++;
            }
            Debug.LogError("Can not find a valid color, you should expand more country colors");
            return NotValidColor;
        }

        public static Color GetRandomColor(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Colors[UnityEngine.Random.Range(0, Colors.Length)];

            int hash = name.GetHashCode();
            int index = Mathf.Abs(hash) % Colors.Length;
            return Colors[index];
        }

        public static Color GetRandomColor(int index)
        {
            if(index < 0)
            {
                index = 0;
            }
            if (Colors == null || Colors.Length == 0)
            {
                return Color.white;
            }

            return Colors[index % Colors.Length];
        }
    }
}
