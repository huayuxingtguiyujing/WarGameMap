using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.HexStruct;
using LZ.WarGameMap.Runtime.Model;
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
            FindOrCreateSO<GridTerrainSO>(ref gridTerrainSO, MapStoreEnum.GamePlayGridTerrainDataPath, "GridTerrainSO_Default.asset");
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
            MountainID_Wrapper_Dict.Clear();
            int index = 0;
            foreach (var mountainData in gridTerrainSO.MountainDatas)
            {
                MountainDataWrapper wrapper = new MountainDataWrapper(mountainData, PaintMountainData, DeleteMountainData, ConfigMountainData);
                MountainDataWrappers.Add(wrapper);
                MountainID_Wrapper_Dict.Add(mountainData.MountainID, wrapper);
                index++;
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


        #region ���ӵ��α༭

        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("�༭ʹ�õĵ���")]
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
        }

        private void GetPreTerrainType()
        {
            CurGridTerrainTypeIndex--;
            FixCurTerrainTypeIdx();
            CurCurGridTerrainTypeName = TerrainTypesList[CurGridTerrainTypeIndex].terrainTypeName;
            SetTerrainTypeByCurIdx();
        }

        private void GetNextTerrainType()
        {
            CurGridTerrainTypeIndex++;
            FixCurTerrainTypeIdx();
            CurCurGridTerrainTypeName = TerrainTypesList[CurGridTerrainTypeIndex].terrainTypeName;
            SetTerrainTypeByCurIdx();
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
            SetBrushColor(CurGridTerrain.terrainEditColor);
            Debug.Log($"Now choose terrain type : {CurGridTerrain.terrainTypeName}");
        }


        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("���ӵ���Դ����SO")]
        public GridTerrainSO gridTerrainSO;

        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("��ʾָ��Layer�ĵ���")]
        [ValueDropdown("GetTerrainLayersList")]
        [OnValueChanged("OnCurFilterLayerChanged")]
        public string CurFilterLayerName;

        string AllLayerFilter = "���в㼶";

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


        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("���β㼶")] 
        public List<GridTerrainLayer> TerrainLayersList = new List<GridTerrainLayer>();

        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("�����б�")]
        public List<GridTerrainType> TerrainTypesList = new List<GridTerrainType>();

        [FoldoutGroup("���ӵ������ݱ༭")]
        [Button("�����������", ButtonSizes.Medium)]
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


        #region ���ӵ������ݵ���

        class HexGridMapTexHelper
        {
            Layout layout;
            int OuputTextureResolution;
            int hexGridSize;
            float innerHexRatio;
            Vector2 offset;

            List<Color> hexGridMapColors;

            Func<Vector2Int, byte> GetTerrainTypeFunc;
            Func<byte, Color> GetColorByTypeFunc;

            public HexGridMapTexHelper(Layout layout, int ouputTextureResolution, int hexGridSize, float innerHexRatio, Vector2 offset,
                List<Color> hexGridMapColors, Func<Vector2Int, byte> getTerrainTypeFunc, Func<byte, Color> getColorByTypeFunc)
            {
                this.layout = layout;
                OuputTextureResolution = ouputTextureResolution;
                this.hexGridSize = hexGridSize;
                this.innerHexRatio = innerHexRatio;
                this.hexGridMapColors = hexGridMapColors;
                this.offset = offset;

                GetTerrainTypeFunc = getTerrainTypeFunc;
                GetColorByTypeFunc = getColorByTypeFunc;
            }

            public void PaintHexGridMapTex(int index)
            {
                int j = index / OuputTextureResolution;
                int i = index % OuputTextureResolution;

                Vector2 pos = new Vector2(i, j);
                Hexagon hex = HexHelper.PixelToAxialHex(pos, hexGridSize, true);

                HexAreaPointData hexData = HexHelper.GetPointHexArea(pos, hex, layout, innerHexRatio);
                HandleHexPointData(index, ref hexData, ref hex);
                //DebugUtility.DebugGameObject("", hexData.worldPos.TransToXZ(), EditorSceneManager.mapScene.hexClusterParentObj.transform);
            }

            private void HandleHexPointData(int idx, ref HexAreaPointData hexData, ref Hexagon hex)
            {
                // get this hexGrid's type type
                Vector2Int OffsetHex = HexHelper.AxialToOffset(hex);
                byte terrainType = GetTerrainTypeFunc(OffsetHex);

                if (hexData.insideInnerHex)
                {
                    hexGridMapColors[idx] = GetColorByTypeFunc(terrainType);
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
                        byte neighborType = GetTerrainTypeFunc(neighbor_offsetHex);
                        Color neighbor_Color = GetColorByTypeFunc(neighborType);
                        Color hex_Color = GetColorByTypeFunc(terrainType);
                        hexGridMapColors[idx] = MathUtil.ColorLinearLerp(hex_Color, neighbor_Color, hexData.ratioBetweenInnerAndOutter);
                        break;
                    case OutHexAreaEnum.LeftCorner:
                        // ��������,���ʱ : cur + 1, neighbor + 2, next + 4 
                        Hexagon nextHex = hex.Hex_Neighbor(hexData.hexAreaDir + 1);
                        Vector2 l_A = hex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 1, innerHexRatio);
                        Vector2 l_B = neighborHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 3, innerHexRatio);
                        Vector2 l_C = nextHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir + 5, innerHexRatio);
                        Color l_a = GetColorByTypeFunc(GetTerrainTypeFunc(HexHelper.AxialToOffset(hex)));
                        Color l_b = GetColorByTypeFunc(GetTerrainTypeFunc(HexHelper.AxialToOffset(neighborHex)));
                        Color l_c = GetColorByTypeFunc(GetTerrainTypeFunc(HexHelper.AxialToOffset(nextHex)));
                        hexGridMapColors[idx] = MathUtil.TriangleLerp(l_a, l_b, l_c, l_A, l_B, l_C, hexData.worldPos);
                        break;
                    case OutHexAreaEnum.RightCorner:
                        // ��������,���ʱ : neighbor - 2, next - 4 
                        Hexagon preHex = hex.Hex_Neighbor(hexData.hexAreaDir - 1);
                        Vector2 r_A = hex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir, innerHexRatio);
                        Vector2 r_B = neighborHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir - 2, innerHexRatio);
                        Vector2 r_C = preHex.Get_Hex_CornerPos(layout, (int)hexData.hexAreaDir - 4, innerHexRatio);
                        Color r_a = GetColorByTypeFunc(GetTerrainTypeFunc(HexHelper.AxialToOffset(hex)));
                        Color r_b = GetColorByTypeFunc(GetTerrainTypeFunc(HexHelper.AxialToOffset(neighborHex)));
                        Color r_c = GetColorByTypeFunc(GetTerrainTypeFunc(HexHelper.AxialToOffset(preHex)));
                        hexGridMapColors[idx] = MathUtil.TriangleLerp(r_a, r_b, r_c, r_A, r_B, r_C, hexData.worldPos);
                        break;
                }
            }

        }

        [FoldoutGroup("���ӵ������ݵ���")]
        [LabelText("grid������ɫ����")]
        public Texture2D hexMapTexture;         // Storage all grid's terrain color in a texture (cluster size)

        [FoldoutGroup("���ӵ������ݵ���")]
        [LabelText("cluster���")]
        public Vector2Int longitudeAndLatitude;     // TODO : Ҫ�����������...

        [FoldoutGroup("���ӵ������ݵ���")]
        [LabelText("����λ��")]
        public string exportHexMapDataPath = MapStoreEnum.GamePlayGridTerrainDataPath;

        [FoldoutGroup("���ӵ������ݵ���")]
        [Button("���ɸ��ӱ༭���", ButtonSizes.Medium)]
        private void GenGridEdit()
        {
            if(hexMapTexture != null)
            {
                GameObject.DestroyImmediate(hexMapTexture);
            }
            int clusterSize = terSet.clusterSize;
            hexMapTexture = new Texture2D(clusterSize, clusterSize);
            List<Color> colors = new List<Color>(hexMapTexture.GetPixels());

            // Init helper
            Vector2 offset = new Vector2(longitudeAndLatitude.x * clusterSize, longitudeAndLatitude.y * clusterSize);
            HexGridMapTexHelper gridMapTexHelper = new HexGridMapTexHelper(
                hexSet.GetScreenLayout(), clusterSize, hexSet.hexGridSize,  0.7f, offset,
                colors, gridTerrainSO.GetGridTerrainDataIdx, gridTerrainSO.GetGridTerrainTypeColorByIdx);

            // Do not delete
            //for(int i = 0; i < clusterSize; i++)
            //{
            //    for(int j = 0; j < clusterSize; j++)
            //    {
            //        int index = i * clusterSize + j;
            //        gridMapTexHelper.PaintHexGridMapTex((int)index);
            //    }
            //}

            Parallel.ForEach(colors, (color, state, index) =>
            {
                gridMapTexHelper.PaintHexGridMapTex((int)index);
            });

            hexMapTexture.SetPixels(colors.ToArray());
            hexMapTexture.Apply();
        }

        [FoldoutGroup("���ӵ������ݵ���")]
        [Button("���ø��ӱ༭���", ButtonSizes.Medium)]
        private void ResetGridEdit()
        {
            LoadTerrainType();
        }

        [FoldoutGroup("���ӵ������ݵ���")]
        [Button("������ӱ༭���", ButtonSizes.Medium)]
        private void SaveGridEdit()
        {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("hexGridColorMap{0}x{0}_{1}", terSet.clusterSize, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(exportHexMapDataPath, texName, hexMapTexture);
        }

        #endregion


        #region ɽ���༭

        [Serializable]
        public class MountainDataWrapper
        {
            [HorizontalGroup("MountainDataWrapper"), LabelText("�༭��"), ReadOnly]
            public bool IsEditing;

            // TODO : ������������޸�״̬��������Ϳˢ������
            [HorizontalGroup("MountainDataWrapper"), LabelText("����")]
            public bool LockEditing;

            [HorizontalGroup("MountainDataWrapper"), LabelText("ID"), ReadOnly]
            public int MountainID;

            [HorizontalGroup("MountainDataWrapper"), LabelText("����")]
            public string MountainName;

            public MountainData mountainData { get; private set; }

            Action<int, MountainData> PaintMountainEvent;
            Action<int, MountainData> DeleteMountainEvent;
            Action<int, MountainData> ConfigMountainEvent;

            public MountainDataWrapper() { mountainData = null; }

            public MountainDataWrapper(MountainData mountainData, Action<int, MountainData> PaintMountainEvent, Action<int, MountainData> DeleteMountainEvent, Action<int, MountainData> ConfigMountainEvent)
            {
                IsEditing = false;
                this.mountainData = mountainData;
                MountainID = mountainData.MountainID;
                MountainName = mountainData.MountainName;
                this.PaintMountainEvent = PaintMountainEvent;
                this.DeleteMountainEvent = DeleteMountainEvent;
                this.ConfigMountainEvent = ConfigMountainEvent;
            }

            [HorizontalGroup("MountainDataWrapper")]
            [Button("Ϳˢɽ��")]
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
            [Button("ɾ��")]
            private void DeleteMountain()
            {
                if (DeleteMountainEvent == null)
                {
                    Debug.LogError("Mountain event is null");
                    return;
                }
                DeleteMountainEvent.Invoke(MountainID, mountainData);
            }

            [HorizontalGroup("MountainDataWrapper")]
            [Button("����")]
            private void ConfigMountain()
            {
                if (ConfigMountainEvent == null)
                {
                    Debug.LogError("Paint mountain event is null");
                    return;
                }
                ConfigMountainEvent.Invoke(MountainID, mountainData);
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


        bool IsEditingMountain = false;
        bool IsNotEditingMountain => !IsEditingMountain;

        [FoldoutGroup("ɽ���༭")]
        [GUIColor(1f, 1f, 0f)]
        [ShowIf("IsNotEditingMountain")]
        [LabelText("����: "), ReadOnly]
        public string warningEditMountainNotValid = "��ǰ�����ڱ༭ɽ��";

        private void UpdateEditingMountain(bool enterFlag = false)
        {
            if (CurCurGridTerrainTypeName != BaseGridTerrainTypes.MountainType.terrainTypeName || CurEditingMountain == null)
            {
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
            bool changeFlag = IsEditingMountain != enterFlag;
            IsEditingMountain = enterFlag;
            Debug.Log($"Change editing mountain statu, edit statu : {enterFlag}");

            // Do not change, so do nothing
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

        [FoldoutGroup("ɽ���༭")]
        [LabelText("ɽ���б�")]
        public List<MountainDataWrapper> MountainDataWrappers = new List<MountainDataWrapper>();

        Dictionary<int, MountainDataWrapper> MountainID_Wrapper_Dict = new Dictionary<int, MountainDataWrapper>();

        [FoldoutGroup("ɽ���༭")]
        [Button("����ɽ��", ButtonSizes.Medium)]
        private void AddMountainData()
        {
            if (gridTerrainSO == null)
            {
                Debug.LogError("Grid Terrain SO is null!");
                return;
            }
            int curIdx = MountainDataWrappers.Count;
            MountainData mountainData = gridTerrainSO.GetNewMountainData();
            MountainDataWrapper wrapper = new MountainDataWrapper(mountainData, PaintMountainData, DeleteMountainData, ConfigMountainData);
            MountainDataWrappers.Add(wrapper);
        }

        [FoldoutGroup("ɽ���༭")]
        [Button("����ɽ��", ButtonSizes.Medium)]
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
            CurCurGridTerrainTypeName = BaseGridTerrainTypes.MountainType.terrainTypeName;
            OnCurGriTerrainChanged();
            CurEditingMountain = mountainData;

            CurEditingMountain.UpdateMountainData();
            UpdateEditingMountain(true);
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
        }

        // TODO : ��һ��ɽ���������ô���
        private void ConfigMountainData(int mountainID, MountainData mountainData)
        {

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

        // TODO : TEST IT
        protected override bool EnablePaintHex(Vector2Int offsetHexPos) 
        {
            // If Cur using mountain and do not set mountain data, disable paint
            if (gridTerrainSO == null)
            {
                Debug.LogError("Grid Terrain SO is null!");
                return false;
            }

            if (!IsEditingMountain)
            {
                return true;
            }

            if (IsEditingMountain && CurEditingMountain == null)
            {
                Debug.LogError("Editing Mountain but have not data!");
                return false;
            }

            bool IsMountain = gridTerrainSO.GetGridIsMountain(offsetHexPos);
            if (IsMountain)
            {
                // If mountain wrapper is locked, can not mod it
                int mountainID = gridTerrainSO.GetGridMountainID(offsetHexPos);
                MountainDataWrapper wrapper = MountainID_Wrapper_Dict[mountainID];
                if (wrapper.LockEditing)
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

            // TODO : ��ɫ Ҫͬ���� ˢ�� hexdatamanager ���У�Ҫ���������ε�ͬ��

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
