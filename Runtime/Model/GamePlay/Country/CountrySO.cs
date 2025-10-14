using NUnit.Framework;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using static PlasticGui.LaunchDiffParameters;

namespace LZ.WarGameMap.Runtime.Model
{

    public class BaseCountryDatas
    {
        public static int MaxLayerNum                   = 3;        // NOTE : unresonable code!

        public static int NotValidLayerIndex            = -99;
        public static string NotValidLayerName          = "未选中层级";

        public static int RootLayerIndex                = -1;
        public static string RootLayerName              = "根层级";

        public static uint NotValidCountryIndex         = 9999;
        public static string NotValidCountryName        = "无";
        public static Color NotValidCountryColor        = Color.black;

        public static CountryLayer RegionLayer          = new CountryLayer(0, "Region", "大区", "");
        public static CountryLayer ProvinceLayer        = new CountryLayer(1, "Province", "省份", "大明王朝共分两京一十三省");
        public static CountryLayer PrefectureLayer      = new CountryLayer(2, "Prefecture", "府", "");
        public static CountryLayer SubPrefectureLayer   = new CountryLayer(3, "SubPrefecture", "州县", "");
    }

    // Storage all administrative divisions data
    public class CountrySO : SerializedScriptableObject
    {
        public int mapWidth;

        public int mapHeight;

        [LabelText("区域层级数据")]
        public List<CountryLayer> CountryLayerList;                         // All CountryLayer, Max layer num : 8


        [LabelText("所有区域数据")]
        public List<LayerCountryData> LayerCountryDataList = new List<LayerCountryData>();

        public CountryData RootCountryData;

        public int TotalCountryCount { 
            get { 
                int count = 0;
                foreach(var layer in LayerCountryDataList)
                {
                    count += layer.Count;
                }
                return count;
            }
        }


        [NonSerialized]
        public Dictionary<int, LayerCountryData> LayerCountryDataDict = new Dictionary<int, LayerCountryData>();

        Dictionary<int, Dictionary<string, CountryData>> LayerCountryNameToDataDict = new Dictionary<int, Dictionary<string, CountryData>>();

        public bool isUpdated => LayerCountryDataDict.Count > 0;            // It mean you have call UpdateCountrySO


        public List<uint4> GridCountryIndiceList = new List<uint4>();       // Storage all country data in this grid, hexmap size

        public int GridNum => GridCountryIndiceList.Count;

        [LabelText("需要初始化数据")]
        [Tooltip("如果设置为 true, 会清空 CountrySO 的数据重新初始化")]
        public bool needInit = true;

        public void InitCountrySO(int mapWidth, int mapHeight)
        {
            if (this.mapWidth != mapWidth || this.mapHeight != mapHeight)
            {
                InitGridCountryIndice();
            }
            if (GridCountryIndiceList.IsNullOrEmpty())
            {
                Debug.LogError($"Warning, grid country indice is null or empty, so re create it");
                InitGridCountryIndice();
            }
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;

            InitCountryLayerAndData();
            UpdateCountrySO();
        }

        private void InitGridCountryIndice()
        {
            uint initCountryIdx = BaseCountryDatas.NotValidCountryIndex;
            GridCountryIndiceList = new List<uint4>(mapWidth * mapHeight);
            for (int i = 0; i < mapWidth * mapHeight; i++)
            {
                GridCountryIndiceList.Add(new uint4(initCountryIdx, initCountryIdx, initCountryIdx, initCountryIdx));
            }
        }

        private void InitCountryLayerAndData()
        {
            if (needInit)
            {
                CountryLayerList = new List<CountryLayer>()
                {
                    BaseCountryDatas.RegionLayer,
                    BaseCountryDatas.ProvinceLayer,
                    BaseCountryDatas.PrefectureLayer,
                    BaseCountryDatas.SubPrefectureLayer,
                };

                LayerCountryDataList.Clear();
                for (int i = 0; i < CountryLayerList.Count; i++)
                {
                    LayerCountryDataList.Add(new LayerCountryData(i));
                }

                InitRootCountry();
                needInit = false;
            }
        }

