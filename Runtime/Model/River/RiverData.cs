using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    [Serializable]
    public struct RiveStartData
    {
        [HorizontalGroup("RiveStartData"), LabelText("河流起点地块")]
        public Vector2Int rvStartClsID;

        [HorizontalGroup("RiveStartData"), LabelText("河流起点位置")]
        public Vector2Int riverStart;   // pixel pos in painted rt

        public RiveStartData(Vector2Int rvStartClsID, Vector2Int riverStart)
        {
            this.rvStartClsID = rvStartClsID;
            this.riverStart = riverStart;
        }
    }

    [Serializable]
    public class RiverData
    {
        public ushort riverID;

        public string riverName;

        public RiveStartData riverStart = MapRiverData.UnvalidRvStart;

        public void UpdateRvStart(RiveStartData riverStart) { this.riverStart = riverStart; }


        // NOTE : river workflow : Texture, Curve
        public BezierCurve curve;

        public List<Vector2Int> pixels;

        Dictionary<Vector2Int, List<Vector2Int>> pixelDict;


        public List<Vector2Int> existTerrainClusterIDs;

        HashSet<Vector2Int> existTerrainClusterDict;


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
            this.riverStart = other.riverStart;

            this.curve = other.curve;
            this.pixels = other.pixels;
            this.existTerrainClusterIDs = new List<Vector2Int>(other.existTerrainClusterIDs);
        }

        // Key : clusterID; Value : pixel
        public void UpdateClusterPixelDict(int clusterSize, ushort paintRTSizeScale)
        {
            if(pixelDict != null)
            {
                pixelDict.Clear();
            }
            pixelDict = new Dictionary<Vector2Int, List<Vector2Int>>();
            foreach (var clusterID in existTerrainClusterIDs)
            {
                pixelDict.Add(clusterID, new List<Vector2Int>());
            }
            // NOTE : 做这个修正，是为了让 dict里面存储的 pixel 对应的是 绘制河流所使用的纹理
            // 为了节省存储空间，绘制河流所使用的纹理会有一个相比于大地图的缩放值
            foreach (Vector2Int pixel in pixels)
            {
                //Vector2Int clusterID = new Vector2Int(pixel.x / clusterSize, pixel.y / clusterSize);    // 为什么是反的？？？
                //Vector2Int inclsPos = new Vector2Int(pixel.y % clusterSize, pixel.x % clusterSize);
                //Vector2Int rvPixelPos = inclsPos / paintRTSizeScale;

                Vector2Int rvPixelPos = MapRiverData.GetRvEditPixelByMapPos(pixel, clusterSize, paintRTSizeScale, out Vector2Int clusterID);
                pixelDict[clusterID].Add(rvPixelPos);
            }

            //foreach (var pair in pixelDict)
            //{
            //    //Debug.Log($"debug, cluster id {pair.Key}, it contains {pair.Value.Count} pixel");
            //}
        }

        public HashSet<Vector2Int> UpdateExistTerClsDict()
        {
            foreach (var clsID in existTerrainClusterIDs)
            {
                existTerrainClusterDict.Add(clsID);
            }
            return existTerrainClusterDict;
        }

        public List<Vector2Int> GetPaintedClsPixles(Vector2Int clusterId)
        {
            List<Vector2Int> clsPixels = new List<Vector2Int>();
            if (pixelDict.ContainsKey(clusterId))
            {
                return pixelDict[clusterId];
            }
            clsPixels.Add(new Vector2Int(-1, -1));
            return clsPixels;
        }

    }
}
