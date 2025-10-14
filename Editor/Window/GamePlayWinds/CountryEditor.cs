using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    using ShowChildCall = Action<int, string, string, bool>;

    public class CountryEditor : BrushHexmapEditor
    {
        public override string EditorName => MapEditorEnum.CountryEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();
            LoadCountrySO();
            InitCountryEditData();
            InitCurChooseSet();
            Debug.Log("country editor has inited!");
        }

        protected override BrushHexmapSetting GetBrushSetting()
        {
            return new BrushHexmapSetting() { texCacheNum = 4, useTexCache = true};
        }

        private void LoadCountrySO()
        {
            FindOrCreateSO<CountrySO>(ref countrySO, MapStoreEnum.GamePlayCountryDataPath, $"CountrySO_{hexSet.mapWidth}x{hexSet.mapHeight}.asset");
            countrySO.InitCountrySO(hexSet.mapWidth, hexSet.mapHeight);
        }

        private void InitCountryEditData()
        {
            CountryLayerFilter.Clear();

            // 9.21 : Now we dont need root layer as filter
            // 0 Index is root country layer
            //CountryLayerFilter.Add(new CountryLayerWrapper(BaseCountryDatas.RootLayerIndex, BaseCountryDatas.RootLayerName, GetCountryDataByParentEvent, ShowChildCountryEvent));

            // (1~lastIdx - 1) Index is other country layer
            // We do not need the min country layer
            List<CountryLayer> allLayers = countrySO.GetAllCountryLayer();
            for (int i = 0; i < allLayers.Count - 1; i++)
            {
                CountryLayerWrapper countryLayerWrapper = new CountryLayerWrapper(allLayers[i], GetCountryDataByParentEvent, ShowChildCountryEvent);
                CountryLayerFilter.Add(countryLayerWrapper);
            }

            CurEditingChildCountryData.Clear();
            // When inited, show root layer's countryData
            ShowChildCountryEvent(BaseCountryDatas.RootLayerIndex, BaseCountryDatas.RootLayerName, BaseCountryDatas.NotValidCountryName, true);
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
            //Debug.Log($"call get country data by parent! parent level : {parentLayerLevel}, got country name : {curCountryData.CountryName}");
            return countrySO.GetChildCountryData(curCountryData);
        }

        private void ShowChildCountryEvent(int LayerLevel, string LayerName, string CurCountryDataName, bool resetRearFilter)
        {
            // Set edit data
            CurEditingLayer = LayerName;
            CurEditingLayerIndex = LayerLevel;
            _CurEditingLayerIndex = LayerLevel;
            if (CurEditingLayerIndex == BaseCountryDatas.RootLayerIndex)
            {
                CurChooseCountryName = BaseCountryDatas.RootLayerName;
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
                CurEditingChildCountryData.Add(new CountryDataWrapper(childCountryData[i], DeleteCountryDataEvent, UpdateLockEvent));
            }

            // Reset rear filter's CurCountryName as "无"
            for(int i = 0; i < CountryLayerFilter.Count; i++)
            {
                if (CountryLayerFilter[i].LayerLevel > LayerLevel)
                {
                    CountryLayerFilter[i].CurCountryName = BaseCountryDatas.NotValidCountryName;
                }
            }
            //Debug.Log($"show child data num : {childCountryData.Count}");
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
                countrySO.RemoveCountryData(countryData, moveChildCountry);

                // Remove from country data wrapper
                for (int i = CurEditingChildCountryData.Count - 1; i >= 0; i--)
                {
                    if (CurEditingChildCountryData[i].OriginCountryName == OriginCountryName)
                    {
                        CurEditingChildCountryData.RemoveAt(i);
                        break;
                    }
                }
            };
            CountryDeletePop instance = CountryDeletePop.GetPopInstance();
            instance.ShowBasePop(countryData, confirmDelEvent, null);
        }

        private void InitCurChooseSet()
        {
            CurCountryName = BaseCountryDatas.NotValidCountryName;
            CurPaintColor = BaseCountryDatas.NotValidCountryColor;
            CurPaintCountryData = null;
        }

        // TODO : 需要性能优化...现在有点慢了
        protected override void PostBuildHexGridMap() 
        {
            int mapWidth = hexSet.mapWidth;
            int mapHeight = hexSet.mapHeight;
            BrushHexmapSetting setting = GetBrushSetting();
            int pageNum = setting.texCacheNum;
            List<Color> hexColors = new List<Color>(mapHeight * mapWidth * pageNum);

            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    Vector2Int idx = new Vector2Int(i, j);
                    List<CountryData> countryDatas = countrySO.GetGridCountry(idx);
                    for(int layer = 0; layer < countryDatas.Count; layer++)
                    {
                        if (countryDatas[layer] != null)
                        {
                            hexColors.Add(countryDatas[layer].CountryColor);
                        }
                        else
                        {
                            hexColors.Add(BaseCountryDatas.NotValidCountryColor);
                        }
                    }
                }
            }

            // Init every layer's texture cache data
            for (int offset = 0; offset < pageNum; offset++)
            {
                List<Color> layerColors = new List<Color>(mapHeight * mapWidth);
                for(int i = 0; i < mapHeight * mapWidth; i++)
                {
                    layerColors.Add(hexColors[i * pageNum + offset]);
                }
                hexmapDataTexManager.InitTexCache(offset, layerColors);
            }

            hexmapDataTexManager.SwitchToTexCache(0);
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
            [OnValueChanged("OnCountryLayerFilterChanged")]
            public string CurCountryName = BaseCountryDatas.NotValidCountryName;

            private string LastCountryName = BaseCountryDatas.NotValidCountryName;

            Func<int, List<CountryData>> GetCountryDataByParent;

            ShowChildCall ShowChildCountryCall;

            public CountryLayerWrapper(CountryLayer layer, Func<int, List<CountryData>> getCountryDataByParent, ShowChildCall showChildCountry)
            {
                this.LayerLevel = layer.LayerLevel;
                this.LayerName = layer.LayerName;
                GetCountryDataByParent = getCountryDataByParent;
                ShowChildCountryCall = showChildCountry;
            }

            public CountryLayerWrapper(int LayerLevel, string LayerName, Func<int, List<CountryData>> getCountryDataByParent, ShowChildCall showChildCountry)
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
                    //Debug.Log($"layer level : {LayerLevel}, get parent call : {GetCountryDataByParent == null}");
                    return dropDownItemList;
                }

                List<CountryData> layerCountryDatas = GetCountryDataByParent(LayerLevel - 1);
                foreach (var countryData in layerCountryDatas)
                {
                    dropDownItemList.Add(new ValueDropdownItem<string>(countryData.CountryName, countryData.CountryName));
                }
                //Debug.Log($"get all country data by choosen parent, num : {layerCountryDatas.Count}");
                return dropDownItemList;
            }

            //[HorizontalGroup("CountryLayerWrapper")]
            //[Button("展示子区域")]
            public void OnCountryLayerFilterChanged()
            {
                if (ShowChildCountryCall == null)
                {
                    Debug.LogError("ShowChildCountryCall is null! should call InitEditor firstlt");
                    return;
                }
                ShowChildCountryCall(LayerLevel, LayerName, CurCountryName, LastCountryName != CurCountryName);
                LastCountryName = CurCountryName;
            }

        }

        [FoldoutGroup("区域数据编辑")]
        [LabelText("过滤器")]
        public List<CountryLayerWrapper> CountryLayerFilter = new List<CountryLayerWrapper>();

        [FoldoutGroup("区域数据编辑")]
        [LabelText("选中区域所在层级"), ReadOnly]
        public string CurEditingLayer           = BaseCountryDatas.NotValidLayerName;
        
        private int CurEditingLayerIndex        = BaseCountryDatas.NotValidLayerIndex;
        static int _CurEditingLayerIndex;

        [FoldoutGroup("区域数据编辑")]
        [LabelText("选中区域名称"), ReadOnly]
        public string CurChooseCountryName      = BaseCountryDatas.NotValidCountryName;

        HashSet<string> lockEditSet = new HashSet<string>();        // Cur locking country names

        [Serializable]
        public class CountryDataWrapper
        {
            [HorizontalGroup("CountryData"), LabelText("锁定")]
            [OnValueChanged("UpdateLockEdit")]
            public bool IsLock;

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


            Action<string> DeleteCountryData;
            Action UpdateLockEvent;

            public CountryDataWrapper()
            {
                OriginCountryName = BaseCountryDatas.NotValidCountryName;
                this.countryData = new CountryData(_CurEditingLayerIndex + 1, BaseCountryDatas.NotValidCountryName, "", BaseCountryDatas.NotValidCountryColor);
                SyncWithCountryData();
            }

            public CountryDataWrapper(CountryData countryData, Action<string> DeleteCountryData, Action UpdateLockEvent)
            {
                this.countryData = new CountryData();
                this.countryData.CopyCountryData(countryData);
                OriginCountryName = countryData.CountryName;
                this.DeleteCountryData = DeleteCountryData;
                this.UpdateLockEvent = UpdateLockEvent;
                SyncWithCountryData();
            }

            public void CopyCountryData(CountryData countryData, Action<string> DeleteCountryData, Action UpdateLockEvent)
            {
                this.countryData.CopyCountryData(countryData);
                OriginCountryName = countryData.CountryName;
                this.DeleteCountryData = DeleteCountryData;
                this.UpdateLockEvent = UpdateLockEvent;
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

                // Set color if color is not valid
                if (countryData.CountryColor == BaseCountryDatas.NotValidCountryColor)
                {
                    countryData.CountryColor = MapColorUtil.GetValidColor_Random();
                }
                
                SyncWithCountryData();
                countryData.SetValidCountryData();
                Debug.Log($"edit over, now CountryData {countryData.CountryName} is valid");
            }

            [HorizontalGroup("CountryData"), Button("删除数据")]
            private void RemoveCountryData()
            {
                // NOTE : Only though this way, you can del CountryData
                if (DeleteCountryData != null)
                {
                    DeleteCountryData(OriginCountryName);
                    Debug.Log($"remove this countryData, origin name : {OriginCountryName}, cur name : {CountryName}");
                }
                else
                {
                    Debug.LogError("DeleteCountryData is null, you can init again");
                }
            }

            private void SyncWithCountryData()
            {
                CountryName = countryData.CountryName;
                CountryColor = countryData.CountryColor;
            }

            private void UpdateLockEdit()
            {
                UpdateLockEvent?.Invoke();
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
        [Button("保存子区域编辑结果", ButtonSizes.Medium)]
        private void SaveChildCountrySO()
        {
            if (!countrySO.CheckCountryLayerIndex(CurEditingLayerIndex))
            {
                Debug.LogError($"not valid layer index : {CurEditingLayerIndex}");
                return;
            }
            if (CurChooseCountryName == BaseCountryDatas.NotValidCountryName && CurEditingLayerIndex != BaseCountryDatas.RootLayerIndex)
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
                    wrapper.CopyCountryData(originData, DeleteCountryDataEvent, UpdateLockEvent);
                }
                else
                {
                    countrySO.AddCountryData(CurEditingLayerIndex, CurChooseCountryName, wrapper.GetCountryData());
                }
            }
            
            // 在Texture上面显示区域名称


            EditorUtility.SetDirty(countrySO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateHexTexManager();
        }

        [FoldoutGroup("区域数据编辑")]
        [Button("展示根层级子区域", ButtonSizes.Medium)]
        private void ShowHighestCountryDatas()
        {
            ShowChildCountryEvent(BaseCountryDatas.RootLayerIndex, BaseCountryDatas.RootLayerName, BaseCountryDatas.NotValidCountryName, true);
        }

        private void UpdateLockEvent()
        {
            lockEditSet.Clear();
            foreach (var wrapper in CurEditingChildCountryData)
            {
                if (wrapper.IsLock)
                {
                    lockEditSet.Add(wrapper.OriginCountryName);
                }
            }
        }

        #endregion

        #region 区域涂刷编辑

        [FoldoutGroup("区域涂刷编辑")]
        [LabelText("当前涂刷的 CountryName")]
        [ValueDropdown("GetCurFilterCountryData")]
        [OnValueChanged("OnCurPaintCountryChanged")]
        public string CurCountryName = BaseCountryDatas.NotValidCountryName;

        [FoldoutGroup("区域涂刷编辑")]
        [LabelText("当前涂刷的 Country 颜色"), ReadOnly]
        public Color CurPaintColor = BaseCountryDatas.NotValidCountryColor;

        private CountryData CurPaintCountryData = null;

        private IEnumerable<ValueDropdownItem<string>> GetCurFilterCountryData()
        {
            List<ValueDropdownItem<string>> dropDownItemList = new List<ValueDropdownItem<string>>();
            if (CurEditingChildCountryData.Count == 0)
            {
                return dropDownItemList;
            }

            foreach (var wrapper in CurEditingChildCountryData)
            {
                dropDownItemList.Add(new ValueDropdownItem<string>(wrapper.OriginCountryName, wrapper.OriginCountryName));
            }
            return dropDownItemList;
        }

        private void OnCurPaintCountryChanged()
        {
            CurPaintCountryData = countrySO.GetCountryDataByName(CurCountryName);
            if (CurPaintCountryData is null)
            {
                Debug.LogError($"Can not get country data, country name : {CurCountryName}");
                return;
            }
            CurPaintColor = CurPaintCountryData.CountryColor;
            SetBrushColor(CurPaintColor);

            // Switch cur RT data
            hexmapDataTexManager.SwitchToTexCache(CurPaintCountryData.Layer);
        }

        [FoldoutGroup("区域涂刷编辑")]
        [LabelText("保存位置")]
        public string saveCountryTexPath = MapStoreEnum.GamePlayCountryDataPath;

        [FoldoutGroup("区域涂刷编辑")]
        [Button("保存绘制结果", ButtonSizes.Medium)]
        private void SavePaintResult()
        {
            EditorUtility.SetDirty(countrySO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateHexTexManager();
            Debug.Log($"已成功保存: {countrySO.name}");
        }

        [FoldoutGroup("区域涂刷编辑")]
        [Tooltip("点击后可以让相邻区域的颜色保证不同")]
        [Button("一键修复区域颜色", ButtonSizes.Medium)]
        private void FixCountryColor()
        {
            // TODO : 需要遍历纹理，找到各个区域 对应的纹理地区，然后重新设置颜色
        }

        #endregion

        #region 区域数据导入导出

        [FoldoutGroup("区域数据导入导出")]
        [LabelText("区域数据导入/导出位置"), ReadOnly]
        public string exportCountryDatasFilePath = MapStoreEnum.GamePlayCountryCSVDataPath;

        [FoldoutGroup("区域数据导入导出")]
        [LabelText("区域纹理导入/导出位置"), ReadOnly]
        public string exportCountryTexFilePath = MapStoreEnum.GamePlayCountryTexDataPath;

        [FoldoutGroup("区域数据导入导出")]
        [LabelText("导入时清空区域数据")]
        public bool clearCountrySOWhenLoad = false;

        [FoldoutGroup("区域数据导入导出")]
        [Button("导入区域数据excel表", ButtonSizes.Medium)]
        private void ImportCountryFile()
        {
            countrySO.LoadCSV(exportCountryDatasFilePath, clearCountrySOWhenLoad);
            AssetDatabase.Refresh();
        }

        [FoldoutGroup("区域数据导入导出")]
        [Button("导出区域数据excel表", ButtonSizes.Medium)]
        private void ExportCountryFile()
        {
            countrySO.SaveCSV(exportCountryDatasFilePath);
            AssetDatabase.Refresh();
        }

        [FoldoutGroup("区域数据导入导出")]
        [Button("导入区域分布纹理", ButtonSizes.Medium)]
        private void ImportCountryTexture()
        {
            // TODO : 要新建一个文件夹，将每层的Country数据用于导入导出
            // exportCountryTexFilePath
        }

        [FoldoutGroup("区域数据导入导出")]
        [Button("导出区域分布纹理", ButtonSizes.Medium)]
        private void ExportCountryTexture()
        {

        }

        #endregion

        #region 测试 CountryManager 的功能（之后可以删掉，因为功能要集成到HexCons里面

        [FoldoutGroup("测试 CountryManager")]
        [Button("初始化 CountryManager", ButtonSizes.Medium)]
        private void InitCountryManager()
        {
            HexCtor.InitCountry(countrySO);
        }

        [FoldoutGroup("测试 CountryManager")]
        [Button("通过 CountryManager 设置区域间的颜色", ButtonSizes.Medium)]
        private void SetColorByCountryManager()
        {
            HexCtor.UpdateCountryColor();
            // 还需要重新调用一次 Init 
        }

        #endregion

        // TODO : 现在山脉、浅海、深海不能作为区域的一部分
        protected override bool EnablePaintHex(Vector2Int offsetHexPos) 
        { 
            if(countrySO is null || CurPaintCountryData is null)
            {
                return false;
            }

            if (!countrySO.IsValidIndice(offsetHexPos))
            {
                return false;
            }

            // 越界
            if (offsetHexPos.x < 0 || offsetHexPos.x >= hexSet.mapWidth || offsetHexPos.y < 0 || offsetHexPos.y >= hexSet.mapHeight)
            {
                return false;
            }

            List<CountryData> countryList = countrySO.GetGridCountry(offsetHexPos);
            CountryData parentData = countrySO.GetParentCountryData(CurPaintCountryData);
            int editingLayer = CurPaintCountryData.Layer;

            //  If paint grid is not cur countrydata's parent data, forbid it
            if (countrySO.IsValidLayer(parentData.Layer) && countryList[parentData.Layer] != parentData)
            {
                Debug.LogError("Not father country, so can not paint!");
                return false;
            }

            // If locked edit, return false
            CountryData thisLayerData = countryList[editingLayer];
            if(thisLayerData is null)
            {
                return true;
            }
            if (!thisLayerData.IsValid)
            {
                return false;
            }
            if (lockEditSet.Contains(thisLayerData.CountryName))
            {
                return false;
            }
            return true;
        }

        protected override void PaintHexRTEvent(List<Vector2Int> offsetHexList) 
        {
            foreach (var offsetHex in offsetHexList)
            {
                countrySO.SetGridCountry(offsetHex, CurPaintCountryData);
            }
            Debug.Log($"You paint grid : {offsetHexList.Count}");
        }


        public override void Disable()
        {
            CountryLayerFilter.Clear();
        }

    }
}
