using CodiceApp.EventTracking.Plastic;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

namespace LZ.WarGameMap.Runtime
{

    // this struct aims to descripe river mesh border vert
    public class RiverVert
    {
        public Vector2Int vertIdx;

        public Vector2 uv;

        public Vector2 tangent;

        public RiverVert(Vector2Int vertIdx, Vector2 uv, Vector2 tangent)
        {
            this.vertIdx = vertIdx;
            this.uv = uv;
            this.tangent = tangent;
        }

        public void UpdateVert(Vector2Int vertIdx, Vector2 uv, Vector2 tangent)
        {
            this.vertIdx = vertIdx;
            this.uv = uv;
            this.tangent = tangent;
        }

        public Vector3 TransToWorldPos()
        {
            return vertIdx.TransToXZ();
        }

        public Vector2Int GetRightVertIdx()
        {
            return new Vector2Int(vertIdx.x, vertIdx.y + 1);
        }

        public Vector2Int GetUpVertIdx()
        {
            return new Vector2Int(vertIdx.x + 1, vertIdx.y);
        }

        public Vector2Int GetRightUpVertIdx()
        {
            return new Vector2Int(vertIdx.x + 1, vertIdx.y + 1);
        }

        public float GetDistance(RiverVert other)
        {
            return Vector2Int.Distance(vertIdx, other.vertIdx);
        }

    }


    [Serializable]
    public class RiverMesh : IBinarySerializer, IDisposable
    {
        // 切线，Vertex，索引需要动态地构建
        int triIdx = 0;

        Vector3[] vertexs;
        Vector2[] uvs;
        Vector2[] tangents;
        int[] triangles;

        MeshFilter meshFiler;
        MeshRenderer renderer;

        Mesh mesh;

        int terWorldWidth;
        int clusterSize;

        public void InitRiverMesh(int borderVertNum, MeshFilter meshFiler, MeshRenderer renderer, int terWorldWidth, int clusterSize)
        {
            triIdx = 0;
            vertexs = new Vector3[borderVertNum];
            uvs = new Vector2[borderVertNum];
            tangents = new Vector2[borderVertNum];
            triangles = new int[1];

            this.meshFiler = meshFiler;
            this.renderer = renderer;

            this.terWorldWidth = terWorldWidth;
            this.clusterSize = clusterSize;
        }

        public void SetVert(int idx, Vector3 vert, Vector2 uv, Vector2 tangent)
        {
            vert.y = -1;    // TODO : riverMesh should be offset down
            vertexs[idx] = vert;
            uvs[idx] = uv;
            //uvs[idx] = new Vector2(vert.x * 4 / clusterSize, vert.z * 4 / clusterSize);
            tangents[idx] = new Vector2(tangent.y, tangent.x).normalized;
        }

        public void SetTriangle(List<int> triangles)
        {
            this.triangles = triangles.ToArray();
        }

        public void BuildOrightMesh()
        {
            if (mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                GameObject.DestroyImmediate(mesh);
            }
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertexs);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(2, tangents);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateNormals();
            meshFiler.mesh = mesh;
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            GameObject.DestroyImmediate(mesh);
            GameObject.DestroyImmediate(renderer.gameObject);
#else
            GameObject.Destroy(mesh);
            GameObject.Destroy(renderer.gameObject);
#endif
        }

        #region Serialized

        public void ReadFromBinary(BinaryReader reader)
        {

        }

        public void WriteToBinary(BinaryWriter writer)
        {

        }

        #endregion

        #region 边界点建立河流Mesh
        // a failed try
        // use EarClipping Algorithm
        [Obsolete]
        public List<int> ExeEarClipping(List<RiverVert> borderVert, int terWorldWidth)
        {
            EarClippingHelper earClippingHelper = new EarClippingHelper();
            earClippingHelper.InitEarClipping(borderVert, terWorldWidth);
            earClippingHelper.ExeEarClipping();
            return earClippingHelper.GetTriangleRes();
        }
        
        [Obsolete]
        class BorderVertLinkNode
        {
            public BorderVertLinkNode pre;

            public BorderVertLinkNode next;

            public int vertIdx;

            public BorderVertLinkNode(int vertIdx)
            {
                this.vertIdx = vertIdx;
            }

            public bool IsBuilded()
            {
                return pre != null && next != null;
            }
        }

        // TODO : 修复这玩意！
        [Obsolete]
        class EarClippingHelper
        {
            int borderVertNum;
            int terWorldWidth;

            List<RiverVert> borderVert;

            bool[] hasDetectedList;
            bool[] isProtrudingList;
            bool[] hasClippedList;
            Dictionary<int, BorderVertLinkNode> vertLinkNodeDict;

            List<int> triangles;

