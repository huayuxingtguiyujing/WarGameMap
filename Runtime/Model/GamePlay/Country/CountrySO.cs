using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LZ.WarGameMap.Runtime.Model
{

    public class BaseCountryDatas
    {
        public static int MaxCountryLayer               = 3;

        public static int NotValidLayerIndex            = -99;
        public static string NotValidLayerName          = "未选中层级";

        public static int RootCountryLayerIndex         = -1;
        public static string RootCountryLayerName       = "根层级";

        public static string NotValidCountryName        = "无";
        public static Color NotValidCountryColor        = Color.black;

        public static CountryLayer ProvinceLayer        = new CountryLayer(0, "Province", "省份", "大明王朝共分两京一十三省");
        public static CountryLayer PrefectureLayer      = new CountryLayer(1, "Prefecture", "府", "");
        public static CountryLayer SubPrefectureLayer   = new CountryLayer(2, "SubPrefecture", "州", "");
        public static CountryLayer CountryLayer         = new CountryLayer(3, "Country", "县", "");
    }

    // Storage all administrative divisions data
    public class CountrySO : SerializedScriptableObject
    {
        public int mapWidth;

        public int mapHeight;

        public List<CountryLayer> CountryLayerList;                         // All CountryLayer, Max layer num : 8

        [NonSerialized]
        //[DictionaryDrawerSettings(KeyLabel = "LayerIndex", ValueLabel = "LayerCountryData")]
        // LayerLevel - LayerCountryData
        public Dictionary<int, LayerCountryData> LayerCountryDataDict = new Dictionary<int, LayerCountryData>();

        public List<LayerCountryData> LayerCountryDataList = new List<LayerCountryData>();


        public CountryData RootCountryData;

        Dictionary<string, CountryData> CountryNameToDataDict = new Dictionary<string, CountryData>();


        public List<UInt32> GridCountryIndiceList;                          // Storage all country data in this grid, hexmap size

        //// TODO : 把这个做完...
        //public int LayerCounter = 0;                                        // Counter
        //public List<int> LayerCountryCounter = new List<int>();

        public bool isInit = false;

        public void InitCountrySO(int mapWidth, int mapHeight)
        {
            if (this.mapWidth != mapWidth || this.mapHeight != mapHeight)
            {
                GridCountryIndiceList = new List<uint>(mapWidth * mapHeight);
                for(int i = 0; i < GridCountryIndiceList.Count; i++)
                {
                    GridCountryIndiceList.Add(0);
                }
            }
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;

            if (!isInit)
            {
                CountryLayerList = new List<CountryLayer>() 
                {
                    BaseCountryDatas.ProvinceLayer, 
                    BaseCountryDatas.PrefectureLayer, 
                    BaseCountryDatas.SubPrefectureLayer,
                    BaseCountryDatas.CountryLayer,
                };

                // Init counter
                //LayerCounter = CountryLayerList.Count;
                //for (int i = 0; i < LayerCounter; i++)
                //{
                //    LayerCountryCounter.Add(0);
                //}

                for (int i = 0; i < CountryLayerList.Count; i++)
                {
                    LayerCountryDataList.Add(new LayerCountryData(i));
                }

                LayerCountryData rootLayer = new LayerCountryData(BaseCountryDatas.RootCountryLayerIndex);
                RootCountryData = CountryData.GetRootCountryData();
                rootLayer.AddCountryData(RootCountryData);
                LayerCountryDataList.Add(rootLayer);
                isInit = true;
            }
            UpdateCountrySO();
        }

        public void UpdateCountrySO()
        {
            LayerCountryDataDict.Clear();
            LayerCountryDataDict = new Dictionary<int, LayerCountryData>();
            for (int i = 0; i < LayerCountryDataList.Count; i++)
            {
                LayerCountryDataDict.Add(LayerCountryDataList[i].LayerLavel, LayerCountryDataList[i]);
            }

            CountryNameToDataDict.Clear();
            List<CountryData> validCountryData;
            foreach (var pair in LayerCountryDataDict)
            {
                LayerCountryData layerCountryData = pair.Value;
                validCountryData = layerCountryData.GetValidCountryData();
                for (int j = 0; j < validCountryData.Count; j++)
                {
                    CountryData countryData = validCountryData[j];
                    CountryNameToDataDict.Add(countryData.CountryName, countryData);
                }
            }
        }

        public void AddCountryData(int layerLevel, string parentCountryName, CountryData countryData)
        {
            if (layerLevel < 0)
            {
                // Add to Root layer
                layerLevel = BaseCountryDatas.RootCountryLayerIndex;
            }
            else if (layerLevel >= BaseCountryDatas.MaxCountryLayer)
            {
                layerLevel = BaseCountryDatas.MaxCountryLayer - 1;
            }

            if (!countryData.IsValid)
            {
                Debug.Log($"not a valid countryData : {countryData.CountryName}");
                return;
            }
            
            Debug.Log($"saving : parent layer level is : {layerLevel}, parent country name : {parentCountryName}, country data name : {countryData.CountryName}");
            LayerCountryData layerCountryData = LayerCountryDataDict[layerLevel + 1];
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

        public void RemoveCountryData(int layerLevel, CountryData countryData, bool moveChildToBrother)
        {
            CountryData parentCountry = GetParentCountryData(countryData);
            countryData.RemoveFromParent(parentCountry);

            // If no child or is in min CountryLayer
            if (countryData.IsMinCountry || countryData.ChildCountry.Count <= 0)
            {
                return;
            }

            if (moveChildToBrother)
            {
                // Find a brother node to move all child, if no brother, delete alls
                ushort brotherCountryIdx = parentCountry.FindOtherChildCountry(countryData.IndexInLayer);
                if (brotherCountryIdx != countryData.IndexInLayer)
                {
                    CountryData brotherCountry = GetCountryDataByIndex(layerLevel, brotherCountryIdx);
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

            // Remove countryData self
            LayerCountryData layerCountryData = LayerCountryDataDict[layerLevel];
            layerCountryData.RemoveCountryData(countryData);
        }

        public void RemoveChildCountryData(CountryData countryData)
        {
            List<CountryData> childCountrys = GetChildCountryData(countryData);
            foreach (var child in childCountrys)
            {
                RemoveCountryData(child.Layer, child, false);
            }
        }


        //// TODO : 似乎 不需要了
        //public void SaveCountryDatas(int CurEditingLayerIndex, string parentCountryName, List<CountryData> childCountryData)
        //{
        //    //List<CountryData> childCountryData_true = ;
        //    // Can not use  to save it
        //}
        //// TODO : 似乎 不需要了
        //public void SaveCountrySO()
        //{
        //    // TODO : 完成它
        //}


        // TODO : 完成它！
        public List<CountryData> GetGridCountry(Vector2Int idx)
        {
            // Deserialize UInt16 struct
            int index = idx.x * mapWidth + idx.y;
            UInt32 countryDataIndex = GridCountryIndiceList[index];

            // TODO : 完成这个！
            //foreach (var item in countryDatas)
            //{
            //  
            //}
            List<CountryData> countryDatas = new List<CountryData>(CountryLayerList.Count);

            byte part1 = (byte)((countryDataIndex >> 12) & 0xF); // 高4位
            byte part2 = (byte)((countryDataIndex >> 8) & 0xF); // 次高4位
            byte part3 = (byte)((countryDataIndex >> 4) & 0xF); // 次低4位
            byte part4 = (byte)(countryDataIndex & 0xF); // 低4位
            return null;
        }

        public void SetGridCountry(Vector2Int idx, CountryData CountryData)
        {
            // Serialize UInt16 struct
            // TODO : 迭代地同时设置自己父层级的 CountryData
        }


        #region get / set

        public bool CheckCountryLayerIndex(int layerLevel)
        {
            return (layerLevel >= 0 || layerLevel <= CountryLayerList.Count - 1) 
                 || layerLevel == BaseCountryDatas.RootCountryLayerIndex;
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
                return null;
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

        public CountryData GetCountryDataByName(string countryName)
        {
            // If edited name in CountryEditor, can not get CountryData in this way
            if (CountryNameToDataDict.Count == 0)
            {
                UpdateCountrySO();
            }
            CountryNameToDataDict.TryGetValue(countryName, out var countryData);
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
                childCountries.Add(child);
            }
            return childCountries;
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

    }

    // All area data of a grid
    // Eg : Jiangxi -> NanChang -> xxx area -> etc
    [Serializable]
    public class LayerCountryData
    {
        [ReadOnly]
        public int LayerLavel;

        public List<CountryData> CountryDataList = new List<CountryData>();

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

        public LayerCountryData(int layerLevel) { LayerLavel = layerLevel; }

        public CountryData AddCountryData(CountryData countryData)
        {
            // Find a valid free indice
            int freeDataIdx = -1;
            while (!CheckIsValidFreeNode(freeDataIdx) && FreeIndiceList.Count > 0)
            {
                int lastCount = FreeIndiceList.Count - 1;
                freeDataIdx = FreeIndiceList[lastCount];
                FreeIndiceList.RemoveAt(lastCount);
            }

            if (CheckIsValidFreeNode(freeDataIdx))
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
