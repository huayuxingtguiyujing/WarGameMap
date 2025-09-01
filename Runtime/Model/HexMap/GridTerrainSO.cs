using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.WSA;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace LZ.WarGameMap.Runtime
{
    public class BaseGridTerrainTypes
    {
        public static GridTerrainLayer SeaLayer =       new GridTerrainLayer(0, "�����", "", true);
        public static GridTerrainLayer BaseLayer =      new GridTerrainLayer(1, "������ò��", "", true);
        public static GridTerrainLayer LandformLayer =  new GridTerrainLayer(2, "���ӵ�ò��", "", true);
        public static GridTerrainLayer DecorateLayer =  new GridTerrainLayer(3, "װ�β�", "", true);
        public static GridTerrainLayer DynamicLayer =   new GridTerrainLayer(4, "��̬��", "", true);

        public static GridTerrainType PlainType =       new GridTerrainType(0, "Plain",     "ƽԭ",   new Color(0.55f, 0.75f, 0.45f, 1.0f), true);
        public static GridTerrainType HillType =        new GridTerrainType(1, "Hill",      "����",   new Color(0.40f, 0.60f, 0.30f, 1.0f), true);
        public static GridTerrainType MountainType =    new GridTerrainType(1, "Mountain",  "ɽ��",   new Color(0.50f, 0.45f, 0.40f, 1.0f), true);
        public static GridTerrainType PlateauType =     new GridTerrainType(1, "Plateau",   "��ԭ",   new Color(0.70f, 0.60f, 0.35f, 1.0f), true);
        public static GridTerrainType SnowType =        new GridTerrainType(1, "Snow",      "ѩ��",   new Color(1, 1, 1, 1.0f), true);
    }

    public class GridTerrainSO : ScriptableObject
    {
        static GridTerrainSO instance;

        public static GridTerrainSO GetInstance()
        {
            if (instance == null)
            {
                string assetPath = $"{MapStoreEnum.TerrainHexmapGridDataPath}/GridTerrainSO_Default.asset";
                instance = AssetDatabase.LoadAssetAtPath<GridTerrainSO>(assetPath);
                if (instance == null)
                {
                    instance = CreateInstance<GridTerrainSO>();
                    AssetDatabase.CreateAsset(instance, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"[GridTerrainSO] Created asset at {MapStoreEnum.TerrainHexmapGridDataPath}");
                }
            }
            return instance;
        }


        public int CurLayerNum;

        public List<GridTerrainLayer> Base_GridLayerList;

        public List<GridTerrainLayer> GridLayerList = new List<GridTerrainLayer>();

        public List<GridTerrainType> Base_GridTypeList;
        
        public List<GridTerrainType> GridTypeList = new List<GridTerrainType>();


        // For fastly get terrain infos
        Dictionary<int, GridTerrainLayer> GridLayerDict_LayerOrder = new Dictionary<int, GridTerrainLayer>();

        Dictionary<string, GridTerrainLayer> GridLayerDict_LayerName = new Dictionary<string, GridTerrainLayer>();

        Dictionary<string, GridTerrainType> GridTypeDict_Name = new Dictionary<string, GridTerrainType>();

        Dictionary<string, GridTerrainType> GridTypeDict_ChineseName = new Dictionary<string, GridTerrainType>();


        bool isInit = false;


        public void UpdateTerSO()
        {
            if (!isInit)
            {
                // check and add base type
                Base_GridLayerList = new List<GridTerrainLayer>
                {
                    BaseGridTerrainTypes.SeaLayer,
                    BaseGridTerrainTypes.BaseLayer,
                    BaseGridTerrainTypes.LandformLayer,
                    BaseGridTerrainTypes.DecorateLayer,
                    BaseGridTerrainTypes.DynamicLayer
                };
                Base_GridTypeList = new List<GridTerrainType>
                {
                    BaseGridTerrainTypes.PlainType,
                    BaseGridTerrainTypes.HillType,
                    BaseGridTerrainTypes.MountainType,
                    BaseGridTerrainTypes.PlateauType,
                    BaseGridTerrainTypes.SnowType
                };
                isInit = true;
            }

            CheckGridTerInfo();
            UpdateTerInfoDict();
            //Debug.Log($" layer : {instance.Base_GridTypeLayerList.Count}, types : {instance.Base_GridTypeList}");
        }

        // Use it to check if all terrain type info are correct
        private bool CheckGridTerInfo()
        {
            if (!isInit)
            {
                Debug.LogError("GridTerrain not inited!");
                return false;
            }

            // All terrainType's layer must be correct, auto fix if not
            HashSet<int> validLayer = new HashSet<int>(Base_GridLayerList.Count + GridLayerList.Count);
            foreach (var layer in Base_GridLayerList)
            {
                validLayer.Add(layer.layerOrder);
            }
            foreach (var layer in GridLayerList)
            {
                validLayer.Add(layer.layerOrder);
            }

            // Will not check layer name... im lazy
            HashSet<string> CurLayerNames = new HashSet<string>(Base_GridLayerList.Count + GridLayerList.Count);

            // Name and chinese name and terrain color can not be the same 
            HashSet<string> CurTerTypeNames = new HashSet<string>(Base_GridTypeList.Count + GridTypeList.Count);
            HashSet<string> CurTerTypeChineseNames = new HashSet<string>(Base_GridTypeList.Count + GridTypeList.Count);
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
            checkGridTerTypeList(Base_GridTypeList);
            checkGridTerTypeList(GridTypeList);
            return true;
        }

        private void UpdateTerInfoDict()
        {
            GridLayerDict_LayerOrder.Clear();
            GridLayerDict_LayerName.Clear();
            Action<List<GridTerrainLayer>> buildLayerDict = (layerList) =>
            {
                foreach (var layers in layerList)
                {
                    GridLayerDict_LayerOrder.TryAdd(layers.layerOrder, layers);
                    GridLayerDict_LayerName.TryAdd(layers.layerName, layers);
                }
            };
            buildLayerDict(Base_GridLayerList);
            buildLayerDict(GridLayerList);

            GridTypeDict_Name.Clear();
            GridTypeDict_ChineseName.Clear();
            Action<List<GridTerrainType>> buildTypeDict = (typeList) =>
            {
                foreach (var type in typeList)
                {
                    GridTypeDict_Name.Add(type.terrainTypeName, type);
                    GridTypeDict_ChineseName.Add(type.terrainTypeChineseName, type);
                }
            };
            buildTypeDict(Base_GridTypeList);
            buildTypeDict(GridTypeList);
        }

        public void SaveGridTerSO(List<GridTerrainLayer> TerrainLayersList, List<GridTerrainType> TerrainTypesList)
        {
            Base_GridLayerList.Clear();
            GridLayerList.Clear();
            foreach (var layer in TerrainLayersList)
            {
                if (layer.IsBaseLayer)
                {
                    Base_GridLayerList.Add(layer);
                }
                else
                {
                    GridLayerList.Add(layer);
                }
            }

            Base_GridTypeList.Clear();
            GridTypeList.Clear();
            foreach (var type in TerrainTypesList)
            {
                if (type.IsBaseType)
                {
                    Base_GridTypeList.Add(type);
                }
                else
                {
                    GridTypeList.Add(type);
                }
            }
            Debug.Log("GridTerrainSO instance call save!");
        }

        #region get/set

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
