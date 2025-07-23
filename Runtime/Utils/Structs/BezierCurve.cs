using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    [Serializable]
    public class BezierNode {
        public Vector3 position;   // ���㣨ê�㣩
        public Vector3 handleIn;   // ����Ʊ�������� position��
        public Vector3 handleOut;  // �����Ʊ�������� position��

        public BezierNode(Vector3 pos) {
            position = pos;
            handleIn = Vector3.zero;
            handleOut = Vector3.zero;
        }

        public BezierNode(Vector3 pos, Vector3 inHandle, Vector3 outHandle) {
            position = pos;
            handleIn = inHandle;
            handleOut = outHandle;
        }

        public Vector3 GetWorldHandleIn() => position + handleIn;
        public Vector3 GetWorldHandleOut() => position + handleOut;
    }

    [Serializable]
    public class BezierCurve {

        [SerializeField]
        private List<BezierNode> nodes;
        public IReadOnlyList<BezierNode> Nodes => nodes;
        public int Count => nodes.Count;

        public BezierCurve() {
            nodes = new List<BezierNode>();
        }

        public void Add(BezierNode node) {
            nodes.Add(node);
        }

        public void Delete(int index) {
            if (index >= 0 && index < nodes.Count) {
                nodes.RemoveAt(index);
            }
        }

        public static BezierCurve FitCurve(List<Vector2Int> rawPoints, int segmentCount) {
            if (rawPoints == null || rawPoints.Count < 2)
                throw new ArgumentException("������Ҫ�����������ê��");

            List<Vector2> points = rawPoints.Select(p => (Vector2)p).ToList();
            int total = points.Count;

            // �Ⱦ໮�ֳɶΣ����� = nodeCount - 1��
            //int segmentCount = nodeCount - 1;
            int step = total / segmentCount;
            if (step < 2) step = 2; // ÿ������������

            BezierCurve curve = new BezierCurve();

            for (int seg = 0; seg < segmentCount; seg++) {
                int start = seg * step;
                int end = (seg == segmentCount - 1) ? total - 1 : (seg + 1) * step;

                List<Vector2> segment = points.GetRange(start, end - start + 1);

                // ȡ�˵�
                Vector2 p0 = segment[0];
                Vector2 p3 = segment[^1];

                // �ҳ�������
                float[] t = new float[segment.Count];
                t[0] = 0f;
                float totalLen = 0f;
                for (int i = 1; i < segment.Count; i++) {
                    totalLen += Vector2.Distance(segment[i], segment[i - 1]);
                    t[i] = totalLen;
                }
                for (int i = 1; i < t.Length; i++)
                    t[i] /= totalLen;

                // ��С������Ͽ��Ƶ�
                float c11 = 0f, c12 = 0f, c22 = 0f;
                Vector2 x1 = Vector2.zero, x2 = Vector2.zero;

                for (int i = 0; i < segment.Count; i++) {
                    float ti = t[i];
                    float b1 = 3 * Mathf.Pow(1 - ti, 2) * ti;
                    float b2 = 3 * (1 - ti) * Mathf.Pow(ti, 2);
                    Vector2 a = Mathf.Pow(1 - ti, 3) * p0 + Mathf.Pow(ti, 3) * p3;
                    Vector2 tmp = segment[i] - a;

                    c11 += b1 * b1;
                    c12 += b1 * b2;
                    c22 += b2 * b2;
                    x1 += b1 * tmp;
                    x2 += b2 * tmp;
                }

                float det = c11 * c22 - c12 * c12;
                Vector2 p1, p2;

                if (Mathf.Abs(det) > 1e-5f) {
                    float invDet = 1f / det;
                    float alpha1 = (c22 * Vector2.Dot(x1, Vector2.right) - c12 * Vector2.Dot(x2, Vector2.right)) * invDet;
                    float alpha2 = (c11 * Vector2.Dot(x2, Vector2.right) - c12 * Vector2.Dot(x1, Vector2.right)) * invDet;
                    p1 = p0 + alpha1 * Vector2.right;
                    p2 = p3 + alpha2 * Vector2.right;

                    // y ������������
                    alpha1 = (c22 * Vector2.Dot(x1, Vector2.up) - c12 * Vector2.Dot(x2, Vector2.up)) * invDet;
                    alpha2 = (c11 * Vector2.Dot(x2, Vector2.up) - c12 * Vector2.Dot(x1, Vector2.up)) * invDet;
                    p1.y = p0.y + alpha1;
                    p2.y = p3.y + alpha2;
                } else {
                    // �˻�Ϊ����
                    p1 = p0 + (p3 - p0) / 3f;
                    p2 = p0 + 2f * (p3 - p0) / 3f;
                }

                // ��һ��ê�㣺���һ�Σ��׶Σ��������ι���β����
                Vector3 p0_fix = p0.TransToXZ();
                Vector3 p1_fix = p1.TransToXZ();
                Vector3 p2_fix = p2.TransToXZ();
                Vector3 p3_fix = p3.TransToXZ();
                if (seg == 0) {
                    var outHandle = p1_fix - p0_fix;
                    curve.Add(new BezierNode(p0_fix, Vector3.zero, outHandle));
                }

                var inHandle = p2_fix - p3_fix;
                var outHandleNext = (seg == segmentCount - 1) ? Vector3.zero : p1_fix - p0_fix; // dummy if last
                curve.Add(new BezierNode(p3_fix, inHandle, outHandleNext));
            }

            return curve;
        }

    }

}
