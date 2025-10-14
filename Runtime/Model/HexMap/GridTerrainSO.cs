using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class BaseGridTerrainTypes
    {
        public static GridTerrainLayer SeaLayer =       new GridTerrainLayer(0, "海洋层", "", true);
        public static GridTerrainLayer BaseLayer =      new GridTerrainLayer(1, "基本地貌层", "", true);
        public static GridTerrainLayer LandformLayer =  new GridTerrainLayer(2, "叠加地貌层", "", true);
        public static GridTerrainLayer DecorateLayer =  new GridTerrainLayer(3, "装饰层", "", true);
        public static GridTerrainLayer DynamicLayer =   new GridTerrainLayer(4, "动态层", "", true);

        public static GridTerrainType ShallowSeaType =  new GridTerrainType(0, "ShallowSea", "浅海", new Color(0.125f, 0.698f, 0.667f, 1.0f), true);
        public static GridTerrainType DeepSeaType =     new GridTerrainType(0, "DeepSea", "深海", new Color(0, 0.47f, 0.62f, 1.0f), true);

        public static GridTerrainType PlainType =       new GridTerrainType(1, "Plain",     "平原",   new Color(0.55f, 0.75f, 0.45f, 1.0f), true);
        public static GridTerrainType HillType =        new GridTerrainType(1, "Hill",      "丘陵",   new Color(0.40f, 0.60f, 0.30f, 1.0f), true);
        public static GridTerrainType MountainType =    new GridTerrainType(1, "Mountain",  "山脉",   new Color(0.50f, 0.45f, 0.40f, 1.0f), true);
        public static GridTerrainType PlateauType =     new GridTerrainType(1, "Plateau",   "高原",   new Color(0.70f, 0.60f, 0.35f, 1.0f), true);
        public static GridTerrainType SnowType =        new GridTerrainType(1, "Snow",      "雪地",   new Color(1, 1, 1, 1.0f), true);
    }

    // Storage the setting of GridTerrain
    // Terrain layers : 地形的层级
    // Terrain types : 地形的种类
    // Grid Terrain : cv like map, hold all grid's terrainData in hex map
    // Size of HexMap : 3000 * 3000, need lazy load
    public class GridTerrainSO : ScriptableObject
    {
        static GridTerrainSO instance;

        public static GridTerrainSO GetInstance()
        {
            if (instance == null)
            {
                //instance = AssetDatabase.LoadAssetAtPath<GridTerrainSO>(assetPath);
                //if (instance == null)
                //{
                //    instance = CreateInstance<GridTerrainSO>();
                //    AssetDatabase.CreateAsset(instance, assetPath);
                //    AssetDatabase.SaveAssets();
                //    AssetDatabase.Refresh();
                //    Debug.Log($"[GridTerrainSO] Created asset at {MapStoreEnum.TerrainHexmapGridDataPath}");
                //}
            }
            return instance;
        }

        [Header(" TerrainLayer TerrainType Data ")]
        [LabelText("当前地形层数目")]
        public int CurLayerNum;

        [LabelText("地形层级列表")]
        public List<GridTerrainLayer> GridTerrainLayerList = new List<GridTerrainLayer>();

        [LabelText("地形种类列表")]
        public List<GridTerrainType> GridTerrainTypeList = new List<GridTerrainType>();


        // For cache terrain info
        Dictionary<int, GridTerrainLayer> GridLayerDict_LayerOrder = new Dictionary<int, GridTerrainLayer>();

        Dictionary<string, GridTerrainLayer> GridLayerDict_LayerName = new Dictionary<string, GridTerrainLayer>();

        Dictionary<string, GridTerrainType> GridTypeDict_Name = new Dictionary<string, GridTerrainType>();

        Dictionary<string, GridTerrainType> GridTypeDict_ChineseName = new Dictionary<string, GridTerrainType>();

        Dictionary<int, List<GridTerrainType>> GridLayer_TypeDict = new Dictionary<int, List<GridTerrainType>>();

        bool isTerTypeInit = false;

        
        // Mountain Data
        [Header(" Mountain Data ")]
        public List<MountainData> MountainDatas = new List<MountainData>();

        Dictionary<int, MountainData> MountainID_DataDict = new Dictionary<int, MountainData>();

        // Hex map grid offset coord -> mountain id dict
        Dictionary<Vector2Int, int> GridOffset_MountainID_Dict = new Dictionary<Vector2Int, int>();

        [LabelText("hex地图宽度"), ReadOnly]
        public int MountainCounter = 0;


        // Hex Grid Terrain Data
        [Header(" Hex Grid Terrain Data ")]
        [LabelText("hex地图宽度"), ReadOnly]
        public int mapWidth;

        [LabelText("hex地图高度"), ReadOnly]
        [Tooltip("由hexsetting决定，勿要更改")]
        public int mapHeight;

        [LabelText("地形格-类型 列表")]
        public List<GridTerrainData> HexmapGridTerDataList;   // TODO : lazy load   // TODO : UNCOMPLETE

        [LabelText("地形格-类型索引 列表")]
        public List<byte> HexmapGridTerTypeList = new List<byte>();

        public int GridCount => HexmapGridTerDataList.Count;

        public bool IsHexmapInit = false;        // Use to reset hex map data in inspector


        public void UpdateTerSO(int width, int height)
        {
            if (!isTerTypeInit)
            {
                // Check and add base type
                GridTerrainLayerList = new List<GridTerrainLayer>
                {
                    BaseGridTerrainTypes.SeaLayer,
                    BaseGridTerrainTypes.BaseLayer,
                    BaseGridTerrainTypes.LandformLayer,
                    BaseGridTerrainTypes.DecorateLayer,
                    BaseGridTerrainTypes.DynamicLayer
                };
                GridTerrainTypeList = new List<GridTerrainType>
                {
                    BaseGridTerrainTypes.ShallowSeaType,
                    BaseGridTerrainTypes.DeepSeaType,
                    
                    BaseGridTerrainTypes.PlainType,
                    BaseGridTerrainTypes.HillType,
                    BaseGridTerrainTypes.MountainType,
                    BaseGridTerrainTypes.PlateauType,
                    BaseGridTerrainTypes.SnowType
                };
                isTerTypeInit = true;
            }

            CheckGridTerInfo();
            UpdateInfoDict();
            UpdateHexmapGrids(width, height);
            UpdateMountainGrids();
            Debug.Log($" Updated grid terrain SO, layer : {GridTerrainLayerList.Count}, types : {GridTerrainTypeList.Count}");
        }

        // Use it to check if all terrain type info are correct
        private bool CheckGridTerInfo()
        {
            if (!isTerTypeInit)
            {
                Debug.LogError("GridTerrain not inited!");
                return false;
            }

            // All terrainType's layer must be correct, auto fix if not
            HashSet<int> validLayer = new HashSet<int>(GridTerrainLayerList.Count);
            foreach (var layer in GridTerrainLayerList)
            {
                validLayer.Add(layer.layerOrder);
            }

            // Will not check layer name... im lazy
            HashSet<string> CurLayerNames = new HashSet<string>(GridTerrainLayerList.Count);

            // Name and chinese name and terrain color can not be the same 
            HashSet<string> CurTerTypeNames = new HashSet<string>(GridTerrainTypeList.Count);
            HashSet<string> CurTerTypeChineseNames = new HashSet<string>(GridTerrainTypeList.Count);
            Action<List<GridTerrainType>> checkGridTerTypeList = (terrainTypeList) =>
            {
                foreach (var type in terrainTypeList)
                {
                    if (CurTerTypeNames.Contains(type.terrainTypeName))
                    {
                        type.terrainTypeName = type.terrainTypeName + "_fix";
                    }
                    if (CurTerTypeChineseNames.Contains(type.terrainTypeChineseName))
                    {
                        type.terrainTypeChineseName = type.terrainTypeChineseName + "_fix";
                    }
                    if (!validLayer.Contains(type.terrainTypeLayer))
                    {
                        type.terrainTypeLayer = 0;
                    }
                }
            };
            checkGridTerTypeList(GridTerrainTypeList);
            return true;
        }

        private void UpdateInfoDict()
        {
            GridLayerDict_LayerOrder.Clear();
            GridLayerDict_LayerName.Clear();
            Action<List<GridTerrainLayer>> buildLayerDict = (layerList) =>
            {
                foreach (var layers in layerList)
                {
                    GridLayerDict_LayerOrder.TryAdd(layers.layerOrder, layers);
                    GridLayerDict_LayerName.TryAdd(layers.layerName, layers);

                    GridLayer_TypeDict.TryAdd(layers.layerOrder, new List<GridTerrainType>());
                }
            };
            buildLayerDict(GridTerrainLayerList);

            GridTypeDict_Name.Clear();
            GridTypeDict_ChineseName.Clear();
            Action<List<GridTerrainType>> buildTypeDict = (typeList) =>
            {
                foreach (var type in typeList)
                {
                    GridTypeDict_Name.Add(type.terrainTypeName, type);
                    GridTypeDict_ChineseName.Add(type.terrainTypeChineseName, type);

                    GridLayer_TypeDict[type.terrainTypeLayer].Add(type);
                }
            };
            buildTypeDict(GridTerrainTypeList);
        }

        private void UpdateHexmapGrids(int width, int height)
        {
            // Hex map not changed, so will not reset 
            if (this.mapWidth == width && this.mapHeight == height && IsHexmapInit)
            {
                return;
            }
            this.mapWidth = width;
            this.mapHeight = height;

            if (!IsHexmapInit)
            {
                IsHexmapInit = true;
            }

            HexmapGridTerDataList = new List<GridTerrainData>(width * height);
            HexmapGridTerTypeList = new List<byte>(width * height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector2Int offsetCoord = new Vector2Int(i, j);
                    Hexagon hexagon = HexHelper.OffsetToAxial(offsetCoord);
                    Vector3 hexCenter = Vector3.zero;       // TODO : hexCenter is a world pos

                    GridTerrainData gridTerrainData = new GridTerrainData();
                    gridTerrainData.InitGridTerrainData(offsetCoord, hexagon, hexCenter);
                    HexmapGridTerDataList.Add(new GridTerrainData());

                    HexmapGridTerTypeList.Add(0);
                }
            }
        }

        // TODO : use it to build mountain data
        private void UpdateMountainGrids()
        {
            Parallel.ForEach(MountainDatas, (mountain) =>
            {
                for (int i = mountain.MountainGridList.Count - 1; i > 0; i--)
                {
                    // If grid is not Mountain, remove it
                    Vector2Int offset = mountain.MountainGridList[i];
                    if (!GetGridIsMountain(offset))
                    {
                        mountain.MountainGridList.RemoveAt(i);
                    }
                }
                mountain.UpdateMountainData();
            });

            MountainID_DataDict.Clear();
            foreach (var mountain in MountainDatas)
            {
                MountainID_DataDict.Add(mountain.MountainID, mountain);
                foreach (var grid in mountain.MountainGridSets)
                {
                    GridOffset_MountainID_Dict.Add(grid, mountain.MountainID);
                }
            }
        }

        public void SaveGridTerSO(List<GridTerrainLayer> terrainLayersList, List<GridTerrainType> terrainTypesList)
        {
            GridTerrainLayerList.Clear();
            foreach (var layer in terrainLayersList)
            {
                GridTerrainLayerList.Add(layer);
            }

            GridTerrainTypeList.Clear();
            foreach (var type in terrainTypesList)
            {
                GridTerrainTypeList.Add(type);
            }
            Debug.Log("GridTerrainSO instance call save!");
        }


        #region Mountain Data handle

        public MountainData GetNewMountainData()
        {
            MountainCounter++;
            MountainData mountainData = new MountainData(MountainCounter);
            return mountainData;
        }

        public void SaveMountainData(List<MountainData> mountainDatas)
        {
            MountainDatas.Clear();
            foreach (var mountain in mountainDatas)
            {
                MountainDatas.Add(mountain);
            }
        }

        #endregion

        #region Hexmap grid terrain

        public void UpdateGridTerrainData(List<Vector2Int> offsetHex, byte terrainTypeIdx)
        {
            for (int i = 0; i < offsetHex.Count; i++)
            {
                if (!CheckPosInHexmap(offsetHex[i]))
                {
                    continue;
                }
                int idx = offsetHex[i].x * mapWidth + offsetHex[i].y;  // 会越界吗？？？
                if (idx >= 0 && idx < HexmapGridTerTypeList.Count)
                {
                    HexmapGridTerTypeList[idx] = terrainTypeIdx;
                }
            }
        }

        private bool CheckPosInHexmap(Vector2Int offset)
        {
            return offset.x >= 0 && offset.x < mapHeight && offset.y >= 0 && offset.y < mapWidth;
        }

        public byte GetGridTerrainDataIdx(Vector2Int offsetHex)
        {
            int idx = offsetHex.x * mapWidth + offsetHex.y;
            if (HexmapGridTerTypeList.Count > idx && idx >= 0)
            {
                return HexmapGridTerTypeList[idx];    // idx
            }
            else
            {
                return 0;
            }
        }

        public bool GetGridIsMountain(Vector2Int offsetHex)
        {
            byte idx = GetGridTerrainDataIdx(offsetHex);
            return GridTerrainTypeList[idx].terrainTypeName == BaseGridTerrainTypes.MountainType.terrainTypeName;
        }

        public int GetGridMountainID(Vector2Int offsetHex)
        {
            if (!GridOffset_MountainID_Dict.ContainsKey(offsetHex))
            {
                //Debug.LogError($"Grid offset is not Mountain : {offsetHex}");
                return -1;
            }
            int mountainID = GridOffset_MountainID_Dict[offsetHex];
            return mountainID;
        }

        public Color GetGridTerrainTypeColorByIdx(byte i)
        {
            return GridTerrainTypeList[i].terrainEditColor;
        }

        #endregion

        #region Get/Set function

        public int GetLayerOrder()
        {
            return CurLayerNum++;
        }

        public GridTerrainLayer GetTerrainLayer(int layerOrder)
        {
            return GridLayerDict_LayerOrder[layerOrder];
        }

        public GridTerrainLayer GetTerrainLayer(string layerName)
        {
            return GridLayerDict_LayerName[layerName];
        }

        public List<GridTerrainType> GetTerrainTypesByLayer(string layerName)
        {
            GridTerrainLayer gridTerrainLayer = GetTerrainLayer(layerName);
            return GridLayer_TypeDict[gridTerrainLayer.layerOrder];
        }

        public GridTerrainType GetTerrainType(string typeName)
        {
            return GridTypeDict_Name[typeName];
        }

        public GridTerrainType GetTerrainType_ChineseName(string chineseName)
        {
            return GridTypeDict_ChineseName[chineseName];
        }

        #endregion

    }
}
