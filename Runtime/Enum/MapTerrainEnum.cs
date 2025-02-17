using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // TODO: 保守估计有 20 多种地形需要设置
    public enum MapTerrainType {
        Plain,      // 平原
        Hill,       // 丘陵
        Mountain,   // 山地
        //
        //
        //
        //
        //
    }


    public static class MapTerrainEnum {

        //public static Vector3Int ClusterSize = new Vector3Int(1024, 1000, 1024);

        public const int ClusterSize = 1024;

        public const int TileSize = 256;

    }

}
