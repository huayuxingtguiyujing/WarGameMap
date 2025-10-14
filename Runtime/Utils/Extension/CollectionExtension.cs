using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public static class CollectionExtension
    {
        public static Vector2 Average(this List<Vector2Int> list)
        {
            if (list == null || list.Count == 0)
                return Vector2.zero;

            float sumX = 0f, sumY = 0f;
            foreach (var v in list)
            {
                sumX += v.x;
                sumY += v.y;
            }
            return new Vector2(sumX / list.Count, sumY / list.Count);
        }

        public static void FillInList<T>(this List<T> list, int count)
        {
            T val = default(T);
            for(int i = 0; i < count; i++)
            {
                list.Add(val);
            }
        }

        public static void FillInList<T>(this NativeList<T> list, int count) where T : unmanaged
        {
            list.Clear();
            for (int i = 0; i < count; i++)
            {
                list.Add(default);
            }
        }

        public static void FillInList<T>(this NativeList<T> list, int count, T value) where T : unmanaged
        {
            list.Clear();
            for (int i = 0; i < count; i++)
            {
                list.Add(value);
            }
        }
    }
}
