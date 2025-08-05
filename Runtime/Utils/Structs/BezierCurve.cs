using NUnit;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor.PackageManager;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem;

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

                float segLength = BezierCurveHelper.EstimateBezierLength(p0, p1, p2, p3);
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

        class BezierSegmentLengthCache
        {
            int Count;
            List<float> lenSegment;

            public BezierSegmentLengthCache(int cnt)
            {
                this.Count = cnt;
                this.lenSegment = new List<float>(cnt);
                for(int i = 0; i < cnt; i++)
                {
                    this.lenSegment.Add(0);
                }
            }

            public float this[int index]
            {
                get {
                    if (index < 0 || index >= Count)
                    {
                        Debug.LogError($"not valid idx : {index}");
                        return 0;
                    }
                    return lenSegment[index]; 
                }
                set {
                    if (index < 0 || index >= Count)
                    {
                        Debug.LogError($"not valid idx : {index}");
                        return;
                    }
                    lenSegment[index] = value; 
                }
            }
        }

        BezierSegmentLengthCache bezierSegmentLengthCache;

        public float GetTotalLength()
        {
            float total = 0f;
            bezierSegmentLengthCache = new BezierSegmentLengthCache(Count);
            for (int i = 0; i < nodes.Count - 1; i++)
            {
                bezierSegmentLengthCache[i] = total;
                Vector3 p0 = nodes[i].position;
                Vector3 p1 = nodes[i].handleOut;
                Vector3 p2 = nodes[i + 1].handleIn;
                Vector3 p3 = nodes[i + 1].position;
                total += BezierCurveHelper.EstimateBezierLength(p0, p1, p2, p3);
            }
            return total;
        }

        public float GetSegmentLength(int i)
        {
            if(bezierSegmentLengthCache == null)
            {
                GetTotalLength();
            }
            return bezierSegmentLengthCache[i];
        }

        // move to BezierCurveHelper

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


        #region 生成BezierCurve

        // 当前的函数中 必须传入 当前这条河流绘制时所使用的 paintScope
        public static BezierCurve GenCurve(List<Vector2Int> rawPoints, RiveStartData rvStart, TerrainSettingSO terSet)
        {
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
                        Vector2Int pixel = MapRiverData.GetRvEditPixelByMapPos(point, terSet.clusterSize, terSet.paintRTSizeScale, out Vector2Int clusterID) + new Vector2Int(i, j);
                        if (!pointSets.Contains(pixel))
                        {
                            bias--;
                            if (bias <= 0) isFeaturePoint = false;
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
            for (int i = 0; i < nodeCnt; i++)
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
            List<Vector2Int> sortedData = new List<Vector2Int>();
            for (int i = 0; i < featurePoint.Count; i++)
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
            for (int i = 0; i < curveCnt; i++)
            {
                BezierCurve curve = bezierCurves[i];
                if (curve.Count != nodeCnt)
                {
                    return null;
                }
            }

            BezierCurve targetCurve = new BezierCurve();
            for (int i = 0; i < nodeCnt; i++)
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

        #endregion


        #region 拟合BezierCurve
        /* An Algorithm for Automatically Fitting Digitized Curves
        by Philip J. Schneider
        from "Graphics Gems", Academic Press, 1990 */

        public static BezierCurve FitCurve(List<Vector2Int> rawPoints, RiveStartData rvStartData, TerrainSettingSO terSet, float error = 2f)
        {
            List<Vector2Int> sortedPoint = SortedFeaturePoints(rvStartData, ref rawPoints, terSet);
            BezierFitter bezierFitter = new BezierFitter();
            bezierFitter.InitBezierFitter(sortedPoint, error);
            return bezierFitter.FitBezierCurve();
        }

        class BezierFitter
        {
            List<Vector3> points;
            float error;
            int totalPointNum;

            public void InitBezierFitter(List<Vector2Int> points, float error)
            {
                this.totalPointNum = points.Count;
                this.error = error;
                this.points = new List<Vector3>(totalPointNum);
                for(int i = 0; i < totalPointNum;i++)
                {
                    this.points.Add(points[i].TransToXZ());
                }
            }

            public BezierCurve FitBezierCurve()
            {
                if (points == null || points.Count == 0)
                {
                    Debug.LogError("points is null, can not fit curve!");
                    return null;
                }
                if (points.Count == 1)
                {
                    Debug.LogError("only one point, can not fit curve!");
                    return null;
                }

                int n = points.Count;
                Vector3 leftTangent = ComputeLeftTangent(0);
                Vector3 rightTangent = ComputeRightTangent(n - 1);

                // storage segments of bezier curve
                List<Vector3[]> segments = new List<Vector3[]>();
                FitCubic(0, n - 1, leftTangent, rightTangent, error * error, segments);

                if (segments.Count <= 0)
                {
                    return null;
                }

                // construct bezier curve by segments
                BezierCurve bezierCurve = new BezierCurve();
                Vector3[] firstSeg = segments[0];
                Vector3 handleIn = Vector3.zero;
                Vector3 handleOut = (firstSeg[1]);
                BezierNode firstNode = new BezierNode(firstSeg[0], handleIn, handleOut);
                bezierCurve.Add(firstNode);

                for (int i = 0; i < segments.Count; i++)
                {
                    Vector3[] seg = segments[i];
                    handleIn = (seg[2]);
                    if (i < segments.Count - 1)
                    {
                        Vector3[] nextSeg = segments[i + 1];
                        handleOut = (nextSeg[1]);
                    }
                    else
                    {
                        handleOut = Vector3.zero;
                    }
                    BezierNode node = new BezierNode(seg[3], handleIn, handleOut);
                    bezierCurve.Add(node);
                }
                return bezierCurve;
            }

            private void FitCubic(int start, int end, Vector3 leftTangent, Vector3 rightTangent, double fixedError, List<Vector3[]> segments)
            {
                int pointNum = end - start + 1;
                if (pointNum == 2)
                {
                    // two point : so we create simple curve
                    double dist = (points[end] - points[start]).magnitude / 3.0;
                    Vector3[] simpleBezCurve = ConsBezierSegment(start, end, leftTangent, rightTangent, (float)dist, (float)dist);
                    segments.Add(simpleBezCurve);
                    return;
                }

                // 1. get chord param of points
                List<double> chordParams = ChordLengthParameterize(start, end);
                // 2. fit bezier curve (generate segment not node)
                Vector3[] bezCurve = GenerateBezier(start, end, chordParams, leftTangent, rightTangent);
                // 3. caculate max error
                int splitPointIdx;
                double maxError = ComputeMaxError(start, end, bezCurve, chordParams, out splitPointIdx);

                // 4. if maxError is less than error,  add it
                // if maxError is less than error*4,  re params to optimize
                if (maxError < error)
                {
                    segments.Add(bezCurve);
                    return;
                }
                else if (maxError < error * 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        List<double> uPrime = Reparameterize(start, end, chordParams, bezCurve);
                        bezCurve = GenerateBezier(start, end, uPrime, leftTangent, rightTangent);
                        maxError = ComputeMaxError(start, end, bezCurve, uPrime, out splitPointIdx);
                        if (maxError < error)
                        {
                            segments.Add(bezCurve);
                            return;
                        }
                        chordParams = uPrime;
                    }
                }

                // 5. fail to fit curve, split 2 segments in splitPointIdx, continue fit curve
                Vector3 tHatCenter = ComputeCenterTangent(splitPointIdx);
                FitCubic(start, splitPointIdx, leftTangent, tHatCenter, error, segments);
                FitCubic(splitPointIdx, end, -tHatCenter, rightTangent, error, segments);
            }

            // TODO : 优化下这里的代码，改用 前缀和，不要每次都计算一遍
            private List<double> ChordLengthParameterize(int start, int end)
            {
                int n = end - start + 1;
                List<double> paras = new List<double>(n);
                for (int i = 0; i < n; i++)
                {
                    paras.Add(0);
                }
                paras[0] = 0.0;

                // accumulate chord length
                for (int i = start + 1; i <= end; i++)
                {
                    paras[i - start] = paras[i - start - 1] + (points[i] - points[i - 1]).magnitude;
                }

                // normalized chord
                for (int i = start + 1; i <= end; i++)
                {
                    paras[i - start] /= paras[end - start];
                }
                return paras;
            }

            // generate Cubic Bezier curve segment
            private Vector3[] GenerateBezier(int start, int end, List<double> chordParams, Vector3 leftTangent, Vector3 rightTangent)
            {
                // 根据参数u和端点切线，求解三次Bezier的4个控制点
                int pointNum = end - start + 1;

                // 构造矩阵A
                Vector3[] A1 = new Vector3[pointNum];
                Vector3[] A2 = new Vector3[pointNum];
                for (int i = 0; i < pointNum; i++)
                {
                    A1[i] = leftTangent * (float)B1(chordParams[i]);
                    A2[i] = rightTangent * (float)B2(chordParams[i]);
                }

                // 构造线性方程 : C * [alpha_l, alpha_r]^T = X
                double[ , ] C = new double[2, 2];
                double[] Xvec = new double[2];
                // init as 0
                C[0, 0] = C[0, 1] = C[1, 0] = C[1, 1] = 0.0;
                Xvec[0] = Xvec[1] = 0.0;
                for (int i = 0; i < pointNum; i++)
                {
                    C[0, 0] += Vector3.Dot(A1[i], A1[i]);
                    C[0, 1] += Vector3.Dot(A1[i], A2[i]);
                    C[1, 0] = C[0, 1];
                    C[1, 1] += Vector3.Dot(A2[i], A2[i]);

                    // B0(u) + B1(u) apply to P0，B2(u) + B3(u) apply to P3
                    Vector3 temp = points[start + i]
                                    - (points[start] * (float)B0(chordParams[i]) + points[start] * (float)B1(chordParams[i])
                                    +  points[end] * (float)B2(chordParams[i]) + points[end] * (float)B3(chordParams[i]));
                    Xvec[0] += Vector3.Dot(A1[i], temp);
                    Xvec[1] += Vector3.Dot(A2[i], temp);
                }

                // 求解矩阵方程（最小二乘法）
                double det = C[0, 0] * C[1, 1] - C[0, 1] * C[1, 0];
                double alphaL = 0, alphaR = 0;
                if (Math.Abs(det) > 1e-6)
                {
                    alphaL = (Xvec[1] * C[0, 0] - Xvec[0] * C[0, 1]) / det;
                    alphaR = (Xvec[0] * C[1, 1] - Xvec[1] * C[1, 0]) / det;
                }
                // 检查解的有效性
                double segLength = (points[end] - points[start]).magnitude;
                double epsilon = 1e-6 * segLength;
                if (alphaL < epsilon || alphaR < epsilon)
                {
                    double dist = segLength / 3;
                    return ConsBezierSegment(start, end, leftTangent, rightTangent, (float)dist, (float)dist);
                }

                return ConsBezierSegment(start, end, leftTangent, rightTangent, (float)alphaL, (float)alphaR);
            }

            // caculate max error, and point index of max error
            private double ComputeMaxError(int start, int end, Vector3[] bezCurve, List<double> u, out int splitPointIdx)
            {
                splitPointIdx = (end - start + 1) / 2;
                double maxDist = 0.0;
                for (int i = start + 1; i < end; i++)
                {
                    Vector3 P = Bezier(bezCurve, u[i - start]);
                    double dist = (P - points[i]).sqrMagnitude;
                    if (dist >= maxDist)
                    {
                        maxDist = dist;
                        splitPointIdx = i;
                    }
                }
                return maxDist;
            }


            // Newton method to Reparameterize
            private List<double> Reparameterize(int first, int last, List<double> u, Vector3[] bezCurve)
            {
                int n = last - first + 1;
                List<double> uPrime = new List<double>(n);
                for(int i = 0; i < n; i++)
                {
                    uPrime.Add(0);
                }
                for (int i = first; i <= last; i++)
                {
                    uPrime[i - first] = NewtonRaphsonRootFind(bezCurve, points[i], u[i - first]);
                }
                return uPrime;
            }

            // Newton-Raphson iter for better param
            private double NewtonRaphsonRootFind(Vector3[] Q, Vector3 P, double u)
            {
                // Q(u)
                Vector3 Q_u = Bezier(Q, u);
                // Q'(u) 和 Q''(u)
                Vector3[] Q1 = new Vector3[3], Q2 = new Vector3[2];
                for (int i = 0; i < 3; i++) Q1[i] = (Q[i + 1] - Q[i]) * 3f;
                for (int i = 0; i < 2; i++) Q2[i] = (Q1[i + 1] - Q1[i]) * 2f;
                // 计算导数值
                // 二阶Bezier评估
                double t = u, t1 = 1 - t;
                Vector3 Q1_u = (float)(t1 * t1) * Q1[0] + (float)(2 * t * t1) * Q1[1] + (float)(t * t) * Q1[2];
                // 一阶Bezier评估
                Vector3 Q2_u = (float)(1 - t) * Q2[0] + (float)t * Q2[1];
                Vector3 diff = Q_u - P;
                double numerator = Vector3.Dot(diff, Q1_u);
                double denominator = Vector3.Dot(Q1_u, Q1_u) + Vector3.Dot(diff, Q2_u);
                if (Math.Abs(denominator) < 1e-6)
                {
                    return u;
                }
                return u - numerator / denominator;
            }

            private Vector3 Bezier(Vector3[] c, double u)
            {
                // caculate cubic bezier curve's position
                double t = u, t1 = 1.0 - t;
                return (float)(t1 * t1 * t1) * c[0]
                     + (float)(3 * t * t1 * t1) * c[1]
                     + (float)(3 * t * t * t1) * c[2]
                     + (float)(t * t * t) * c[3];
            }

            private Vector3[] ConsBezierSegment(int start, int end, Vector3 leftTangent, Vector3 rightTangent, float fixL, float fixR)
            {
                Vector3[] bezCurve = new Vector3[4];
                bezCurve[0] = points[start];
                bezCurve[3] = points[end];
                bezCurve[1] = points[start] + leftTangent * (float)fixL;
                bezCurve[2] = points[end] + rightTangent * (float)fixR;
                return bezCurve;
            }

            private Vector3 ComputeLeftTangent(int index)
            {
                return (points[index + 1] - points[index]).normalized;
            }

            private Vector3 ComputeRightTangent(int index)
            {
                return (points[index - 1] - points[index]).normalized;
            }

            private Vector3 ComputeCenterTangent(int center)
            {
                Vector3 V1 = points[center - 1] - points[center];
                Vector3 V2 = points[center] - points[center + 1];
                return (V1 + V2).normalized;
            }

            // Bernstein基函数
            private double B0(double u) { double t = 1 - u; return t * t * t; }
            private double B1(double u) { double t = 1 - u; return 3 * u * t * t; }
            private double B2(double u) { double t = 1 - u; return 3 * u * u * t; }
            private double B3(double u) { return u * u * u; }

        }

        #endregion

    }

    public static class BezierCurveHelper
    {
        public static Vector3 CubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            return u * u * u * p0 +
                   3 * u * u * t * p1 +
                   3 * u * t * t * p2 +
                   t * t * t * p3;
        }

        public static Vector3 CubicBezierTangent(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return 3 * Mathf.Pow(1 - t, 2) * (p1 - p0)
                 + 6 * (1 - t) * t * (p2 - p1)
                 + 3 * Mathf.Pow(t, 2) * (p3 - p2);
        }

        public static float EstimateBezierLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int steps = 15)
        {
            float length = 0f;
            Vector3 prev = p0;
            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 pt = CubicBezierPoint(t, p0, p1, p2, p3);
                length += Vector3.Distance(prev, pt);
                prev = pt;
            }
            return length;
        }


        // Newton method for point
        public static bool FindPointWithTangent_Newton( Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
            Vector3 targetTangent, out Vector3 resultPoint, out float resultT, float learningRate = 0.01f, int maxIter = 100, float epsilon = 1e-5f)
        {
            targetTangent.Normalize();

            float t = 0.5f; // 初始猜测
            for (int i = 0; i < maxIter; i++)
            {
                float cost = AngleCost(t, p0, p1, p2, p3, targetTangent);
                float delta = 1e-4f;
                float costForward = AngleCost(t + delta, p0, p1, p2, p3, targetTangent);
                float costBackward = AngleCost(t - delta, p0, p1, p2, p3, targetTangent);
                float gradient = (costForward - costBackward) / (2 * delta);

                float tNext = t - learningRate * gradient;
                tNext = Mathf.Clamp01(tNext);

                if (Mathf.Abs(tNext - t) < epsilon)
                {
                    resultT = tNext;
                    resultPoint = CubicBezierPoint(tNext, p0, p1, p2, p3);
                    return true;
                }

                t = tNext;
            }

            resultT = t;
            resultPoint = CubicBezierPoint(t, p0, p1, p2, p3);
            return true;
        }

        // 目标函数：1 - cos(theta)
        private static float AngleCost(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 targetDir)
        {
            Vector3 tangent = CubicBezierTangent(t, p0, p1, p2, p3);
            if (tangent == Vector3.zero) return 1f; // 极端情况，避免除0
            return 1f - Vector3.Dot(tangent.normalized, targetDir);
        }


        // 点到贝塞尔曲线的最小距离（采样+迭代法）
        public static float DistancePointToBezier(Vector3 point, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, out Vector3 closestPoint, out Vector3 closestTangent, out float closestT, int sampleCount = 500)
        {
            float minDistSqr = float.MaxValue;
            closestT = 0f;
            closestPoint = Vector3.zero;
            closestTangent = Vector3.zero;

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = i / (float)sampleCount;
                Vector3 curvePoint = CubicBezierPoint(t, p0, p1, p2, p3);
                float distSqr = (curvePoint - point).sqrMagnitude;

                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closestT = t;
                    closestPoint = curvePoint;
                }
            }

            // 可选：对 bestT(closestT) 再做一轮局部细采样以提高精度
            float delta = 1f / sampleCount;
            float fineStep = delta / 10f;
            for (float t = Mathf.Max(0, closestT - delta); t <= Mathf.Min(1, closestT + delta); t += fineStep)
            {
                Vector3 curvePoint = CubicBezierPoint(t, p0, p1, p2, p3);
                float distSqr = (curvePoint - point).sqrMagnitude;

                if (distSqr < minDistSqr)
                {
                    minDistSqr = distSqr;
                    closestT = t;
                    closestPoint = curvePoint;
                }
            }

            closestTangent = CubicBezierTangent(closestT, p0, p1, p2, p3);

            return Mathf.Sqrt(minDistSqr);
        }

    }


}
