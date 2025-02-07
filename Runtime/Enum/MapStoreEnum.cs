using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {
    public static class MapStoreEnum
    {

        public const string WarGameMapRootPath = "Assets/WarGameMap";

        // 地形相关
        public const string TerrainMeshPath = "Assets/WarGameMap/TileMeshs/";

        public const string TerrainTexArrayPath = "Assets/WarGameMap/Texture/Terrain";

        public const string TerrainTexOutputPath = "Assets/WarGameMap/Texture/Output";

        // 高度图相关
        public const string HeightMapInputPath = "Assets/WarGameMap/HeightMap/Origin";

        public const string HeightMapOutputPath = "Assets/WarGameMap/HeightMap/Output";

        public const string HeightMapScriptableObjPath = "Assets/WarGameMap/HeightMap/ScriptableObj";

        // 地貌相关
        public const string LandformTexOutputPath = "Assets/WarGameMap/Landform/Output";

        //public const string Path = "Assets/WarGameMap/Landform/Output";


        // 编辑器窗口 SO
        public static string MapWindowPath = "Assets/WarGameMap/EditorWindow";

    }
}
