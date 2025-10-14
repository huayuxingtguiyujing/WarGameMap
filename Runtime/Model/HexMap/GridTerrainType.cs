using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace LZ.WarGameMap.Runtime
{
    // GridTerrainLayer :  like unity-layer, terrain type in the same layer can not overlay
    // Ordered by layernum when different terrain type overlay. smaller layer will be in the below
    // eg : sea layer       （海洋类型）                : 浅海、深海
    //      base layer      （基本地貌）                : 平原、丘陵、山地、高原
    //      landform layer  （可叠加的风格化地貌）      : 热带雨林、热带草原、温带森林、温带草原、荒漠地貌、寒带地貌、苔原地貌
    //      decorate layer  （丰富表现细节）            : 森林、农田、沼泽、海岸/河岸、城市
    //      effect layer    （动态的地貌）              : 战争废土、雨地、雪地etc
    //
    // eg : 沼泽可以叠加在平原、草原、丘陵etc上
    //      农田可以叠加在平原上，不能与沼泽共同出现
    //      森林 可以 叠加在沼泽上，也勉强可以叠加在农田上

    [Serializable]
    public class GridTerrainLayer : ICopyable<GridTerrainLayer>
    {
        [HorizontalGroup("GridTerrainLayer"), LabelText("")]
        public int layerOrder;              // layer order, auto gen

        [HorizontalGroup("GridTerrainLayer"), LabelText("  层级名称")]
        public string layerName;

        [HorizontalGroup("GridTerrainLayer"), LabelText("  层级描述")]
        public string layerDiscription;

        [HorizontalGroup("GridTerrainLayer"), LabelText("  内置层级"), ReadOnly]
        public bool IsBaseLayer;

        int layerOverlayMask;        // TODO : able to overlay with other layers

        public GridTerrainLayer(int layerOrder, string layerName, string layerDiscription, bool IsBaseLayer = false)
        {
            this.layerOrder = layerOrder;
            this.layerName = layerName;
            this.layerDiscription = layerDiscription;
            this.IsBaseLayer = IsBaseLayer;
        }

        public static GridTerrainLayer Deserialized(SerializedProperty obj)
        {
            int order = obj.FindPropertyRelative("layerOrder").intValue;
            string name = obj.FindPropertyRelative("layerName").stringValue;
            string desc = obj.FindPropertyRelative("layerDiscription").stringValue;
            bool isBase = obj.FindPropertyRelative("IsBaseLayer").boolValue;
            return new GridTerrainLayer(order, name, desc, isBase);
        }

        public GridTerrainLayer CopyObject()
        {
            return new GridTerrainLayer(layerOrder, layerName, layerDiscription, IsBaseLayer);
        }

        public void Copy(GridTerrainLayer layer)
        {
            this.layerOrder = layer.layerOrder;
            this.layerName = layer.layerName;
            this.layerDiscription = layer.layerDiscription;
            this.IsBaseLayer = layer.IsBaseLayer;           // Warning : do not mod this field
        }
    }

    // In the cv-like map gen flow, every hex grid has a GridTerrainType
    // eg : plain, hill, sea ...
    [Serializable]
    public class GridTerrainType : ICopyable<GridTerrainType>
    {
        [HorizontalGroup("GridTerrainType"), LabelText("地形名称")]
        public string terrainTypeName;

        [HorizontalGroup("GridTerrainType"), LabelText("中文名称")]
        public string terrainTypeChineseName;

        [HorizontalGroup("GridTerrainType"), LabelText("地形层级")]
        public int terrainTypeLayer;

        [HorizontalGroup("GridTerrainType"), LabelText("地形Edit颜色")]
        public Color terrainEditColor;

        [HorizontalGroup("GridTerrainType"), LabelText("内置地形"), ReadOnly]
        public bool IsBaseType;

        public GridTerrainType(int terrainTypeLayer, string terrainTypeName, string terrainTypeChineseName,Color terrainEditColor, bool IsBaseLayer)
        {
            this.terrainTypeName = terrainTypeName;
            this.terrainTypeChineseName = terrainTypeChineseName;
            this.terrainTypeLayer = terrainTypeLayer;
            this.terrainEditColor = terrainEditColor;
            this.IsBaseType = IsBaseLayer;
        }

        public static GridTerrainType Deserialized(SerializedProperty obj)
        {
            int layer = obj.FindPropertyRelative("terrainTypeLayer").intValue;
            string name = obj.FindPropertyRelative("terrainTypeName").stringValue;
            string chineseName = obj.FindPropertyRelative("terrainTypeChineseName").stringValue;
            Color color = obj.FindPropertyRelative("terrainEditColor").colorValue;
            bool isBaseLayer = obj.FindPropertyRelative("IsBaseLayer").boolValue;
            var type = new GridTerrainType(layer, name, chineseName, color, isBaseLayer);
            return type;
        }

        public GridTerrainType CopyObject()
        {
            return new GridTerrainType(terrainTypeLayer, terrainTypeName, terrainTypeChineseName, terrainEditColor, IsBaseType);
        }

        public void Copy(GridTerrainType type)
        {
            this.terrainTypeName = type.terrainTypeName;
            this.terrainTypeChineseName = type.terrainTypeChineseName;
            this.terrainTypeLayer = type.terrainTypeLayer;
            this.terrainEditColor = type.terrainEditColor;
            this.IsBaseType = type.IsBaseType;
        }
    }

}
