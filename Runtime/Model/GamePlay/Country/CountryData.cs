using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace LZ.WarGameMap.Runtime.Model
{
    [Serializable]
    public class CountryLayer
    {
        [HorizontalGroup("CountryLayer"), LabelText("�����㼶���")]
        public int LayerLevel;                          // Layer level should be unique

        [HorizontalGroup("CountryLayer"), LabelText("�����㼶����")]
        public string LayerName;

        [HorizontalGroup("CountryLayer"), LabelText("�����㼶��������")]
        public string LayerChineseName;

        [HorizontalGroup("CountryLayer"), LabelText("�����㼶����")]
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
    public class CountryData : CSVInterface
    {
        [LabelText("���������㼶")]   // ReadOnly
        public int Layer;

        [LabelText("����")]
        public string CountryName;      // CountryName is a key

        [LabelText("����")]
        public string CountryDesc;

        [LabelText("��ɫ")]
        public Color CountryColor;

        [LabelText("�Ƿ�����С����")]
        public bool IsMinCountry => (Layer >= BaseCountryDatas.MaxLayerNum);

        [LabelText("��Ч"), ReadOnly]
        public bool IsValid;

        // Storage index, and find parent in CountrySO
        // Only 1~4 bit is used, do not mod it by hand
        [Tooltip("��Ҫ�ڱ༭�����޸ģ������ֶλ��Զ�����")]
        [LabelText("���������"), ReadOnly]
        public ushort ParentCountry;

        [LabelText("���������"), ReadOnly]
        public ushort IndexInLayer;            // Indice in LayerCountryData, do not mod it when empting data

        [LabelText("���������"), ReadOnly]
        public List<ushort> ChildCountry = new List<ushort>();

        public CountryData()
        {
            IsValid = false;
        }

        public CountryData(int layer, string countryName, string countryDesc, Color countryColor)
        {
            Layer = layer;
            CountryName = countryName;
            CountryDesc = countryDesc;
            CountryColor = countryColor;
            IsValid = false;
        }

        public static CountryData GetRootCountryData()
        {
            return new CountryData(BaseCountryDatas.RootLayerIndex, 0, BaseCountryDatas.RootLayerName, "", 
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

        // TODO : ���������⣬ParentCountry ������ index Ҫ�Զ����ɣ������� copy��
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

        #region Serialize

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(CSVUtil.FormatValue(Layer));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(CountryName));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(CountryDesc));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(CountryColor));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(IsValid));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(ParentCountry));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(IndexInLayer));
            sb.Append(",");
            sb.Append(CSVUtil.FormatValue(ChildCountry));
            return sb.ToString();
        }

        public void Deserialize(string lineData)
        {
            string[] datas = lineData.Split(',');
            Layer           = (int)CSVUtil.ParseValue(datas[0], typeof(int));
            CountryName     = (string)CSVUtil.ParseValue(datas[1], typeof(string));
            CountryDesc     = (string)CSVUtil.ParseValue(datas[2], typeof(string));
            CountryColor    = (Color)CSVUtil.ParseValue(datas[3], typeof(Color));
            IsValid         = (bool)CSVUtil.ParseValue(datas[4], typeof(bool));
            ParentCountry   = (ushort)CSVUtil.ParseValue(datas[5], typeof(ushort));
            IndexInLayer    = (ushort)CSVUtil.ParseValue(datas[6], typeof(ushort));
            ChildCountry    = (List<ushort>)CSVUtil.ParseValue(datas[7], typeof(List<ushort>));
        }

        #endregion

    }
}
