using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime.Model
{
    [Serializable]
    public class CountryLayer
    {
        [HorizontalGroup("CountryLayer"), LabelText("行政层级序号")]
        public int LayerLevel;                          // Layer level should be unique

        [HorizontalGroup("CountryLayer"), LabelText("行政层级名称")]
        public string LayerName;

        [HorizontalGroup("CountryLayer"), LabelText("行政层级中文名称")]
        public string LayerChineseName;

        [HorizontalGroup("CountryLayer"), LabelText("行政层级描述")]
        public string LayerDesc;

        public CountryLayer(int layerLevel, string layerName, string layerChineseName, string layerDesc)
        {
            LayerLevel = layerLevel;
            LayerName = layerName;
            LayerChineseName = layerChineseName;
            LayerDesc = layerDesc;
        }

        public override bool Equals(object obj)
        {
            return obj is CountryLayer layer && LayerLevel == layer.LayerLevel;
        }

        public override int GetHashCode()
        {
            return LayerLevel;
        }
    }

    [Serializable]
    public class CountryData
    {
        [LabelText("所属行政层级"), ReadOnly]
        public int Layer;

        [LabelText("名称")]
        public string CountryName;      // CountryName is a key

        [LabelText("描述")]
        public string CountryDesc;

        [LabelText("颜色")]
        public Color CountryColor;

        [LabelText("是否是最小区划")]
        public bool IsMinCountry => (Layer >= BaseCountryDatas.MaxCountryLayer);

        [LabelText("有效"), ReadOnly]
        public bool IsValid;

        // Storage index, and find parent in CountrySO
        // Only 1~4 bit is used, do not mod it by hand
        [Tooltip("不要在编辑器中修改，以下字段会自动设置")]
        [LabelText("父区域序号"), ReadOnly]
        public ushort ParentCountry;

        [LabelText("本区域序号"), ReadOnly]
        public ushort IndexInLayer;            // Indice in LayerCountryData, do not mod it when empting data

        [LabelText("子区域序号"), ReadOnly]
        public List<ushort> ChildCountry = new List<ushort>();

        public CountryData()
        {
            IsValid = false;
        }

        public CountryData(string countryName, string countryDesc, Color countryColor)
        {
            CountryName = countryName;
            CountryDesc = countryDesc;
            CountryColor = countryColor;
            IsValid = false;
        }

        public static CountryData GetRootCountryData()
        {
            return new CountryData(BaseCountryDatas.RootCountryLayerIndex, 0, BaseCountryDatas.RootCountryLayerName, "", 
                BaseCountryDatas.NotValidCountryColor, false, 0);
        }

        public CountryData(int layer, int indexInLayer, string countryName, string countryDesc, Color countryColor, bool isMinCountry, ushort parentCountry)
        {
            Layer = layer;
            IndexInLayer = (ushort)indexInLayer;
            CountryName = countryName;
            CountryDesc = countryDesc;
            CountryColor = countryColor;
            ParentCountry = parentCountry;
            IsValid = true;
        }

        // TODO : 有问题现在，ParentCountry 与其他 index 要自动生成，不能用 copy的
        public void CopyCountryData(CountryData other)
        {
            Layer = other.Layer;
            IndexInLayer = other.IndexInLayer;
            CountryName = other.CountryName;
            CountryDesc = other.CountryDesc;
            CountryColor = other.CountryColor;
            ParentCountry = other.ParentCountry;
            ChildCountry = other.ChildCountry;
            IsValid = true;
        }

        public void EmptyCountryData()
        {
            Layer = -1;
            ParentCountry = 0;
            CountryName = "";
            CountryDesc = "";
            CountryColor = Color.black;
            ChildCountry.Clear();
            IsValid = false;
        }

        public void SetValidCountryData()
        {
            IsValid = true;
        }

        public ushort FindOtherChildCountry(ushort indexInLayer)
        {
            for(int i = 0; i < ChildCountry.Count; i++)
            {
                if (indexInLayer != ChildCountry[i])
                {
                    return ChildCountry[i];
                }
            }
            return indexInLayer;
        }

        public void AddAsChild(CountryData newChild)
        {
            newChild.ParentCountry = IndexInLayer;
            ChildCountry.Add(newChild.IndexInLayer);
        }

        public void RemoveFromParent(CountryData parentCountry)
        {
            for (int i = 0; i < parentCountry.ChildCountry.Count; i++)
            {
                if (ParentCountry != parentCountry.ChildCountry[i])
                {
                    parentCountry.ChildCountry.RemoveAt(i);
                    break;
                }
            }
        }

    }
}
