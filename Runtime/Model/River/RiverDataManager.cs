using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Profiling;
using static Codice.Client.BaseCommands.BranchExplorer.Layout.BrExLayout;

namespace LZ.WarGameMap.Runtime
{
    // NOTE : it is a runtime/editor manager, which will manager the river data. given some river method to terrainCons
    // and it will be held by terrainCons for river is a part of terrainCons
    public class RiverDataManager : IDisposable
    {
        MapRiverData mapRiverData;

        Dictionary<int, float> pointDownOffsetDict;

        struct BorderVert
        {
            public int vertIdx;

            public Vector2 tangent;

            public BorderVert(int vertIdx, Vector2 tangent)
            {
                this.vertIdx = vertIdx;
                this.tangent = tangent;
            }

            public Vector3 TransIndexToVert(int terWorldWidth)
            {
                int x = vertIdx % terWorldWidth;
                int z = vertIdx / terWorldWidth;
                return new Vector3(x, 0, z);
            }
        }
        
        Dictionary<int, List<BorderVert>> riverBorderPoints_Left;        // Key : riverID, Value : river border points

        Dictionary<int, List<BorderVert>> riverBorderPoints_Right;        // Key : riverID, Value : river border points, but right border

        Dictionary<int, RiverMesh> riverMeshDict;

        HashSet<int> hasLoadedRiverSets;


        float riverDownOffset;

        int clusterSize;
        Vector3 terrainSize;
        int terWorldWidth;
        int terWorldHeight;

        Transform riverParentTrans;

        public bool IsValid {  get; private set; }

        public RiverDataManager() { IsValid = false; }

        public RiverDataManager(MapRiverData mapRiverData, float riverDownOffset, int clusterSize, Vector3 terrainSize, Transform parentTrans)
        {
            InitRiverDataManager(mapRiverData, riverDownOffset, clusterSize, terrainSize, parentTrans);
        }

        public void InitRiverDataManager(MapRiverData mapRiverData, float riverDownOffset, int clusterSize, Vector3 terrainSize, Transform parentTrans)
        {
            if (mapRiverData == null)
            {
                IsValid = false;
                return;
            }
            this.mapRiverData = mapRiverData;

            pointDownOffsetDict = new Dictionary<int, float>();
            riverBorderPoints_Left = new Dictionary<int, List<BorderVert>>();
            riverBorderPoints_Right = new Dictionary<int, List<BorderVert>>();
            hasLoadedRiverSets = new HashSet<int>();

            riverMeshDict = new Dictionary<int, RiverMesh>();

            this.riverDownOffset = riverDownOffset;
            this.clusterSize = clusterSize;
            this.terrainSize = terrainSize;
            terWorldWidth = (int)(terrainSize.x * clusterSize);
            terWorldHeight = (int)(terrainSize.z * clusterSize);

            this.riverParentTrans = parentTrans;

            IsValid = true;
        }

        //struct GetPoint

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

