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
        public override string MapSettingType {
            get {
                return "HexSetting";
            }
        }

        public override string MapSettingDescription {
            get {
                return "setting for hex map, include grid size/width etc, hex map is about gameplay";
            }
        }

        [LabelText("Hex��ͼ���")]
        public int mapWidth = 100;

        [LabelText("Hex��ͼ�߶�")]
        public int mapHeight = 100;

        [LabelText("Hex���С")]
        public int hexGridSize = 5;

        [LabelText("cluster�����е�Hex����Ŀ")]
        public int clusterSize = 10;

        [LabelText("Hex��Terrain��ӳ�䷶Χ")]
        [Tooltip("��ͨ��terrain���ɳ���hex����ʱ�������hex���ķ�Χ�ڵ�һ��vertȷ����hex�ĵ��Σ�ƽԭ�����ꡢ�ߵء�ɽ����")]
        public int hexCalcuVertScope = 3;



    }
}
