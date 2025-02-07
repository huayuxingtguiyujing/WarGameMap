

using System;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public static class MapEnum
    {

        // go in the hierarchy!
        public static string MapRootName = "mapRoot";

        public static string ClusterParentName = "clusters";

        public static string SignParentName = "signs";


        // map cluster 应该包含 x * x 个 map grid
        public static int ClusterSize = 5;

        public static float GridInnerRatio = 0.7f;


        public static Color DefaultGridColor = Color.white;


    }
}
