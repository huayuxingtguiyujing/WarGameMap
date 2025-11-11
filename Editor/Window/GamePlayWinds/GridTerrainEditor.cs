using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class GridTerrainEditor : BrushHexmapEditor
    {
        public override string EditorName => MapEditorEnum.GridTerrainEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();
            LoadTerrainType();
            LoadMountainData();
            InitCurTerrainType();
            Debug.Log("Init grid terrrain editor over !");
        }
        
        protected override BrushHexmapSetting GetBrushSetting()
        {
            BrushHexmapSetting setting = new BrushHexmapSetting();
            setting.enableKeyCode = true;
            setting.useTexCache = true;
            setting.texCacheNum = 2;    // Grid Terrain + Mountain Grid
            return setting;
        }

        private void LoadTerrainType()
        {
            gridTerrainSO = EditorSceneManager.GridTerrainSO;
            //FindOrCreateSO<GridTerrainSO>(ref gridTerrainSO, MapStoreEnum.GamePlayGridTerrainDataPath, "GridTerrainSO_Default.asset");
            gridTerrainSO.UpdateTerSO(hexSet.mapWidth, hexSet.mapHeight);

            TerrainLayersList.Clear();
            TerrainTypesList.Clear();

            // Load layer and all terrainType
            foreach (var layer in gridTerrainSO.GridTerrainLayerList)
            {
                TerrainLayersList.Add(layer.CopyObject());
            }

            foreach (var type in gridTerrainSO.GridTerrainTypeList)
            {
                TerrainTypesList.Add(type.CopyObject());
            }
            //Debug.Log($"load terrain types over, TerrainLayersList : {TerrainLayersList.Count}, TerrainTypesList : {TerrainTypesList.Count}");
        }

        private void LoadMountainData()
        {
            if(gridTerrainSO == null)
            {
                Debug.LogError("GridTerrainSO not loaded!");
                return;
            }
            MountainDataWrappers.Clear();
            foreach (var mountainData in gridTerrainSO.MountainDatas)
            {
                MountainDataWrapper wrapper = new MountainDataWrapper(mountainData, PaintMountainData, DeleteMountainData);
                MountainDataWrappers.Add(wrapper);
            }
            UpdateMountainData();
        }

        private void UpdateMountainData()
        {
            MountainID_Wrapper_Dict.Clear();
            foreach (var wrapper in MountainDataWrappers)
            {
                MountainID_Wrapper_Dict.Add(wrapper.MountainID, wrapper);
            }
        }

        private void InitCurTerrainType()
        {
            if(TerrainTypesList.Count > 1)
            {
                CurCurGridTerrainTypeName = TerrainTypesList[0].terrainTypeName;
                OnCurGriTerrainChanged();
            }
        }

        protected override void PostBuildHexGridMap()
        {
            // Use Grid Terrain Set first layer
            List<Color> gridTerrainColors = hexmapDataTexManager.GetHexDataTexColors();
            hexmapDataTexManager.InitTexCache(0, gridTerrainColors);

            int mapWidth = hexSet.mapWidth;
            int mapHeight = hexSet.mapHeight;
            List<Color> mountainColors = new List<Color>(mapHeight * mapWidth);
            for (int j = 0; j < mapHeight; j++)
            {
                for (int i = 0; i < mapWidth; i++)
                {
                    Vector2Int idx = new Vector2Int(i, j);
                    bool IsMountain = gridTerrainSO.GetGridIsMountain(idx);
                    if (IsMountain)
                    {
                        int mountainID = gridTerrainSO.GetGridMountainID(idx);
                        mountainColors.Add(MapColorUtil.GetRandomColor(mountainID));
                    }
                    else
                    {
                        mountainColors.Add(MapColorUtil.NotValidColor);
                    }
                }
            }
            hexmapDataTexManager.InitTexCache(1, mountainColors);

            hexmapDataTexManager.SwitchToTexCache(0);
        }

        public override void Destory()
        {
            base.Destory();
        }


        #region 格子地形编辑

        [FoldoutGroup("格子地形数据编辑")]
        [LabelText("编辑使用的地形")]
        [ValueDropdown("GetTerrainTypesList")]
        [OnValueChanged("OnCurGriTerrainChanged")]
        public string CurCurGridTerrainTypeName;

        int CurGridTerrainTypeIndex;

        GridTerrainType CurGridTerrain;

        private IEnumerable<ValueDropdownItem<string>> GetTerrainTypesList()
        {
            List<ValueDropdownItem<string>> dropDownItemList = new List<ValueDropdownItem<string>>();
            foreach (var terrainType in TerrainTypesList)
            {
                dropDownItemList.Add(new ValueDropdownItem<string>(terrainType.terrainTypeChineseName, terrainType.terrainTypeName));
            }
            return dropDownItemList;
        }

        private void OnCurGriTerrainChanged()
        {
            if (notInitScene)
            {
                return;
            }
            SetTerrainTypeByCurIdx();
            UpdateEditingMountain();
            UpdateBrushColor();
        }

        private void GetPreTerrainType()
        {
            CurGridTerrainTypeIndex--;
            FixCurTerrainTypeIdx();
            CurCurGridTerrainTypeName = TerrainTypesList[CurGridTerrainTypeIndex].terrainTypeName;
            SetTerrainTypeByCurIdx();
            UpdateBrushColor();
        }

        private void GetNextTerrainType()
        {
            CurGridTerrainTypeIndex++;
            FixCurTerrainTypeIdx();
            CurCurGridTerrainTypeName = TerrainTypesList[CurGridTerrainTypeIndex].terrainTypeName;
            SetTerrainTypeByCurIdx();
            UpdateBrushColor();
        }

        private void FixCurTerrainTypeIdx()
        {
            if (CurGridTerrainTypeIndex > TerrainTypesList.Count - 1)
            {
                CurGridTerrainTypeIndex = 0;
            }
            else if (CurGridTerrainTypeIndex < 0)
            {
                CurGridTerrainTypeIndex = TerrainTypesList.Count - 1;
            }
        }

        private void SetTerrainTypeByCurIdx()
        {
            // Find cur terrain type index
            for(int i = 0;  i < TerrainTypesList.Count; i++)
            {
                if(CurCurGridTerrainTypeName == TerrainTypesList[i].terrainTypeName)
                {
                    CurGridTerrainTypeIndex = i;
                    break;
                }
            }
            CurGridTerrain = gridTerrainSO.GetTerrainType(CurCurGridTerrainTypeName);

            Debug.Log($"Now choose terrain type : {CurGridTerrain.terrainTypeName}");
        }

        private void UpdateBrushColor()
        {
            if (IsEditingMountain)
            {
                Color moutainColor = MapColorUtil.NotValidColor;
                if (BaseGridTerrain.IsMountain(CurGridTerrain))
                {
                    moutainColor = MountainID_Wrapper_Dict[CurEditingMountain.MountainID].MountainEdtingColor;
                }

                SetBrushColor(moutainColor);
                // Length : BrushHexmapSetting.texDataPageNum
                Color[] cacheColors = new Color[2] { CurGridTerrain.terrainEditColor, moutainColor };
                SetBrushCacheColor(cacheColors);
            }
            else
            {
                SetBrushColor(CurGridTerrain.terrainEditColor);
                Color[] cacheColors = new Color[2] { CurGridTerrain.terrainEditColor, MapColorUtil.NotValidColor };
                SetBrushCacheColor(cacheColors);
            }
        }


        [FoldoutGroup("格子地形数据编辑")]
        [LabelText("格子地形源数据SO")]
        public GridTerrainSO gridTerrainSO;

        [FoldoutGroup("格子地形数据编辑")]
        [LabelText("显示指定Layer的地形")]
        [ValueDropdown("GetTerrainLayersList")]
        [OnValueChanged("OnCurFilterLayerChanged")]
        public string CurFilterLayerName;

        string AllLayerFilter = "所有层级";

        private IEnumerable<ValueDropdownItem<string>> GetTerrainLayersList()
        {
            List<ValueDropdownItem<string>> dropDownItemList = new List<ValueDropdownItem<string>>() {
                new ValueDropdownItem<string>(AllLayerFilter, AllLayerFilter)
            };
            foreach (var terrainLayer in TerrainLayersList)
            {
                dropDownItemList.Add(new ValueDropdownItem<string>(terrainLayer.layerName, terrainLayer.layerName));
            }
            return dropDownItemList;
        }

        private void OnCurFilterLayerChanged()
        {
            if (notInitScene)
            {
                return;
            }
            
            TerrainTypesList.Clear();
            if (CurFilterLayerName == AllLayerFilter)
            {
                foreach (var type in gridTerrainSO.GridTerrainTypeList)
                {
                    TerrainTypesList.Add(type.CopyObject());
                }
            }
            else
            {
                List<GridTerrainType> layerTypes = gridTerrainSO.GetTerrainTypesByLayer(CurFilterLayerName);
                foreach (var type in layerTypes)
                {
                    TerrainTypesList.Add(type.CopyObject());
                }
            }
        }


        [FoldoutGroup("格子地形数据编辑")]
        [LabelText("地形层级")] 
        public List<GridTerrainLayer> TerrainLayersList = new List<GridTerrainLayer>();

        [FoldoutGroup("格子地形数据编辑")]
        [LabelText("地形列表")]
        public List<GridTerrainType> TerrainTypesList = new List<GridTerrainType>();

        [FoldoutGroup("格子地形数据编辑")]
        [Button("保存地形数据", ButtonSizes.Medium)]
        private void SaveTerrainTypeDatas()
        {
            gridTerrainSO.SaveGridTerSO(TerrainLayersList, TerrainTypesList);
            EditorUtility.SetDirty(gridTerrainSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateHexTexManager();
            Debug.Log($"Save terrain type datas, save terrain grid datas. grid num : {gridTerrainSO.GridCount}");
        }

        #endregion


        #region 格子地形纹理导出

        class GridTerrainTexHelper
        {
            Layout layout;
            int clusterSize;
            int fixedClsSize;
            int hexGridSize;
            float innerHexRatio;
            Vector2 offset;

            GridTerrainSO gridTerrainSO;

            List<Color> hexGridMapColors;

            public GridTerrainTexHelper(Vector2Int longitudeAndLatitude, int fixedClsSize, TerrainSettingSO terSet, HexSettingSO hexSet, GridTerrainSO gridTerrainSO, List<Color> hexGridMapColors)
            {
                this.gridTerrainSO = gridTerrainSO;

                this.layout = hexSet.GetScreenLayout();
                this.clusterSize = terSet.clusterSize;
                this.hexGridSize = hexSet.hexGridSize;
                this.fixedClsSize = fixedClsSize;
                this.innerHexRatio = 0.6f;
                this.hexGridMapColors = hexGridMapColors;
                offset = new Vector2(longitudeAndLatitude.x * terSet.clusterSize, longitudeAndLatitude.y * terSet.clusterSize);
            }

            public void PaintHexGridMapTex(int index)
            {
                int fix = (fixedClsSize - clusterSize) / 2;
                int j = index / fixedClsSize - fix;
                int i = index % fixedClsSize - fix;

                Vector2 pos = new Vector2(i, j) + offset;
                Hexagon hex = HexHelper.PixelToAxialHex(pos, hexGridSize, true);

                HexAreaPointData hexData = HexHelper.GetPointHexArea(pos, hex, layout, innerHexRatio);
                HandleHexPointData(index, ref hexData, ref hex);
                //DebugUtility.DebugGameObject("", hexData.worldPos.TransToXZ(), EditorSceneManager.mapScene.hexClusterParentObj.transform);
            }

            private void HandleHexPointData(int idx, ref HexAreaPointData hexData, ref Hexagon hex)
            {
                // Get this hexGrid's type type
                Vector2Int OffsetHex = HexHelper.AxialToOffset(hex);
                byte terrainType = gridTerrainSO.GetGridTerrainDataIdx(OffsetHex);

                if (hexData.insideInnerHex)
                {
                    hexGridMapColors[idx] = gridTerrainSO.GetGridTerrainTypeColorByIdx(terrainType);
                }
                else
                {
                    BlendWithNeighborHex(terrainType, idx, ref hexData, ref hex);
                }
            }

            private void BlendWithNeighborHex(byte terrainType, int idx, ref HexAreaPointData hexData, ref Hexagon hex)
            {
                Hexagon neighborHex = hex.Hex_Neighbor(hexData.hexAreaDir);

                switch (hexData.outHexAreaEnum)
                {
                    case OutHexAreaEnum.Edge:
                        Vector2Int neighbor_offsetHex = HexHelper.AxialToOffset(neighborHex);
                        byte neighborType = gridTerrainSO.GetGridTerrainDataIdx(neighbor_offsetHex);
                        Color neighbor_Color = gridTerrainSO.GetGridTerrainTypeColorByIdx(neighborType);
                        Color hex_Color = gridTerrainSO.GetGridTerrainTypeColorByIdx(terrainType);
                        hexGridMapColors[idx] = MathUtil.ColorLinearLerp(hex_Color, neighbor_Color, hexData.ratioBetweenInnerAndOutter);
                        break;
                    case OutHexAreaEnum.LeftCorner:
                        // 根据推演,混合时 : cur + 1, neighbor + 2, next + 4 
                        Hexagon nextHex = hex.Hex_Neighbor(hexData.hexAreaDir + 1);
                        Vector2 l_A = hex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 1, innerHexRatio);
                        Vector2 l_B = neighborHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 3, innerHexRatio);
                        Vector2 l_C = nextHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 5, innerHexRatio);
                        Color l_a = gridTerrainSO.GetGridTerrainTypeColorByIdx(gridTerrainSO.GetGridTerrainDataIdx(HexHelper.AxialToOffset(hex)));
                        Color l_b = gridTerrainSO.GetGridTerrainTypeColorByIdx(gridTerrainSO.GetGridTerrainDataIdx(HexHelper.AxialToOffset(neighborHex)));
                        Color l_c = gridTerrainSO.GetGridTerrainTypeColorByIdx(gridTerrainSO.GetGridTerrainDataIdx(HexHelper.AxialToOffset(nextHex)));
                        hexGridMapColors[idx] = MathUtil.TriangleLerp(l_a, l_b, l_c, l_A, l_B, l_C, hexData.worldPos);
                        break;
                    case OutHexAreaEnum.RightCorner:
                        // 根据推演,混合时 : neighbor - 2, next - 4 
                        Hexagon preHex = hex.Hex_Neighbor(hexData.hexAreaDir - 1);
                        Vector2 r_A = hex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir, innerHexRatio);
                        Vector2 r_B = neighborHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir - 2, innerHexRatio);
                        Vector2 r_C = preHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir - 4, innerHexRatio);
                        Color r_a = gridTerrainSO.GetGridTerrainTypeColorByIdx(gridTerrainSO.GetGridTerrainDataIdx(HexHelper.AxialToOffset(hex)));
                        Color r_b = gridTerrainSO.GetGridTerrainTypeColorByIdx(gridTerrainSO.GetGridTerrainDataIdx(HexHelper.AxialToOffset(neighborHex)));
                        Color r_c = gridTerrainSO.GetGridTerrainTypeColorByIdx(gridTerrainSO.GetGridTerrainDataIdx(HexHelper.AxialToOffset(preHex)));
                        hexGridMapColors[idx] = MathUtil.TriangleLerp(r_a, r_b, r_c, r_A, r_B, r_C, hexData.worldPos);
                        break;
                }
            }

        }

        [FoldoutGroup("格子地形纹理导出")]
        [LabelText("cluster序号")]
        public List<Vector2Int> longitudeAndLatitudes;     // TODO : 要用上这个东西...

        [FoldoutGroup("格子地形纹理导出"), ReadOnly]
        [LabelText("导出位置")]
        public string exportHexMapDataPath = MapStoreEnum.GamePlayGridTerrainTexDataPath;

        [FoldoutGroup("格子地形纹理导出")]
        [Button("生成并保存 格子地形纹理图", ButtonSizes.Medium)]
        private void GenGridEdit()
        {
            // Remove repeated long-la val
            int genTexNum = longitudeAndLatitudes.Count;
            HashSet<Vector2Int> LALSets = new HashSet<Vector2Int>(genTexNum);
            for (int i = genTexNum - 1; i >= 0; i--)
            {
                Vector2Int longitudeAndLatitude = longitudeAndLatitudes[i];
                if (LALSets.Contains(longitudeAndLatitude))
                {
                    longitudeAndLatitudes.RemoveAt(i);
                }
                LALSets.Add(longitudeAndLatitude);
            }

            genTexNum = longitudeAndLatitudes.Count;
            int clusterSize = terSet.clusterSize;
            // NOTE : fixedClsSize 的存在是为了扩展采样，以便地块接缝的处理
            int fixedClsSize = terSet.fixedClusterSize; 
            List<Texture2D> gridTerrainTexList = new List<Texture2D>(genTexNum);
            for(int i = 0; i < genTexNum; i++)
            {
                Vector2Int longitudeAndLatitude = longitudeAndLatitudes[i];
                Texture2D gridTerrainTex = new Texture2D(fixedClsSize, fixedClsSize);
                List<Color> colors = new List<Color>(gridTerrainTex.GetPixels());

                // Init helper to help build GridTerrainTexture
                GridTerrainTexHelper gridMapTexHelper = new GridTerrainTexHelper(longitudeAndLatitude, fixedClsSize, terSet, hexSet, gridTerrainSO, colors);
                
                Parallel.ForEach(colors, (color, state, index) =>
                {
                    gridMapTexHelper.PaintHexGridMapTex((int)index);
                });

                gridTerrainTex.SetPixels(colors.ToArray());
                gridTerrainTex.Apply();
                gridTerrainTexList.Add(gridTerrainTex);
            }

            // Save all gened grid terrain texture
            for(int i = 0; i < gridTerrainTexList.Count; i++)
            {
                Vector2Int longitudeAndLatitude = longitudeAndLatitudes[i];
                Texture2D texture = gridTerrainTexList[i];
                DateTime dateTime = DateTime.Now;
                string texName = string.Format("GridTerrain_x{0}_y{1}_{2}x{2}_Batch{3}_{4}", longitudeAndLatitude.x, longitudeAndLatitude.y, terSet.clusterSize, i, dateTime.Ticks);
                TextureUtility.SaveTextureAsAsset(exportHexMapDataPath, texName, texture);
            }
            
            AssetDatabase.Refresh();
        }

        #endregion


        #region 山脉编辑

        [Serializable]
        public class MountainDataWrapper
        {
            [HorizontalGroup("MountainDataWrapper"), LabelText("编辑中"), ReadOnly]
            public bool IsEditing;

            [HorizontalGroup("MountainDataWrapper"), LabelText("锁定")]
            public bool LockEditing;

            //[HorizontalGroup("MountainDataWrapper"), LabelText("ID"), ReadOnly]
            public int MountainID { get; private set; }

            [HorizontalGroup("MountainDataWrapper"), LabelText("名称")]
            public string MountainName;

            [HorizontalGroup("MountainDataWrapper"), LabelText("颜色")]
            public Color MountainEdtingColor;

            public MountainData mountainData { get; private set; }

            Action<int, MountainData> PaintMountainEvent;
            Action<int, MountainData> DeleteMountainEvent;

            public MountainDataWrapper() { mountainData = null; }

            public MountainDataWrapper(MountainData mountainData, Action<int, MountainData> PaintMountainEvent, Action<int, MountainData> DeleteMountainEvent)
            {
                IsEditing = false;
                this.mountainData = mountainData;
                MountainID = mountainData.MountainID;
                MountainName = mountainData.MountainName;
                MountainEdtingColor = MapColorUtil.GetRandomColor(MountainID);
                this.PaintMountainEvent = PaintMountainEvent;
                this.DeleteMountainEvent = DeleteMountainEvent;
            }

            [HorizontalGroup("MountainDataWrapper")]
            [Button("涂刷山脉")]
            private void PaintCurMountain()
            {
                if (PaintMountainEvent == null)
                {
                    Debug.LogError("Mountain event is null");
                    return;
                }
                PaintMountainEvent.Invoke(MountainID, mountainData);
                IsEditing = true;
            }

            [HorizontalGroup("MountainDataWrapper")]
            [Button("删除")]
            private void DeleteMountain()
            {
                if (DeleteMountainEvent == null)
                {
                    Debug.LogError("Delete event is null");
                    return;
                }
                DeleteMountainEvent.Invoke(MountainID, mountainData);
            }

            [HorizontalGroup("MountainDataWrapper")]
            [Button("配置")]
            private void ConfigMountain()
            {
                Action<MountainNoiseData> ConfirmMountainEdit = (noiseData) =>
                {
                    mountainData.MountainNoiseData = noiseData;
                };

                MountainNoiseSubWindow.GetPopInstance().ShowSubWindow(mountainData.MountainNoiseData, ConfirmMountainEdit, null);
            }

            public void SyncMountainData()
            {
                mountainData.MountainName = this.MountainName;
            }

            public bool IsMountainValid()
            {
                return mountainData.IsMountainValid();
            }
        }

        [FoldoutGroup("山脉编辑")]
        public bool IsEditingMountain = false;
        bool IsNotEditingMountain => !IsEditingMountain;

        [FoldoutGroup("山脉编辑")]
        [GUIColor(1f, 1f, 0f)]
        [ShowIf("IsNotEditingMountain")]
        [LabelText("警告: "), ReadOnly]
        public string warningEditMountainNotValid = "当前并不在编辑山脉";

        private void UpdateEditingMountain()
        {
            if (!BaseGridTerrain.IsMountain(CurGridTerrain) || CurEditingMountain == null)
            {
                // If not mountain / mountain data is null
                if (IsEditingMountain)
                {
                    Parallel.ForEach(MountainDataWrappers, (wrapper) =>
                    {
                        wrapper.IsEditing = false;
                    });
                    Debug.LogError($"Wrong statu, cur editing mountain is null or cur terrain name is not mountain!");
                    IsEditingMountain = false;
                    // Switch to gridTerrain scene
                    hexmapDataTexManager.SwitchToTexCache(0);
                }
                return;
            }

            bool enterFlag = false;
            if (BaseGridTerrain.IsMountain(CurCurGridTerrainTypeName) && CurEditingMountain != null)
            {
                enterFlag = true;
            }
            bool changeFlag = IsEditingMountain != enterFlag;
            IsEditingMountain = enterFlag;
            Debug.Log($"Change editing mountain statu, edit statu : {enterFlag}");

            // If not change, do nothing
            if (!changeFlag)
            {
                return;
            }

            // Refresh scene hexmap, load gridTerrain (0) or mountain (1)
            if (IsEditingMountain)
            {
                hexmapDataTexManager.SwitchToTexCache(1);
            }
            else
            {
                hexmapDataTexManager.SwitchToTexCache(0);
            }
        }

        int LastEditingWrapperIdx = -1;

        MountainData CurEditingMountain;        // TODO : use it in painting

        [FoldoutGroup("山脉编辑")]
        [LabelText("山脉列表")]
        public List<MountainDataWrapper> MountainDataWrappers = new List<MountainDataWrapper>();

        Dictionary<int, MountainDataWrapper> MountainID_Wrapper_Dict = new Dictionary<int, MountainDataWrapper>();

        [FoldoutGroup("山脉编辑")]
        [Button("新增山脉", ButtonSizes.Medium)]
        private void AddMountainData()
        {
            if (gridTerrainSO == null)
            {
                Debug.LogError("Grid Terrain SO is null!");
                return;
            }
            int curIdx = MountainDataWrappers.Count;
            MountainData mountainData = gridTerrainSO.GetNewMountainData();
            MountainDataWrapper wrapper = new MountainDataWrapper(mountainData, PaintMountainData, DeleteMountainData);
            MountainDataWrappers.Add(wrapper);
            UpdateMountainData();
        }

        [FoldoutGroup("山脉编辑")]
        [Button("保存山脉", ButtonSizes.Medium)]
        private void SaveMountainDatas()
        {
            if (gridTerrainSO == null)
            {
                Debug.LogError("Grid Terrain SO is null!");
                return;
            }
            List<MountainData> validMountainDatas = new List<MountainData>(MountainDataWrappers.Count);
            foreach (var wrapper in MountainDataWrappers)
            {
                wrapper.SyncMountainData();
                if (wrapper.IsMountainValid())
                {
                    validMountainDatas.Add(wrapper.mountainData);
                }
            }
            gridTerrainSO.SaveMountainData(validMountainDatas);

            EditorUtility.SetDirty(gridTerrainSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UpdateHexTexManager();
            Debug.Log($"Save mountain data success, valid mountain num : {validMountainDatas.Count}");
        }

        private void PaintMountainData(int mountainID, MountainData mountainData)
        {
            // Force change cur terrain type
            Parallel.ForEach(MountainDataWrappers, (wrapper) =>
            {
                wrapper.IsEditing = false;
            });
            CurCurGridTerrainTypeName = BaseGridTerrain.GetMountainName();


            CurEditingMountain = mountainData;
            CurEditingMountain.UpdateMountainData();
            OnCurGriTerrainChanged();
        }

        private void DeleteMountainData(int mountainID, MountainData mountainData)
        {
            for(int i = MountainDataWrappers.Count - 1; i >= 0; i--)
            {
                if (MountainDataWrappers[i].MountainID == mountainID)
                {
                    MountainDataWrappers.RemoveAt(i);
                    MountainID_Wrapper_Dict.Remove(mountainID);
                    break;
                }
            }
            UpdateMountainData();
        }

        #endregion

        protected override void OnKeyCodeW()
        {
            base.OnKeyCodeW();
            GetPreTerrainType();
        }

        protected override void OnKeyCodeS()
        {
            base.OnKeyCodeS();
            GetNextTerrainType();
        }

        protected override bool EnablePaintHex(Vector2Int offsetHexPos) 
        {
            // If Cur using mountain and do not set mountain data, disable paint
            if (gridTerrainSO == null)
            {
                Debug.LogError("Grid Terrain SO is null!");
                return false;
            }

            if (IsEditingMountain && CurEditingMountain == null)
            {
                Debug.LogError("Editing Mountain but have not data!");
                return false;
            }

            if (!IsEditingMountain)
            {
                return true;
            }

            bool IsMountain = gridTerrainSO.GetGridIsMountain(offsetHexPos);
            if (IsMountain)
            {
                // If mountain wrapper is locked, can not mod it
                int mountainID = gridTerrainSO.GetGridMountainID(offsetHexPos);
                MountainID_Wrapper_Dict.TryGetValue(mountainID, out MountainDataWrapper wrapper);
                if(wrapper != null && wrapper.LockEditing)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void PaintHexRTEvent(List<Vector2Int> offsetHexList)
        {
            base.PaintHexRTEvent(offsetHexList);
            if (gridTerrainSO == null)
            {
                Debug.LogError("hexMapSO is null");
                return;
            }
            if(CurGridTerrain == null)
            {
                Debug.Log("CurGriTerrain is null");
                return;
            }

            byte idx = (byte)GetIdxByCurGridTerrain();
            gridTerrainSO.UpdateGridTerrainData(offsetHexList, idx);

            // TODO : 颜色 要同步地 刷到 hexdatamanager 当中，要完成两个层次的同步

            if (IsEditingMountain)
            {
                CurEditingMountain.AddMountainGrid(offsetHexList);
            }
        }

        protected override Color PaintHexGridWhenLoad(int index)
        {
            if (gridTerrainSO == null)
            {
                Debug.LogError("hexMapSO is null");
                return Color.white;
            }
            int i = index / hexSet.mapWidth;
            int j = index % hexSet.mapHeight;
            Vector2Int offsetHex = new Vector2Int(j, i);
            byte terrainTypeIdx = gridTerrainSO.GetGridTerrainDataIdx(offsetHex);
            return gridTerrainSO.GetGridTerrainTypeColorByIdx(terrainTypeIdx);
        }

        private byte GetIdxByCurGridTerrain()
        {
            for(int i = 0; i < TerrainTypesList.Count; i++)
            {
                if (CurGridTerrain.terrainTypeName == TerrainTypesList[i].terrainTypeName)
                {
                    return (byte)i;
                }
            }
            return 0;
        }

    }
}