            public void InitEarClipping(List<RiverVert> borderVert, int terWorldWidth)
            {

                borderVertNum = borderVert.Count;
                this.terWorldWidth = terWorldWidth;
                this.borderVert = borderVert;

                triangles = new List<int>(borderVertNum * 3);

                hasDetectedList = new bool[borderVertNum];
                isProtrudingList = new bool[borderVertNum];
                hasClippedList = new bool[borderVertNum];
                vertLinkNodeDict = new Dictionary<int, BorderVertLinkNode>();
                for (int i = 0; i < borderVertNum; i++)
                {
                    hasDetectedList[i] = false;
                    isProtrudingList[i] = false;
                    hasClippedList[i] = false;
                    vertLinkNodeDict.Add(i, new BorderVertLinkNode(i));
                }
            }

            public void ExeEarClipping()
            {
                // build link list
                int buildedCnt = 0;
                BorderVertLinkNode curNode = vertLinkNodeDict[0];
                float highestZ = -1;
                int highestIdx = -1;
                while (buildedCnt < borderVertNum)
                {
                    int curIdx = curNode.vertIdx;
                    // find highest vert
                    //float high = borderVert[curIdx].GetHeight_Z(terWorldWidth);
                    float high = borderVert[curIdx].vertIdx.y;

                    if (high > highestZ)
                    {
                        highestZ = high;
                        highestIdx = curIdx;
                    }

                    // TODO : 也许这种方法不太好建立起轮廓
                    int closestVertIdx = GetCloestPoint(curIdx);
                    if (closestVertIdx == -1)
                    {
                        Debug.LogError("impossible situation! closest vert idx == -1");
                        break;
                    }
                    hasDetectedList[curIdx] = true;

                    BorderVertLinkNode next = vertLinkNodeDict[closestVertIdx];
                    curNode.next = next;
                    next.pre = curNode;
                    curNode = next;

                    buildedCnt++;
                }

                if (highestIdx == -1)
                {
                    Debug.LogError("impossible situation! highest Idx == -1");
                }

                Debug.Log($"debug buildedCnt : {buildedCnt}, and borderVertNum : {borderVertNum}, result : {borderVertNum == buildedCnt}");

                // start from highestIdx, and get protruding field of vert
                // the highestIdx vert must be protruding
                curNode = vertLinkNodeDict[highestIdx];
                buildedCnt = 0;

                // orientation ! is same as the highest point
                int orientation = IsProtruding(curNode, 1) ? 1 : -1;

                Debug.Log($"debug highestIdx : {highestIdx}, orientation : {orientation}");

                BorderVertLinkNode nodeRec = vertLinkNodeDict[0];
                while (nodeRec != null)
                {
                    DebugUtil.DebugGameObject($"node{nodeRec.vertIdx}", borderVert[nodeRec.vertIdx].TransToWorldPos(), null);
                    nodeRec = nodeRec.next;
                }


                /*// Temp : a fix try
                BorderVertLinkNode noPre = null;
                BorderVertLinkNode noNext = null;
                foreach (var pair in vertLinkNodeDict)
                {
                    BorderVertLinkNode node = pair.Value;
                    if (node.pre == null)
                    {
                        noPre = node;
                    }
                    if (node.next == null)
                    {
                        noNext = node;
                    }

                    if (!pair.Value.IsBuilded())
                    {
                        Debug.LogError($"check there is node not builded!!! : {pair.Value.vertIdx}");
                    }
                }
                if (noPre != null && noNext != null)
                {
                    noNext.next = noPre;
                    noPre.pre = noNext;
                }

                // Temp !!!
                bool[] restartFlag = new bool[borderVertNum];
                for(int i = 0; i < borderVertNum; i++)
                {
                    restartFlag[i] = true;
                }

                int isProTrudingCnt = 0;
                while (buildedCnt < borderVertNum)
                {
                    if (curNode == null)
                    {

                        // Temp : try to find a vert that can start ~
                        for(int i = 0; i < borderVertNum; i++)
                        {
                            if (restartFlag[i])
                            {
                                curNode = vertLinkNodeDict[i];
                                break;
                            }
                        }
                        if (curNode == null)
                        {
                            break;
                        }

                        Debug.LogError($"null link node, cur buildedCnt : {buildedCnt}");
                        //break;
                    }
                    if (!curNode.IsBuilded())
                    {
                        Debug.LogError($"there is node not builded!!! : {curNode.vertIdx}");
                    }

                    int curIdx = curNode.vertIdx;
                    isProtrudingList[curIdx] = IsProtruding(curNode, orientation);

                    if (isProtrudingList[curIdx])
                    {
                        isProTrudingCnt++;
                    }
                    //else
                    //{
                    //    Debug.Log($"find not protruding, curIdx : {curIdx}");
                    //}

                    if (highestIdx == curIdx && isProtrudingList[curIdx] == false)
                    {
                        Debug.LogError("why the highest point is not a protruding point?");
                    }
                    restartFlag[curIdx] = false;

                    curNode = curNode.next;
                    buildedCnt++;
                }

                Debug.Log($"debug protruding count : {buildedCnt}, and we find proTruding count : {isProTrudingCnt}, result : {isProTrudingCnt > 0}");

                // exe ear clipping algorithm
                int curVertCnt = borderVertNum;
                while (curVertCnt > 3)
                {
                    int iterCnt = borderVertNum - curVertCnt;
                    int curIdx = FindProtrudingIdx();

                    if(curIdx == -1)
                    {
                        Debug.Log($"debug cur iter, now curIdx == -1, iterCnt : {iterCnt}");
                        break;
                    }

                    ClipVert(curIdx, vertLinkNodeDict[curIdx], orientation, iterCnt);
                    hasClippedList[curIdx] = true;
                    curVertCnt--;
                }*/

            }

