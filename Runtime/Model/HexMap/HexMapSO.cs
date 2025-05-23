using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LZ.WarGameMap.Runtime.RawHexMapSO;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    public class RawHexMapSO : ScriptableObject {

        public int width;

        public int height;

        // hex grid data 
        [SerializeField]
        List<GridTerrainData> HexMapGridTers = new List<GridTerrainData>();

        public List<GridTerrainData> HexMapGridTersList { get { return HexMapGridTers; } }

        Dictionary<Vector2Int, GridTerrainData> HexMapGridTerDic = new Dictionary<Vector2Int, GridTerrainData>();

        public Dictionary<Vector2Int, GridTerrainData> HexMapGridTersDic { get { return HexMapGridTerDic; } }


        // grid type
        public enum GridTerrainType {
            Sea,
            Plain1,     // 平原地形 ： 湿地/农田/平原/海岸
            Plain2,
            HighLand1,
            HighLand2,
            Moutain1,
            Moutain2,
        }

        public enum GridSlopeType {
            Flat,
            Hill,
        }
        

        [Serializable]
        public class GridTerrainData {
            Vector2Int idx;      // offset hex coord
            Vector2Int hex_q_r;

            Vector2 hexGridCenter;

            public float baseHeight = 0;
            public GridTerrainType terrainType = GridTerrainType.Plain1;

            public float hillDegree = 1;            // 控制地形起伏程度
            public float hillHeightFix = 1;
            public GridSlopeType slopeType = GridSlopeType.Flat;

            public void CaculateTerrainData(Vector2Int hexIdx, Hexagon hexagon, Vector3 hexCenter, TDList<float> scopeHeights) {
                this.idx = hexIdx;
                this.hex_q_r = new Vector2Int(hexagon.q, hexagon.r);

                this.hexGridCenter = new Vector2(hexCenter.x, hexCenter.z);

                // caculate the terrain data, to decide the grid type and othe field
                int height = scopeHeights.GetLength(0);
                int width = scopeHeights.GetLength(1);

                float fix_k = 1000;

                float average = scopeHeights.Data.Average();
                float average_height = average * fix_k;
                float range = (scopeHeights.Data.Max() - scopeHeights.Data.Min()) * fix_k;
                float variance = scopeHeights.Data.Average(x => (x - average) * (x - average));

                // 根据平均高度 决定该单元格的 初步地形
                if (average_height <= 0f) {
                    terrainType = GridTerrainType.Sea;
                } else if (average_height <= 10f) {
                    terrainType = GridTerrainType.Plain1;
                } else if (average_height <= 14f) {
                    terrainType = GridTerrainType.Plain2;
                } else if (average_height <= 21f) {
                    terrainType = GridTerrainType.HighLand1;
                } else if (average_height <= 28f) {
                    terrainType = GridTerrainType.HighLand2;
                } else if (average_height <= 32f) {
                    terrainType = GridTerrainType.Moutain1;
                } else {
                    terrainType = GridTerrainType.Moutain2;
                }
                baseHeight = average_height;

                // 根据地形起伏程度 决定是否为丘陵
                if (variance > 10f) {
                    slopeType = GridSlopeType.Hill;
                }
                hillDegree = 0.5f;
                hillHeightFix = range;
            }

            public Color GetTerrainColor() {
                // test
                //if (q == r || q == 150 || r == 150) {
                //    return new Color(0.53f, 0.81f, 0.92f);
                //}
                switch (terrainType) {
                    case GridTerrainType.Sea:
                        return new Color(0.53f, 0.81f, 0.92f); // 浅蓝色 #87CEEB
                    case GridTerrainType.Plain1:
                        return new Color(0.20f, 0.60f, 0.20f); // 浅绿色 #339933 (表示平原)
                    case GridTerrainType.Plain2:
                        return new Color(0.42f, 0.69f, 0.30f); // 草绿色 #6AB04C

                    //case GridTerrainType.Hill:
                    //    return new Color(0.78f, 0.65f, 0.35f); // 黄绿色 #C8A55A
                    case GridTerrainType.HighLand1:
                        return new Color(0.62f, 0.49f, 0.34f); // 浅棕色 #9E7E57
                    case GridTerrainType.HighLand2:
                        return new Color(0.85f, 0.85f, 0.85f); // 浅灰色 #D8D8D8

                    case GridTerrainType.Moutain1:
                        return new Color(0.85f, 0.85f, 0.85f); // 浅灰色 #D8D8D8
                    case GridTerrainType.Moutain2:
                        return new Color(1f, 1f, 1f);           // 纯白色 #FFFFFF
                }
                return Color.white;
            }

            #region get data

            public Vector2Int GetHexPos() {
                return idx;
            }

            public Vector2 GetHexCenter() {
                return hexGridCenter;
            }

            public Hexagon GetHexagon() {
                return new Hexagon(hex_q_r.x, hex_q_r.y, -hex_q_r.x - hex_q_r.y);
            }
            
            #endregion
        }

        public void InitRawHexMap(int w, int h) {
            width = w; height = h;
        }

        public void AddGridTerrainData(Vector2Int hexIdx, Hexagon hexagon, Vector3 hexCenter, TDList<float> scopeHeights) {
            GridTerrainData gridTerrainData = new GridTerrainData();
            gridTerrainData.CaculateTerrainData(hexIdx, hexagon, hexCenter, scopeHeights);
            HexMapGridTers.Add(gridTerrainData);
        }

        public void LerpGridTerrainHeight(Vector2Int hexIdx) {
            // 使格子变得更加平滑...
            if (!HexMapGridTerDic.ContainsKey(hexIdx)) {
                Debug.LogError($"do not exist hex idx : {hexIdx}");
                return;
            }

            GridTerrainData gridTerrainData = HexMapGridTerDic[hexIdx];

            if (hexIdx.x % 2 == 1) {
                // 奇数行的格子
                
            } else {
                // 偶数行的格子

            }
        }

        public void UpdateGridTerrainData() {
            HexMapGridTerDic.Clear();
            foreach (var gridData in HexMapGridTers)
            {
                HexMapGridTerDic.Add(gridData.GetHexPos(), gridData);
            }
        }


        #region get/set 方法

        int cnt = 10;
        int iterCnt = 0;
        public GridTerrainData GetTerrainData(Vector2Int offsetHex) {
            iterCnt++;
            if (HexMapGridTerDic.ContainsKey(offsetHex)) {
                return HexMapGridTerDic[offsetHex];
            } else {
                if(cnt > 20) {
                    cnt--;
                    Debug.LogError($"missing!, offsetHex : {offsetHex}, iterCnt : {iterCnt}");
                }
                return null;
            }
        }

        #endregion

    }


}
