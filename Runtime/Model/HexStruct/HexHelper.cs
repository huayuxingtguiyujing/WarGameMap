using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections;
using System.Collections.Generic;
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
        //    a----b
        //     \  /
        //       C
    }

    public struct HexAreaPointData {
        public Vector2 worldPos;

        public bool insideInnerHex;
        public HexDirection hexAreaDir;
        public OutHexAreaEnum outHexAreaEnum;

        public float distanceToInnerAB;

    }



    public static class HexHelper {


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

        public static Hexagon PixelToAxialHex(Vector2 worldPos, int HexGridSize) {
            Vector3Int hex = PixelToAxialHexVector(worldPos, HexGridSize);
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

        public static Vector2Int[] GetOffsetHexNeighbour(Vector2Int offsetHex) {
            Vector2Int[] neighbour;
            if (offsetHex.x % 2 == 1) {
                // 奇数行的邻居 偏移，[0] 是左边的邻居，顺时针转动
                neighbour = new Vector2Int[6]{
                    new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(1, 1),
                    new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(-1, 0)};
            } else {
                // 偶数行的邻居 偏移
                neighbour = new Vector2Int[6]{
                    new Vector2Int( 0, -1), new Vector2Int(1, -1), new Vector2Int(1, 0),
                    new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(-1, -1)};
            }
            return neighbour;
        }



        public static HexAreaPointData GetPointHexArea(Vector2 worldPos, Hexagon hex, Layout layout, float fix) {
            Point center = hex.Hex_To_Pixel(layout).ConvertToXZ();
            Vector2 hexCenter = new Vector2((float)center.x, (float)center.z);

            HexAreaPointData data = new HexAreaPointData();
            data.worldPos = worldPos;
            data.outHexAreaEnum = OutHexAreaEnum.None;

            for (int k = 0; k < 6; k++) {
                Vector2 curPoint = hexCenter + hex.Hex_Corner_Offset(layout, k) * fix;
                Vector2 nextPoint = hexCenter + hex.Hex_Corner_Offset(layout, k + 1) * fix;
                bool inTriangle = IsInsideTriangle(hexCenter, curPoint, nextPoint, worldPos);

                Vector2 curOutPoint = hex.Get_Hex_CornerPos(layout, k);
                Vector2 nextOutPoint = hex.Get_Hex_CornerPos(layout, k + 1);
                bool inOutTriangle = IsInsideTriangle(hexCenter, curOutPoint, nextOutPoint, worldPos);

                if (inTriangle) {
                    data.insideInnerHex = true;
                    data.hexAreaDir = (HexDirection)Enum.ToObject(typeof(HexDirection), k);
                    return data;
                }
                if (inOutTriangle) {
                    data.hexAreaDir = (HexDirection)Enum.ToObject(typeof(HexDirection), k);
                }
            }

            // NOTE : 现在导出的纹理似乎会x对称，不知道原因，待修
            // this pos is not inside the hex, so we need know more info to blend hexgrid texture
            data.insideInnerHex = false;
            // TODO : 目前的方法有问题，主要是 insideInnerHex 会判断错误
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
            data.outHexAreaEnum = GetOutHexArea(innerA, innerB, center, worldPos);

            data.distanceToInnerAB = DistancePointToLine(innerA, innerB, worldPos);
        }

        // NOTE : A must be the left, please view graph in OutHexAreaEnum 
        private static OutHexAreaEnum GetOutHexArea(Vector2 innerA, Vector2 innerB, Vector2 center, Vector2 worldPos) {
            Vector2 ab_center = (innerA + innerB) / 2;
            float ab_distance_half = Vector2.Distance(innerA, innerB) / 2;
            float pos_center_distance = DistancePointToLine(ab_center, center, worldPos);
            float pos_a_distance = Vector2.Distance(worldPos, innerA);
            float pos_b_distance = Vector2.Distance(worldPos, innerB);

            if (pos_center_distance >= ab_distance_half && pos_a_distance >= pos_b_distance) {
                return OutHexAreaEnum.LeftCorner;
            } else if (pos_center_distance >= ab_distance_half && pos_a_distance < pos_b_distance) {
                return OutHexAreaEnum.RightCorner;
            } else {
                return OutHexAreaEnum.Edge;
            }
        }


        public static bool IsInsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
            Vector2 ab = B - A;
            Vector2 bc = C - B;
            Vector2 ca = A - C;

            Vector2 ap = P - A;
            Vector2 bp = P - B;
            Vector2 cp = P - C;

            float cross1 = ab.x * ap.y - ab.y * ap.x;
            float cross2 = bc.x * bp.y - bc.y * bp.x;
            float cross3 = ca.x * cp.y - ca.y * cp.x;

            bool hasNeg = (cross1 < 0) || (cross2 < 0) || (cross3 < 0);
            bool hasPos = (cross1 > 0) || (cross2 > 0) || (cross3 > 0);

            return !(hasNeg && hasPos);
        }

        public static float DistancePointToLine(Vector2 A, Vector2 B, Vector2 P) {
            Vector2 AB = B - A;
            Vector2 AP = P - A;

            float abLengthSquared = Vector2.Dot(AB, AB);
            if (abLengthSquared == 0) {     // if A is B
                return Vector2.Distance(P, A);
            }

            float t = Vector2.Dot(AP, AB) / abLengthSquared;
            Vector2 projection = A + t * AB;
            return Vector2.Distance(P, projection);
        }

    }
}