        private void InitRootCountry()
        {
            // Build root country data
            LayerCountryData rootLayer = new LayerCountryData(BaseCountryDatas.RootLayerIndex);
            LayerCountryDataList.Add(rootLayer);
            RootCountryData = CountryData.GetRootCountryData();
            rootLayer.AddCountryData(RootCountryData);
        }

        public void UpdateCountrySO()
        {
            LayerCountryDataDict.Clear();
            for (int i = 0; i < LayerCountryDataList.Count; i++)
            {
                LayerCountryData layerCountryData = LayerCountryDataList[i];
                layerCountryData.UpdateCountryData();
                LayerCountryDataDict.Add(layerCountryData.LayerLevel, layerCountryData);
            }

            // Totally re build
            LayerCountryNameToDataDict.Clear();
            List<CountryData> validCountryData;
            foreach (var pair in LayerCountryDataDict)
            {
                Dictionary<string, CountryData> countryNameToData = new Dictionary<string, CountryData>();
                LayerCountryNameToDataDict.Add(pair.Key, countryNameToData);

                LayerCountryData layerCountryData = pair.Value;
                validCountryData = layerCountryData.GetValidCountryData();
                for (int j = 0; j < validCountryData.Count; j++)
                {
                    CountryData countryData = validCountryData[j];
                    countryNameToData.Add(countryData.CountryName, countryData);
                }
            }
        }

        private bool AddLayerCountryDict(int layerNum, CountryData countryData)
        {
            if (!LayerCountryNameToDataDict.ContainsKey(layerNum))
            {
                return false;
            }
            var countryNameToData = LayerCountryNameToDataDict[layerNum];
            if (countryNameToData.ContainsKey(countryData.CountryName))
            {
                countryNameToData[countryData.CountryName] = countryData;
                //Debug.LogError($"warning : {countryData.CountryName} already exist in cur layer country data");
            }
            else
            {
                countryNameToData.Add(countryData.CountryName, countryData);
            }
            return true;
        }

        private void RemoveLayerCountryDict(CountryData countryData)
        {
            if (!LayerCountryNameToDataDict.ContainsKey(countryData.Layer))
            {
                return;
            }

            var countryNameToData = LayerCountryNameToDataDict[countryData.Layer];
            if (countryNameToData.ContainsKey(countryData.CountryName))
            {
                countryNameToData.Remove(countryData.CountryName);
            }
        }

        public void AddCountryData(int parentLayerLevel, string parentCountryName, CountryData countryData)
        {
            if (parentLayerLevel < 0)
            {
                // Add to Root layer
                parentLayerLevel = BaseCountryDatas.RootLayerIndex;
            }
            else if (parentLayerLevel >= BaseCountryDatas.MaxLayerNum)
            {
                parentLayerLevel = BaseCountryDatas.MaxLayerNum - 1;
            }

            if (!countryData.IsValid)
            {
                Debug.LogError($"not a valid countryData : {countryData.CountryName}");
                return;
            }

            bool canAddToCache = AddLayerCountryDict(parentLayerLevel + 1, countryData);
            if (!canAddToCache)
            {
                Debug.LogError($"can not add to layerCountryDict : {countryData.CountryName}");
                return;
            }

            Debug.Log($"saving : parent layer level is : {parentLayerLevel}, parent country name : {parentCountryName}, country data name : {countryData.CountryName}");
            LayerCountryData layerCountryData = LayerCountryDataDict[parentLayerLevel + 1];
            CountryData parentCountry = GetCountryDataByName(parentCountryName);
            CountryData newChildCountry = layerCountryData.AddCountryData(countryData);
            parentCountry.AddAsChild(newChildCountry);

            Debug.Log($"save successfully!");
        }

        public void MoveChildCountryData(CountryData newParent, CountryData oldParent)
        {
            ushort childLayer = (ushort)(oldParent.Layer + 1);
            foreach (var childIndex in oldParent.ChildCountry)
            {
                CountryData childCountry = GetCountryDataByIndex(childLayer, childIndex);
                newParent.AddAsChild(childCountry);
            }
            oldParent.ChildCountry.Clear();
        }

