using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor {

    public static class MapEditorClass {

        public static string TerrainClass = "地貌编辑";

        public static string BuildingsClass = "城建编辑";

        public static string DecorateClass = "装饰编辑";

        public static string GamePlayClass = "GamePlay编辑";

        public static string ToolClass = "通用工具";

    }

    public static class MapEditorEnum {

        public static string MapEditor = "地图/地图编辑器/";

        // 基本地貌
        public static string HexMapEditor = "六边形网格编辑";

        public static string TerrainEditor = "地形编辑";

        public static string HeightMapEditor = "高度图编辑";

        public static string LandformEditor = "地貌编辑";

        public static string WaterEditor = "水文编辑";


        //城建
        //public static string CityEditor = "城市编辑";
        //public static string LoadEditor = "道路编辑";

        // 装饰物
        public static string PlantEditor = "植被编辑";

        // GamePlay
        //public static string CountryEditor = "国家编辑";
        //public static string ResFieldEditor = "资源编辑";
        //public static string PeopleEditor = "人口编辑";

        // 通用工具
        public static string TextureToolEditor = "地形纹理编辑工具";

    }

    public static class MapBrushEnum {
        
        public static Color DefaultColor = Color.white;
    }

}

