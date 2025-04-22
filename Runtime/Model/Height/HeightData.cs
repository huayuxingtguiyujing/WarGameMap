using LZ.WarGameCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    [Serializable]
    [SerializeField]
    public class HeightData {

        public int longitude;
        public int latitude;
        public int size;

        [SerializeField]
        List<float> heightDatas;
        public List<float> HeightDatas {  get { return heightDatas; } }

        public HeightData() { }

        public HeightData(int longitude, int latitude, int size, float[,] heightDatas) {
            this.longitude = longitude;
            this.latitude = latitude;
            this.size = size;

            this.heightDatas = new List<float>(size * size);

            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    SetHeight(i, j, heightDatas[i, j]);
                }
            }
        }

        public float this[int x, int y] {
            get {
                if (x < 0 || x >= size || y < 0 || y >= size) {
                    throw new ArgumentOutOfRangeException($"Ë÷Òý ({x}, {y}) ³¬³ö·¶Î§");
                }
                return heightDatas[x + size * y];
            }
            private set {
                if (x < 0 || x >= size || y < 0 || y >= size) {
                    throw new ArgumentOutOfRangeException($"Ë÷Òý ({x}, {y}) ³¬³ö·¶Î§");
                }
                heightDatas[x + size * y] = value;
            }
        }

        public float GetHeight(int i, int j) {
            if (i < 0 || i >= size || j < 0 || j >= size) {
                Debug.LogError($"out of index, error index {i}, {j}");
                return -1;
            }
            int idx = i * size + j;
            return heightDatas[idx];
        }

        public void SetHeight(int i, int j, float height) {
            if (i < 0 || i >= size || j < 0 || j >= size) {
                Debug.LogError($"out of index, error index {i}, {j}, size is {size}");
                return;
            }
            int idx = i * size + j;
            if (idx >= heightDatas.Count) {
                this.heightDatas.Add(height);
            } else {
                this.heightDatas[idx] = height;
            }
        }

        public int GetLength() {
            return size;
        }

    }
}