        public void RemoveCountryData(CountryData countryData, bool moveChildToBrother)
        {
            RemoveLayerCountryDict(countryData);
            CountryData parentCountry = GetParentCountryData(countryData);
            countryData.RemoveFromParent(parentCountry);

            // If no child or is in min CountryLayer
            if (countryData.IsMinCountry)
            {
                return;
            }

            if (countryData.ChildCountry.Count > 0)
            {
                if (moveChildToBrother)
                {
                    // Find a brother node to move all child, if no brother, delete alls
                    ushort brotherCountryIdx = parentCountry.FindOtherChildCountry(countryData.IndexInLayer);
                    if (brotherCountryIdx != countryData.IndexInLayer)
                    {
                        CountryData brotherCountry = GetCountryDataByIndex(countryData.Layer, brotherCountryIdx);
                        MoveChildCountryData(brotherCountry, countryData);
                    }
                    else
                    {
                        RemoveChildCountryData(countryData);
                    }
                }
                else
                {
                    // Remove all child countryData
                    RemoveChildCountryData(countryData);
                }
            }
            
            // Remove countryData self
            LayerCountryData layerCountryData = LayerCountryDataDict[countryData.Layer];
            layerCountryData.RemoveCountryData(countryData);
        }

        public void RemoveChildCountryData(CountryData countryData)
        {
            List<CountryData> childCountrys = GetChildCountryData(countryData);
            foreach (var child in childCountrys)
            {
                RemoveCountryData(child, false);
            }
        }

        // NEED MORE TEST : get and set, paint more!
        public List<CountryData> GetGridCountry(Vector2Int idx)
        {
            // Deserialize hex country index struct
            int index = idx.y * mapWidth + idx.x;
            uint4 countryDataIndex = GridCountryIndiceList[index];  // uint4 * 500 * 500 = 4mb, 不算很大

            uint regionIdx = countryDataIndex.x;
            uint provinceIdx = countryDataIndex.y;
            uint prefectureIdx = countryDataIndex.z;
            uint subPrefectureIdx = countryDataIndex.w;

            List<CountryData> countryDatas = new List<CountryData>(CountryLayerList.Count)
            {
                GetCountryDataByIndex(0, (int)regionIdx),
                GetCountryDataByIndex(1, (int)provinceIdx),
                GetCountryDataByIndex(2, (int)prefectureIdx),
                GetCountryDataByIndex(3, (int)subPrefectureIdx)
            };
            return countryDatas;
        }

        public bool IsValidIndice(Vector2Int idx)
        {
            int index = idx.y * mapWidth + idx.x;
            return index >= 0 && index < GridCountryIndiceList.Count;
        }

        public void SetGridCountry(Vector2Int idx, CountryData countryData)
        {
            if (countryData is not null && !countryData.IsValid)
            {
                Debug.LogError("you are setting a not valid country data");
                return;
            }
            int index = idx.y * mapWidth + idx.x;
            uint4 originIdx = GridCountryIndiceList[index];
            List<uint> res = new List<uint>() { originIdx.x, originIdx.y, originIdx.z, originIdx.w };

            // Serialize hex country index struct   // Set parent countryData
            int setTargetLayer = countryData.Layer;
            CountryData curData = countryData;
            while (setTargetLayer >= 0)
            {
                res[setTargetLayer] = curData.IndexInLayer;
                curData = GetParentCountryData(curData);
                setTargetLayer--;
            }

            GridCountryIndiceList[index] = new uint4(res[0], res[1], res[2], res[3]);
        }


        #region get / set

        public bool CheckCountryLayerIndex(int layerLevel)
        {
            return (layerLevel >= 0 || layerLevel <= CountryLayerList.Count - 1) 
                 || layerLevel == BaseCountryDatas.RootLayerIndex;
        }

        public bool CheckCountryLayerName(string layerName)
        {
            foreach (var layer in CountryLayerList)
            {
                if (layer.LayerName == layerName)
                {
                    return true;
                }
            }
            return false;
        }

        public CountryLayer GetCountryLayerByIndex(int index)
        {
            if (index < 0 || index > CountryLayerList.Count - 1)
            {
                return CountryLayerList.Last();     // Root layer is in the last
            }
            return CountryLayerList[index];
        }

