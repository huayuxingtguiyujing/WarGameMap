using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // NOTE : it is a runtime/editor manager, which will manager the river data. given some river method to terrainCons
    // and it will be held by terrainCons for river is a part of terrainCons
    public class RiverDataManager : IDisposable
    {
        MapRiverData mapRiverData;

        Dictionary<Vector2Int, float> pointDownOffsetDict;      // 



        Dictionary<int, Dictionary<Vector2Int, RiverVert>> riverVertsDict;        // Key : riverID, Value : river border points, but right border

        Dictionary<int, RiverMesh> riverMeshDict;

        HashSet<int> hasLoadedRiverSets;


        float riverDownOffset;

        public int tileSize { get; private set; }
        public int clusterSize { get; private set; }
        public Vector3 terrainSize { get; private set; }
        public int terWorldWidth { get; private set; }
        public int terWorldHeight { get; private set; }

        Transform riverParentTrans;

        public bool IsValid {  get; private set; }

        public RiverDataManager() { IsValid = false; }

        public RiverDataManager(MapRiverData mapRiverData, float riverDownOffset, int tileSize, int clusterSize, Vector3 terrainSize, Transform parentTrans)
        {
            InitRiverDataManager(mapRiverData, riverDownOffset, tileSize, clusterSize, terrainSize, parentTrans);
        }

        public void InitRiverDataManager(MapRiverData mapRiverData, float riverDownOffset, int tileSize, int clusterSize, Vector3 terrainSize, Transform parentTrans)
        {
            if (mapRiverData == null)
            {
                IsValid = false;
                return;
            }
            this.mapRiverData = mapRiverData;

            pointDownOffsetDict = new Dictionary<Vector2Int, float>();
            riverVertsDict = new Dictionary<int, Dictionary<Vector2Int, RiverVert>>();
            hasLoadedRiverSets = new HashSet<int>();

            riverMeshDict = new Dictionary<int, RiverMesh>();

            this.riverDownOffset = riverDownOffset;
            this.tileSize = clusterSize / tileSize;
            this.clusterSize = clusterSize;
            this.terrainSize = terrainSize;
            terWorldWidth = (int)(terrainSize.x * clusterSize);
            terWorldHeight = (int)(terrainSize.z * clusterSize);

            this.riverParentTrans = parentTrans;

            IsValid = true;
        }

        // Lazy Build : when building a cluster of terrain, we will build the riverdata exists in this cluster
        public void BuildRiverData(int clsX, int clsY)
        {
            mapRiverData.UpdateClsExistRiverDict();
            Vector2Int clusterID = new Vector2Int(clsX, clsY);
            List<RiverData> existRiverDatas = mapRiverData.GetClsExistRiverData(clusterID);
            foreach (var riverData in existRiverDatas)
            {
                int riverID = riverData.riverID; 
                if (hasLoadedRiverSets.Contains(riverID))
                {
                    continue;
                }
                hasLoadedRiverSets.Add(riverID);

                int effectScope = 8;        // TODO : effect scope should store in mapRiverData

                GenCurveData(riverData, effectScope);
                DebugUtility.Log($"build river id {riverData.riverID}");
            }
        }

        private void GenCurveData(RiverData riverData, int effectScope)
        {
            if (riverVertsDict.ContainsKey(riverData.riverID))
            {
                riverVertsDict[riverData.riverID].Clear();
            }
            else
            {
                riverVertsDict.Add(riverData.riverID, new Dictionary<Vector2Int, RiverVert>());
            }

            BezierCurve curve = riverData.curve;

            // establish bound for evert curve segment
            int initCnt = 0;
            int nodeNum = curve.Count;
            List<MapBoundStruct> bounds = new List<MapBoundStruct>(nodeNum);
            for (int i = 0; i < nodeNum - 1; i ++)
            {
                Vector3 p0 = curve.Nodes[i].position;
                Vector3 p1 = curve.Nodes[i].handleOut;
                Vector3 p2 = curve.Nodes[i + 1].handleIn;
                Vector3 p3 = curve.Nodes[i + 1].position;

                Vector3 horizontalTangent = new Vector3(1, 0, 0);
                Vector3 verticalTangent = new Vector3(0, 0, 1);
                Vector3 horiTangentPoint;
                Vector3 vertTangentPoint;
                float horiTangentRatio;
                float vertTangentRatio;

                BezierCurveHelper.FindPointWithTangent_Newton(p0, p1, p2, p3, horizontalTangent, out horiTangentPoint, out horiTangentRatio);
                BezierCurveHelper.FindPointWithTangent_Newton(p0, p1, p2, p3, verticalTangent, out vertTangentPoint, out vertTangentRatio);

                int Left = Mathf.Min((int)p0.x, (int)p3.x, (int)vertTangentPoint.x);
                int Right = Mathf.Max((int)p0.x, (int)p3.x, (int)vertTangentPoint.x);
                int Down = Mathf.Min((int)p0.z, (int)p3.z, (int)horiTangentPoint.z);
                int Up = Mathf.Max((int)p0.z, (int)p3.z, (int)horiTangentPoint.z);
                bounds.Add(new MapBoundStruct(Left - effectScope, Right + effectScope, Up + effectScope, Down - effectScope));
                initCnt += (Right - Left + 1) * (Up - Down + 1);
            }

            float totalLen = curve.GetTotalLength(); // cache it

            // effect all pixel inside bound
            for (int i = 0; i < nodeNum - 1; i++)
            {
                Vector3 p0 = curve.Nodes[i].position;
                Vector3 p1 = curve.Nodes[i].handleOut;
                Vector3 p2 = curve.Nodes[i + 1].handleIn;
                Vector3 p3 = curve.Nodes[i + 1].position;

                float segmentLen = curve.GetSegmentLength(i);

                List<Vector2Int> pixels = bounds[i].GetPixelInsideBound();
                foreach (var pixel in pixels)
                {
                    Vector3 closestPoint;
                    Vector3 closestPTangent;
                    float closestT;
                    float distance = BezierCurveHelper.DistancePointToBezier(pixel.TransToXZ(), p0, p1, p2, p3, out closestPoint, out closestPTangent, out closestT);

                    if (distance > effectScope)
                    {
                        continue;
                    }

                    // TODO : 也许需要记录边界顶点，进行保护
                    float len = BezierCurveHelper.EstimateBezierLength(p0, p1, closestPTangent, closestPoint);
                    float curLen = segmentLen + len;

                    Vector2 uv = new Vector2(curLen / totalLen, distance / effectScope);
                    float offsetDown = LerpDownOffset(distance, effectScope);
                    Vector2 tangentNormalized = closestPTangent.TransFromXZ().normalized;
                    AddPointDownOffset(riverData.riverID, pixel, uv, tangentNormalized, offsetDown);     // 
                }
            }
            
        }

        private float LerpDownOffset(float distance, float maxDistance, int preSpeed = 2, int lateSpeed = 2)
        {
            float mid = maxDistance / 2;
            float ratio = 1;
            if (distance <= mid)
            {
                float u = distance / mid;
                ratio = 0.5f * Mathf.Pow(u, preSpeed);
            }
            else if (distance <= 2f * mid)
            {
                float u = (distance - mid) / mid;
                ratio = 1f - 0.5f * Mathf.Pow(1f - u, lateSpeed);
            }
            return (1 - ratio) * riverDownOffset;
        }

        private void AddPointDownOffset(int riverID, Vector2Int pixelIdx, Vector2 uv, Vector2 tangent, float offsetDown)
        {
            if (pointDownOffsetDict.ContainsKey(pixelIdx))
            {
                if (pointDownOffsetDict[pixelIdx] < offsetDown)
                {
                    pointDownOffsetDict[pixelIdx] = offsetDown;
                    riverVertsDict[riverID][pixelIdx].UpdateVert(pixelIdx, uv, tangent);
                }
            }
            else
            {
                pointDownOffsetDict.Add(pixelIdx, offsetDown);
                riverVertsDict[riverID].Add(pixelIdx, new RiverVert(pixelIdx, uv, tangent));
            }
        }

        public void BuildRiverMesh(int clsX, int clsY)
        {
            mapRiverData.UpdateClsExistRiverDict();
            Vector2Int clusterID = new Vector2Int(clsX, clsY);
            List<RiverData> existRiverDatas = mapRiverData.GetClsExistRiverData(clusterID);
            foreach (var riverData in existRiverDatas)
            {
                int riverID = riverData.riverID;
                // If has been builded, then skip
                if (riverMeshDict.ContainsKey(riverID))
                {
                    continue;
                }
                BuildRiverMesh(riverID, riverVertsDict[riverID]);
            }
        }

        private void BuildRiverMesh(int riverID, Dictionary<Vector2Int, RiverVert> riverVerts)
        {
            GameObject riverMeshGo = new GameObject($"riverMeshGo_{riverID}");
            MeshFilter meshFiler = riverMeshGo.AddComponent<MeshFilter>();
            MeshRenderer renderer = riverMeshGo.AddComponent<MeshRenderer>();
            riverMeshGo.transform.parent = riverParentTrans;

            int borderVertNum = riverVerts.Count;
            RiverMesh riverMesh = new RiverMesh();
            riverMesh.InitRiverMesh(borderVertNum, meshFiler, renderer, terWorldWidth, clusterSize);
            riverMeshDict.Add(riverID, riverMesh);

            int vertIdx = 0;
            Dictionary<Vector2Int, int> riverVertIndiceDict = new Dictionary<Vector2Int, int>(borderVertNum);
            Dictionary<Vector2Int, RiverVert> riverVertDict = new Dictionary<Vector2Int, RiverVert>(borderVertNum);

            foreach (var riverVert in riverVerts.Values)
            {
                //DebugUtil.DebugGameObject($"node{vertIdx}", riverVert.TransToWorldPos(), null);
                riverMesh.SetVert(vertIdx, riverVert.vertIdx.TransToXZ(), riverVert.uv, riverVert.tangent);
                riverVertIndiceDict.Add(riverVert.vertIdx, vertIdx);
                riverVertDict.Add(riverVert.vertIdx, riverVert);
                vertIdx++;
            }

            // 对于每个点 (i, 0, j) 
            // 如果有下面两点存在：(i, 0, j) -> (i + 1, 0, j) -> (i + 1, 0, j + 1)
            // 如果有下面两点存在：(i, 0, j) -> (i, 0, j + 1) -> (i + 1, 0, j + 1)
            HashSet<Vector2Int> hasAddTriangle = new HashSet<Vector2Int>(borderVertNum);
            List<int> triangles = new List<int>(borderVertNum * 3);
            foreach (var riverVert in riverVerts.Values)
            {
                Vector2Int curIdx = riverVert.vertIdx;
                Vector2Int rightIdx = riverVert.GetRightVertIdx();
                Vector2Int upIdx = riverVert.GetUpVertIdx();
                Vector2Int rightUpIdx = riverVert.GetRightUpVertIdx();

                bool hasRightUp = riverVertDict.ContainsKey(rightUpIdx);
                bool hasRight = riverVertDict.ContainsKey(rightIdx);
                bool hasUp = riverVertDict.ContainsKey(upIdx);

                if (hasRight && hasRightUp)
                {
                    triangles.Add(riverVertIndiceDict[curIdx]);
                    triangles.Add(riverVertIndiceDict[rightIdx]);
                    triangles.Add(riverVertIndiceDict[rightUpIdx]);
                }
                if (hasUp && hasRightUp)
                {
                    triangles.Add(riverVertIndiceDict[curIdx]);
                    triangles.Add(riverVertIndiceDict[rightUpIdx]);
                    triangles.Add(riverVertIndiceDict[upIdx]);
                }
            }
            riverMesh.SetTriangle(triangles);
            riverMesh.BuildOrightMesh();

        }

        public void SampleRiverRatio(Vector3 terWorldPos, out bool IsEffectByRiver, out float offsetDown, out Vector2Int bindWorldPos)
        {
            Vector2Int pixelIdx = terWorldPos.TransIntFromXZ();

            IsEffectByRiver = false;
            offsetDown = 0;
            bindWorldPos = Vector2Int.zero;
            if (pointDownOffsetDict.ContainsKey(pixelIdx))
            {
                IsEffectByRiver = true;
                offsetDown = pointDownOffsetDict[pixelIdx];
            }
        }

        public void Dispose()
        {
            if (IsValid == false)
            {
                return;
            }
            //riverTexData.Dispose();
            pointDownOffsetDict.Clear();


            foreach (var pair in riverMeshDict)
            {
                pair.Value.Dispose();
            }
            riverMeshDict.Clear();

            riverParentTrans.ClearObjChildren();

            hasLoadedRiverSets.Clear();
        }

    }
}
