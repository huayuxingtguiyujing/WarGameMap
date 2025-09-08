using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class CountryEditor : BrushHexmapEditor
    {
        public override string EditorName => MapEditorEnum.CountryEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();
            LoadCountrySO();
            InitCountryEditData();
            Debug.Log("country editor has inited!");
        }

        protected override BrushHexmapSetting GetBrushSetting()
        {
            throw new System.NotImplementedException();
        }

        private void LoadCountrySO()
        {
            FindOrCreateSO<CountrySO>(ref countrySO, MapStoreEnum.GamePlayCountryDataPath, $"CountrySO_{hexSet.mapWidth}x{hexSet.mapHeight}.asset");
            countrySO.InitCountrySO(hexSet.mapWidth, hexSet.mapHeight);
        }

        private void InitCountryEditData()
        {
            CountryLayerFilter.Clear();
            CountryLayerFilter.Add(new CountryLayerWrapper(BaseCountryDatas.RootCountryLayerIndex, BaseCountryDatas.RootCountryLayerName, GetCountryDataByParentEvent, ShowChildCountryEvent));

            List<CountryLayer> allLayers = countrySO.GetAllCountryLayer();
            for (int i = 0; i < allLayers.Count; i++)
            {
                CountryLayerWrapper countryLayerWrapper = new CountryLayerWrapper(allLayers[i], GetCountryDataByParentEvent, ShowChildCountryEvent);
                CountryLayerFilter.Add(countryLayerWrapper);
            }

            CurEditingChildCountryData.Clear();
            // TODO : ��ʼ��ʱչʾ ���㼶��������
            ShowChildCountryEvent(BaseCountryDatas.RootCountryLayerIndex, BaseCountryDatas.RootCountryLayerName, BaseCountryDatas.NotValidCountryName);
        }

        private List<CountryData> GetCountryDataByParentEvent(int parentLayerLevel)
        {
            // Show all top layer CountryDatas if parent layer is lesser than 0
            if (parentLayerLevel < 0)
            {
                return countrySO.GetCountryDataByLayer(0);
            }
            string ParentCountryDataName = CountryLayerFilter[parentLayerLevel].CurCountryName;
            if (ParentCountryDataName == BaseCountryDatas.NotValidCountryName)
            {
                return new List<CountryData>();
            }
            CountryData curCountryData = countrySO.GetCountryDataByName(ParentCountryDataName);
            return countrySO.GetChildCountryData(curCountryData);
        }

        private void ShowChildCountryEvent(int LayerLevel, string LayerName, string CurCountryDataName)
        {
            // Set edit data
            CurEditingLayer = LayerName;
            CurEditingLayerIndex = LayerLevel;
            if (CurEditingLayerIndex == BaseCountryDatas.RootCountryLayerIndex)
            {
                CurChooseCountryName = BaseCountryDatas.RootCountryLayerName;
            }
            else
            {
                CurChooseCountryName = CurCountryDataName;
            }

            CurEditingChildCountryData.Clear();
            List<CountryData> childCountryData;
            if (LayerLevel < 0)
            {
                childCountryData = countrySO.GetCountryDataByLayer(0);
            }
            else
            {
                CountryData curCountryData = countrySO.GetCountryDataByName(CurCountryDataName);
                if (curCountryData == null)
                {
                    Debug.Log($"CountryName : \"{CurCountryDataName}\" has no CountryData");
                    return;
                }
                childCountryData = countrySO.GetChildCountryData(curCountryData);
            }

            // Deep copy, Add child countryData
            for (int i = 0; i < childCountryData.Count; i++)
            {
                CurEditingChildCountryData.Add(new CountryDataWrapper(childCountryData[i]));
            }
            Debug.Log($"show child data num : {childCountryData.Count}");
        }

        private void ChooseCountryDataEdit(string OriginCountryName)
        {
            
        }

        private void ConfirmEditEvent(CountryData countryData)
        {
            // Sync with cur wrapper
        }

        private void ChooseCountryDataPaint()
        {

        }

        private void DeleteCountryDataEvent(string OriginCountryName)
        {
            CountryData countryData = countrySO.GetCountryDataByName(OriginCountryName);
            if (countryData == null)
            {
                Debug.LogError($"can not find {OriginCountryName} country data, so can not remove it");
                return;
            }

            Action<bool> confirmDelEvent = (moveChildCountry) => 
            {
                ConfirmDeleteEvent(countryData.Layer, countryData, moveChildCountry);
            };
            CountryDeletePop.GetPopInstance().ShowBasePop(countryData, confirmDelEvent, null);
        }

        private void ConfirmDeleteEvent(int layerLevel, CountryData countryData, bool moveChildCountry)
        {
            countrySO.RemoveCountryData(layerLevel, countryData, moveChildCountry);
        }


        #region �������ݱ༭

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("��������SO")]
        public CountrySO countrySO;     // SerializedScriptableObject

        [Serializable]
        public class CountryLayerWrapper
        {

            [HorizontalGroup("CountryLayerWrapper"), LabelText("�㼶���")]
            public int LayerLevel;

            [HorizontalGroup("CountryLayerWrapper"), LabelText("�㼶����")]
            public string LayerName;

            [HorizontalGroup("CountryLayerWrapper"), LabelText("ѡ������")]
            [ValueDropdown("GetLayerCountryDatas")]
            //[OnValueChanged("OnCountryLayerFilterChanged")]
            public string CurCountryName = "��";

            Func<int, List<CountryData>> GetCountryDataByParent;

            Action<int, string, string> ShowChildCountryCall;

            public CountryLayerWrapper(CountryLayer layer, Func<int, List<CountryData>> getCountryDataByParent, Action<int, string, string> showChildCountry)
            {
                this.LayerLevel = layer.LayerLevel;
                this.LayerName = layer.LayerName;
                GetCountryDataByParent = getCountryDataByParent;
                ShowChildCountryCall = showChildCountry;
            }

            public CountryLayerWrapper(int LayerLevel, string LayerName, Func<int, List<CountryData>> getCountryDataByParent, Action<int, string, string> showChildCountry)
            {
                this.LayerLevel = LayerLevel;
                this.LayerName = LayerName;
                GetCountryDataByParent = getCountryDataByParent;
                ShowChildCountryCall = showChildCountry;
            }

            private IEnumerable<ValueDropdownItem<string>> GetLayerCountryDatas()
            {
                List<ValueDropdownItem<string>> dropDownItemList = new List<ValueDropdownItem<string>>() {
                    new ValueDropdownItem<string>(BaseCountryDatas.NotValidCountryName, BaseCountryDatas.NotValidCountryName)
                };
                if (LayerLevel < 0 || GetCountryDataByParent == null)
                {
                    return dropDownItemList;
                }

                List<CountryData> layerCountryDatas = GetCountryDataByParent(LayerLevel - 1);
                foreach (var countryData in layerCountryDatas)
                {
                    dropDownItemList.Add(new ValueDropdownItem<string>(countryData.CountryName, countryData.CountryName));
                }
                return dropDownItemList;
            }

            [HorizontalGroup("CountryLayerWrapper")]
            [Button("չʾ������")]
            private void ShowChildCountry()
            {
                if (ShowChildCountryCall == null)
                {
                    Debug.LogError("ShowChildCountryCall is null!");
                    return;
                }
                ShowChildCountryCall(LayerLevel, LayerName, CurCountryName);
            }
        }

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("������")]
        public List<CountryLayerWrapper> CountryLayerFilter = new List<CountryLayerWrapper>();

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("ѡ���������ڲ㼶"), ReadOnly]
        public string CurEditingLayer           = BaseCountryDatas.NotValidLayerName;

        private int CurEditingLayerIndex        = BaseCountryDatas.NotValidLayerIndex;

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("ѡ����������"), ReadOnly]
        public string CurChooseCountryName      = BaseCountryDatas.NotValidCountryName;

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("����Ϳˢ����������"), ReadOnly]
        public string CurPaintingCountryName    = BaseCountryDatas.NotValidCountryName;

        private CountryData paintCountryData = null;

        [Serializable]
        public class CountryDataWrapper
        {
            // Field only to show
            [HorizontalGroup("CountryData"), LabelText("ԭ����"), ReadOnly]
            public string OriginCountryName;    // Do not mod it!!!

            [HorizontalGroup("CountryData"), LabelText("����")]
            public string CountryName;

            [HorizontalGroup("CountryData"), LabelText("��ɫ")]
            public Color CountryColor;

            CountryData countryData;

            [HideInInspector]
            public bool IsValid => countryData.IsValid;

            public CountryDataWrapper()
            {
                OriginCountryName = BaseCountryDatas.NotValidCountryName;
                this.countryData = new CountryData(BaseCountryDatas.NotValidCountryName, "", BaseCountryDatas.NotValidCountryColor);
                SyncWithCountryData();
            }

            // TODO : ���캯��û����
            public CountryDataWrapper(CountryData countryData)
            {
                this.countryData = new CountryData();
                this.countryData.CopyCountryData(countryData);
                OriginCountryName = countryData.CountryName;
                SyncWithCountryData();
            }

            public void CopyCountryData(CountryData countryData)
            {
                this.countryData.CopyCountryData(countryData);
                OriginCountryName = countryData.CountryName;
                SyncWithCountryData();
            }


            [HorizontalGroup("CountryData"), Button("���б༭")]
            private void ChooseCountryDataEdit()
            {
                Action<CountryData> confirmEdit = ConfirmEditEvent;
                CountryEditWindow.GetPopInstance().ShowSubWindow(countryData, confirmEdit, null);
            }

            private void ConfirmEditEvent(CountryData countryData)
            {
                if (!IsValid)
                {
                    OriginCountryName = countryData.CountryName;
                }
                //this.countryData.CopyCountryData(countryData);
                SyncWithCountryData();
                countryData.SetValidCountryData();
                Debug.Log($"edit over, now CountryData {countryData.CountryName} is valid");
            }

            [HorizontalGroup("CountryData"), Button("ɾ������")]
            private void RemoveCountryData()
            {
                // TODO : ֻ��ʹ�øò������Ż� ����ɾ�� CountryData
                Debug.Log($"remove this countryData, origin name : {OriginCountryName}, name : {CountryName}");
            }

            private void ConfirmDeleteEvent()
            {

            }

            private void SyncWithCountryData()
            {
                CountryName = countryData.CountryName;
                CountryColor = countryData.CountryColor;
            }

            public CountryData GetCountryData()
            {
                return this.countryData;
            }
        }

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("��ǰ�༭�����������")]
        [Tooltip("���δ��� ɾ������ �� CountryData ���ᱻ�����Ƴ�")]
        public List<CountryDataWrapper> CurEditingChildCountryData = new List<CountryDataWrapper>();

        [FoldoutGroup("�������ݱ༭")]
        [Button("һ������������ɫ", ButtonSizes.Medium)]
        private void SetCountryDataColor()
        {
            // TODO : �����
        }

        [FoldoutGroup("�������ݱ༭")]
        [Button("����������༭���", ButtonSizes.Medium)]
        private void SaveChildCountrySO()
        {
            if (!countrySO.CheckCountryLayerIndex(CurEditingLayerIndex))
            {
                Debug.LogError($"not valid layer index : {CurEditingLayerIndex}");
                return;
            }
            if (CurChooseCountryName == BaseCountryDatas.NotValidCountryName && CurEditingLayerIndex != BaseCountryDatas.RootCountryLayerIndex)
            {
                Debug.LogError("you have not choosen a parent area");
                return;
            }

            // Will not sync delete result
            foreach (var wrapper in CurEditingChildCountryData)
            {
                if (!wrapper.IsValid)
                {
                    continue;
                }
                CountryData originData = countrySO.GetCountryDataByName(wrapper.OriginCountryName);
                if (originData != null)
                {
                    // If found country data, then sync with it
                    originData.CopyCountryData(wrapper.GetCountryData());
                    wrapper.CopyCountryData(originData);
                }
                else
                {
                    countrySO.AddCountryData(CurEditingLayerIndex, CurChooseCountryName, wrapper.GetCountryData());
                }
            }
            countrySO.DebugAllLayerCountryData();
            //countrySO.SaveCountryDatas(CurEditingLayerIndex, CurEditingCountry, CurEditingChildCountryData);
        }

        [FoldoutGroup("�������ݱ༭")]
        [Button("����CountrySO", ButtonSizes.Medium)]
        private void SaveCountrySO()
        {
            // TODO : �Ƿ���Ҫ���������
            //countrySO.SaveCountrySO();
        }

        #endregion


        #region ����Ϳˢ�༭

        // TODO : complete it

        #endregion


        protected override bool EnablePaintHex(Vector2Int offsetHexPos) { 
            return true; 
        }

        public override void Disable()
        {
            CountryLayerFilter.Clear();
        }

    }
}