        public List<CountryLayer> GetAllCountryLayer()
        {
            return CountryLayerList;
        }

        public List<CountryData> GetCountryDataByLayer(int LayerLevel)
        {
            return LayerCountryDataDict[LayerLevel].GetValidCountryData();
        }

        int CachedLayer = 0;

        public CountryData GetCountryDataByName(string countryName)
        {
            CountryData countryData = GetCountryDataByName(CachedLayer, countryName);
            if(countryData == null)
            {
                foreach (var layerCountryPair in LayerCountryNameToDataDict)
                {
                    int curLayer = layerCountryPair.Key;
                    countryData = GetCountryDataByName(curLayer, countryName);
                    if(countryData != null)
                    {
                        CachedLayer = curLayer;
                        break;
                    }
                }
            }
            return countryData;
        }

        public CountryData GetCountryDataByName(int layer, string countryName)
        {
            // If edited name in CountryEditor, can not get CountryData in this way
            if (LayerCountryNameToDataDict == null)
            {
                UpdateCountrySO();
            }
            if (!LayerCountryNameToDataDict.ContainsKey(layer))
            {
                return null;
            }
            var countryNameToData = LayerCountryNameToDataDict[layer];
            countryNameToData.TryGetValue(countryName, out var countryData);
            return countryData;
        }

        public CountryData GetCountryDataByIndex(int LayerLevel, int IndexInLayer)
        {
            LayerCountryData layerCountryData = LayerCountryDataDict[LayerLevel];
            if(layerCountryData == null || !layerCountryData.CheckIsValidIndex(IndexInLayer))
            {
                return null;
            }
            return layerCountryData[IndexInLayer];
        }

        public CountryData GetParentCountryData(CountryData countryData)
        {
            int parentLayer = countryData.Layer - 1;
            ushort parentIndex = countryData.ParentCountry;
            return GetCountryDataByIndex(parentLayer, parentIndex);
        }

        public List<CountryData> GetChildCountryData(int LayerLevel, int IndexInLayer)
        {
            CountryData countryData = GetCountryDataByIndex(LayerLevel, IndexInLayer);
            return GetChildCountryData(countryData);
        }

        public List<CountryData> GetChildCountryData(CountryData countryData)
        {
            if (countryData == null)
            {
                return new List<CountryData>();
            }
            int childLayer = countryData.Layer + 1;
            if (childLayer > CountryLayerList.Count - 1)
            {
                //throw new Exception($"child layer is not valid : {childLayer}, cur countryData : {countryData.CountryName}");
                return new List<CountryData>();
            }
            List<ushort> childCountryIdxs = countryData.ChildCountry;
            List<CountryData> childCountries = new List<CountryData>(childCountryIdxs.Count);
            for (int i = 0; i < childCountryIdxs.Count; i++)
            {
                CountryData child = GetCountryDataByIndex(childLayer, childCountryIdxs[i]);
                if (child is not null)
                {
                    childCountries.Add(child);
                }
            }
            return childCountries;
        }

        public bool IsValidLayer(int layer)
        {
            return layer >= 0 && layer <= BaseCountryDatas.MaxLayerNum;
        }

        #endregion

        public void DebugAllLayerCountryData()
        {
            foreach (var pair in LayerCountryDataDict)
            {
                int layerLevel = pair.Key;
                Debug.Log($"----Now Debug Layer {layerLevel}----");
                pair.Value.LogAllCountryData();
            }
        }

        #region import and export CSV 

