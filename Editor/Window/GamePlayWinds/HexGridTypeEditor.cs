using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class HexGridTypeEditor : BrushHexmapEditor
    {
        public override string EditorName => MapEditorEnum.HexGridTypeEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();
            LoadTerrainType();
            LoadHexMapSO();
            Debug.Log("init hex grid type Editor over !");
        }

        //SerializedObject serializedGridTerrainSO;
        GridTerrainSO gridTerrainSO;

        List<GridTerrainLayer> layerList;
        List<GridTerrainType> terrainTypeList;

        private void LoadTerrainType()
        {
            gridTerrainSO = GridTerrainSO.GetInstance();
            gridTerrainSO.UpdateTerSO();

            TerrainLayersList.Clear();
            TerrainTypesList.Clear();

            // load base list and etc
            foreach (var layer in gridTerrainSO.GridLayerList)
            {
                TerrainLayersList.Add(layer.CopyObject());
            }

            foreach (var type in gridTerrainSO.GridTypeList)
            {
                TerrainTypesList.Add(type.CopyObject());
            }
            //Debug.Log($"load terrain types over, TerrainLayersList : {TerrainLayersList.Count}, TerrainTypesList : {TerrainTypesList.Count}");
        }

        private void LoadHexMapSO()
        {
            string soName = $"RawHexMap_{hexSet.mapWidth}x{hexSet.mapHeight}_{UnityEngine.Random.Range(0, 100)}.asset";
            string RawHexPath = exportHexMapDataPath + $"/{soName}";
            if (hexMapSO == null)
            {
                hexMapSO = AssetDatabase.LoadAssetAtPath<HexMapSO>(RawHexPath);
                if (hexMapSO == null)
                {
                    hexMapSO = CreateInstance<HexMapSO>();
                    AssetDatabase.CreateAsset(hexMapSO, RawHexPath);
                    Debug.Log($"successfully create Hex Map, path : {RawHexPath}");
                }
            }
            hexMapSO.InitRawHexMap(EditorSceneManager.hexSet.mapWidth, EditorSceneManager.hexSet.mapHeight);
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
        public string CurGridTerrainName;

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
            CurGridTerrain = gridTerrainSO.GetTerrainType(CurGridTerrainName);
            SetBrushColor(CurGridTerrain.terrainEditColor);
            Debug.Log($"now you choose terrain type : {CurGridTerrain.terrainTypeName}");
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

        }

        #endregion


        #region 格子地形数据导出

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
                        // 根据推演,混合时 : cur + 1, neighbor + 2, next + 4 
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
                        // 根据推演,混合时 : neighbor - 2, next - 4 
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

        [FoldoutGroup("格子地形数据导出")]
        [LabelText("当前格子数据")]
        public HexMapSO hexMapSO;

        [FoldoutGroup("格子地形数据导出")]
        [LabelText("grid地形颜色纹理")]
        public Texture2D hexMapTexture;         // Storage all grid's terrain color in a texture (cluster size)

        [FoldoutGroup("格子地形数据导出")]
        [LabelText("cluster序号")]
        public Vector2Int longitudeAndLatitude;     // TODO : 要用上这个东西...

        [FoldoutGroup("格子地形数据导出")]
        [LabelText("导出位置")]
        public string exportHexMapDataPath = MapStoreEnum.TerrainHexmapGridDataPath;

        [FoldoutGroup("格子地形数据导出")]
        [Button("生成格子编辑结果", ButtonSizes.Medium)]
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
                colors, hexMapSO.GetGridTerrainData, GetGridTerrainTypeColorByIdx);

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

        [FoldoutGroup("格子地形数据导出")]
        [Button("重置格子编辑结果", ButtonSizes.Medium)]
        private void ResetGridEdit()
        {
            LoadTerrainType();
        }

        [FoldoutGroup("格子地形数据导出")]
        [Button("保存格子编辑结果", ButtonSizes.Medium)]
        private void SaveGridEdit()
        {
            DateTime dateTime = DateTime.Now;
            string texName = string.Format("hexGridColorMap{0}x{0}_{1}", terSet.clusterSize, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(exportHexMapDataPath, texName, hexMapTexture);
        }
        
        #endregion

        
        protected override void PaintHexRTEvent(List<Vector2Int> offsetHexList)
        {
            base.PaintHexRTEvent(offsetHexList);
            if (hexMapSO == null)
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
            hexMapSO.UpdateGridTerrainData(offsetHexList, idx);
        }

        protected override Color PaintHexGridWhenLoad(int index)
        {
            if (hexMapSO == null)
            {
                Debug.LogError("hexMapSO is null");
                return Color.white;
            }
            int i = index / hexSet.mapWidth;
            int j = index % hexSet.mapHeight;
            Vector2Int offsetHex = new Vector2Int(j, i);
            byte terrainTypeIdx = hexMapSO.GetGridTerrainData(offsetHex);
            return GetGridTerrainTypeColorByIdx(terrainTypeIdx);
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

        private Color GetGridTerrainTypeColorByIdx(byte i)
        {
            return TerrainTypesList[i].terrainEditColor;
        }

    }
}
