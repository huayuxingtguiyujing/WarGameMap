using LZ.WarGameCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Idx = System.Tuple<int, int>;

namespace LZ.WarGameMap.Runtime
{
    // use to sample landform data
    public class LandformDataModel {

        static Color Snow = new Color(248, 248, 248);

        static Color Grass = new Color(195, 211, 169);

        static Color GreenLand = new Color(116, 182, 88);

        static Color DenceForest = new Color(71, 160, 71);
        
        static Color Forest = new Color(155, 203, 149);

        static Color Desert = new Color(235, 220, 194);

        // temperature - humidity : Color  dictionary
        // humidity : low - high
        // temp : low - high
        static List<Color> LandformModel = new List<Color> {
            new Color(153, 153, 153), new Color(187, 187, 187),new Color(221, 221, 187),
            Snow, Snow, Snow,

            new Color(187, 187, 187), new Color(220, 224, 195), new Color(196, 204, 187),
            new Color(196, 204, 187), new Color(204, 212, 187),new Color(204, 212, 187),

            new Color(220, 224, 195), Grass, Grass,
            new Color(180, 201, 169), new Color(180, 201, 169),  new Color(164, 196, 168),

            new Color(220, 224, 195), Grass, new Color(180, 201, 169), 
            new Color(180, 201, 169), new Color(156, 187, 169), GreenLand,

            Desert, Grass, new Color(193, 208, 174),new Color(156, 187, 169), GreenLand, GreenLand, 

            Desert, new Color(192, 214, 158), Forest, Forest,  DenceForest, DenceForest 
        };

        public static Color SampleColor(float humidity, float temperature) {
            float HLevel = GetHumidityLevel(humidity);
            float TLevel = GetTemperatureLevel(temperature);

            int HLevel_l = Mathf.Clamp((int)HLevel, 0, 5);
            int HLevel_r = Mathf.Clamp(HLevel_l + 1, 0, 5);

            int TLevel_l = Mathf.Clamp((int)TLevel, 0, 5);
            int TLevel_r = Mathf.Clamp(TLevel_l + 1, 0, 5);

            float HRatio = HLevel - HLevel_l;
            float TRatio = TLevel - TLevel_l;

            // bilinear caculate
            Color leftUp = LandformModel[TLevel_l * 6 + HLevel_l];
            Color leftDown = LandformModel[TLevel_r * 6 + HLevel_l];
            Color rightUp = LandformModel[TLevel_l * 6 + HLevel_r];
            Color rightDown = LandformModel[TLevel_r * 6 + HLevel_r];

            Color color1 = Color.Lerp(leftUp, rightUp, HRatio);
            Color color2 = Color.Lerp(leftDown, rightDown, HRatio);
            Color color = Color.Lerp(color1, color2, TRatio);
            color.r /= 255.0f;
            color.g /= 255.0f;
            color.b /= 255.0f;

            return color;
        }

        public static float GetHumidityLevel(float humidity) {
            // level 1 : 0, level 6 : 100;
            float level1H = 0;
            float level6H = 100;

            float level = Mathf.Lerp(level1H, level6H, humidity / level6H);
            return level;
        }

        // TODO :
        public static float GetHumidity(Vector3 vertPos, Vector2Int startLongitudeLatitude) {
            return 3;
        }

        public static float GetTemperatureLevel(float temperature) {
            // level 1 : 0, level 6 : 30
            float maxT = 30;
            float level1T = 0;
            float level6T = 5;

            float level = Mathf.Lerp(level1T, level6T, temperature / maxT);
            return level;
        }

        public static float GetTemperature(Vector3 vertPos, Vector2Int startLongitudeLatitude) {
            

            // 每纬度 0.1°C 变化
            float latitude = startLongitudeLatitude.y + vertPos.z * 0.001f;
            float baseTemperature = 30f - Mathf.Abs(latitude) * 0.1f;  
            // 每升高 100 米，气温降低 2°C
            float altitude = vertPos.y;
            float temperatureWithAltitude = baseTemperature - (altitude / 5f) * 4.0f;

            return temperatureWithAltitude;
        }



    }
}
