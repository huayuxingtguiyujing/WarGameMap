
namespace LZ.WarGameMap.Runtime.Enums {
    public static class MapStoreEnum
    {

        public const string RuntimeManifestPath = "Assets/WarGameMap/MapSetting/MapRuntimeAssets_Default.asset";
        public const string RuntimeManifestAddress = "WarGameMap/DefaultManifest";
        public const string RuntimeConfigGroup = "WarGameMapConfig";
        public const string RuntimeRenderGroup = "WarGameMapRender";
        public const string WarGameMapRootPath = "Assets/WarGameMap";

        // 地图设置
        public const string WarGameMapSettingPath = "Assets/WarGameMap/MapSetting";

        public const string WarGameMapEditObjPath = "Assets/WarGameMap/MapEditObj";     // 存放编辑器时态的一些数据


        // 地形相关
        public const string TerrainRootPath = "Assets/WarGameMap/Terrain";

        public const string TerrainMeshSerializedPath = "Assets/WarGameMap/Terrain/TerrainMeshsSerl";

        public const string TerrainMeshAssetPath = "Assets/WarGameMap/Terrain/TerrainMeshs";

        // terrain mesh 的 ab 包
        public const string TerrainMeshAssetGroupName = "TerrainClusterDatas";
        public const string TerrainMeshAssetABPath = "Assets/WarGameMap/Terrain/TerrainMeshABs";

        public const string TerrainTexArrayPath = "Assets/WarGameMap/Terrain/Texture/Terrain";

        public const string TerrainTexOutputPath = "Assets/WarGameMap/Terrain/Texture/Output";

        // 地形 - Hex
        public const string TerrainHexMapPath = "Assets/WarGameMap/Terrain/HexMap";

        public const string TerrainHexmapDataPath = "Assets/WarGameMap/Terrain/HexmapData";

        public const string TerrainHexmapGridDataPath = "Assets/WarGameMap/Terrain/HexmapData/GridData";

        // 地形 - 河流 山脉
        public const string RiverDataPath = "Assets/WarGameMap/Terrain/River";

        public const string RiverTexDataPath = "Assets/WarGameMap/Terrain/River/RvTexture";

        public const string MountainDataPath = "Assets/WarGameMap/Terrain/Mountain";


        // 高度图相关
        public const string HeightMapInputPath = "Assets/WarGameMap/HeightMap/Origin";

        public const string HeightMapOutputPath = "Assets/WarGameMap/HeightMap/Output";

        public const string HeightMapScriptableObjPath = "Assets/WarGameMap/HeightMap/ScriptableObj";

        public const string HeightMapNormalTexOutputPath = "Assets/WarGameMap/HeightMap/Normal_Output";

        // 地貌相关
        public const string LandformTexOutputPath = "Assets/WarGameMap/Landform/Landform_Output";

        public const string NormalTexOutputPath = "Assets/WarGameMap/Landform/Normal_Output";

        public const string HexLandformTexOutputPath = "Assets/WarGameMap/Landform/Hex_Output";

        public const string NoiseTexOutputPath = "Assets/WarGameMap/Landform/Noise_Output";
        //public const string Path = "Assets/WarGameMap/Landform/Output";

        // GamePlay相关
        public const string GamePlayGridTerrainDataPath = "Assets/WarGameMap/GamePlay/GridTerData";

        public const string GamePlayGridTerrainTexDataPath = "Assets/WarGameMap/GamePlay/GridTerData/Texture";

        public const string GamePlayCountryDataPath = "Assets/WarGameMap/GamePlay/CountryData";

        public const string GamePlayCountryCSVDataPath = "Assets/WarGameMap/GamePlay/CountryData/CSV";

        public const string GamePlayCountryTexDataPath = "Assets/WarGameMap/GamePlay/CountryData/Texture";

        // 编辑器窗口 SO
        public static string MapWindowPath = "Assets/WarGameMap/EditorWindow";

    }
}