        public void SaveCSV(string saveDir)
        {
            if (!isUpdated)
            {
                UpdateCountrySO();
            }
            LayerCountryData firstLayerData = LayerCountryDataList.Find(data => data.LayerLevel == 0);

            int totalCount = 0;
            for (int i = 0; i < LayerCountryDataList.Count; i++)
            {
                totalCount += LayerCountryDataList[i].Count;
            }

            // Record save num
            int regionNum = 0, provinceNum = 0, prefectureNum = 0, subPrefecture = 0;

            // Enqueue country data in first layer
            Queue<CountryData> dataQueue = new Queue<CountryData>(8);
            foreach (var countryData in firstLayerData.CountryDataList)
            {
                List<CountryData> countryDatas = new List<CountryData>(totalCount / 4) { countryData };

                // Add all child country data to list
                dataQueue.Enqueue(countryData);
                while (dataQueue.Count > 0)
                {
                    CountryData cur = dataQueue.Dequeue();
                    int nextLayer = cur.Layer + 1;
                    for (int i = 0; i < cur.ChildCountry.Count; i++)
                    {
                        CountryData child = GetCountryDataByIndex(nextLayer, cur.ChildCountry[i]);
                        dataQueue.Enqueue(child);
                        countryDatas.Add(child);

                        // Record layer country data num
                        switch(child.Layer)
                        {
                            case 0:
                                regionNum++; break;
                            case 1:
                                provinceNum++; break;
                            case 2:
                                prefectureNum++; break;
                            case 3:
                                subPrefecture++; break;
                        }
                    }
                }
                dataQueue.Clear();

                // Start write to CSV file
                string filePath = saveDir + $"/DaMing_Country_{countryData.CountryName}.csv";
                CSVUtil.SaveCsv(countryDatas, filePath);
            }

            Debug.Log($"save csv data, region : {firstLayerData.Count}, province : {provinceNum}, prefectute : {prefectureNum}, subPrefecture : {subPrefecture}");
        }

        public void LoadCSV(string loadDir, bool clearWhenLoad)
        {
            if (clearWhenLoad)
            {
                needInit = true;
                InitCountryLayerAndData();
            }
            if (!isUpdated)
            {
                UpdateCountrySO();
            }

            // Read all csv files
            List<string> fileNames = AssetsUtility.GetFileNames(loadDir, ".csv");
            List<List<CountryData>> layerCountryDatas = new List<List<CountryData>>();
            for (int i = 0; i < BaseCountryDatas.MaxLayerNum + 1; i++)
            {
                layerCountryDatas.Add(new List<CountryData>(12));
            }

            foreach (var loadPath in fileNames)
            {
                List<CountryData> datas = new List<CountryData>();
                CSVUtil.LoadCsv(datas, loadPath);

                int[] layerCountryNums = new int[BaseCountryDatas.MaxLayerNum + 1];
                foreach (var countryData in datas)
                {
                    layerCountryNums[countryData.Layer]++;
                }

                foreach (var data in datas)
                {
                    layerCountryDatas[data.Layer].Add(data);
                }
            }

            // Sort layer CountryDatas by IndexInLayer
            for (int i = 0; i < BaseCountryDatas.MaxLayerNum + 1; i++)
            {
                layerCountryDatas[i] = layerCountryDatas[i].OrderBy(data => data.IndexInLayer).ToList();
            }

            for (int i = 0; i < BaseCountryDatas.MaxLayerNum + 1; i++)
            {
                foreach (var countryData in layerCountryDatas[i])
                {
                    // Layer is Sorted excluded root layer (-1)
                    // Do not call CountrySO's AddCountryData
                    LayerCountryDataList[i].AddCountryData(countryData);
                }
            }

            LayerCountryData firstLayer = LayerCountryDataList.First();
            for(int i = 0; i < firstLayer.Count; i++)
            {
                RootCountryData.AddAsChild(firstLayer[i]);
            }

            UpdateCountrySO();
            Debug.Log($"load csv data, region : {layerCountryDatas[0].Count}, province : {layerCountryDatas[1].Count}, prefectute : {layerCountryDatas[2].Count}, subPrefecture : {layerCountryDatas[3].Count}");
        }

        #endregion

    }

    // All area data of a grid
    // Eg : Jiangxi -> NanChang -> xxx area -> etc
    [Serializable]
    public class LayerCountryData
    {
        [LabelText("layer索引"), ReadOnly]
        public int LayerLevel;

        [LabelText("展示空数据索引表")]
        public bool ShowFreeIndice = false;

        [LabelText("layer下的区域数据")]
        public List<CountryData> CountryDataList = new List<CountryData>();

        public int Count => CountryDataList.Count;

        [LabelText("空数据索引表"), ShowIf("ShowFreeIndice")]
        public List<int> FreeIndiceList = new List<int>();                 // Storage free CountryData

