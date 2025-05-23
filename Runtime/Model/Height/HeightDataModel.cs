
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // NOTE : heightEditor 中使用反序列化之后得到的这个 HeightDataModel 占空间非常大，几乎扩大了有5倍存储
    public class HeightDataModel : ScriptableObject {

        public int heightFileNums;

        public int singleHeightFileSize;

        [SerializeField] List<HeightData> heightDataList;
        [SerializeField] Dictionary<string, HeightData> heightDataDict;

        public List<HeightData> HeightDataList {  get { return heightDataList; } }

        [Tooltip("该 model 下属的所有地理数据对应的经纬度，对此的修改没有任何作用，仅供展示")]
        public List<Vector2Int> GeographyList;

        private void OnEnable() {
            UpdateData();
        }

        public HeightDataModel() { }

        public void InitHeightModel(int heightFileNums, int size) {
            this.heightFileNums = heightFileNums;
            this.singleHeightFileSize = size;
            heightDataList = new List<HeightData>();
            heightDataDict = new Dictionary<string, HeightData>();
            GeographyList = new List<Vector2Int>();
        }

        public void AddHeightData(int longitude, int latitude, int size, float[,] heightDatas) {
            HeightData heightData = new HeightData(longitude, latitude, size, heightDatas);
            heightDataList.Add(heightData);
            GeographyList.Add(new Vector2Int(longitude, latitude));
        }

        public HeightData GetHeightData(int longitude, int latitude) {
            string key = GetHeightDataKey(longitude, latitude);
            return heightDataDict[key];
        }

        public bool ExistHeightData(int longitude, int latitude) {
            UpdateData();
            string key = GetHeightDataKey(longitude, latitude);
            return heightDataDict.ContainsKey(key);
        }

        internal string GetHeightDataKey(int longitude, int latitude) {
            return string.Format("n{0}_e{1}", longitude, latitude); // GC 太严重了，不要这样写
        }

        private void UpdateData(){
            if(heightDataDict == null) {
                heightDataDict = new Dictionary<string, HeightData>();
            }
            if(heightDataList == null) {
                heightDataList = new List<HeightData>();
            }

            foreach (var heightData in heightDataList) {
                string key = GetHeightDataKey(heightData.longitude, heightData.latitude);
                if (heightDataDict.ContainsKey(key)) {
                    heightDataDict[key] = heightData;
                } else {
                    heightDataDict.Add(key, heightData);
                }
            }
        }

    }

    // NOTE : Runtime 的时候 用这个
    public struct HeightDataModel_Blittable : IDisposable {

        public int heightFileNums;
        public int singleHeightFileSize;

        NativeArray<HeightData_Blittable> heightDataList;
        public NativeHashSet<Vector2Int> GeographySet;

        public NativeArray<HeightData_Blittable> HeightDataList { get { return heightDataList; } }

        public HeightDataModel_Blittable(HeightDataModel heightDataModel) {
            heightFileNums = heightDataModel.heightFileNums;
            singleHeightFileSize = heightDataModel.singleHeightFileSize;

            GeographySet = new NativeHashSet<Vector2Int>(singleHeightFileSize, Allocator.Persistent);
            for(int i = 0;  i < heightFileNums; i++) {
                GeographySet.Add(heightDataModel.GeographyList[i]);
            }

            heightDataList = new NativeArray<HeightData_Blittable>(heightFileNums, Allocator.Persistent);
            for(int i = 0; i < heightFileNums; i++) {
                heightDataList[i] = new HeightData_Blittable(heightDataModel.HeightDataList[i]);
            }
            //heightDataDict = new NativeHashMap<int, HeightData_Blittable>(heightFileNums, Allocator.Persistent);
        }

        public void Dispose() {
            //heightDataDict.Dispose();
            foreach (var heightData in heightDataList)
            {
                heightData.Dispose();
            }
            heightDataList.Dispose();
            GeographySet.Dispose();
        }

        public half GetHeightData(int longitude, int latitude, int x, int y) {
            CacheData();

            return new half(1.0f);
            //return heightDataDict[key];
        }

        public bool ExistHeightData(int longitude, int latitude) {
            Vector2Int key = new Vector2Int(longitude, latitude);
            return GeographySet.Contains(key); // 遍历二十次？
        }


        public void CacheData() {
            //foreach (var heightData in heightDataList) {
            //    int key = GetHeightDataKey(heightData.longitude, heightData.latitude);
            //    if (heightDataDict.ContainsKey(key)) {
            //        heightDataDict[key] = heightData;
            //    } else {
            //        heightDataDict.Add(key, heightData);
            //    }
            //}
        }
    }
}
