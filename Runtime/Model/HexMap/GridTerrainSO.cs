using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace LZ.WarGameMap.Runtime
{
    public class BaseGridTerrain
    {
        //public static GridTerrainLayer SeaLayer =       new GridTerrainLayer(0, "海洋层", "", true);
        public static GridTerrainLayer BaseLayer =      new GridTerrainLayer(0, "基本地貌层", "", true);
        public static GridTerrainLayer LandformLayer =  new GridTerrainLayer(1, "叠加地貌层", "", true);
        public static GridTerrainLayer DecorateLayer =  new GridTerrainLayer(2, "装饰层", "", true);
        public static GridTerrainLayer DynamicLayer =   new GridTerrainLayer(3, "动态层", "", true);

        public static GridTerrainType ShallowSeaType =  new GridTerrainType(0, "ShallowSea", "浅海", new Color(0.125f, 0.698f, 0.667f, 1.0f), true);
        public static GridTerrainType DeepSeaType =     new GridTerrainType(0, "DeepSea", "深海", new Color(0, 0.47f, 0.62f, 1.0f), true);
        public static GridTerrainType PlainType =       new GridTerrainType(0, "Plain",     "平原",   new Color(0.55f, 0.75f, 0.45f, 1.0f), true);
        public static GridTerrainType HillType =        new GridTerrainType(0, "Hill",      "丘陵",   new Color(0.40f, 0.60f, 0.30f, 1.0f), true);
        public static GridTerrainType MountainType =    new GridTerrainType(0, "Mountain",  "山脉",   new Color(0.50f, 0.45f, 0.40f, 1.0f), true);
        public static GridTerrainType PlateauType =     new GridTerrainType(0, "Plateau",   "高原",   new Color(0.70f, 0.60f, 0.35f, 1.0f), true);
        public static GridTerrainType SnowType =        new GridTerrainType(0, "Snow",      "雪地",   new Color(1, 1, 1, 1.0f), true);

        public static GridTerrainType TropicalType = new GridTerrainType(1, "Tropical", "热带", new Color(0.90f, 0.75f, 0.30f, 1), true);
        public static GridTerrainType SubtropicsType = new GridTerrainType(1, "Subtropics", "亚热带", new Color(0.78f, 0.85f, 0.40f, 1), true);
        public static GridTerrainType TemperateType = new GridTerrainType(1, "Temperate", "温带", new Color(0.65f, 0.80f, 0.55f, 1), true);
        public static GridTerrainType CoastType = new GridTerrainType(1, "Coast", "海岸", new Color(0.85f, 0.90f, 0.65f, 1), true);
        public static GridTerrainType SandType = new GridTerrainType(1, "Sand", "沙漠", new Color(0.92f, 0.78f, 0.68f, 1), true);
        //public static GridTerrainType SandType = new GridTerrainType(1, "Sand", "湖泊", new Color(0.92f, 0.78f, 0.68f, 1), true);

        public static GridTerrainType WetlandType = new GridTerrainType(2, "Wetland", "湿地", new Color(0.30f, 0.55f, 0.45f, 1), true);
        public static GridTerrainType ForestType = new GridTerrainType(2, "Forest", "森林", new Color(0.20f, 0.50f, 0.25f, 1), true);
        public static GridTerrainType FarmlandType = new GridTerrainType(2, "Farmland", "农田", new Color(0.80f, 0.75f, 0.45f, 1), true);
        public static GridTerrainType TownType = new GridTerrainType(2, "Town", "村镇", new Color(0.70f, 0.60f, 0.55f, 1), true);
        public static GridTerrainType CityType = new GridTerrainType(2, "City", "城市", new Color(0.55f, 0.55f, 0.65f, 1), true);

        public static GridTerrainType WastelandType = new GridTerrainType(3, "Wasteland", "废土", new Color(0.70f, 0.55f, 0.35f, 1), true);
        public static GridTerrainType FloodingType = new GridTerrainType(3, "Flooding", "洪涝", new Color(0.15f, 0.50f, 0.75f, 1), true);


        public static bool IsMountain(GridTerrainType type1)
        {
            return type1.terrainTypeName == MountainType.terrainTypeName;
        }

        public static bool IsMountain(string typeName)
        {
            return typeName == MountainType.terrainTypeName;
        }

        public static bool IsSea(string typeName)
        {
            return typeName == ShallowSeaType.terrainTypeName || typeName == DeepSeaType.terrainTypeName;
        }

        public static string GetMountainName()
        {
            return MountainType.terrainTypeName;
        }

        public static Color GetPlainColor()
        {
            return PlainType.terrainEditColor;
        }

        public static Color GetMountainColor()
        {
            return MountainType.terrainEditColor;
        }
    }

    public static class TerrainLayerIdxs
    {
        public static int BaseLayer = 0;
        public static int LandformLayer = 1;
        public static int DecorateLayer = 2;
        public static int DynamicLayer = 3;

        public static int MountainLayer = 4;

        public const int GridTerCacheNum = 5;
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

        public bool isTerTypeInit = false;

        
        // Mountain Data
        [Header(" Mountain Data ")]
        public List<MountainData> MountainDatas = new List<MountainData>();

        Dictionary<int, MountainData> MountainID_DataDict = new Dictionary<int, MountainData>();

        // Hex map grid offset coord -> mountain id dict
        Dictionary<Vector2Int, int> GridOffset_MountainID_Dict = new Dictionary<Vector2Int, int>();

        [LabelText("hex地图宽度"), ReadOnly]
        public int MountainCounter = 1;


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
        public List<uint4> HexmapGridTerTypeList = new List<uint4>();           // uint3x3 : type[0] - layer 0, type[1] - layer 1 (基本地形层), 以此类推


        public int GridCount => HexmapGridTerDataList.Count;

        public bool IsHexmapInit = false;        // Use to reset hex map data in inspector


        #region Init Update

        public void UpdateTerSO(int width, int height)
        {
            if (!isTerTypeInit)
            {
                // Check and add base type
                GridTerrainLayerList = new List<GridTerrainLayer>
                {
                    BaseGridTerrain.BaseLayer,
                    BaseGridTerrain.LandformLayer,
                    BaseGridTerrain.DecorateLayer,
                    BaseGridTerrain.DynamicLayer
                };
                GridTerrainTypeList = new List<GridTerrainType>
                {
                    BaseGridTerrain.ShallowSeaType,
                    BaseGridTerrain.DeepSeaType,
                    BaseGridTerrain.PlainType,
                    BaseGridTerrain.HillType,
                    BaseGridTerrain.MountainType,
                    BaseGridTerrain.PlateauType,
                    BaseGridTerrain.SnowType,

                    BaseGridTerrain.TropicalType,
                    BaseGridTerrain.SubtropicsType,
                    BaseGridTerrain.TemperateType,
                    BaseGridTerrain.CoastType,
                    BaseGridTerrain.SandType,

                    BaseGridTerrain.WetlandType,
                    BaseGridTerrain.ForestType,
                    BaseGridTerrain.FarmlandType,
                    BaseGridTerrain.TownType,
                    BaseGridTerrain.CityType,

                    BaseGridTerrain.WastelandType,
                    BaseGridTerrain.FloodingType
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
            HexmapGridTerTypeList = new List<uint4>(width * height);
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

                    HexmapGridTerTypeList.Add(new uint4(0));     // unvalid type
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
            GridOffset_MountainID_Dict.Clear();
            foreach (var mountain in MountainDatas)
            {
                MountainID_DataDict.Add(mountain.MountainID, mountain);
                foreach (var grid in mountain.MountainGridSets)
                {
                    GridOffset_MountainID_Dict.Add(grid, mountain.MountainID);
                }
            }
        }

        #endregion

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
                mountain.SaveMountainGrid();
            }
        }
        // TODO : 现在的山脉编辑无法编辑到 每个格子的 mountainID ！！！到gridTerrainEditor中改
        public MountainData GetMountainData(int mountainID)
        {
            if (!MountainID_DataDict.ContainsKey(mountainID))
            {
                //Debug.LogError($"Do not contain mountain ID : {mountainID}");
                return null;
            }
            return MountainID_DataDict[mountainID];
        }

        #endregion


        #region Hexmap grid terrain

        public void UpdateGridTerrainData(List<Vector2Int> offsetHex, byte terrainTypeIdx, int layer)
        {
            for (int i = 0; i < offsetHex.Count; i++)
            {
                if (!CheckPosInHexmap(offsetHex[i]))
                {
                    continue;
                }
                int idx = offsetHex[i].x * mapWidth + offsetHex[i].y;
                if (idx >= 0 && idx < HexmapGridTerTypeList.Count)
                {
                    uint4 gridTerType = HexmapGridTerTypeList[idx];
                    gridTerType[layer] = terrainTypeIdx;
                    HexmapGridTerTypeList[idx] = gridTerType;
                }
            }
        }

        private bool CheckPosInHexmap(Vector2Int offset)
        {
            return offset.x >= 0 && offset.x < mapHeight && offset.y >= 0 && offset.y < mapWidth;
        }

        public byte GetGridTerBaseDataIdx(Vector2Int offsetHex)
        {
            return GetGridTerrainDataIdx(offsetHex, TerrainLayerIdxs.BaseLayer);
        }

        public byte GetGridTerLandformDataIdx(Vector2Int offsetHex)
        {
            return GetGridTerrainDataIdx(offsetHex, TerrainLayerIdxs.LandformLayer);
        }

        public byte GetGridTerDecorateDataIdx(Vector2Int offsetHex)
        {
            return GetGridTerrainDataIdx(offsetHex, TerrainLayerIdxs.DecorateLayer);
        }

        public byte GetGridTerDynamicDataIdx(Vector2Int offsetHex)
        {
            return GetGridTerrainDataIdx(offsetHex, TerrainLayerIdxs.DynamicLayer);
        }

        private byte GetGridTerrainDataIdx(Vector2Int offsetHex, int layer)
        {
            int idx = offsetHex.x * mapWidth + offsetHex.y;
            if (HexmapGridTerTypeList.Count > idx && idx >= 0)
            {
                return (byte) HexmapGridTerTypeList[idx][layer];
            }
            else
            {
                return 0;
            }
        }

        public bool GetGridIsMountain(Vector2Int offsetHex)
        {
            byte idx = GetGridTerBaseDataIdx(offsetHex);
            return GridTerrainTypeList[idx].terrainTypeName == BaseGridTerrain.MountainType.terrainTypeName;
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
            if(i < 0 || i >= GridTerrainTypeList.Count)
            {
                return MapColorUtil.NotValidColor;
            }
            return GridTerrainTypeList[i].terrainEditColor;
        }

        public bool GetGridCanCountry(Vector2Int offsetHex)
        {
            byte idx = GetGridTerBaseDataIdx(offsetHex);
            GridTerrainType gridTerrainType = GridTerrainTypeList[idx];
            GridTerrainLayer gridTerrainLayer = GetTerrainLayerByType(gridTerrainType);
            // Sea and mountain can not be Country
            bool IsMountain = BaseGridTerrain.IsMountain(gridTerrainType.terrainTypeName);
            bool IsSea = BaseGridTerrain.IsSea(gridTerrainType.terrainTypeName);
            return !IsMountain && !IsSea;
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

        public GridTerrainLayer GetTerrainLayerByType(GridTerrainType terrainType)
        {
            int layerOrder = terrainType.terrainTypeLayer;
            return GetTerrainLayer(layerOrder);
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
