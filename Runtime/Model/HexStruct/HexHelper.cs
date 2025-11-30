using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    public enum OutHexAreaEnum {
        Edge, LeftCorner, RightCorner, None
        // l : left corner 
        // r : right corner
        // e : Edge
        // A----------B
        //  \l| e  |r/
        //   \|    |/
        //    a----b(inner triangle)
        //     \  /
        //       C

        // v2
        // A----------B
        //  \    |   /
        //   \ l |r /
        //    \  | /
        //     \ |/
        //       C
    }

    // This struct discribe the relation of point in a hex
    public struct HexAreaPointData {

        public Vector2 worldPos;

        public bool insideInnerHex;
        
        public HexDirection hexAreaDir;     // point's existing direction of hex

        public OutHexAreaEnum outHexAreaEnum;       // 

        public float ratioBetweenInnerAndOutter;
    }

    public static class HexHelper 
    {
        // Transfer offset and axial
        public static Vector3Int PixelToAxialHexVector(Vector2 worldPos, int HexGridSize) {
            float q = (Mathf.Sqrt(3) / 3 * worldPos.x - 1.0f / 3 * worldPos.y) / HexGridSize;
            float r = 2.0f / 3 * worldPos.y / HexGridSize;
            float s = -q - r;

            int fix_q = Mathf.RoundToInt(q);
            int fix_r = Mathf.RoundToInt(r);
            int fix_s = Mathf.RoundToInt(s);

            float q_diff = Mathf.Abs(fix_q - q);
            float r_diff = Mathf.Abs(fix_r - r);
            float s_diff = Mathf.Abs(fix_s - s);

            int final_q = fix_q, final_r = fix_r, final_s = fix_s;
            if (q_diff > r_diff && q_diff > s_diff) {
                final_q = -fix_r - fix_s;
            } else if (r_diff > s_diff) {
                final_r = -fix_q - fix_s;
            } else {
                final_s = -fix_q - fix_r;
            }
            return new Vector3Int(final_q, final_r, final_s);
        }

        public static Hexagon PixelToAxialHex(Vector2 worldPos, int HexGridSize, bool reverseAxial = false) {
            Vector3Int hex = PixelToAxialHexVector(worldPos, HexGridSize);
            if (reverseAxial) {
                //hex.x = -hex.x;
            }
            return new Hexagon(hex.x, hex.y, hex.z);
        }

        public static Vector2Int AxialToOffset(Hexagon hex) {
            var col = hex.q + (hex.r - (hex.r & 1)) / 2;
            var row = hex.r;
            return new Vector2Int(col, row);
        }

        public static Hexagon OffsetToAxial(Vector2Int offset) {
            var q = offset.x - (offset.y - (offset.y & 1)) / 2;
            var r = offset.y;
            return new Hexagon(q, r, -q - r);
        }

        public static Vector3 OffsetToWorld(Layout layout, Vector2Int offset)
        {
            Hexagon hexagon = OffsetToAxial(offset);
            Vector2 center = hexagon.Get_Hex_Center(layout);
            return center.TransToXZ();
        }

        // Neighbor and Edge point
        public static Vector2Int[] GetOffsetHexNeighbour(Vector2Int offsetHex) {
            Vector2Int[] neighbour;
            if (offsetHex.y % 2 == 1)
            { // offsetHex.x % 2 == 1  // ??? 哪个是对的？
                // W  NW  NE  E  SE  SW
                // 奇数行邻居偏移，[0] 是左边的邻居，顺时针转动
                neighbour = new Vector2Int[6]{
                    OffsetHexOddNeighbor.W, OffsetHexOddNeighbor.NW, OffsetHexOddNeighbor.NE,
                    OffsetHexOddNeighbor.E, OffsetHexOddNeighbor.SE, OffsetHexOddNeighbor.SW};
            } 
            else
            {
                neighbour = new Vector2Int[6]{
                    OffsetHexEvenNeighbor.W, OffsetHexEvenNeighbor.NW, OffsetHexEvenNeighbor.NE,
                    OffsetHexEvenNeighbor.E, OffsetHexEvenNeighbor.SE, OffsetHexEvenNeighbor.SW};
            }
            return neighbour;
        }

        public static List<Vector2Int> GetOffsetHexNeighbour_Scope(Vector2Int offsetHex, int scope)
        {
            int count = 0;
            HashSet<Vector2Int> neighborList = new HashSet<Vector2Int>(scope * scope);
            Queue<Vector2Int> curLevelNeighbor = new Queue<Vector2Int>(scope * scope);
            curLevelNeighbor.Enqueue(offsetHex);
            neighborList.Add(offsetHex);
            while (scope > 0)
            {
                int curLevelNum = curLevelNeighbor.Count;
                for(int i = 0; i < curLevelNum; i++)
                {
                    Vector2Int hex = curLevelNeighbor.Dequeue();
                    Vector2Int[] neighbour = GetOffsetHexNeighbour(hex);
                    for (int j = 0; j < 6; j++)
                    {
                        Vector2Int neighbor = hex + neighbour[j];
                        if (!neighborList.Contains(neighbor))
                        {
                            curLevelNeighbor.Enqueue(neighbor);
                            neighborList.Add(neighbor);
                        }
                    }
                    count++;
                }
                scope--;
            }
            //Debug.Log($"iter time : {count}");
            return neighborList.ToList();
        }

        // TODO : 现在获取不到正确的边缘点，为什么？
        public static Vector2[] GetOffsetHexEdgePoint(Layout layout, Vector2Int offsetHex, Vector2Int neighbor)
        {
            // NOTE : 
            // W  NW  NE  E  SE  SW
            //
            //   NW / 1 \ NE
            //    2/     \0 (corner)
            //    |       | 
            //  W |       | E
            //    3\     /5
            //      \   / 
            //   SW   4   SE
            //
            Vector2[] edgePoint = new Vector2[2];
            Hexagon hexagon = OffsetToAxial(offsetHex);
            HexDirection direction = GetOffsetHexDir(offsetHex, neighbor);
            switch (direction)
            {
                
                case HexDirection.W:
                    edgePoint[0] = hexagon.Get_Hex_CornerPos(layout, 2);
                    edgePoint[1] = hexagon.Get_Hex_CornerPos(layout, 3);
                    break;
                case HexDirection.NW:
                    edgePoint[0] = hexagon.Get_Hex_CornerPos(layout, 1);
                    edgePoint[1] = hexagon.Get_Hex_CornerPos(layout, 2);
                    break;
                case HexDirection.NE:
                    edgePoint[0] = hexagon.Get_Hex_CornerPos(layout, 0);
                    edgePoint[1] = hexagon.Get_Hex_CornerPos(layout, 1);
                    break;
                case HexDirection.E:
                    edgePoint[0] = hexagon.Get_Hex_CornerPos(layout, 0);
                    edgePoint[1] = hexagon.Get_Hex_CornerPos(layout, 5);
                    break;
                case HexDirection.SE:
                    edgePoint[0] = hexagon.Get_Hex_CornerPos(layout, 4);
                    edgePoint[1] = hexagon.Get_Hex_CornerPos(layout, 5);
                    break;
                case HexDirection.SW:
                    edgePoint[0] = hexagon.Get_Hex_CornerPos(layout, 3);
                    edgePoint[1] = hexagon.Get_Hex_CornerPos(layout, 4);
                    break;
            }
            return edgePoint;
        }

        public static HexDirection GetOffsetHexDir(Vector2Int offsetHex, Vector2Int neighbor)
        {
            Vector2Int dir = neighbor - offsetHex;
            if (offsetHex.y % 2 == 1)
            {
                if (dir == OffsetHexOddNeighbor.W)
                {
                    return HexDirection.W;
                }
                else if (dir == OffsetHexOddNeighbor.NW)
                {
                    return HexDirection.NW;
                }
                else if (dir == OffsetHexOddNeighbor.NE)
                {
                    return HexDirection.NE;
                }
                else if (dir == OffsetHexOddNeighbor.E)
                {
                    return HexDirection.E;
                }
                else if (dir == OffsetHexOddNeighbor.SE)
                {
                    return HexDirection.SE;
                }
                else if (dir == OffsetHexOddNeighbor.SW)
                {
                    return HexDirection.SW;
                }
            }
            else
            {
                if (dir == OffsetHexEvenNeighbor.W)
                {
                    return HexDirection.W;
                }
                else if (dir == OffsetHexEvenNeighbor.NW)
                {
                    return HexDirection.NW;
                }
                else if (dir == OffsetHexEvenNeighbor.NE)
                {
                    return HexDirection.NE;
                }
                else if (dir == OffsetHexEvenNeighbor.E)
                {
                    return HexDirection.E;
                }
                else if (dir == OffsetHexEvenNeighbor.SE)
                {
                    return HexDirection.SE;
                }
                else if (dir == OffsetHexEvenNeighbor.SW)
                {
                    return HexDirection.SW;
                }
            }
            return HexDirection.None;
        }

        // Hex grid terrain
        public static HexAreaPointData GetPointHexArea(Vector2 worldPos, Hexagon hex, Layout layout, float fix) {
            Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();
            Vector2 hexCenter = new Vector2((float)center.x, (float)center.z);

            HexAreaPointData data = new HexAreaPointData();
            data.worldPos = worldPos;
            data.outHexAreaEnum = OutHexAreaEnum.None;

            for (int k = 0; k < 6; k++) {
                Vector2 curPoint = hexCenter + hex.Hex_Corner_Offset(layout, k) * fix;
                Vector2 nextPoint = hexCenter + hex.Hex_Corner_Offset(layout, k + 1) * fix;
                bool inTriangle = MathUtil.IsInsideTriangle(hexCenter, curPoint, nextPoint, worldPos);

                Vector2 curOutPoint = hex.Get_Hex_CornerPos(layout, k);
                Vector2 nextOutPoint = hex.Get_Hex_CornerPos(layout, k + 1);
                bool inOutTriangle = MathUtil.IsInsideTriangle(hexCenter, curOutPoint, nextOutPoint, worldPos);

                if (inTriangle) {
                    data.insideInnerHex = true;
                    data.hexAreaDir = (HexDirection)Enum.ToObject(typeof(HexDirection), k);
                    return data;
                }
                if (inOutTriangle) {
                    data.hexAreaDir = (HexDirection)Enum.ToObject(typeof(HexDirection), k);
                }
            }

            // this pos is not inside the hex, so we need know more info to blend hexgrid texture
            data.insideInnerHex = false;
            HandleOutAreaPointData(worldPos, hex, layout, fix, ref data);

            return data;
        }

        private static void HandleOutAreaPointData(Vector2 worldPos, Hexagon hex, Layout layout, float fix, ref HexAreaPointData data) {
            Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();
            Vector2 hexCenter = new Vector2((float)center.x, (float)center.z);

            // construct the innerA, innerB and Point
            // find the relation between worldPos and a、b
            int idx = (int)data.hexAreaDir;
            Vector2 innerA = hexCenter + hex.Hex_Corner_Offset(layout, idx) * fix;
            Vector2 innerB = hexCenter + hex.Hex_Corner_Offset(layout, idx + 1) * fix;
            data.outHexAreaEnum = GetOutHexArea(innerA, innerB, hexCenter, worldPos);
            float distanceToInnerAB = MathUtil.DistancePointToLine(innerA, innerB, worldPos);

            Vector2 outterA = hexCenter + hex.Hex_Corner_Offset(layout, idx);
            float innerToOutter = Vector2.Distance(innerA, outterA);
            float distanceOutterToInner = innerToOutter * Mathf.Sqrt(3) / 2;

            data.ratioBetweenInnerAndOutter = distanceToInnerAB / (distanceOutterToInner * 2);  //distanceOutterToInner
        }

        // NOTE : A must be the left, please view graph in OutHexAreaEnum 
        private static OutHexAreaEnum GetOutHexArea(Vector2 innerA, Vector2 innerB, Vector2 center, Vector2 worldPos) {
            Vector2 ab_center = (innerA + innerB) / 2;
            float ab_distance_half = Vector2.Distance(innerA, innerB) / 2;
            float pos_center_distance = MathUtil.DistancePointToLine(ab_center, center, worldPos);
            float pos_a_distance = Vector2.Distance(worldPos, innerA);
            float pos_b_distance = Vector2.Distance(worldPos, innerB);

            // DistanceLineToLine

            if (pos_center_distance >= ab_distance_half && pos_a_distance >= pos_b_distance) {
                return OutHexAreaEnum.LeftCorner;
            } else if (pos_center_distance >= ab_distance_half && pos_a_distance < pos_b_distance) {
                return OutHexAreaEnum.RightCorner;
            } else {
                return OutHexAreaEnum.Edge;
            }
        }

        public static HexAreaPointData GetPointHexArea_NoInner(Vector2 worldPos, Hexagon hex, Layout layout) {
            // this method blend hex with its six neighbor hex, no divide of inner and outter triangle
            Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();
            Vector2 hexCenter = new Vector2((float)center.x, (float)center.z);

            HexAreaPointData data = new HexAreaPointData();
            data.worldPos = worldPos;
            data.hexAreaDir = HexDirection.None;
            data.outHexAreaEnum = OutHexAreaEnum.LeftCorner;

            for (int k = 0; k < 6; k++) {
                Vector2 curOutPoint = hex.Get_Hex_CornerPos(layout, k);
                Vector2 nextOutPoint = hex.Get_Hex_CornerPos(layout, k + 1);
                bool inHexTriangle = MathUtil.IsInsideTriangle(hexCenter, curOutPoint, nextOutPoint, worldPos);

                if (inHexTriangle) {
                    data.hexAreaDir = (HexDirection)Enum.ToObject(typeof(HexDirection), k);

                    Vector2 pointCenter = (curOutPoint + nextOutPoint) / 2;
                    bool inLeftTriangle = MathUtil.IsInsideTriangle(hexCenter, pointCenter, nextOutPoint, worldPos);
                    if (inLeftTriangle) {
                        data.outHexAreaEnum = OutHexAreaEnum.LeftCorner;
                    } else {
                        data.outHexAreaEnum = OutHexAreaEnum.RightCorner;
                    }
                    break;
                }
            }
            return data;
        }

    }
}
