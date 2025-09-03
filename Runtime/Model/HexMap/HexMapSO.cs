using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // HexMapSO : cv like map, it hold all grid's terrainData in hex map
    // TODO : HexMap : 3000 * 3000, need lazy load
    [Serializable]
    public class HexMapSO : ScriptableObject {

        public int width;

        public int height;

        public List<GridTerrainData> GridTerDataList;   // TODO : lazy load

        public List<byte> GridTerrainTypeList;

        public bool IsDirty = false;

        public void InitRawHexMap(int width, int height) {
            if (this.width == width && this.height == height && !IsDirty)
            {
                // Hex map is not change, so will not init
                return;
            }

            // If IsDirty true, force to update
            if (IsDirty)
            {
                IsDirty = false;
            }

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
                if (idx >= 0 && idx < GridTerrainTypeList.Count)
                {
                    GridTerrainTypeList[idx] = terrainTypeIdx;
                }
            }
        }

        public void UpdateGridTerrainData(int i, int j, Vector2Int hexIdx, Hexagon hexagon, Vector3 hexCenter) {
            int idx = i * width + j;
            GridTerDataList[idx].InitGridTerrainData(hexIdx, hexagon, hexCenter);
        }

        public byte GetGridTerrainData(Vector2Int offsetHex)
        {
            int idx = offsetHex.x * width + offsetHex.y;
            if (GridTerrainTypeList.Count > idx && idx >= 0)
            {
                return GridTerrainTypeList[idx];    // idx
            }
            else
            {
                return 0;
            }
        }

        #region get/set ·½·¨

        public GridTerrainData GetTerrainData(List<Vector2Int> offsetHex) {
            // TODO : offset coord
            return null;
        }

        public void SetDirty()
        {
            IsDirty = true;
        }

        #endregion

    }
}
