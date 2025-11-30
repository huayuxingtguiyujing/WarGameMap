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

        // 即 HexGrid 的数目，目前推荐 一个TerrainCluster对应 30*30 个 Grid
        // 则 大地图共有 600 * 600 个 Grid
        [LabelText("Hex地图宽度")]
        public int mapWidth = 256;

        [LabelText("Hex地图高度")]
        public int mapHeight = 256;

        [LabelText("Hex格大小")]
        public int hexGridSize = 20;

        [LabelText("Hex边界比例")]
        public float hexEdgeRatio = 0.8f;

        [LabelText("cluster所具有的Hex格数目")]
        public int clusterSize = 16;

        [LabelText("Hex对Terrain的映射范围")]
        [Tooltip("在通过terrain生成初版hex数据时，会根据hex中心范围内的一定vert确定该hex的地形（平原、丘陵、高地、山脉）")]
        public int hexCalcuVertScope = 3;

        [LabelText("动态加载时Hex的AOI范围")]
        [Tooltip("根据周围距离加载 x * x 的HexCluster, 只能是奇数")]
        public int hexAOIScope = 5;

        [LabelText("显示HexCluster的最大数目")]
        [Tooltip("根据周围距离加载 x * x 的HexCluster, 只能是奇数")]
        public int mapHexClusterNumLimit = 400;

        [LabelText("hex地图偏移")]
        [Tooltip("HexMap 的原点相较于 terrain 的原点的偏移")]
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
