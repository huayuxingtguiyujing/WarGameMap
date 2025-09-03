using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public static class MathUtil
    {

        /// <summary>
        /// 线性插值：返回从 a 到 b 的 ratio 位置的颜色
        /// </summary>
        public static Color ColorLinearLerp(Color a, Color b, float ratio) {
            return Color.LerpUnclamped(a, b, ratio);
        }

        public static float ColorInverseLerp(Color a, Color b, Color value)
        {
            Vector4 av = new Vector4(a.r, a.g, a.b, a.a);
            Vector4 bv = new Vector4(b.r, b.g, b.b, b.a);
            Vector4 vv = new Vector4(value.r, value.g, value.b, value.a);
            Vector4 ab = bv - av;
            Vector4 avv = vv - av;
            float t = Vector4.Dot(avv, ab) / Vector4.Dot(ab, ab);
            return Mathf.Clamp01(t);
        }


        /// <summary>
        /// 加速减速插值：两端慢，中间快（基于 SmoothStep 曲线变形）
        /// </summary>
        public static Color AccelerateDecelerateLerp(Color a, Color b, float ratio) {
            // Clamp ratio to [0,1] to ensure smooth behavior
            ratio = Mathf.Clamp01(ratio);

            // 使用加速减速曲线公式（cos函数变形）
            // t = (cos((x + 1) * PI) / 2) + 0.5
            float t = (Mathf.Cos((1 - ratio) * Mathf.PI) + 1f) * 0.5f;

            return Color.LerpUnclamped(a, b, t);
        }

        public static Color TriangleLerp(Color a, Color b, Color c, Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
            float areaABC = (B.x - A.x) * (C.y - A.y) - (C.x - A.x) * (B.y - A.y);
            if (Mathf.Approximately(areaABC, 0f)) {
                return a;
            }

            float u = ((B.x - P.x) * (C.y - P.y) - (C.x - P.x) * (B.y - P.y)) / areaABC;
            float v = ((C.x - P.x) * (A.y - P.y) - (A.x - P.x) * (C.y - P.y)) / areaABC;
            float w = 1f - u - v;
            return a * u + b * v + c * w;
        }

        public static Color TriangleLerp(Color a, Color b, Color c, Vector3 barycentric) {
            return a * barycentric.x + b * barycentric.y + c * barycentric.z;
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


        public static Vector3 ComputeBarycentric(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
            Vector2 v0 = B - A;
            Vector2 v1 = C - A;
            Vector2 v2 = P - A;

            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);

            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-6f) {
                // 三角形面积过小或退化，返回默认值
                return new Vector3(1f, 0f, 0f);
            }

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new Vector3(u, v, w);
        }

        /// <summary>
        /// 计算两条线段 AB 和 CD 之间的最短距离（默认平行，若相交返回0）
        /// </summary>
        public static float DistanceLineToLine(Vector2 A, Vector2 B, Vector2 C, Vector2 D) {
            // TODO : 目前这个方法好像有问题！
            Vector2 AB = B - A;
            Vector2 CD = D - C;
            float cross = AB.x * CD.y - AB.y * CD.x;
            if (Mathf.Approximately(cross, 0f)) {
                return DistancePointToLine(C, D, A);
            }
            return 0f;
        }

        /// <summary>
        /// 判断两线段 AB 与 CD 是否相交
        /// </summary>
        public static bool SegmentsIntersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D) {
            bool CCW(Vector2 p1, Vector2 p2, Vector2 p3) {
                return (p3.y - p1.y) * (p2.x - p1.x) > (p2.y - p1.y) * (p3.x - p1.x);
            }

            return CCW(A, C, D) != CCW(B, C, D) && CCW(A, B, C) != CCW(A, B, D);
        }
    
    }
}
