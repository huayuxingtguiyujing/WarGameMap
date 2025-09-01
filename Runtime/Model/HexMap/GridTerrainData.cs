using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime.HexStruct;
using System;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    public class GridTerrainData
    {
        Hexagon hexagon;
        Vector2Int hexIdx;      // offset hex coord
        Vector2 hexGridCenter;

        public GridTerrainType terrainType;

        public GridTerrainData() { }

        public void InitGridTerrainData(Vector2Int hexIdx, Hexagon hexagon, Vector3 hexCenter)
        {
            this.hexIdx = hexIdx;
            this.hexagon = hexagon;
            this.hexGridCenter = new Vector2(hexCenter.x, hexCenter.z);     // TODO : hexCenter is world pos
        }

        #region get data

        public Color GetTerrainColor()
        {
            return terrainType.terrainEditColor;
        }

        public Vector2Int GetHexPos()
        {
            return hexIdx;
        }

        public Vector2 GetHexCenter()
        {
            return hexGridCenter;
        }

        public Hexagon GetHexagon()
        {
            return hexagon;
        }

        #endregion
    }

}
