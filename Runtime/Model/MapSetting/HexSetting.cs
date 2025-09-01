using log4net.Util;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    [CreateAssetMenu(fileName = "HexSetting_Default", menuName = "WarGameMap/Set/HexSettig", order = 1)]
    public class HexSettingSO : MapSettingSO {
        public override string MapSettingName {
            get {
                return "HexSetting_Default.asset";
            }
        }

        public override string MapSettingDescription {
            get {
                return "setting for hex map, include grid size/width etc, hex map is about gameplay";
            }
        }

        // �� HexGrid ����Ŀ��Ŀǰ�Ƽ� һ��TerrainCluster��Ӧ 30*30 �� Grid
        // �� ���ͼ���� 600 * 600 �� Grid
        [LabelText("Hex��ͼ���")]
        public int mapWidth = 256;

        [LabelText("Hex��ͼ�߶�")]
        public int mapHeight = 256;

        [LabelText("Hex���С")]
        public int hexGridSize = 20;

        [LabelText("cluster�����е�Hex����Ŀ")]
        public int clusterSize = 16;

        [LabelText("Hex��Terrain��ӳ�䷶Χ")]
        [Tooltip("��ͨ��terrain���ɳ���hex����ʱ�������hex���ķ�Χ�ڵ�һ��vertȷ����hex�ĵ��Σ�ƽԭ�����ꡢ�ߵء�ɽ����")]
        public int hexCalcuVertScope = 3;

        [LabelText("��̬����ʱHex��AOI��Χ")]
        [Tooltip("������Χ������� x * x ��HexCluster, ֻ��������")]
        public int hexAOIScope = 5;

        [LabelText("��ʾHexCluster�������Ŀ")]
        [Tooltip("������Χ������� x * x ��HexCluster, ֻ��������")]
        public int mapHexClusterNumLimit = 400;

        [LabelText("hex��ͼƫ��")]
        [Tooltip("HexMap ��ԭ������� terrain ��ԭ���ƫ��")]
        public Vector3 originOffset = new Vector3(50, 0, 50);

        public Layout GetScreenLayout() {
            Vector2 startPoint = new Vector2(0, 0);
            Layout layout = new Layout(
                Orientation.Layout_Pointy,
                new Point(hexGridSize, hexGridSize),
                new Point(startPoint.x, startPoint.y), mapHeight, mapWidth
            );
            return layout;
        }


    }
}