            private int GetCloestPoint(int curIdx)
            {
                // find the closest vert idx, and take it as next vert
                int closestVertIdx = -1;
                float curDistance = float.MaxValue;
                for (int j = 0; j < borderVertNum; j++)
                {
                    if (hasDetectedList[j] || j == curIdx)
                    {
                        continue;
                    }
                    float distance = borderVert[curIdx].GetDistance(borderVert[j]);
                    if (distance < curDistance)
                    {
                        closestVertIdx = j;
                        curDistance = distance;
                    }
                }
                return closestVertIdx;
            }

            //private bool IsProtruding(BorderVertLinkNode curNode, int orientation)
            //{
            //    Vector3 pre = borderVert[curNode.pre.vertIdx].TransIndexToVert(terWorldWidth);
            //    Vector3 cur = borderVert[curNode.vertIdx].TransIndexToVert(terWorldWidth);
            //    Vector3 next = borderVert[curNode.next.vertIdx].TransIndexToVert(terWorldWidth);

            //    Vector3 preToCur = cur - pre;
            //    Vector3 curToNext = next - cur;
            //    Vector3 crossVal = Vector3.Cross(preToCur, curToNext);
            //    return crossVal.y * orientation > 0;
            //}

            private bool IsProtruding(BorderVertLinkNode curNode, int orientation, float eps = 1e-6f)
            {
                if (curNode.pre == null || curNode.next == null)
                {
                    return false;
                }
                Vector3 pre = borderVert[curNode.pre.vertIdx].TransToWorldPos();
                Vector3 cur = borderVert[curNode.vertIdx].TransToWorldPos();
                Vector3 next = borderVert[curNode.next.vertIdx].TransToWorldPos();
                Vector3 u = cur - pre;
                Vector3 v = next - cur;
                float c = Vector3.Cross(u, v).y * orientation;
                return c > eps;
            }

            private int FindProtrudingIdx()
            {
                for (int i = 0; i < borderVertNum; i++)
                {
                    if (hasClippedList[i])
                    {
                        continue;
                    }
                    if (isProtrudingList[i])
                    {
                        return i;
                    }
                }
                return -1;
            }

            private void ClipVert(int idx, BorderVertLinkNode curNode, int orientation, int iterCnt)
            {
                if (!isProtrudingList[idx])
                {
                    throw new Exception($"(ClipVert) why the point is not a protruding point? iterCnt : {iterCnt}");
                }
                if (hasClippedList[idx])
                {
                    throw new Exception($"(ClipVert) this point has clipped, iterCnt : {iterCnt}");
                }
                if (curNode == null)
                {
                    return;
                }

                BorderVertLinkNode preNode = curNode.pre;
                BorderVertLinkNode nextNode = curNode.next;

                Vector3 pre = borderVert[curNode.pre.vertIdx].TransToWorldPos();
                Vector3 next = borderVert[curNode.next.vertIdx].TransToWorldPos();

                Vector3 prePre = borderVert[preNode.vertIdx].TransToWorldPos();
                Vector3 nextNext = borderVert[nextNode.next.vertIdx].TransToWorldPos();
                Vector3 preToNext = next - pre;

                triangles.Add(curNode.vertIdx);
                triangles.Add(preNode.vertIdx);
                triangles.Add(nextNode.vertIdx);

                // reset link node, and update them
                preNode.next = nextNode;
                nextNode.pre = preNode;
                curNode.next = null;
                curNode.pre = null;

                isProtrudingList[preNode.vertIdx] = IsProtruding(preNode, orientation);
                isProtrudingList[nextNode.vertIdx] = IsProtruding(nextNode, orientation);

                // preNode.vertIdx
                // nextNode.vertIdx

            }

            public List<int> GetTriangleRes()
            {
                Debug.Log($"ear clipping over! triangles cnt : {triangles.Count}");

                return triangles;
            }

        }

        #endregion

    }
}
