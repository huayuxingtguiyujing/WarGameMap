using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public struct MapBoundStruct
    {
        int Left;
        int Right;
        int Up;
        int Down;

        public MapBoundStruct(int left, int right, int up, int down)
        {
            Left = left;
            Right = right;
            Up = up;
            Down = down;

            if (!CheckValid())
            {
                Debug.LogError($"bound is not valid : {Left}, {Right}, {Up}, {Down}");
            }
        }

        public bool InsideBound(Vector3 pos)
        {
            return (pos.x > Left) && (pos.x < Right) && (pos.z > Down) && (pos.z < Up);
        }

        public List<Vector2Int> GetPixelInsideBound()
        {
            int count = (int)(Up - Down + 1) * (int)(Right - Left + 1);
            count = Mathf.Abs(count);
            List<Vector2Int> pixels = new List<Vector2Int>(count);
            for (int i = (int)Left; i <= (int)Right; i++)
            {
                for (int j = (int)Down; j <= (int)Up; j++)
                {
                    pixels.Add(new Vector2Int(i, j));
                }
            }
            return pixels;
        }

        public void AddPixelInsideBound(HashSet<Vector2Int> pixelsSet)
        {
            for (int i = (int)Left; i <= (int)Right; i++)
            {
                for (int j = (int)Down; j <= (int)Up; j++)
                {
                    pixelsSet.Add(new Vector2Int(i, j));    // if pixel exist in bound, Add() return false
                }
            }
        }

        public bool CheckValid()
        {
            return (Right >= Left) && (Up >= Down);
        }
    }
}
