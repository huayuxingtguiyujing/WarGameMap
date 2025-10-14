using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class TerrainSimplifier
    {
        // 1.初始化： 根据传入的网格先建立一些辅助数据结构，同时为网格中的每个顶点计算 误差矩阵 Q
        // 根据：
        // vertor3[] vertexs；
        // vertor3[] normals；
        // int[] riangles；

        // 建立如下结构：
        // bool[] vertex_valid；
        // dictionary<int, list<int>> vert_link_triangleIdx；
        // dictionary<int, matrix> vert_Q_dict；

        // bool[] triangle_valid；
        // dictionary<int, list<int>> triangle_contains_vertIdx；
        // dictionary<int, matrix> triangle_Q_dict；
        // 2.构建顶点对集合： 根据相邻关系和距离阈值，确定所有可收缩的顶点对，计算每个顶点对合并后的误差度量
        // 3.构建最小堆： 将所有顶点对按照误差度量组织成一个最小堆，以便快速获取误差最小的顶点对
        // 4.迭代合并：从最小堆中取出误差最小的顶点对，进行合并该顶点对，更新所有相关要素，重复该步骤，直到达到预定的简化程度
        // 5.完成简化：去除过程中被标注为，重新计算 normal 和 uv 输出 当达到预定的简化程度或无法继续合并时，输出简化后的网格模型
        // https://zhuanlan.zhihu.com/p/545507892

        Vector3 clusterStartPoint;
        int clusterSize;

        // Simplify metric
        int vertCnt;
        int curReducedCnt;
        int targetCnt;
        bool isSimplifyOver;

        // Mesh's data
        string meshName;
        Vector3[] vertexs = new Vector3[1];
        int[] triangles = new int[1];

        // Data to help simplify
        bool[] vertex_valid;
        Dictionary<int, List<int>> vertex_link_triangle_dict;
        Dictionary<int, Matrix4x4> vertex_Q_dict;
        Dictionary<int, List<VectorPair>> vertex_pair_dict;

        bool[] triangle_valid;
        Dictionary<int, List<int>> triangle_contains_vert_dict;
        Dictionary<int, Matrix4x4> triangle_Q_dict;

        QEMMinHeap qemMinHeap;

        // Data to help get mesh
        HashSet<int> edgeVertMap;   // To judge whether a vert is edge vert
        HashSet<string> hasAddThisPairMap;     // To judge whether a vert pair has been added
        //Dictionary<int, Vector3> edgeVert_normals_dict;

        public void ResetRecorder()
        {
            Interlocked.Exchange(ref curReducedCnt, 0);
        }

        public void InitSimplifyer(MeshWrapper meshWrapper, List<int> edgeVerts, List<Vector3> edgeNormals, int targetVertCnt, Vector3 clusterStartPoint, int clusterSize) {
            isSimplifyOver = false;
            this.clusterSize = clusterSize;
            this.clusterStartPoint = clusterStartPoint;

            meshName = meshWrapper.meshName;
            vertexs = meshWrapper.GetVertex().ToArray();
            triangles = meshWrapper.GetTriangles().ToArray();

            // NOTE : simplifyTarget 40% : end util 40% vertex
            vertCnt = vertexs.Length;
            targetCnt = targetVertCnt;

            // init vertex's data
            int vertNum = vertexs.Length;
            vertex_valid = new bool[vertNum];
            Array.Fill(vertex_valid, true);

            // init vert links to triangle......
            vertex_link_triangle_dict = new Dictionary<int, List<int>>(vertNum);
            vertex_Q_dict = new Dictionary<int, Matrix4x4>(vertNum);
            vertex_pair_dict = new Dictionary<int, List<VectorPair>>(vertNum);
            for (int i = 0; i < vertNum; i++) {
                vertex_link_triangle_dict[i] = new List<int>();
                vertex_pair_dict[i] = new List<VectorPair>();
            }

            // construct triangle's data
            int triIdxNum = triangles.Length;
            if(triIdxNum % 3 != 0) {
                DebugUtility.Log($"error! not valid triangle num : {triIdxNum}");
                return;
            }
            int triNum = triIdxNum / 3;
            triangle_valid = new bool[triNum];
            Array.Fill(triangle_valid, true);

            // init triangle to vert dict
            triangle_contains_vert_dict = new Dictionary<int, List<int>>(triNum);
            triangle_Q_dict = new Dictionary<int, Matrix4x4>(triNum);

            for ( int triIdx = 0; triIdx < triNum; triIdx++ ) {
                int tri_vert_start = triIdx * 3;
                int v1 = triangles[tri_vert_start];
                int v2 = triangles[tri_vert_start + 1];
                int v3 = triangles[tri_vert_start + 2];

                triangle_contains_vert_dict[triIdx] = new List<int> { v1, v2, v3 };

                vertex_link_triangle_dict[v1].Add(triIdx);
                vertex_link_triangle_dict[v2].Add(triIdx);
                vertex_link_triangle_dict[v3].Add(triIdx);

                // set triangle's Q
                Vector4 triPlane = SimplifyHelper.GetPlaneEquation(vertexs[v1], vertexs[v2], vertexs[v3]);
                triangle_Q_dict[triIdx] = SimplifyHelper.CaculateKpForPlane(triPlane);
            }

            // init vert Idx to Q dict
            for (int i = 0; i < vertNum; i++) {
                List<int> vert_tri_idxs = vertex_link_triangle_dict[i];
                Matrix4x4 res = Matrix4x4.zero;
                foreach (var triIdx in vert_tri_idxs)
                {
                    res = SimplifyHelper.AddMatrices(res, triangle_Q_dict[triIdx]);
                }
                vertex_Q_dict[i] = res;
            }

            // set edge vert's message
            edgeVertMap = new HashSet<int>(edgeVerts.Count);
            //edgeVert_normals_dict = new Dictionary<int, Vector3>(edgeVerts.Count);
            for(int i = 0; i < edgeVerts.Count; i++) {
                edgeVertMap.Add(edgeVerts[i]);
                // init egde normals, so that in the last we can easily get correct edge normal
                //edgeVert_normals_dict.Add(edgeVerts[i], edgeNormals[i]);
            }

            // init vert pair, add them to heap;
            qemMinHeap = new QEMMinHeap();
            hasAddThisPairMap = new HashSet<string>();
            for (int triIdx = 0; triIdx < triNum; triIdx++) {
                int tri_vert_start = triIdx * 3;
                int v1 = triangles[tri_vert_start];
                int v2 = triangles[tri_vert_start + 1];
                int v3 = triangles[tri_vert_start + 2];

                bool canDelV1 = !edgeVertMap.Contains(v1);
                bool canDelV2 = !edgeVertMap.Contains(v2);
                bool canDelV3 = !edgeVertMap.Contains(v3);

                // NOTE :  初步构建时是不会出现这种情况的
                if (v1 == v2) {
                    DebugUtility.LogError($"传入的triangle有问题，v1 和 v2 相同！:{v1}");
                } else if (v2 == v3) {
                    DebugUtility.LogError($"传入的triangle有问题，v2 和 v3 相同！:{v2}");
                } else if (v1 == v3) {
                    DebugUtility.LogError($"传入的triangle有问题，v1 和 v3 相同！:{v1}");
                }

                // TODO:  ERROR: 为什么处于边缘的 vert pair 也被加入了？
                // 三角型有共边，同样的顶点对会被加入两次！！！！
                if (canDelV1 && canDelV2 &&  !HasAddThisPair(v1, v2)) {
                    // TODO: 建立顶点对，然后加入到最小堆中
                    VectorPair vectorPair = new VectorPair(v1, v2, vertexs[v1], vertexs[v2], vertex_Q_dict[v1], vertex_Q_dict[v2]);
                    qemMinHeap.Insert(vectorPair);
                    vertex_pair_dict[v1].Add(vectorPair);
                    vertex_pair_dict[v2].Add(vectorPair);
                    SetPairAdded(v1, v2);
                }
                if (canDelV2 && canDelV3 && !HasAddThisPair(v2, v3)) {
                    VectorPair vectorPair = new VectorPair(v2, v3, vertexs[v2], vertexs[v3], vertex_Q_dict[v2], vertex_Q_dict[v3]);
                    qemMinHeap.Insert(vectorPair);
                    vertex_pair_dict[v2].Add(vectorPair);
                    vertex_pair_dict[v3].Add(vectorPair);
                    SetPairAdded(v2, v3);
                }
                if (canDelV1 && canDelV3 && !HasAddThisPair(v1, v3)) {
                    VectorPair vectorPair = new VectorPair(v1, v3, vertexs[v1], vertexs[v3], vertex_Q_dict[v1], vertex_Q_dict[v3]);
                    qemMinHeap.Insert(vectorPair);
                    vertex_pair_dict[v1].Add(vectorPair);
                    vertex_pair_dict[v3].Add(vectorPair);
                    SetPairAdded(v1, v3);
                }
            }
        }

        private bool HasAddThisPair(int v1, int v2) {
            string pair_id1 = string.Format("{0}_{1}", v1, v2);
            string pair_id2 = string.Format("{0}_{1}", v2, v1);
            if (hasAddThisPairMap.Contains(pair_id1) || hasAddThisPairMap.Contains(pair_id2)) {
                return true;
            }
            return false;
        }

        private void SetPairAdded(int v1, int v2) {
            string pair_id1 = string.Format("{0}_{1}", v1, v2);
            string pair_id2 = string.Format("{0}_{1}", v2, v1);
            hasAddThisPairMap.Add(pair_id1);
            hasAddThisPairMap.Add(pair_id2);
        }

        public void StartSimplify() {
            DebugUtility.Log("now we start simplify!");

            int curCnt = vertCnt;
            int iterCnt = 0;    // 防止无限循环
            int maxIter = vertCnt - 1;
            while(curCnt > targetCnt && iterCnt < maxIter) {

                iterCnt++;
                if (iterCnt >= maxIter * 2) {
                    DebugUtility.LogError($"simplify end because iterCnt has reach its limit : {maxIter}");
                }

                VectorPair pair = qemMinHeap.GetTop();
                if (pair == null) {
                    //throw new Exception($"pair is null, so heap is null, iterCnt: {iterCnt}, curVer: {curCnt}, startCnt: {vertCnt}, targetCnt: {targetCnt}");
                    //DebugUtility.LogError($"pair is null, so heap is null, iterCnt: {iterCnt}, curVer: {curCnt}, startCnt: {vertCnt}, targetCnt: {targetCnt}");
                    continue;
                }

                Vector3 newV = pair.GetMergedPosition_Optimal();
                Matrix4x4 newM = SimplifyHelper.AddMatrices(pair.Q1, pair.Q2);
                int idx1 = pair.v1Idx;
                int idx2 = pair.v2Idx;
                if (idx1 == idx2) {
                    DebugUtility.LogError($"不应该出现的情况，idx1 = idx2, {idx1}， 本次 iter cnt ：{iterCnt}");
                    continue;
                }
                if (!vertex_valid[idx1]) {
                    //DebugUtility.LogError($"vert {idx1} is not valid， 本次 iter cnt ：{iterCnt}");
                    continue;
                }
                if (!vertex_valid[idx2]) {
                    //DebugUtility.LogError($"vert {idx2} is not valid， 本次 iter cnt ：{iterCnt}");
                    continue;
                }

                // keep v1, use v1 to storage new V, erase v2
                vertexs[idx1] = newV;
                vertexs[idx2] = newV;   // 不应该有这步的...
                vertex_valid[idx2] = false;

                // update all pair that link to idx2, idx1
                int length2 = vertex_pair_dict[idx2].Count;
                for(int i = length2 - 1; i >= 0; i--) {
                    var idx2_pair = vertex_pair_dict[idx2][i];
                    if (pair.EqualTo(idx2_pair) || pair == idx2_pair) {
                        vertex_pair_dict[idx2].RemoveAt(i);
                        continue;
                    }
                    // 同样的顶点对加入两次
                    idx2_pair.UpdatePair(idx2, idx1, newV, newM);
                    qemMinHeap.UpdatePair(idx2_pair, $"{iterCnt}_idx2_update");
                }

                int length1 = vertex_pair_dict[idx1].Count;
                for (int i = length1 - 1; i >= 0; i--) {
                    var idx1_pair = vertex_pair_dict[idx1][i];
                    if (pair.EqualTo(idx1_pair) || pair == idx1_pair) {
                        vertex_pair_dict[idx1].RemoveAt(i);
                        continue;
                    }

                    idx1_pair.UpdatePair(idx1, idx1, newV, newM);
                    qemMinHeap.UpdatePair(idx1_pair, $"{iterCnt}_idx1_update");
                }

                // ensure to not add the same vertex pair
                HashSet<int> vertex_pair_idx1_set = new HashSet<int>();
                foreach (var idx1_pair in vertex_pair_dict[idx1]) {
                    if (idx1_pair.v1Idx == idx1) {
                        vertex_pair_idx1_set.Add(idx1_pair.v2Idx);
                    } else if (idx1_pair.v2Idx == idx1) {
                        vertex_pair_idx1_set.Add(idx1_pair.v1Idx);
                    }
                }

                // add all v2's vertex pair to v1
                foreach (var idx2_pair in vertex_pair_dict[idx2]) {
                    if (idx2_pair.v1Idx != idx1 && idx2_pair.v2Idx != idx1) {
                        DebugUtility.LogError($"不应该出现的情况，idx1 没有设置正确 * 2 ：{idx2_pair.v1Idx},{idx2_pair.v2Idx},{idx1},{idx2},iterCnt: {iterCnt}");
                        //continue;
                    }

                    bool noExistSamePair = (idx2_pair.v1Idx == idx1 && !vertex_pair_idx1_set.Contains(idx2_pair.v2Idx));
                    noExistSamePair = noExistSamePair || (idx2_pair.v2Idx == idx1 && !vertex_pair_idx1_set.Contains(idx2_pair.v1Idx));

                    if (noExistSamePair) {
                        vertex_pair_dict[idx1].Add(idx2_pair);
                    } 
                    //else {
                    //  idx2_pair.IsValid = false;
                    //}
                }
                vertex_pair_dict[idx2].Clear();

                // update all triangle that linked to idx2, replace them to idx1
                int commonTriCnt = 0;
                foreach (var triIdx in vertex_link_triangle_dict[idx2]) {
                    if (!triangle_valid[triIdx]) {
                        continue;
                    }

                    if (IsCommonTriangle(idx1, idx2, triIdx)) {
                        triangle_valid[triIdx] = false;
                        commonTriCnt++;
                    }

                    if (!triangle_valid[triIdx]) {
                        continue;
                    }

                    UpdateTriangleVert(idx2, idx1, triIdx);
                }

                // 把 idx2 的所有相邻三角形，移交给 idx1 顶点
                foreach (var triIdx in vertex_link_triangle_dict[idx2]) {
                    if (!triangle_valid[triIdx]) {
                        continue;
                    }

                    if (!vertex_link_triangle_dict[idx1].Contains(triIdx)) {
                        vertex_link_triangle_dict[idx1].Add(triIdx);
                    }
                }

                if (commonTriCnt != 2 && commonTriCnt != 0) {
                    DebugUtility.LogError($"不应该出现的情况！检测到当前vertpair的共边三角型 ：{commonTriCnt}");
                }

                // TODO : 在这里更新进度
                //SimplifyProcess.AddSimplifyProcess();
                //int progressID, string taskName, float progress
                //ProgressManager.GetInstance().SyncProgress();

                curCnt--;
                Interlocked.Increment(ref curReducedCnt);
            }

        }

        private bool IsCommonTriangle(int idx1, int idx2, int triIdx) {

            // triangle that use this pair as common egde, set them not valid
            int tri_vert_start = triIdx * 3;
            int tri_v1 = triangles[tri_vert_start];
            int tri_v2 = triangles[tri_vert_start + 1];
            int tri_v3 = triangles[tri_vert_start + 2];

            bool exist_idx1 = tri_v1 == idx1 || tri_v2 == idx1 || tri_v3 == idx1;
            bool exist_idx2 = tri_v1 == idx2 || tri_v2 == idx2 || tri_v3 == idx2;

            bool flag = false;
            if (tri_v1 == idx1 && tri_v1 == idx2) {
                flag = true;
            } else if (tri_v2 == idx1 && tri_v2 == idx2) {
                flag = true;
            } else if (tri_v3 == idx1 && tri_v3 == idx2) {
                flag = true;
            }
            if (flag) {
                DebugUtility.LogError($"不应该出现的情况！计算共边三角型时发现 两个顶点落在了同样的索引上！{idx1}");
                return false;
            }

            if (exist_idx1 && exist_idx2) {
                return true;
            } else {
                return false;
            }
        }

        private void UpdateTriangleVert(int oldIdx, int newIdx, int triIdx) {
            int tri_vert_start = triIdx * 3;
            if (triangle_contains_vert_dict[triIdx][0] == oldIdx) {
                triangles[tri_vert_start] = newIdx;
                triangle_contains_vert_dict[triIdx][0] = newIdx;
            }
            if (triangle_contains_vert_dict[triIdx][1] == oldIdx) {
                triangles[tri_vert_start + 1] = newIdx;
                triangle_contains_vert_dict[triIdx][1] = newIdx;
            } 
            if (triangle_contains_vert_dict[triIdx][2] == oldIdx) {
                triangles[tri_vert_start + 2] = newIdx;
                triangle_contains_vert_dict[triIdx][2] = newIdx;
            }
        }

        public MeshWrapper EndSimplify(ref List<int> newEdgeVerts, ref List<int> outOfMeshTriangles) {
            DebugUtility.Log("now we end simplify, and you can get the mesh", DebugPriority.Medium);
            //DebugSimplifyResult();

            // 去掉所有 not valid 的顶点的vertex
            int vertNum = vertexs.Length;
            int curOffset = 0;

            // record the origin idx in triangle to new idx(after simplify)
            List<Vector3> newVerts = new List<Vector3>();
            Dictionary<int, int> originVertIdx_NewIdx_Dict = new Dictionary<int, int>(vertNum);

            for (int i = 0; i < vertNum; i++) {
                if (!vertex_valid[i]) {
                    curOffset++;
                    continue;
                }

                int newIdx = i - curOffset;

                // if this vert is used to be edge vert, then keep it
                if (edgeVertMap.Contains(i)) {
                    newEdgeVerts.Add(newIdx);
                }

                // old idx 是较大的数，new idx 是较小的数，所以不会发生冲突
                foreach (var triIdx in vertex_link_triangle_dict[i]) {
                    UpdateTriangleVert(i, newIdx, triIdx);
                }
                newVerts.Add(vertexs[i]);
                originVertIdx_NewIdx_Dict.Add(i, newIdx);
            }

            // 把 out triangle 里的idx 换成对应的新 idx
            for (int i = 0; i < outOfMeshTriangles.Count; i++) {
                int idx = outOfMeshTriangles[i];
                if (idx >= 0) {
                    outOfMeshTriangles[i] = originVertIdx_NewIdx_Dict[idx];
                }
            }

            // 去掉 不合法的 tris
            List<int> newTriangles = new List<int>();
            int triIdxNum = triangles.Length;
            int triNum = triIdxNum / 3;
            for (int triIdx = 0; triIdx < triNum; triIdx++) {
                if (!triangle_valid[triIdx]) {
                    // 该三角型不合法
                    continue;
                }

                int tri_vert_start = triIdx * 3;
                int v1 = triangles[tri_vert_start];
                int v2 = triangles[tri_vert_start + 1];
                int v3 = triangles[tri_vert_start + 2];

                newTriangles.Add(v1);
                newTriangles.Add(v2);
                newTriangles.Add(v3);
            }

            // NOTE : 法线改用法线贴图了
            for (int i = 0; i < newVerts.Count; i++) {
                
            }

            // TODO : 为每个顶点重新计算一下 uv
            List<Vector2> newUvs = new List<Vector2>();
            for (int i = 0; i < newVerts.Count; i++) {
                Vector3 curPos = newVerts[i] - clusterStartPoint;
                newUvs.Add(new Vector2(curPos.x / clusterSize, curPos.z / clusterSize));
            }


            MeshWrapper meshWrapper = new MeshWrapper(meshName, newVerts, newTriangles, null, newUvs, null);

            isSimplifyOver = true;
            return meshWrapper;
        }

        private void DebugSimplifyResult() {

            int vertValidCnt = 0;
            int triValidCnt = 0;
            foreach (var valid in vertex_valid) {
                if (valid) vertValidCnt++;
            }
            foreach (var valid in triangle_valid) {
                if (valid) triValidCnt++;
            }

            int reducedVertCnt = vertexs.Length - vertValidCnt;
            int reducedTriCnt = triangles.Length / 3 - triValidCnt;

            //DebugUtility.Log($"start vert : {vertexs.Length} end valid num : {vertValidCnt}");
            //DebugUtility.Log($"start tri : {triangles.Length / 3} end valid num : {triValidCnt}");
            DebugUtility.Log($"reduced vert cnt: {reducedVertCnt}, reduced tri cnt: {reducedTriCnt}", DebugPriority.Medium);
            if(reducedVertCnt * 2 != reducedTriCnt) {
                DebugUtility.LogError($"wrong vert-tri relation, should have {reducedVertCnt * 2} reduced tri");
            }
        }


        public int GetTargetCnt() { return targetCnt; }

        public int GetReducedCnt() {
            return Interlocked.CompareExchange(ref curReducedCnt, 0, 0);
        }

        public bool GetSimplifyOver() { return isSimplifyOver; }

    }

    // ERROR : 当前计算误差度量值，也是有问题的！
    internal class VectorPair {

        public int idxHeap { get; private set; }
        public void SetHeapIdx(int idx) {  idxHeap = idx; }

        public int v1Idx { get; private set; }
        public int v2Idx { get; private set; }
        public Vector3 v1 { get; private set; }
        public Vector3 v2 { get; private set; }
        public Matrix4x4 Q1 { get; private set; }
        public Matrix4x4 Q2 { get; private set; }
        public float Error { get; private set; } // 存储合并的误差值

        public bool IsValid { get; set; }

        public VectorPair(int idx1, int idx2, Vector3 v1, Vector3 v2, Matrix4x4 q1, Matrix4x4 q2) {
            v1Idx = idx1;
            v2Idx = idx2;
            this.v1 = v1;
            this.v2 = v2;
            Q1 = q1;
            Q2 = q2;
            Error = ComputePairError(); // 计算误差
            IsValid = true;
        }

        public bool ContainsVert(int idx) {
            return idx == v1Idx || idx == v2Idx;
        }

        public void UpdatePair(int oldIdx, int newIdx, Vector3 newV, Matrix4x4 newQ) {
            if (v1Idx == v2Idx) {
                DebugUtility.LogError($"不应该出现的状况！顶点对的两个顶点一样，{v1Idx}, {v2Idx}, 传入顶点：{oldIdx}，{newIdx}");
                IsValid = false;
                return;
            } else if ((v1Idx == oldIdx && v2Idx == newIdx) || (v2Idx == oldIdx && v1Idx == newIdx) && (oldIdx != newIdx)) {
                // TODO : 到底什么时候会发出这个错误？？？
                DebugUtility.LogError($"不应该出现的状况！新顶点和另一个顶点一样，{v1Idx}, {v2Idx}, 传入顶点：{oldIdx}，{newIdx}");
                //IsValid = false;
                return;
            }

            if (oldIdx == v1Idx) {
                v1Idx = newIdx;
                v1 = newV;
                Q1 = newQ;
            } else if(oldIdx == v2Idx) {
                v2Idx = newIdx;
                v2 = newV;
                Q2 = newQ;
            }

            Error = ComputePairError();
        }

        public float ComputePairError() {
            Vector3 vOptimal = GetMergedPosition_Optimal();
            Vector4 vH = new Vector4(vOptimal.x, vOptimal.y, vOptimal.z, 1);
            Matrix4x4 Q = SimplifyHelper.AddMatrices(Q1, Q2);
            return Vector4.Dot(vH, Q * vH);
        }

        public Vector3 GetMergedPosition_Optimal() {
            Matrix4x4 Q = SimplifyHelper.AddMatrices(Q1, Q2);
            Matrix4x4 A = new Matrix4x4();
            A.SetRow(0, new Vector4(Q.m00, Q.m01, Q.m02, 0));
            A.SetRow(1, new Vector4(Q.m10, Q.m11, Q.m12, 0));
            A.SetRow(2, new Vector4(Q.m20, Q.m21, Q.m22, 0));
            Vector3 b = new Vector3(-Q.m03, -Q.m13, -Q.m23);

            // 求 A 的行列式，判断是否可逆
            float det = A.determinant;
            if (Mathf.Abs(det) > 1e-6f) {
                Matrix4x4 A_inv = A.inverse;
                return A_inv.MultiplyPoint3x4(b);
            } else {
                return (v1 + v2) * 0.5f;
            }
        }

        public Vector3 GetMergedPosition() {
            return (v1 + v2) / 2;
        }

        public bool EqualTo(VectorPair other) {
            return (v1Idx == other.v1Idx && v2Idx == other.v2Idx) || (v1Idx == other.v2Idx && v2Idx == other.v1Idx);
        }

        //public override int GetHashCode() {
        //    int min = Mathf.Min(v1Idx, v2Idx);
        //    int max = Mathf.Max(v1Idx, v2Idx);
        //    return (int)Mathf.Pow(min, max);
        //    //return (int)Mathf.Pow(v1Idx.GetHashCode(), v2Idx.GetHashCode());
        //}

    }

    // TODO: 重写一下这个堆结构！！！
    // ERROR : 堆是有问题的！！！为什么同样的顶点对 会被操作多次？
    internal class QEMMinHeap {
        
        List<VectorPair> heap = new List<VectorPair>();

        public int Count => heap.Count;

        private int Parent(int i) {
            return (i - 1) / 2;
        }
        private int LeftChild(int i) {
            return 2 * i + 1;
        }
        private int RightChild(int i) {
            return 2 * i + 2;
        }

        private int primerCnt = 0;

        public void Insert(VectorPair pair) {
            pair.SetHeapIdx(primerCnt);
            heap.Add(pair);
            HeapifyUp(heap.Count - 1);
            primerCnt++;
        }

        public VectorPair GetTop() {
            if (heap.Count == 0) {
                return null;
            }

            while (heap.Count > 0 && !heap[0].IsValid) {
                Pop();
            }

            if (heap.Count == 0) {
                return null;
            }
            VectorPair min = Pop();
            return min;
        }

        public VectorPair Pop() {
            VectorPair tmp = heap[0];
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            HeapifyDown(0);
            tmp.IsValid = false;
            return tmp;
        }


        public void UpdatePair(VectorPair pair, string logMes) {
            if (!pair.IsValid) {
                return;
            }

            // fix the error
            int idx = pair.idxHeap;
            int idxRec = idx;
            if (idx < 0 || idx >= heap.Count) {
                DebugUtility.LogError($"错误的 pair idx : {pair.idxHeap}, {pair.v1Idx}, {pair.v2Idx}, {logMes}");
                for (int i = 0; i < heap.Count; i++) {
                    heap[i].SetHeapIdx(i);
                    if (heap[i] == pair) {
                        idx = pair.idxHeap;
                        break;
                    }
                }
            }

            if(idxRec == idx) {
                // TODO : 这一步是关键！！！！
                pair.IsValid = false;
                return;
            }

            HeapifyUp(idx);
            HeapifyDown(idx);
        }


        private void HeapifyUp(int i) {
            while (i > 0 && heap[i].Error < heap[Parent(i)].Error) {
                Swap(i, Parent(i));
                i = Parent(i);
            }
        }

        private void HeapifyDown(int i) {
            int smallest = i;
            int left = LeftChild(i);
            int right = RightChild(i);

            if (left < heap.Count && heap[left].Error < heap[smallest].Error)
                smallest = left;

            if (right < heap.Count && heap[right].Error < heap[smallest].Error)
                smallest = right;

            if (smallest != i) {
                Swap(i, smallest);
                HeapifyDown(smallest);
            }
        }

        private void Swap(int i, int j) {
            heap[i].SetHeapIdx(j);
            heap[j].SetHeapIdx(i);
            (heap[i], heap[j]) = (heap[j], heap[i]);
        }

    }


    internal static class SimplifyHelper {

        internal static Matrix4x4 CaculateKpForPlane(Vector4 plane) {
            float a = plane.x;
            float b = plane.y;
            float c = plane.z;
            float d = plane.w;

            // 计算 K_p 矩阵的每个元素
            return new Matrix4x4(
                new Vector4(a * a, a * b, a * c, a * d),
                new Vector4(a * b, b * b, b * c, b * d),
                new Vector4(a * c, b * c, c * c, c * d),
                new Vector4(a * d, b * d, c * d, d * d)
            );
        }
        
        internal static Vector4 GetPlaneEquation(Vector3 p1, Vector3 p2, Vector3 p3) {
            Vector3 v1 = p2 - p1;
            Vector3 v2 = p3 - p1;

            // plane's normal
            Vector3 normal = Vector3.Cross(v1, v2);
            if (normal.sqrMagnitude == 0) {
                DebugUtility.LogError("v1 v2 v3 are in a line, error!");
                return Vector4.zero;
            }

            normal.Normalize();
            float d = -Vector3.Dot(normal, p1);
            return new Vector4(normal.x, normal.y, normal.z, d);
        }

        internal static Matrix4x4 AddMatrices(Matrix4x4 A, Matrix4x4 B) {
            Matrix4x4 result = new Matrix4x4();
            for (int i = 0; i < 4; i++) {
                for (int j = 0; j < 4; j++) {
                    result[i, j] = A[i, j] + B[i, j];
                }
            }
            return result;
        }

    }

    // TODO : 需要一个线程安全的 全局计数器，最好不要使用 下面这种 static 的方式实现...
    public class SimplifyProcess
    {
        static readonly object lockObj = new object();

        static int SimplifiedVertCount = 0;
        static float SimplifiedProcess = 0;
        static int SimplifiedTargetCount = 1;

        public static void StartRecSimplifyProcess(int startTotalVertNum, int targetVertNum)
        {
            SimplifiedVertCount = 0;
            SimplifiedProcess = 0;
            SimplifiedTargetCount = startTotalVertNum - targetVertNum;
        }

        public static void AddSimplifyProcess()
        {
            lock (lockObj)
            {
                SimplifiedVertCount++;
            }
        }

        public static float GetSimplifyProcess()
        {
            SimplifiedProcess = SimplifiedVertCount / SimplifiedTargetCount;
            return SimplifiedProcess;
        }

        public static void EndRecSimplifyProcess()
        {
            SimplifiedVertCount = 0;
            SimplifiedProcess = 0;
            SimplifiedTargetCount = 1;
        }

    }

}
