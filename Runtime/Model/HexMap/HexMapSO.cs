using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // HexMapSO : cv like map, it hold all grid's terrainData in hex map
    [Serializable]
    public class HexMapSO : ScriptableObject {

        public int width;

        public int height;

        // TODO : hex 的地图规模预计是3000 * 3000 后续要动态加载，不能一次全部载入进去
        public List<GridTerrainData> GridTerDataList;

        public List<byte> GridTerrainTypeList;

        public void InitRawHexMap(int width, int height) {
            this.width = width;
            this.height = height;
            GridTerDataList = new List<GridTerrainData>(width * height);
            GridTerrainTypeList = new List<byte>(width * height);
            for(int i = 0; i < width; i++)
            {
                for(int j = 0; j < height; j++)
                {
                    Vector2Int offsetCoord = new Vector2Int(i, j);
                    Hexagon hexagon = HexHelper.OffsetToAxial(offsetCoord);
                    Vector3 hexCenter = Vector3.zero;       // TODO : hexCenter is a world pos

                    GridTerrainData gridTerrainData = new GridTerrainData();
                    gridTerrainData.InitGridTerrainData(offsetCoord, hexagon, hexCenter);
                    GridTerDataList.Add(new GridTerrainData());

                    GridTerrainTypeList.Add(0);
                }
            }
        }

        public void UpdateGridTerrainData(List<Vector2Int> offsetHex, byte terrainTypeIdx)
        {
            for(int i = 0; i < offsetHex.Count; i++)
            {
                int idx = offsetHex[i].x * width + offsetHex[i].y;
                GridTerrainTypeList[idx] = terrainTypeIdx;
            }
        }

        public void UpdateGridTerrainData(int i, int j, Vector2Int hexIdx, Hexagon hexagon, Vector3 hexCenter) {
            int idx = i * width + j;
            GridTerDataList[idx].InitGridTerrainData(hexIdx, hexagon, hexCenter);
        }


        #region get/set 方法

        public GridTerrainData GetTerrainData(List<Vector2Int> offsetHex) {
            // TODO : offset coord
            return null;
        }

        #endregion

    }
}
