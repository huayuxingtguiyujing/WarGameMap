using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {
    public static class MapStoreEnum
    {

        public const string WarGameMapRootPath = "Assets/WarGameMap";

        // 地图设置
        public const string WarGameMapSettingPath = "Assets/WarGameMap/MapSetting";


        // 地形相关
        public const string TerrainRootPath = "Assets/WarGameMap/Terrain";

        public const string TerrainMeshPath = "Assets/WarGameMap/Terrain/TerrainMeshs";

        public const string TerrainTexArrayPath = "Assets/WarGameMap/Terrain/Texture/Terrain";

        public const string TerrainTexOutputPath = "Assets/WarGameMap/Terrain/Texture/Output";

        public const string TerrainHexMapPath = "Assets/WarGameMap/Terrain/HexMap";


        // 高度图相关
        public const string HeightMapInputPath = "Assets/WarGameMap/HeightMap/Origin";

        public const string HeightMapOutputPath = "Assets/WarGameMap/HeightMap/Output";

        public const string HeightMapScriptableObjPath = "Assets/WarGameMap/HeightMap/ScriptableObj";

        public const string HeightMapNormalTexOutputPath = "Assets/WarGameMap/HeightMap/Normal_Output";

        // 地貌相关
        public const string LandformTexOutputPath = "Assets/WarGameMap/Landform/Landform_Output";

        public const string NormalTexOutputPath = "Assets/WarGameMap/Landform/Normal_Output";

        public const string HexLandformTexOutputPath = "Assets/WarGameMap/Landform/HexOutput";

        //public const string Path = "Assets/WarGameMap/Landform/Output";


        // 编辑器窗口 SO
        public static string MapWindowPath = "Assets/WarGameMap/EditorWindow";

    }
}
