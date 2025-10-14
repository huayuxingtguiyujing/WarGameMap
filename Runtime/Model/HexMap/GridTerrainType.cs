using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace LZ.WarGameMap.Runtime
{
    // GridTerrainLayer :  like unity-layer, terrain type in the same layer can not overlay
    // Ordered by layernum when different terrain type overlay. smaller layer will be in the below
    // eg : sea layer       ���������ͣ�                : ǳ�����
    //      base layer      ��������ò��                : ƽԭ�����ꡢɽ�ء���ԭ
    //      landform layer  ���ɵ��ӵķ�񻯵�ò��      : �ȴ����֡��ȴ���ԭ���´�ɭ�֡��´���ԭ����Į��ò��������ò��̦ԭ��ò
    //      decorate layer  ���ḻ����ϸ�ڣ�            : ɭ�֡�ũ����󡢺���/�Ӱ�������
    //      effect layer    ����̬�ĵ�ò��              : ս����������ء�ѩ��etc
    //
    // eg : ������Ե�����ƽԭ����ԭ������etc��
    //      ũ����Ե�����ƽԭ�ϣ�����������ͬ����
    //      ɭ�� ���� �����������ϣ�Ҳ��ǿ���Ե�����ũ����

    [Serializable]
    public class GridTerrainLayer : ICopyable<GridTerrainLayer>
    {
        [HorizontalGroup("GridTerrainLayer"), LabelText("")]
        public int layerOrder;              // layer order, auto gen

        [HorizontalGroup("GridTerrainLayer"), LabelText("  �㼶����")]
        public string layerName;

        [HorizontalGroup("GridTerrainLayer"), LabelText("  �㼶����")]
        public string layerDiscription;

        [HorizontalGroup("GridTerrainLayer"), LabelText("  ���ò㼶"), ReadOnly]
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
        [HorizontalGroup("GridTerrainType"), LabelText("��������")]
        public string terrainTypeName;

        [HorizontalGroup("GridTerrainType"), LabelText("��������")]
        public string terrainTypeChineseName;

        [HorizontalGroup("GridTerrainType"), LabelText("���β㼶")]
        public int terrainTypeLayer;

        [HorizontalGroup("GridTerrainType"), LabelText("����Edit��ɫ")]
        public Color terrainEditColor;

        [HorizontalGroup("GridTerrainType"), LabelText("���õ���"), ReadOnly]
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