        public CountryData this[int index]
        {
            get 
            {
                return CountryDataList[index]; 
            }
            set 
            { 
                CountryDataList[index] = value; 
            }
        }

        public LayerCountryData(int layerLevel) { LayerLevel = layerLevel; }

        public void UpdateCountryData()
        {
            // Update free indice list
            HashSet<int> tempFreeIndiceSet = new HashSet<int>(FreeIndiceList);
            for(int i = 0; i < CountryDataList.Count; i++)
            {
                CountryData countryData = CountryDataList[i];
                if (!countryData.IsValid && !tempFreeIndiceSet.Contains(countryData.IndexInLayer))
                {
                    tempFreeIndiceSet.Add(i);
                }
            }

            // If there are too many no valid node, try remove them
            for(int i = CountryDataList.Count - 1; i >= 0; i--)
            {
                CountryData countryData = CountryDataList[i];
                if (!countryData.IsValid)
                {
                    CountryDataList.RemoveAt(i);
                    if (tempFreeIndiceSet.Contains(i))
                    {
                        tempFreeIndiceSet.Remove(i);
                    }
                }
                else
                {
                    break;
                }
            }

            // Sorted country data by indexInLayer
            CountryDataList = CountryDataList.OrderBy(data => data.IndexInLayer).ToList();

            FreeIndiceList = new List<int>(tempFreeIndiceSet);
        }

        public CountryData AddCountryData(CountryData countryData)
        {
            // Find a valid free indice
            int freeDataIdx = -1;
            while (!CheckIsValidFreeNode(freeDataIdx) && FreeIndiceList.Count > 0)
            {
                int lastCount = FreeIndiceList.Count - 1;
                freeDataIdx = FreeIndiceList[lastCount];
                FreeIndiceList.RemoveAt(lastCount);
                if (!CheckIsValidFreeNode(freeDataIdx))
                {
                    break;
                }
            }

            if (!CheckIsValidFreeNode(freeDataIdx) && CheckIsValidIndex(freeDataIdx))
            {
                CountryData tailerData = CountryDataList[freeDataIdx];
                countryData.IndexInLayer = tailerData.IndexInLayer;
                tailerData.CopyCountryData(countryData);
                return tailerData;
            }
            else
            {
                // No free data to use, add a new countryData
                int indexInLayer = CountryDataList.Count;
                countryData.IndexInLayer = (ushort)indexInLayer;
                CountryDataList.Add(countryData);
                return countryData;
            }
        }

        public void RemoveCountryData(CountryData countryData)
        {
            RemoveCountryData(countryData.IndexInLayer);
        }

        public void RemoveCountryData(int index)
        {
            if (!CheckIsValidIndex(index))
            {
                Debug.LogError($"RemoveCountryData call, not valid index : {index}");
                return;
            }
            // Remove will empty the countryData, but will not delete it
            CountryData target = CountryDataList[index];
            target.EmptyCountryData();
            FreeIndiceList.Add(target.IndexInLayer);
        }

        public bool CheckIsValidFreeNode(int index)
        {
            if (index < 0 || index >= CountryDataList.Count)
            {
                return false;
            }
            CountryData data = CountryDataList[index];
            return data.IsValid;
        }

        public bool CheckIsValidIndex(int index)
        {
            return (index >= 0 && index <= CountryDataList.Count - 1); 
        }
    
        public List<CountryData> GetValidCountryData()
        {
            List<CountryData> validCountryDatas = new List<CountryData>(CountryDataList.Count);
            for(int i = 0; i < CountryDataList.Count; i++)
            {
                if (CountryDataList[i].IsValid)
                {
                    validCountryDatas.Add(CountryDataList[i]);
                }
            }
            return validCountryDatas;
        }

        public void LogAllCountryData()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"log all, countryData num : {CountryDataList.Count}, ");
            foreach (var countryData in CountryDataList)
            {
                stringBuilder.Append(countryData.CountryName); 
                stringBuilder.Append(", ");
                stringBuilder.Append(countryData.IsValid);
                stringBuilder.Append("; ");
            }
            Debug.Log(stringBuilder.ToString());
        }

    }

}
