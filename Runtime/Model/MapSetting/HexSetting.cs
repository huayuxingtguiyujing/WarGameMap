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

        [LabelText("Hex地图宽度")]
        public int mapWidth = 100;

        [LabelText("Hex地图高度")]
        public int mapHeight = 100;

        [LabelText("Hex格大小")]
        public int hexGridSize = 5;

        [LabelText("cluster所具有的Hex格数目")]
        public int clusterSize = 10;

        [LabelText("Hex对Terrain的映射范围")]
        [Tooltip("在通过terrain生成初版hex数据时，会根据hex中心范围内的一定vert确定该hex的地形（平原、丘陵、高地、山脉）")]
        public int hexCalcuVertScope = 3;



    }
}
