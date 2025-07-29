using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace LZ.WarGameMap.Runtime
{

    [Serializable]
    public struct BezierNode {
        public Vector3 position;   // 主点（锚点）
        public Vector3 handleIn;   // 入控制柄（相对于 position）
        public Vector3 handleOut;  // 出控制柄（相对于 position）

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

        public static BezierNode Average(BezierNode node1, BezierNode node2)
        {
            return new BezierNode((node1.position + node2.position) / 2,
                (node1.handleIn + node2.handleIn) / 2,
                (node1.handleOut + node2.handleOut) / 2);
        }

    }

    [Serializable]
    public class BezierCurve {

        [SerializeField]
        private List<BezierNode> nodes;

        public IReadOnlyList<BezierNode> Nodes => nodes;

        public int Count => nodes.Count;

        public void SetNode(int i, BezierNode node) { nodes[i] = node; }

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


        #region Bezier步进

        // Cache node, so faster
        class BezierCache_StepDistance
        {
            public int nodeIdx;
            public List<float> NodeDistances;
            public List<float> NodeSegments;

            public BezierCache_StepDistance(int nodeIdx, int nodeCnt)
            {
                this.nodeIdx = nodeIdx;
                NodeDistances = new List<float>(nodeCnt);
                NodeSegments = new List<float>(nodeCnt);
            }
            
            public void ResetCache(int nodeIdx)
            {
                this.nodeIdx = nodeIdx;
            }

            public void ClearCache()
            {
                nodeIdx = 0;
                if(NodeDistances != null)
                {
                    NodeDistances.Clear();
                }
                if (NodeSegments != null)
                {
                    NodeSegments.Clear();
                }
            }

            public override bool Equals(object obj)
            {
                return obj is BezierCache_StepDistance cache && nodeIdx == cache.nodeIdx;
            }
        }

        BezierCache_StepDistance sampleCache;

        public void InitGetDistanceCache()
        {
            if (sampleCache == null)
            {
                sampleCache = new BezierCache_StepDistance(0, nodes.Count);
            }
            else
            {
                sampleCache.ClearCache();
            }

            float accumulated = 0f;
            // NOTE : NodeDistances[1] : bezier node[0] to bezier node[1]
            //        NodeDistances[i] : bezier node[0] to bezier node[i]
            // NOTE : NodeSegments[0] : bezier node[0] to bezier node[1]
            //        NodeSegments[i] : bezier node[i] to bezier node[i + 1]
            sampleCache.NodeDistances.Add(0);
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Vector3 p0 = nodes[i].position;
                Vector3 p1 = nodes[i].handleOut;
                Vector3 p2 = nodes[i + 1].handleIn;
                Vector3 p3 = nodes[i + 1].position;

                float segLength = EstimateBezierLength(p0, p1, p2, p3);
                accumulated += segLength;
                
                sampleCache.NodeDistances.Add(accumulated);
                sampleCache.NodeSegments.Add(segLength);
            }
        }

        // Distance -> position on curve
        public void GetPointAtDistance(float distance, out Vector3 point, out Vector3 tangent, out bool isFinal)
        {
            if(sampleCache == null)
            {
                Debug.LogError("please call InitGetDistanceCache firstly!");
                point =  Vector3.zero;
                tangent = Vector3.zero;
                isFinal = true;
                return;
            }

            int iterCnt = 0;
            int curIdx = sampleCache.nodeIdx;
            while (iterCnt < nodes.Count)
            {
                if(curIdx >= nodes.Count - 1)
                {
                    curIdx = 0;
                }

                // find node that this distance exist in. and 
                float curDistance = sampleCache.NodeDistances[curIdx];
                float nextDistance = sampleCache.NodeDistances[curIdx + 1];
                float curSegment = sampleCache.NodeSegments[curIdx];
                if (distance > curDistance && distance < nextDistance)
                {
                    Vector3 p0 = nodes[curIdx].position;
                    Vector3 p1 = nodes[curIdx].handleOut;
                    Vector3 p2 = nodes[curIdx + 1].handleIn;
                    Vector3 p3 = nodes[curIdx + 1].position;

                    float remaining = distance - curDistance;
                    float t = FindTForDistance(p0, p1, p2, p3, remaining, curSegment);
                    tangent = EvaluateTangent(p0, p1, p2, p3, t).normalized;
                    point = EvaluateCubicBezier(p0, p1, p2, p3, t);
                    isFinal = (curIdx == nodes.Count - 2) && (point == p3);

                    sampleCache.ResetCache(curIdx);
                    return;
                }

                curIdx++;
                iterCnt++;
            }

            point = Vector3.zero;
            tangent = Vector3.zero;
            isFinal = false;
            return;
        }

        public float GetTotalLength()
        {
            float total = 0f;
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                Vector3 p0 = nodes[i].position;
                Vector3 p1 = nodes[i].handleOut;
                Vector3 p2 = nodes[i + 1].handleIn;
                Vector3 p3 = nodes[i + 1].position;
                total += EstimateBezierLength(p0, p1, p2, p3);
            }
            return total;
        }


        // TODO : 完成这里的逻辑
        class BezierCache_PointDistance
        {
            public int nodeIdx;

        }

        public float GetDistanceToBezier(Vector3 point)
        {
            // TODO : 这里的功能是 传入一个point，返回这个point到贝塞尔曲线的距离
            // 目前想到了一种简化处理的方法
            return 0;
        }

        private void GetClosetNodeInCurve(Vector3 point)
        {

        }

        private float EstimateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps = 15)
        {
            float length = 0f;
            Vector3 prev = p0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pt = EvaluateCubicBezier(p0, p1, p2, p3, t);
                length += Vector3.Distance(prev, pt);
                prev = pt;
            }
            return length;
        }

        private float FindTForDistance(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float targetDist, float segmentLength)
        {
            float low = 0f, high = 1f, mid = 0f;
            const float tolerance = 0.01f;

            for (int iter = 0; iter < 20; iter++)
            {
                mid = (low + high) * 0.5f;
                float dist = 0f;
                Vector3 prev = p0;
                for (int i = 1; i <= 10; i++)
                {
                    float t = mid * i / 10f;
                    Vector3 pt = EvaluateCubicBezier(p0, p1, p2, p3, t);
                    dist += Vector3.Distance(prev, pt);
                    prev = pt;
                }

                if (Mathf.Abs(dist - targetDist) < tolerance)
                    break;
                if (dist < targetDist)
                    low = mid;
                else
                    high = mid;
            }
            return mid;
        }

        private Vector3 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return u * u * u * p0
                 + 3 * u * u * t * p1
                 + 3 * u * t * t * p2
                 + t * t * t * p3;
        }

        private Vector3 EvaluateTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return 3 * u * u * (p1 - p0)
                 + 6 * u * t * (p2 - p1)
                 + 3 * t * t * (p3 - p2);
        }

        #endregion


        // 当前的函数中 必须传入 当前这条河流绘制时所使用的 paintScope
        public static BezierCurve GetFitCurve(List<Vector2Int> rawPoints, RiveStartData rvStart, TerrainSettingSO terSet, int paintScope)
        {
            //List<BezierCurve> bezierCurves = new List<BezierCurve>(); // , int iterTime
            //for (int i = 0; i < iterTime; i++)
            //{
            //List<Vector2Int> featurePoint = FindFeaturePoints(rawPoints, segmentCnt, 5, 2);
            //List<Vector2Int> featurePoint = FindFeaturePoints(rawPoints, paintScope);
            //BezierCurve bezierCurve = ConvertPolylineToBezier(featurePoint);
            //bezierCurves.Add(bezierCurve);
            //}

            //List<Vector2Int> featurePoint = FindFeaturePoints(rawPoints, terSet, paintScope);


            //List<Vector2Int> featurePoint = SimplifyPoints(rawPoints, 5);
            List<Vector2Int> featurePoint = SortedFeaturePoints(rvStart, ref rawPoints, terSet);

            BezierCurve bezierCurve = ConvertPolylineToBezier(featurePoint);
            int nodeNum = bezierCurve != null ? bezierCurve.Count : -1;

            Debug.Log($"get feature point num {featurePoint.Count}, curve node num : {nodeNum} ");
            return bezierCurve;

            //return AverageBezierCurve(bezierCurves);
        }

        [Obsolete]
        private static List<Vector2Int> FindFeaturePoints(List<Vector2Int> rawPoints, TerrainSettingSO terSet, int paintScope)
        {
            HashSet<Vector2Int> pointSets = new HashSet<Vector2Int>();
            foreach (var point in rawPoints)
            {
                Vector2Int rvEditPixelPos = MapRiverData.GetRvEditPixelByMapPos(point, terSet.clusterSize, terSet.paintRTSizeScale, out Vector2Int clusterID);
                pointSets.Add(rvEditPixelPos);
            }

            // NOTE : feature point is point in the spline, but this method seems not working
            List<Vector2Int> featurePoints = new List<Vector2Int>();
            foreach (var point in rawPoints)
            {
                int bias = 5;
                bool isFeaturePoint = true;
                for (int i = -paintScope; i <= paintScope; i++)
                {
                    for (int j = -paintScope; j <= paintScope; j++)
                    {
                        Vector2Int pixel = MapRiverData.GetRvEditPixelByMapPos(point, terSet.clusterSize, terSet.paintRTSizeScale, out Vector2Int clusterID)  + new Vector2Int(i, j);
                        if (!pointSets.Contains(pixel))
                        {
                            bias--;
                            if (bias <= 0)  isFeaturePoint = false;
                            break;
                        }
                    }
                    if (!isFeaturePoint) break;
                }
                if (isFeaturePoint)
                {
                    featurePoints.Add(point);
                }
            }
            return featurePoints;
        }

        private static List<Vector2Int> SimplifyPoints(List<Vector2Int> rawPoints, int segmentCnt, int fixSegment = 5, int randScope = 2)
        {
            // this function will find the feature point set in a discrete points set
            // 1. get point in rawPoints randomly
            // 2. fix the curve, point which is too far to expected step will be corrected 
            List<Vector2Int> featurePoints = new List<Vector2Int>();
            int nodeCnt = rawPoints.Count / segmentCnt;
            int step = 0;
            for(int i = 0; i < nodeCnt; i++)
            {
                if (step >= rawPoints.Count)
                {
                    step = rawPoints.Count - 1;
                }
                featurePoints.Add(rawPoints[step]);
                step += segmentCnt;
                step += UnityEngine.Random.Range(-randScope, randScope);

                // fix the curve
                if (i % fixSegment == 0)
                {
                    int expectedStep = i * segmentCnt;
                    if (Math.Abs(expectedStep - step) > segmentCnt)
                    {
                        step = expectedStep;
                    }
                }
            }
            return featurePoints;
        }

        private static List<Vector2Int> SortedFeaturePoints(RiveStartData rvStartData, ref List<Vector2Int> featurePoint, TerrainSettingSO terSet)
        {
            // sorted : find closest point to cur point
            Vector2Int rvStartInWorldPos = MapRiverData.GetMapPosByRvEditPixel(rvStartData.riverStart, terSet.clusterSize, terSet.paintRTSizeScale, rvStartData.rvStartClsID);

            HashSet<Vector2Int> hasChoosenPointSets = new HashSet<Vector2Int>();
            foreach (var point in featurePoint)
            {
                hasChoosenPointSets.Add(point);
            }

            Vector2Int curPoint = rvStartInWorldPos;
            List <Vector2Int> sortedData = new List<Vector2Int>();
            for (int i = 0; i < featurePoint.Count; i ++)
            {
                sortedData.Add(curPoint);
                if (hasChoosenPointSets.Contains(curPoint))
                {
                    hasChoosenPointSets.Remove(curPoint);
                }

                Vector2Int nextPoint = new Vector2Int(-1000, -1000);
                
                foreach (var point in hasChoosenPointSets)
                {
                    if (Vector2Int.Distance(curPoint, point) < Vector2Int.Distance(curPoint, nextPoint))
                    {
                        nextPoint = point;
                    }
                }
                curPoint = nextPoint;
            }

            return sortedData;
        }

        private static BezierCurve ConvertPolylineToBezier(List<Vector2Int> polyline, float smoothness = 0.6f)
        {
            if (polyline == null || polyline.Count < 2)
            {
                return null;
            }
            BezierCurve bezier = new BezierCurve();
            for (int i = 0; i < polyline.Count; i++)
            {
                Vector3 pPrev = (i > 0) ? polyline[i - 1].TransToXZ() : polyline[i].TransToXZ();
                Vector3 pCurr = polyline[i].TransToXZ();
                Vector3 pNext = (i < polyline.Count - 1) ? polyline[i + 1].TransToXZ() : polyline[i].TransToXZ();

                Vector3 dir = (pNext - pPrev).normalized;
                float distPrev = Vector3.Distance(pCurr, pPrev);
                float distNext = Vector3.Distance(pCurr, pNext);
                Vector3 handleIn = pCurr - dir * distPrev * smoothness;
                Vector3 handleOut = pCurr + dir * distNext * smoothness;

                if (i == 0)
                {
                    handleIn = pCurr;
                }
                if (i == polyline.Count - 1)
                {
                    handleOut = pCurr;
                }
                bezier.Add(new BezierNode(pCurr, handleIn, handleOut));
            }

            return bezier;
        }

        private static BezierCurve AverageBezierCurve(List<BezierCurve> bezierCurves)
        {
            // check curve data valid
            if (bezierCurves == null || bezierCurves.Count <= 1)
            {
                return null;
            }
            int curveCnt = bezierCurves.Count;
            int nodeCnt = bezierCurves[0].Count;
            for(int i = 0; i < curveCnt; i++)
            {
                BezierCurve curve = bezierCurves[i];
                if (curve.Count != nodeCnt)
                {
                    return null;
                }
            }

            BezierCurve targetCurve = new BezierCurve();
            for (int i = 0; i < nodeCnt; i ++)
            {
                BezierNode averageNode = bezierCurves[0].Nodes[i];
                for (int j = 1; j < curveCnt; j++) 
                {
                    averageNode = BezierNode.Average(averageNode, bezierCurves[j].Nodes[i]);
                }
                targetCurve.Add(averageNode);
            }
            return targetCurve;
        }

    }

}
