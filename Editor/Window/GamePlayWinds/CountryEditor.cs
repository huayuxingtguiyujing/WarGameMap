using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Text;
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
            InitCountryManager();
            UpdateCountryNames();
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

            // TODO : 以后 countrySO 和 gridTerrainSO 的初始化都要放到编辑器的其他地方！
            gridTerrainSO = EditorSceneManager.GridTerrainSO;
            gridTerrainSO.UpdateTerSO(hexSet.mapWidth, hexSet.mapHeight);
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
            CountryData curCountryData = countrySO.GetCountryDataByName(parentLayerLevel, ParentCountryDataName);
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
                string countryNameComplete = GetCurCountryNameComplete();
                CountryData curCountryData = countrySO.GetCountryDataByName(LayerLevel, countryNameComplete);
                if (curCountryData == null)
                {
                    //Debug.Log($"CountryName : {CurCountryDataName} has no CountryData, complete countryName : {countryNameComplete}");
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
            
            // Set wrapper lock statu here
            SetAllLock(IsAllLock);
            UpdateCountryNames();

            //Debug.Log($"show child data num : {childCountryData.Count}");
        }

        private void DeleteCountryDataEvent(int LayerLevel, string OriginCountryName)
        {
            string originCountryNameComplete = GetCurCountryNameComplete() + OriginCountryName;
            CountryData countryData = countrySO.GetCountryDataByName(LayerLevel, originCountryNameComplete);
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

        private void InitCountryManager()
        {
            HexCtor.InitCountry(countrySO);
        }


        // Country Info (Scene) datas
        int[] fontSizes = new int[4] { 38, 30, 22, 12 };

        float[] maxDrawTxtDistances = new float[4] {8500, 5200, 4200, 3200 };

        Dictionary<int, string> CurCountryIdxNameDict = new Dictionary<int, string>();

        Dictionary<int, Vector3> CurCountryIdxPosDict = new Dictionary<int, Vector3>();

        private void UpdateCountryNames()
        {
            if(CurEditingChildCountryData is null || CurEditingChildCountryData.Count <= 0)
            {
                return;
            }

            CurCountryIdxNameDict.Clear();
            CurCountryIdxPosDict.Clear();

            // Root layer do not need show country names
            if (CurEditingLayerIndex == BaseCountryDatas.MaxLayerNum)
            {
                return;
            }

            // Build country name's data
            int nextLayer = CurEditingLayerIndex + 1;
            foreach (var wrapper in CurEditingChildCountryData)
            {
                int indexInLayer = wrapper.GetCountryData().IndexInLayer;
                Vector2Int offsetPos = HexCtor.GetBoundCenterByIndex(nextLayer, indexInLayer);
                Vector3 worldPos = HexHelper.OffsetToWorld(hexSet.GetScreenLayout(), offsetPos);
                CurCountryIdxNameDict.Add(indexInLayer, wrapper.CountryName);
                CurCountryIdxPosDict.Add(indexInLayer, worldPos);
            }
        }

        private void RegisterCountryNames()
        {
            GizmosCtrl.GetInstance().RegisterGizmoEvent(DrawCountryInfos);
        }

        private void UnregisterCountryNames()
        {
            GizmosCtrl.GetInstance().UnregisterGizmoEvent(DrawCountryInfos);
        }

        private void DrawCountryInfos()
        {
            if (CurCountryIdxNameDict is null || CurCountryIdxNameDict.Count <= 0)
            {
                return;
            }
            if (CurCountryIdxPosDict is null || CurCountryIdxPosDict.Count <= 0)
            {
                return;
            }

            int nextLayer = CurEditingLayerIndex + 1;
            foreach (var pair in CurCountryIdxNameDict)
            {
                int indexInLayer = pair.Key;
                string countryName = pair.Value;
                Vector3 position = CurCountryIdxPosDict[indexInLayer];
                GizmosUtils.DrawText(position, countryName, fontSizes[nextLayer], BaseCountryDatas.NotValidCountryColor, 5f, maxDrawTxtDistances[nextLayer]);
            }
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
                            if (gridTerrainSO.GetGridCanCountry(idx))
                            {
                                hexColors.Add(BaseCountryDatas.NotValidCountryColor);
                            }
                            else
                            {
                                hexColors.Add(BaseCountryDatas.CanNotBeCountryColor);
                            }
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

            if(CurEditingLayerIndex >= 0 && CurEditingLayerIndex < BaseCountryDatas.MaxLayerNum)
            {
                hexmapDataTexManager.SwitchToTexCache(CurEditingLayerIndex + 1);
            }
            else
            {
                hexmapDataTexManager.SwitchToTexCache(0);
            }
        }
        
        #region 区域数据编辑

        [FoldoutGroup("区域数据编辑")]
        [LabelText("区域数据SO"), ReadOnly]
        public CountrySO countrySO;     // SerializedScriptableObject

        [FoldoutGroup("区域数据编辑")]
        [LabelText("格子地形SO"), ReadOnly]
        public GridTerrainSO gridTerrainSO;

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
            public string CurCountryName = BaseCountryDatas.NotValidCountryName;    // 当前在这一层级中 选中的区域名称

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

        // 可见 : CountrySO - GetCountryNameComplete
        // 获取当前的区域名称前缀
        private string GetCurCountryNameComplete()
        {
            StringBuilder sb = new StringBuilder(16);
            for (int layer = 0; layer <= CurEditingLayerIndex; layer++)
            {
                if (CountryLayerFilter[layer].CurCountryName == BaseCountryDatas.NotValidCountryName)
                {
                    //Debug.LogError($"Wrong, layer {layer} cur country name is not valid");
                    continue;
                }
                sb.Append(CountryLayerFilter[layer].CurCountryName);
            }
            return sb.ToString();
        }

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

            [HorizontalGroup("CountryData"), LabelText("名称"), ReadOnly]
            public string CountryName;

            [HorizontalGroup("CountryData"), LabelText("颜色"), ReadOnly]
            public Color CountryColor;

            CountryData countryData;

            [HideInInspector]
            public bool IsValid => countryData.IsValid;


            Action<int, string> DeleteCountryData;
            Action UpdateLockEvent;

            public CountryDataWrapper()
            {
                OriginCountryName = BaseCountryDatas.NotValidCountryName;
                this.countryData = new CountryData(_CurEditingLayerIndex + 1, BaseCountryDatas.NotValidCountryName, "", BaseCountryDatas.NotValidCountryColor);
                SyncWithCountryData();
            }

            public CountryDataWrapper(CountryData countryData, Action<int, string> DeleteCountryData, Action UpdateLockEvent)
            {
                this.countryData = new CountryData();
                this.countryData.CopyCountryData(countryData);
                OriginCountryName = countryData.CountryName;
                this.DeleteCountryData = DeleteCountryData;
                this.UpdateLockEvent = UpdateLockEvent;
                SyncWithCountryData();
            }

            public void CopyCountryData(CountryData countryData, Action<int, string> DeleteCountryData, Action UpdateLockEvent)
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
                    // _CurEditingLayerIndex + 1 is the layer index of country data
                    DeleteCountryData(_CurEditingLayerIndex + 1, OriginCountryName);
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

            public void SetCountryLock(bool flag)
            {
                IsLock = flag;
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
                Debug.LogError($"Not valid layer index : {CurEditingLayerIndex}");
                return;
            }
            if (CurChooseCountryName == BaseCountryDatas.NotValidCountryName && CurEditingLayerIndex != BaseCountryDatas.RootLayerIndex)
            {
                Debug.LogError("You have not choosen a parent area");
                return;
            }

            // Will not sync delete result
            foreach (var wrapper in CurEditingChildCountryData)
            {
                if (!wrapper.IsValid)
                {
                    continue;
                }
                string originCountryNameComplete = GetCurCountryNameComplete() + wrapper.OriginCountryName;
                CountryData originData = countrySO.GetCountryDataByName(CurEditingLayerIndex + 1, originCountryNameComplete);
                if (originData != null)
                {
                    // If found country data, then sync with it
                    originData.CopyCountryData(wrapper.GetCountryData());
                    wrapper.CopyCountryData(originData, DeleteCountryDataEvent, UpdateLockEvent);
                }
                else
                {
                    // Not found origin country data, it is new added
                    originData = countrySO.AddCountryData(CurEditingLayerIndex, GetCurCountryNameComplete(), wrapper.GetCountryData());
                    wrapper.CopyCountryData(originData, DeleteCountryDataEvent, UpdateLockEvent);
                }
            }
            // TODO : 修复问题！！！
            UpdateCountryNames();

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
        [LabelText("正在擦除")]
        [OnValueChanged("OnErasingModeChange")]
        public bool IsErasingMode = false;      // TODO : 后续在切换到 ErasingMode 时，要同时将 scene 的按钮切换成橡皮擦

        bool IsNotErasing => !IsErasingMode;

        [FoldoutGroup("区域涂刷编辑")]
        [LabelText("全部锁定")]
        [OnValueChanged("OnAllLockChange")]
        public bool IsAllLock = false;

        [FoldoutGroup("区域涂刷编辑")]
        [LabelText("当前涂刷的 CountryName"), ShowIf("IsNotErasing")]
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
            string curCountryNameComplete = GetCurCountryNameComplete() + CurCountryName;
            CurPaintCountryData = countrySO.GetCountryDataByName(CurEditingLayerIndex + 1, curCountryNameComplete);
            if (CurPaintCountryData is null)
            {
                Debug.LogError($"Can not get country data, country name : {CurCountryName}");
                return;
            }
            CurPaintColor = CurPaintCountryData.CountryColor;
            SetBrushColor(CurPaintColor);

            // Set every layer's color
            Color[] cacheColors = countrySO.GetCountryDataColors(CurPaintCountryData).ToArray();
            SetBrushCacheColor(cacheColors);

            // Switch cur RT data
            hexmapDataTexManager.SwitchToTexCache(CurPaintCountryData.Layer);

            SetErasingMode(false);
        }

        private void SetErasingMode(bool flag)
        {
            IsErasingMode = flag;
            OnErasingModeChange();
        }

        private void OnErasingModeChange()
        {
            if (IsErasingMode)
            {
                Color eraseColor = BaseCountryDatas.NotValidCountryColor;
                CurPaintColor = eraseColor;

                // NOTE : 目前的擦除会把所有层级的区域数据都擦除
                Color[] cacheColors = new Color[4] { eraseColor, eraseColor, eraseColor, eraseColor };
                SetBrushCacheColor(cacheColors);
            }
            else
            {
                if (CurPaintCountryData is null)
                {
                    return;
                }
                CurPaintColor = CurPaintCountryData.CountryColor;
            }
            SetBrushColor(CurPaintColor);
        }

        private void SetAllLock(bool flag)
        {
            IsAllLock = flag;
            OnAllLockChange();
        }

        private void OnAllLockChange()
        {
            foreach(var wrapper in CurEditingChildCountryData)
            {
                wrapper.SetCountryLock(IsAllLock);
            }
            UpdateLockEvent();
        }


        [FoldoutGroup("区域涂刷编辑")]
        [LabelText("保存位置"), ReadOnly]
        public string saveCountryTexPath = MapStoreEnum.GamePlayCountryDataPath;

        [FoldoutGroup("区域涂刷编辑")]
        [Button("保存绘制结果", ButtonSizes.Medium)]
        private void SavePaintResult()
        {
            InitCountryManager();
            UpdateCountryNames();
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
            if(HexCtor == null)
            {
                Debug.LogError("HexCtor is null, can not fix country color!");
                return;
            }
            HexCtor.UpdateCountryColor();
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
        [LabelText("导入时覆盖区域数据")]
        public bool overrideCountrySOWhenLoad = false;

        [FoldoutGroup("区域数据导入导出")]
        [Button("导入区域数据excel表", ButtonSizes.Medium)]
        private void ImportCountryFile()
        {
            countrySO.LoadCSV(exportCountryDatasFilePath, overrideCountrySOWhenLoad);
            AssetDatabase.Refresh();
        }

        [FoldoutGroup("区域数据导入导出")]
        [Button("导出区域数据excel表", ButtonSizes.Medium)]
        private void ExportCountryFile()
        {
            countrySO.SaveCSV(exportCountryDatasFilePath);
            AssetDatabase.Refresh();
        }

        //[FoldoutGroup("区域数据导入导出")]
        //[Button("导入区域分布纹理", ButtonSizes.Medium)]
        //private void ImportCountryTexture()
        //{
        //    // TODO : 要新建一个文件夹，将每层的Country数据用于导入导出
        //    // exportCountryTexFilePath
        //}

        [FoldoutGroup("区域数据导入导出")]
        [Button("导出区域分布纹理", ButtonSizes.Medium)]
        private void ExportCountryTexture()
        {
            List<Texture2D> countryTexs = ExportTexture();
            int randomInt = new System.Random().Next(0, int.MaxValue);
            string[] countryNames = new string[]
            {
                $"regionTex_{countrySO.mapWidth}x{countrySO.mapHeight}_{randomInt}",
                $"provinceTex_{countrySO.mapWidth}x{countrySO.mapHeight}_{randomInt}",
                $"prefectureTex_{countrySO.mapWidth}x{countrySO.mapHeight}_{randomInt}",
                $"subPrefectureTex_{countrySO.mapWidth}x{countrySO.mapHeight}_{randomInt}",
                $"edgeRelationTex_{countrySO.mapWidth}x{countrySO.mapHeight}_{randomInt}"
            };
            for(int i = 0; i < countryTexs.Count; i++)
            {
                TextureUtility.SaveTextureAsAsset(exportCountryTexFilePath, countryNames[i], countryTexs[i]);
            }
            
            Debug.Log($"Export country tex over, num : {countryTexs.Count}, width : {countrySO.mapWidth}, height : {countrySO.mapHeight}");
        }

        private List<Texture2D> ExportTexture()
        {
            int mapWidth = countrySO.mapWidth;
            int mapHeight = countrySO.mapHeight;
            int layerNum = BaseCountryDatas.MaxLayerNum + 1;

            List<Texture2D> countryTexs = new List<Texture2D>(layerNum);
            List<Color[]> countryColors = new List<Color[]>(layerNum);
            for (int i = 0; i < layerNum; i++)
            {
                Texture2D countryTex = new Texture2D(mapWidth, mapHeight);
                countryTex.filterMode = FilterMode.Point;
                countryTexs.Add(countryTex);
                Color[] colors = countryTex.GetPixels();
                countryColors.Add(colors);
            }

            // Storage edge relation, use it in country shader
            Texture2D edgeRelationTex = new Texture2D(mapWidth, mapHeight);
            edgeRelationTex.filterMode = FilterMode.Point;
            Color[] edgeRelationColors = edgeRelationTex.GetPixels();
            //byte[] edgeRelationBytes = new byte[mapWidth * mapHeight * 4];

            int count = 0;
            //int edgeRelationIdx = 0;
            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    Vector2Int idx = new Vector2Int(i, j);  // TODO : 顺序是对的吗
                    List<CountryData> countryDatas = countrySO.GetGridCountry(idx);
                    int index = idx.y * mapWidth + idx.x;

                    // Apply color by grid's countryDatas
                    // countryDatas[0] - Region, [1] - Province, [2] - Fecture, [3] - SubFecture
                    for (int k = 0; k < countryDatas.Count; k++)
                    {
                        if (countryDatas[k] != null)
                        {
                            countryColors[k][index] = countryDatas[k].CountryColor;
                        }
                        else if (gridTerrainSO.GetGridCanCountry(idx))
                        {
                            countryColors[k][index] = BaseCountryDatas.NotValidCountryColor;
                        }
                        else
                        {
                            countryColors[k][index] = BaseCountryDatas.CanNotBeCountryColor;
                        }
                    }

                    // NOTE : 对于每个点，计算它和邻居点的区域关系，存储在 edgeRelationTex 中
                    //          edgeRelationTex[i] = (R, G, B, A)
                    //          R : region 层级的边界关系 (1111 1111) 前六位表示六边形的六个邻居是否属于不同区域 (1 是不同区域) , 后两位待定
                    //          G : province 层级的边界关系
                    //          B : fecture 层级的边界关系
                    //          A : subFecture 层级的边界关系
                    byte[] gridRelation = countrySO.GetGridCountryNeighbor(idx);

                    edgeRelationColors[index].r = (gridRelation[0] != 0) ? 1 : 0;
                    if (edgeRelationColors[index].r == 1)
                    {
                        count++;
                    }
                    edgeRelationColors[index].g = (gridRelation[1] != 0) ? 1 : 0;
                    edgeRelationColors[index].b = (gridRelation[2] != 0) ? 1 : 0;
                    edgeRelationColors[index].a = (gridRelation[3] != 0) ? 1 : 0;
                    edgeRelationColors[index].a = 1;

                    //edgeRelationBytes[edgeRelationIdx] = gridRelation[0];
                    //edgeRelationBytes[edgeRelationIdx + 1] = gridRelation[0];
                    //edgeRelationBytes[edgeRelationIdx + 2] = gridRelation[0];
                    //edgeRelationBytes[edgeRelationIdx + 3] = gridRelation[0];
                    //edgeRelationIdx += 4;
                }
            }

            for (int i = 0; i < layerNum; i++)
            {
                Texture2D countryTex = countryTexs[i];
                countryTex.SetPixels(countryColors[i]);
                countryTex.Apply();
            }

            edgeRelationTex.SetPixels(edgeRelationColors);
            //edgeRelationTex.LoadRawTextureData(edgeRelationBytes);
            edgeRelationTex.Apply(false, false);
            // Add to country texs, and then save them
            countryTexs.Add(edgeRelationTex);

            Debug.Log($"Edge tex : {count}");

            return countryTexs;
        }

        #endregion


        protected override bool EnablePaintHex(Vector2Int offsetHexPos) 
        { 
            if(countrySO is null)
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

            // Mountain, Sea can not be country
            if (!gridTerrainSO.GetGridCanCountry(offsetHexPos))
            {
                return false;
            }

            // Dont erase CanNotCountry grids
            if (IsErasingMode)
            {
                return true;
            }
            else if (CurPaintCountryData is null)
            {
                // ErasingMode permit nullble CurPaintCountryData
                return false;
            }

            List<CountryData> countryList = countrySO.GetGridCountry(offsetHexPos);
            CountryData parentData = countrySO.GetParentCountryData(CurPaintCountryData);
            int editingLayer = CurPaintCountryData.Layer;

            //  If paint grid is not cur countrydata's parent data, forbid it
            if (countrySO.IsValidLayer(parentData.Layer) && countryList[parentData.Layer] != parentData)
            {
                //Debug.LogError("Not father country, so can not paint!");
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
            if (IsErasingMode)
            {
                countrySO.SetGridCountryNotValid(offsetHexList);
            }
            else
            {
                countrySO.SetGridCountry(offsetHexList, CurPaintCountryData);
            }

            Debug.Log($"You paint grid : {offsetHexList.Count}");
        }


        public override void Enable()
        {
            base.Enable();
            RegisterCountryNames();
        }

        public override void Disable()
        {
            base.Disable();
            CountryLayerFilter.Clear();
            UnregisterCountryNames();
        }

    }
}