                InitRiverBorderList(riverData);
                GenCurveData(riverData);
                BuildRiverMesh(riverID, riverBorderPoints_Left[riverID], riverBorderPoints_Right[riverID]);
                Debug.Log($"build river id {riverData.riverID}");
            }
        }

        private void InitRiverBorderList(RiverData riverData)
        {
            // init border point data struct
            if (riverBorderPoints_Left.ContainsKey(riverData.riverID))
            {
                riverBorderPoints_Left[riverData.riverID].Clear();
            }
            else
            {
                riverBorderPoints_Left.Add(riverData.riverID, new List<BorderVert>());
            }

            if (riverBorderPoints_Right.ContainsKey(riverData.riverID))
            {
                riverBorderPoints_Right[riverData.riverID].Clear();
            }
            else
            {
                riverBorderPoints_Right.Add(riverData.riverID, new List<BorderVert>());
            }
        }

        private void GenCurveData(RiverData riverData)
        {
            int effectScope = 12;
            int fixScope = 3;
            float maxIter = 80000;    // maybe it need modify
            float iterTime = 0;
            bool isFinal = false;
            riverData.curve.InitGetDistanceCache();

            while (!isFinal && iterTime < maxIter)
            {
                riverData.curve.GetPointAtDistance(iterTime, out Vector3 point, out Vector3 tangent, out isFinal);
                iterTime += 1.0f;       // 必须大于1.0，不能小！！！否则 riverMesh 的 vert 会出错
                if (point == Vector3.zero)
                {
                    continue;
                }

                Vector3 normal = new Vector3(-tangent.z, 0, tangent.x).normalized;
                for(int j = -fixScope; j <= fixScope; j++)
                {
                    for (int i = -effectScope; i <= effectScope; i++)
                    {
                        float distance = Mathf.Sqrt(i * i + j * j);
                        float dir = i >= 0 ? 1 : -1;
                        Vector3 sample = point + normal * distance * dir * 0.5f;
                        float ratio = 1 - 0.9f * Mathf.Abs(distance) / effectScope;
                        int idx_sample_pixel = (int)sample.z * terWorldWidth + (int)sample.x;

                        if (i == -effectScope && j == 0)
                        {
                            riverBorderPoints_Left[riverData.riverID].Add(new BorderVert(idx_sample_pixel, tangent.TransFromXZ()));
                        }else if(i == effectScope && j == 0)
                        {
                            riverBorderPoints_Right[riverData.riverID].Add(new BorderVert(idx_sample_pixel, tangent.TransFromXZ()));
                        }

                        if (pointDownOffsetDict.ContainsKey(idx_sample_pixel))
                        {
                            float val = pointDownOffsetDict[idx_sample_pixel];
                            pointDownOffsetDict[idx_sample_pixel] = Mathf.Max(LerpDownOffset(ratio), val);
                        }
                        else
                        {
                            pointDownOffsetDict.Add(idx_sample_pixel, LerpDownOffset(ratio));
                        }
                    }
                }
                
            }
        }

        [Obsolete]
        private void GenBorderData(RiverData riverData)
        {

            int effectScope = 12;
            float maxIter = 80000;    // maybe it need modify
            float iterTime = 0;
            bool isFinal = false;
            riverData.curve.InitGetDistanceCache();

            while (!isFinal && iterTime < maxIter)
            {
                riverData.curve.GetPointAtDistance(iterTime, out Vector3 point, out Vector3 tangent, out isFinal);
                iterTime += 5.0f;
                if (point == Vector3.zero)
                {
                    continue;
                }

                Vector3 normal = new Vector3(-tangent.z, 0, tangent.x).normalized;

                Vector3 sampleLeft = point + normal * effectScope * 0.5f;
                int idx_sample_pixel_left = (int)sampleLeft.z * terWorldWidth + (int)sampleLeft.x;
                Vector3 sampleRight = point - normal * effectScope * 0.5f;
                int idx_sample_pixel_right = (int)sampleRight.z * terWorldWidth + (int)sampleRight.x;
                riverBorderPoints_Left[riverData.riverID].Add(new BorderVert(idx_sample_pixel_left, tangent.TransFromXZ()));
                riverBorderPoints_Right[riverData.riverID].Add(new BorderVert(idx_sample_pixel_right, tangent.TransFromXZ()));

            }
        }

        private void BuildRiverMesh(int riverID, List<BorderVert> leftBorderVert, List<BorderVert> rightBorderVert)
            {
            if (leftBorderVert.Count != rightBorderVert.Count)
            {
                Debug.LogError($"wrong border vert num : {leftBorderVert.Count}, {rightBorderVert.Count}");
                return;
            }

            GameObject riverMeshGo = new GameObject($"riverMeshGo_{riverID}");
            MeshFilter meshFiler = riverMeshGo.AddComponent<MeshFilter>();
            MeshRenderer renderer = riverMeshGo.AddComponent<MeshRenderer>();
            riverMeshGo.transform.parent = riverParentTrans;

            int borderVertNum = leftBorderVert.Count;
            RiverMesh riverMesh = new RiverMesh();
            riverMesh.InitRiverMesh(borderVertNum, meshFiler, renderer);
            riverMeshDict.Add(riverID, riverMesh);

            // set vertex firstly
            for (int i = 0; i < borderVertNum; i++)
            {
                riverMesh.SetBorderVert(i, leftBorderVert[i].TransIndexToVert(terWorldWidth), leftBorderVert[i].tangent, 0);
            }
            for (int i = 0; i < borderVertNum; i++)
            {
                riverMesh.SetBorderVert(i, rightBorderVert[i].TransIndexToVert(terWorldWidth), rightBorderVert[i].tangent, borderVertNum);
            }

            // set triangles
            for (int i = 0; i < borderVertNum - 1; i++)
            {
                int curLeftIdx = i;
                int nextLeftIdx = i + 1;
                int curRightIdx = i + borderVertNum;
                int nextRightIdx = i + borderVertNum + 1;

                //Vector3 curLeftVert = leftBorderVert[i].TransIndexToVert(terWorldWidth);
                //Vector3 nextLeftVert = leftBorderVert[i + 1].TransIndexToVert(terWorldWidth);
                //Vector3 curRightVert = rightBorderVert[i].TransIndexToVert(terWorldWidth);
                //Vector3 nextRightVert = rightBorderVert[i + 1].TransIndexToVert(terWorldWidth);

                riverMesh.AddTriangle(curLeftIdx, curRightIdx, nextLeftIdx);
                riverMesh.AddTriangle(nextLeftIdx, curRightIdx, nextRightIdx);
            }

            riverMesh.BuildOrightMesh();
        }

        private float LerpDownOffset(float ratio)
        {
            // TODO : need more netural lerp (a curve ?)
            return ratio * riverDownOffset;
        }


        public float SampleRiverRatio(Vector3 terWorldPos, out bool IsEffectByRiver)
        {
            Vector3Int sample = terWorldPos.GetSimilarVInt();
            int idx_sample_pixel = (int)sample.z * terWorldWidth + (int)sample.x;
            if (pointDownOffsetDict.ContainsKey(idx_sample_pixel))
            {
                IsEffectByRiver = true;
                return pointDownOffsetDict[idx_sample_pixel];
            }
            else
            {
                IsEffectByRiver = false;
                return 0;
            }

            //Vector2 worldUV = new Vector2(terWorldPos.x / terWorldWidth, terWorldPos.z / terWorldHeight);
            //Vector2Int pixelPos = new Vector2Int((int)(worldUV.x * riverTexWidth), (int)(worldUV.y * riverTexHeight));
            //int index = pixelPos.y * riverTexHeight + pixelPos.x;
            //Color texColor = riverTexData[index];
            //float ratio = texColor.LerpColor(noRiverColor, riverColor);
            //IsEffectByRiver = ratio > 0;
            //return LerpDownOffset(ratio);
        }


        public void Dispose()
        {
            if (IsValid == false)
            {
                return;
            }
            //riverTexData.Dispose();
            pointDownOffsetDict.Clear();

            riverBorderPoints_Left.Clear();
            riverBorderPoints_Right.Clear();

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
