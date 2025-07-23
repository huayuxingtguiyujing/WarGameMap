using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    public class RiverData
    {
        public ushort riverID;

        public string riverName;
        
        // NOTE : river workflow : Texture, Curve
        public BezierCurve curve;

        public List<Vector2Int> pixels;


        public List<Vector2Int> existTerrainClusterIDs;

        public RiverData(ushort riverID, string riverName, BezierCurve curve, List<Vector2Int> pixels, List<Vector2Int> existTerrainClusterIDs) {
            this.riverID = riverID;
            this.riverName = riverName;
            this.curve = curve;
            this.pixels = pixels;
            this.existTerrainClusterIDs = existTerrainClusterIDs;
        }

        public void CopyRiverData(RiverData other) {
            this.riverID = other.riverID;
            this.riverName = other.riverName;
            this.curve = other.curve;
            this.pixels = other.pixels;
            this.existTerrainClusterIDs = new List<Vector2Int>(other.existTerrainClusterIDs);
        }
    }
}
