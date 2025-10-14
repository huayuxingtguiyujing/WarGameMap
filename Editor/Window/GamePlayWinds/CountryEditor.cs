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

            // Reset rear filter's CurCountryName as "��"
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

        // TODO : ��Ҫ�����Ż�...�����е�����
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
            //[Button("չʾ������")]
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

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("������")]
        public List<CountryLayerWrapper> CountryLayerFilter = new List<CountryLayerWrapper>();

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("ѡ���������ڲ㼶"), ReadOnly]
        public string CurEditingLayer           = BaseCountryDatas.NotValidLayerName;
        
        private int CurEditingLayerIndex        = BaseCountryDatas.NotValidLayerIndex;
        static int _CurEditingLayerIndex;

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("ѡ����������"), ReadOnly]
        public string CurChooseCountryName      = BaseCountryDatas.NotValidCountryName;

        HashSet<string> lockEditSet = new HashSet<string>();        // Cur locking country names

        [Serializable]
        public class CountryDataWrapper
        {
            [HorizontalGroup("CountryData"), LabelText("����")]
            [OnValueChanged("UpdateLockEdit")]
            public bool IsLock;

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

                // Set color if color is not valid
                if (countryData.CountryColor == BaseCountryDatas.NotValidCountryColor)
                {
                    countryData.CountryColor = MapColorUtil.GetValidColor_Random();
                }
                
                SyncWithCountryData();
                countryData.SetValidCountryData();
                Debug.Log($"edit over, now CountryData {countryData.CountryName} is valid");
            }

            [HorizontalGroup("CountryData"), Button("ɾ������")]
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

        [FoldoutGroup("�������ݱ༭")]
        [LabelText("��ǰ�༭�����������")]
        [Tooltip("���δ��� ɾ������ �� CountryData ���ᱻ�����Ƴ�")]
        public List<CountryDataWrapper> CurEditingChildCountryData = new List<CountryDataWrapper>();

        [FoldoutGroup("�������ݱ༭")]
        [Button("����������༭���", ButtonSizes.Medium)]
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
            
            // ��Texture������ʾ��������


            EditorUtility.SetDirty(countrySO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateHexTexManager();
        }

        [FoldoutGroup("�������ݱ༭")]
        [Button("չʾ���㼶������", ButtonSizes.Medium)]
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

        #region ����Ϳˢ�༭

        [FoldoutGroup("����Ϳˢ�༭")]
        [LabelText("��ǰͿˢ�� CountryName")]
        [ValueDropdown("GetCurFilterCountryData")]
        [OnValueChanged("OnCurPaintCountryChanged")]
        public string CurCountryName = BaseCountryDatas.NotValidCountryName;

        [FoldoutGroup("����Ϳˢ�༭")]
        [LabelText("��ǰͿˢ�� Country ��ɫ"), ReadOnly]
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

        [FoldoutGroup("����Ϳˢ�༭")]
        [LabelText("����λ��")]
        public string saveCountryTexPath = MapStoreEnum.GamePlayCountryDataPath;

        [FoldoutGroup("����Ϳˢ�༭")]
        [Button("������ƽ��", ButtonSizes.Medium)]
        private void SavePaintResult()
        {
            EditorUtility.SetDirty(countrySO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateHexTexManager();
            Debug.Log($"�ѳɹ�����: {countrySO.name}");
        }

        [FoldoutGroup("����Ϳˢ�༭")]
        [Tooltip("���������������������ɫ��֤��ͬ")]
        [Button("һ���޸�������ɫ", ButtonSizes.Medium)]
        private void FixCountryColor()
        {
            // TODO : ��Ҫ���������ҵ��������� ��Ӧ�����������Ȼ������������ɫ
        }

        #endregion

        #region �������ݵ��뵼��

        [FoldoutGroup("�������ݵ��뵼��")]
        [LabelText("�������ݵ���/����λ��"), ReadOnly]
        public string exportCountryDatasFilePath = MapStoreEnum.GamePlayCountryCSVDataPath;

        [FoldoutGroup("�������ݵ��뵼��")]
        [LabelText("����������/����λ��"), ReadOnly]
        public string exportCountryTexFilePath = MapStoreEnum.GamePlayCountryTexDataPath;

        [FoldoutGroup("�������ݵ��뵼��")]
        [LabelText("����ʱ�����������")]
        public bool clearCountrySOWhenLoad = false;

        [FoldoutGroup("�������ݵ��뵼��")]
        [Button("������������excel��", ButtonSizes.Medium)]
        private void ImportCountryFile()
        {
            countrySO.LoadCSV(exportCountryDatasFilePath, clearCountrySOWhenLoad);
            AssetDatabase.Refresh();
        }

        [FoldoutGroup("�������ݵ��뵼��")]
        [Button("������������excel��", ButtonSizes.Medium)]
        private void ExportCountryFile()
        {
            countrySO.SaveCSV(exportCountryDatasFilePath);
            AssetDatabase.Refresh();
        }

        [FoldoutGroup("�������ݵ��뵼��")]
        [Button("��������ֲ�����", ButtonSizes.Medium)]
        private void ImportCountryTexture()
        {
            // TODO : Ҫ�½�һ���ļ��У���ÿ���Country�������ڵ��뵼��
            // exportCountryTexFilePath
        }

        [FoldoutGroup("�������ݵ��뵼��")]
        [Button("��������ֲ�����", ButtonSizes.Medium)]
        private void ExportCountryTexture()
        {

        }

        #endregion

        #region ���� CountryManager �Ĺ��ܣ�֮�����ɾ������Ϊ����Ҫ���ɵ�HexCons����

        [FoldoutGroup("���� CountryManager")]
        [Button("��ʼ�� CountryManager", ButtonSizes.Medium)]
        private void InitCountryManager()
        {
            HexCtor.InitCountry(countrySO);
        }

        [FoldoutGroup("���� CountryManager")]
        [Button("ͨ�� CountryManager ������������ɫ", ButtonSizes.Medium)]
        private void SetColorByCountryManager()
        {
            HexCtor.UpdateCountryColor();
            // ����Ҫ���µ���һ�� Init 
        }

        #endregion

        // TODO : ����ɽ����ǳ�����������Ϊ�����һ����
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

            // Խ��
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
