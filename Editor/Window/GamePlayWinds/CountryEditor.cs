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
            // TODO : 初始化时展示 根层级的子区域
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


        #region 区域数据编辑

        [FoldoutGroup("区域数据编辑")]
        [LabelText("区域数据SO")]
        public CountrySO countrySO;     // SerializedScriptableObject

        [Serializable]
        public class CountryLayerWrapper
        {

            [HorizontalGroup("CountryLayerWrapper"), LabelText("层级序号")]
            public int LayerLevel;

            [HorizontalGroup("CountryLayerWrapper"), LabelText("层级名称")]
            public string LayerName;

            [HorizontalGroup("CountryLayerWrapper"), LabelText("选中区域")]
            [ValueDropdown("GetLayerCountryDatas")]
            //[OnValueChanged("OnCountryLayerFilterChanged")]
            public string CurCountryName = "无";

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
            [Button("展示子区域")]
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

        [FoldoutGroup("区域数据编辑")]
        [LabelText("过滤器")]
        public List<CountryLayerWrapper> CountryLayerFilter = new List<CountryLayerWrapper>();

        [FoldoutGroup("区域数据编辑")]
        [LabelText("选中区域所在层级"), ReadOnly]
        public string CurEditingLayer           = BaseCountryDatas.NotValidLayerName;

        private int CurEditingLayerIndex        = BaseCountryDatas.NotValidLayerIndex;

        [FoldoutGroup("区域数据编辑")]
        [LabelText("选中区域名称"), ReadOnly]
        public string CurChooseCountryName      = BaseCountryDatas.NotValidCountryName;

        [FoldoutGroup("区域数据编辑")]
        [LabelText("正在涂刷的区域名称"), ReadOnly]
        public string CurPaintingCountryName    = BaseCountryDatas.NotValidCountryName;

        private CountryData paintCountryData = null;

        [Serializable]
        public class CountryDataWrapper
        {
            // Field only to show
            [HorizontalGroup("CountryData"), LabelText("原名称"), ReadOnly]
            public string OriginCountryName;    // Do not mod it!!!

            [HorizontalGroup("CountryData"), LabelText("名称")]
            public string CountryName;

            [HorizontalGroup("CountryData"), LabelText("颜色")]
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

            // TODO : 构造函数没搞完
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


            [HorizontalGroup("CountryData"), Button("进行编辑")]
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

            [HorizontalGroup("CountryData"), Button("删除数据")]
            private void RemoveCountryData()
            {
                // TODO : 只有使用该操作，才会 真正删除 CountryData
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

        [FoldoutGroup("区域数据编辑")]
        [LabelText("当前编辑区域的子区域")]
        [Tooltip("如果未点击 删除数据 则 CountryData 不会被真正移除")]
        public List<CountryDataWrapper> CurEditingChildCountryData = new List<CountryDataWrapper>();

        [FoldoutGroup("区域数据编辑")]
        [Button("一键设置区域颜色", ButtonSizes.Medium)]
        private void SetCountryDataColor()
        {
            // TODO : 完成它
        }

        [FoldoutGroup("区域数据编辑")]
        [Button("保存子区域编辑结果", ButtonSizes.Medium)]
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

        [FoldoutGroup("区域数据编辑")]
        [Button("保存CountrySO", ButtonSizes.Medium)]
        private void SaveCountrySO()
        {
            // TODO : 是否还需要这个方法？
            //countrySO.SaveCountrySO();
        }

        #endregion


        #region 区域涂刷编辑

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
