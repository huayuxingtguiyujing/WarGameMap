using LZ.WarGameCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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
                    throw new ArgumentOutOfRangeException($"索引 ({x}, {y}) 超出范围");
                }
                return heightDatas[x + size * y];
            }
            private set {
                if (x < 0 || x >= size || y < 0 || y >= size) {
                    throw new ArgumentOutOfRangeException($"索引 ({x}, {y}) 超出范围");
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

    // 如何支持嵌套的 height data？？？
    // NOTE : 为了适配 JobSystem 的HeightData类
    public struct HeightData_Blittable : IDisposable{
        public int longitude;
        public int latitude;
        public int size;

        NativeArray<half> heightDatas;

        public HeightData_Blittable(HeightData heightData) {
            this.longitude = heightData.longitude;
            this.latitude = heightData.latitude;
            this.size = heightData.size;

            //// copy data to this height Datas
            //this.heightDatas = new NativeArray<half>(size * size, Allocator.Persistent);
            //NativeArray<float> temp = new NativeArray<float>(size * size, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            //temp.CopyFrom(heightData.HeightDatas.ToArray());
            //for (int i = 0; i < temp.Length; i++) {
            //    this.heightDatas[i] = (half)temp[i];
            //}
            //temp.Dispose();

            this.heightDatas = new NativeArray<half>(size * size, Allocator.Persistent);
            NativeArray<float> temp = new NativeArray<float>(size * size, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            temp.CopyFrom(heightData.HeightDatas.ToArray());

            var convertJob = new ConvertHeightDataJob {
                input = temp,
                output = heightDatas
            };
            JobHandle handle = convertJob.Schedule(temp.Length, 64);
            handle.Complete();
            temp.Dispose();

            //for (int i = 0; i < size; i++) {
            //    for (int j = 0; j < size; j++) {
            //        SetHeight(i, j, heightData[i, j]);
            //    }
            //}
        }

        public void Dispose() {
            heightDatas.Dispose();
        }

        public half this[int x, int y] {
            get {
                if (x < 0 || x >= size || y < 0 || y >= size) {
                    throw new ArgumentOutOfRangeException($"索引 ({x}, {y}) 超出范围");
                }
                return heightDatas[x + size * y];
            }
            private set {
                if (x < 0 || x >= size || y < 0 || y >= size) {
                    throw new ArgumentOutOfRangeException($"索引 ({x}, {y}) 超出范围");
                }
                heightDatas[x + size * y] = value;
            }
        }

        public void SetHeight(int i, int j, float height) {
            //if (i < 0 || i >= size || j < 0 || j >= size) {
            //    Debug.LogError($"out of index, error index {i}, {j}, size is {size}");
            //    return;
            //}
            int idx = i * size + j;
            heightDatas[idx] = (half)height;
        }


        struct ConvertHeightDataJob : IJobParallelFor {
            [ReadOnly] public NativeArray<float> input;
            public NativeArray<half> output;

            public void Execute(int index) {
                output[index] = (half)input[index];
            }
        }

    }

}
