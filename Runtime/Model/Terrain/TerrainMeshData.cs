using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.Mesh;

namespace LZ.WarGameMap.Runtime
{

    public class TerrainMeshData : IBinarySerializer, IDisposable {

        public int tileIdxX { get; private set; }
        public int tileIdxY { get; private set; }
        public int curLODLevel { get; private set; }

        Vector3[] vertexs = new Vector3[1];
        Vector3[] outofMeshVertexs = new Vector3[1];
        Vector3[] normals = new Vector3[1];
        Vector2[] uvs = new Vector2[1];
        Color[] colors = new Color[1];

        int[,] vertexIndiceMap = new int[1, 1];         // map the (x, y) to index in vertexs/outofMeshVertexs

        int[] triangles = new int[1];
        int[] outOfMeshTriangles = new int[1];

        private Vector3[] fixedVertexs = new Vector3[1];
        private Vector3[] fixedOutMeshVertexs = new Vector3[1];

        List<int> edgeVertIdxs;                             // storage the vertex indice on egde

        private int triangleIndex = 0;
        private int outOfMeshTriangleIndex = 0;
        private int vertexPerLine;
        private int vertexPerLineFixed;

        Mesh tileMesh;


        public void InitMeshData(int tileIdxX, int tileIdxY, int lodLevel, int gridNumPerLine, int gridNumPerLineFixed, int vertexPerLine, int vertexPerLineFixed) {
            this.tileIdxX = tileIdxX;
            this.tileIdxY = tileIdxY;

            this.curLODLevel = lodLevel;
            this.vertexPerLine = vertexPerLine;
            this.vertexPerLineFixed = vertexPerLineFixed;

            vertexs = new Vector3[vertexPerLine * vertexPerLine];
            outofMeshVertexs = new Vector3[vertexPerLine * 4 + 4];
            normals = new Vector3[vertexPerLine * vertexPerLine];
            uvs = new Vector2[vertexPerLine * vertexPerLine];
            colors = new Color[vertexPerLine * vertexPerLine];

            vertexIndiceMap = new int[vertexPerLineFixed, vertexPerLineFixed];

            triangles = new int[gridNumPerLine * gridNumPerLine * 2 * 3];
            outOfMeshTriangles = new int[(gridNumPerLine + 1) * 4 * 2 * 3];

            triangleIndex = 0;
            outOfMeshTriangleIndex = 0;
        }

        public void Dispose() {
#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(tileMesh);
#else
            UnityEngine.Object.Destroy(tileMesh);
#endif
        }


        #region add vertex; geometry handle

        public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertIndex) {
            if (vertIndex < 0) {
                outofMeshVertexs[-vertIndex - 1] = vertexPosition;
            } else {
                vertexs[vertIndex] = vertexPosition;
                uvs[vertIndex] = uv;
                normals[vertIndex] = new Vector3(0, 1, 0);

                colors[vertIndex] = GetColorByHeight(vertexPosition.y);
            }
        }

        public void AddTriangle(int a, int b, int c, int i = 0, int j = 0) {
            if (a < 0 || b < 0 || c < 0) {
                if (outOfMeshTriangleIndex + 1 > outOfMeshTriangles.Length - 1) {
                    Debug.LogError(string.Format("triangle idx : {0}, {1} !", i, j));
                    Debug.LogError(string.Format("out of bound! cur idx : {0}, cur a : {1}, cur b : {2}, cur c : {3}, length : {4}", outOfMeshTriangleIndex, a, b, c, outOfMeshTriangles.Length));
                }
                outOfMeshTriangles[outOfMeshTriangleIndex] = a;
                outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
                outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
                outOfMeshTriangleIndex += 3;
            } else {
                if (triangleIndex == 391684 || triangleIndex == 391680 || triangleIndex == 391681 || triangleIndex == 391682 || triangleIndex == 391683) {
                    int test = 1;
                }
                if (triangleIndex == 1533) {
                    int test = 1;
                }
                triangles[triangleIndex] = a;
                triangles[triangleIndex + 1] = b;
                triangles[triangleIndex + 2] = c;
                triangleIndex += 3;
            }
        }


        struct InnerTriangleNormalJob : IJobParallelFor {

            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> newNormals;

            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<int> triangles;

            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<Vector3> vertexs;

            public void Execute(int i) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex + 1];
                int vertexIndexC = triangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices_Mesh(vertexIndexA, vertexIndexB, vertexIndexC);

                newNormals[vertexIndexA] += triangleNormal;
                newNormals[vertexIndexB] += triangleNormal;
                newNormals[vertexIndexC] += triangleNormal;
            }

            private Vector3 SurfaceNormalFromIndices_Mesh(int indexA, int indexB, int indexC) {
                Vector3 pointA = vertexs[indexA];
                Vector3 pointB = vertexs[indexB];
                Vector3 pointC = vertexs[indexC];

                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                return Vector3.Cross(sideAB, sideAC).normalized;
            }

        }

        struct OutterTriangleNormalJob : IJobParallelFor {
            [NativeDisableParallelForRestriction]
            public NativeArray<Vector3> newNormals;

            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<int> outOfMeshTriangles;

            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<Vector3> outofMeshVertexs;

            [NativeDisableParallelForRestriction]
            [ReadOnly] public NativeArray<Vector3> vertexs;

            public void Execute(int i) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices_Mesh(vertexIndexA, vertexIndexB, vertexIndexC);

                if (vertexIndexA >= 0) {
                    newNormals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0) {
                    newNormals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0) {
                    newNormals[vertexIndexC] += triangleNormal;
                }
            }

            private Vector3 SurfaceNormalFromIndices_Mesh(int indexA, int indexB, int indexC) {
                Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : vertexs[indexA];
                Vector3 pointB = (indexB < 0) ? outofMeshVertexs[-indexB - 1] : vertexs[indexB];
                Vector3 pointC = (indexC < 0) ? outofMeshVertexs[-indexC - 1] : vertexs[indexC];

                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                return Vector3.Cross(sideAB, sideAC).normalized;
            }

        }

        struct NormalizeJob : IJobParallelFor {
            //[NativeDisableParallelForRestriction]
            public NativeArray<Vector3> newNormals;

            public void Execute(int index) {
                newNormals[index].Normalize();
            }
        }

        // code ref: Procedural-Landmass-Generation-master\Proc Gen E21
        private void RecaculateNormal_Origin() {

            int triangleCount = triangles.Length / 3;
            for (int i = 0; i < triangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex + 1];
                int vertexIndexC = triangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                //Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                normals[vertexIndexA] += triangleNormal;
                normals[vertexIndexB] += triangleNormal;
                normals[vertexIndexC] += triangleNormal;
            }

            // border triangle, caculate their value to normal
            int borderTriangleCount = outOfMeshTriangles.Length / 3;
            for (int i = 0; i < borderTriangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                //Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0) {
                    normals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0) {
                    normals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0) {
                    normals[vertexIndexC] += triangleNormal;
                }
            }

            for (int i = 0; i < normals.Length; i++) {
                normals[i].Normalize();
            }
        }

        public void RecaculateNormal_Mesh(Mesh mesh) {

            int vertCnt = mesh.vertices.Length;

            NativeArray<Vector3> newNormals = new NativeArray<Vector3>(vertCnt, Allocator.TempJob);

            NativeArray<Vector3> vertexs_job = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob);
            NativeArray<int> triangles_job = new NativeArray<int>(mesh.triangles, Allocator.TempJob);

            NativeArray<Vector3> outofMeshVertexs_job = new NativeArray<Vector3>(outofMeshVertexs, Allocator.TempJob);
            NativeArray<int> outofMeshTriangles_job = new NativeArray<int>(outOfMeshTriangles, Allocator.TempJob);

            // caculate inner triangle normals by job
            int triangleCount = mesh.triangles.Length / 3;
            InnerTriangleNormalJob innerTriangleNormalJob = new InnerTriangleNormalJob() {
                newNormals = newNormals,
                triangles = triangles_job,
                vertexs = vertexs_job,
            };
            JobHandle jobHandle1 = innerTriangleNormalJob.Schedule(triangleCount, 64);
            jobHandle1.Complete();

            // NOTE : the outOfMeshTriangles never change
            // caculate outter triangle normals by job
            int borderTriangleCount = outOfMeshTriangles.Length / 3;
            OutterTriangleNormalJob outterTriangleNormalJob = new OutterTriangleNormalJob() {
                newNormals = newNormals,
                outofMeshVertexs = outofMeshVertexs_job,
                outOfMeshTriangles = outofMeshTriangles_job,
                vertexs = vertexs_job,
            };
            JobHandle jobHandle2 = outterTriangleNormalJob.Schedule(borderTriangleCount, 64);
            jobHandle2.Complete();

            // normalize the newNormal
            NormalizeJob normalizeJob = new NormalizeJob() {
                newNormals = newNormals,
            };
            JobHandle jobHandle3 = normalizeJob.Schedule(vertCnt, 64);
            jobHandle3.Complete();

            //for (int i = 0; i < newNormals.Length; i++) {
            //    newNormals[i].Normalize();
            //}

            mesh.SetNormals(newNormals);
        }

        [Obsolete]
        public void RecaculateNormal_Mesh_v1(Mesh mesh) {
            // this function will set a normal for current mesh

            int vertCnt = mesh.vertices.Length;
            Vector3[] newNormals = new Vector3[vertCnt];

            int triangleCount = mesh.triangles.Length / 3;
            for (int i = 0; i < triangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = mesh.triangles[normalTriangleIndex];
                int vertexIndexB = mesh.triangles[normalTriangleIndex + 1];
                int vertexIndexC = mesh.triangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices_Mesh(vertexIndexA, vertexIndexB, vertexIndexC, mesh);
                //Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                newNormals[vertexIndexA] += triangleNormal;
                newNormals[vertexIndexB] += triangleNormal;
                newNormals[vertexIndexC] += triangleNormal;
            }

            // NOTE : the outOfMeshTriangles never change
            int borderTriangleCount = outOfMeshTriangles.Length / 3;
            for (int i = 0; i < borderTriangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices_Mesh(vertexIndexA, vertexIndexB, vertexIndexC, mesh);
                //Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0) {
                    newNormals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0) {
                    newNormals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0) {
                    newNormals[vertexIndexC] += triangleNormal;
                }
            }

            for (int i = 0; i < newNormals.Length; i++) {
                newNormals[i].Normalize();
            }
            mesh.SetNormals(newNormals);
        }

        private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
            // 算三个点构成的三角型的叉乘
            Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : vertexs[indexA];
            Vector3 pointB = (indexB < 0) ? outofMeshVertexs[-indexB - 1] : vertexs[indexB];
            Vector3 pointC = (indexC < 0) ? outofMeshVertexs[-indexC - 1] : vertexs[indexC];

            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointA;
            return Vector3.Cross(sideAB, sideAC).normalized;
        }

        [Obsolete]
        private Vector3 SurfaceNormalFromIndices_Mesh(int indexA, int indexB, int indexC, Mesh mesh) {
            // caculate cross, but use mesh
            if (-indexA - 1 >= outofMeshVertexs.Length || -indexB - 1 >= outofMeshVertexs.Length || -indexC - 1 >= outofMeshVertexs.Length) {
                Debug.Log(111);
                return Vector3.zero;
            }

            Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : mesh.vertices[indexA];  // ！！！
            Vector3 pointB = (indexB < 0) ? outofMeshVertexs[-indexB - 1] : mesh.vertices[indexB];
            Vector3 pointC = (indexC < 0) ? outofMeshVertexs[-indexC - 1] : mesh.vertices[indexC];

            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointA;
            return Vector3.Cross(sideAB, sideAC).normalized;
        }

        private Vector3 SurfaceNormalFromIndices_Fixed(int indexA, int indexB, int indexC) {
            Vector3 pointA = (indexA < 0) ? fixedOutMeshVertexs[-indexA - 1] : fixedVertexs[indexA];
            Vector3 pointB = (indexB < 0) ? fixedOutMeshVertexs[-indexB - 1] : fixedVertexs[indexB];
            Vector3 pointC = (indexC < 0) ? fixedOutMeshVertexs[-indexC - 1] : fixedVertexs[indexC];

            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointA;
            return Vector3.Cross(sideAB, sideAC).normalized;
        }

        public void ApplyRiverEffect(RiverDataManager riverDataManager)
        {
            int len = vertexs.Length;
            for (int i = 0; i < len; i++)
            {
                Vector3 point = vertexs[i];
                bool IsEffectByRiver;
                float offset = riverDataManager.SampleRiverRatio(point, out IsEffectByRiver);
                if (IsEffectByRiver)
                {
                    vertexs[i] = new Vector3(point.x, -offset, point.z);
                }
            }

        }

        // this function build a terrain tile mesh (tiled mesh)
        public void BuildOriginMesh() {
            RecaculateNormal_Origin();

            if (tileMesh == null) {
                tileMesh = new Mesh();
                tileMesh.name = string.Format("TerrainMesh_LOD{0}_Idx{1}_{2}", curLODLevel, tileIdxX, tileIdxY);

                if (vertexs.Length >= UInt16.MaxValue) {
                    tileMesh.indexFormat = IndexFormat.UInt32;
                }
            }

            tileMesh.vertices = vertexs;
            tileMesh.normals = normals;
            tileMesh.triangles = triangles;
            tileMesh.uv = uvs;
            tileMesh.colors = colors;
        }

        #endregion


        public void SetMesh(Mesh mesh) {
            GameObject.DestroyImmediate(tileMesh);
            tileMesh = mesh;
        }


        #region mesh data get/set

        public List<int> GetOutOfMeshTris() {
            return outOfMeshTriangles.ToList();
        }

        public void SetOutOfMeshTris(List<int> outOfMeshTriangles) {
            this.outOfMeshTriangles = outOfMeshTriangles.ToArray();
        }

        public void UpdateEdgeVertInfoByOrigin(List<Vector3> edgeRawNormals) {
            if(edgeVertIdxs != null) {
                return;
            }

            edgeVertIdxs = new List<int>(); 
            edgeRawNormals = new List<Vector3>();       // NOTE : 不需要传入 normal数组

            // firstly, we caculate the contribute of the outOfVert to the edgeNormals
            int borderTriangleCount = outOfMeshTriangles.Length / 3;
            Vector3[] rawNormals = new Vector3[normals.Length];
            for (int i = 0; i < borderTriangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
                int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
                int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0) {
                    rawNormals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0) {
                    rawNormals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0) {
                    rawNormals[vertexIndexC] += triangleNormal;
                }
            }

            int width = vertexIndiceMap.GetLength(0);
            int height = vertexIndiceMap.GetLength(1);

            // init the edge verts and normals
            for (int i = 1; i < width - 1; i++) {
                edgeVertIdxs.Add(vertexIndiceMap[i, 1]);
                edgeVertIdxs.Add(vertexIndiceMap[i, height - 2]);
                Vector3 v1 = rawNormals[vertexIndiceMap[i, 1]];
                edgeRawNormals.Add(v1);
                edgeRawNormals.Add(rawNormals[vertexIndiceMap[i, height - 2]]);
            }
            // start with 2, because vert[1] has been added in 上面的
            for (int i = 2; i < height - 2; i++) {
                edgeVertIdxs.Add(vertexIndiceMap[1, i]);
                edgeVertIdxs.Add(vertexIndiceMap[width - 2, i]);
                edgeRawNormals.Add(rawNormals[vertexIndiceMap[1, i]]);
                edgeRawNormals.Add(rawNormals[vertexIndiceMap[width - 2, i]]);
            }
        }

        public List<int> GetEdgeVertInfo() {
            return this.edgeVertIdxs;
        }

        public void SetEdgeVertInfo(List<int> edgeVertIdxs) {
            this.edgeVertIdxs = edgeVertIdxs;
        }

        public Mesh GetMesh_LODHeight() {
            return tileMesh;
        }

        public Mesh GetMesh_LODDistance(int tileIdxX, int tileIdxY, int fixDirection) {
            Mesh mesh = new Mesh();
            mesh.name = string.Format("TerrainMesh_LOD{0}_Idx{1}_{2}", curLODLevel, tileIdxX, tileIdxY);

            // fix the lod seam
            fixedVertexs = vertexs;
            fixedOutMeshVertexs = outofMeshVertexs;
            bool fixLeft = ((fixDirection >> 0) & 1) == 1;
            bool fixRight = ((fixDirection >> 1) & 1) == 1;
            bool fixTop = ((fixDirection >> 2) & 1) == 1;
            bool fixBottom = ((fixDirection >> 3) & 1) == 1;
            if (fixLeft) {
                // NOTE : 这块代码和外层 TileMeshData.SetMeshData 存在耦合，很重的耦合
                FixLODEdgeSeam(true, 0, 1);
            }
            if (fixRight) {
                FixLODEdgeSeam(true, vertexPerLineFixed - 1, vertexPerLineFixed - 2);
            }
            if (fixTop) {
                FixLODEdgeSeam(false, vertexPerLineFixed - 1, vertexPerLineFixed - 2);
            }
            if (fixBottom) {
                FixLODEdgeSeam(false, 0, 1);
            }

            //RecaculateNormal();
            //RecaculateBorderNormal();

            mesh.vertices = fixedVertexs;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;

            return mesh;
        }

        private void FixLODEdgeSeam(bool isVertical, int outIdx, int inIdx) {
            for (int i = 2; i < vertexPerLine + 1; i += 2) {
                // TODO : change it, do not set to average, stick to neighbor vert;
                // traverse the indice map and reset the vertex position to neighbor's average

                int outNgb1Idx, outNgb2Idx, outofMeshIdx;
                int inNgb1Idx, inNgb2Idx, inMeshIdx;
                //Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : vertexs[indexA];
                if (isVertical) {
                    outNgb1Idx = vertexIndiceMap[outIdx, i - 1];
                    outNgb2Idx = vertexIndiceMap[outIdx, i + 1];
                    outofMeshIdx = vertexIndiceMap[outIdx, i];

                    inNgb1Idx = vertexIndiceMap[inIdx, i - 1];
                    inNgb2Idx = vertexIndiceMap[inIdx, i + 1];
                    inMeshIdx = vertexIndiceMap[inIdx, i];
                } else {
                    outNgb1Idx = vertexIndiceMap[i - 1, outIdx];
                    outNgb2Idx = vertexIndiceMap[i + 1, outIdx];
                    outofMeshIdx = vertexIndiceMap[i, outIdx];

                    inNgb1Idx = vertexIndiceMap[i - 1, inIdx];
                    inNgb2Idx = vertexIndiceMap[i + 1, inIdx];
                    inMeshIdx = vertexIndiceMap[i, inIdx];
                }
                fixedOutMeshVertexs[-outofMeshIdx - 1] = (fixedOutMeshVertexs[-outNgb1Idx - 1] + fixedOutMeshVertexs[-outNgb2Idx - 1]) / 2;
                fixedVertexs[inMeshIdx] = (fixedVertexs[inNgb1Idx] + fixedVertexs[inNgb2Idx]) / 2;
            }

        }

        public int GetIndiceInMap(int x, int y) {
            return vertexIndiceMap[x, y];
        }

        public void SetIndiceInMap(int x, int y, int idx) {
            vertexIndiceMap[x, y] = idx;
        }

        #endregion


        #region set landform (color) data

        public void InitLandform() {
            int len = vertexs.Length;
            for (int i = 0; i < len; i++) {
                Vector3 vertexPosition = vertexs[i];
                colors[i] = GetColorByHeight(vertexPosition.y);
                // 采样周围四个点来生成？
            }
        }

        private Color GetColorByHeight(float height) {
            Color lowLandColor = new Color(0.13f, 0.54f, 0.13f); // 深绿色，低地
            Color midLandColor = new Color(0.61f, 0.80f, 0.19f); // 浅绿色，中地
            Color highLandColor = new Color(0.85f, 0.65f, 0.13f); // 棕黄色，高地
            Color mountainColor = new Color(0.50f, 0.50f, 0.50f); // 灰色，山地
            Color snowColor = new Color(1.00f, 1.00f, 1.00f); // 白色，雪地

            if (height < 10f)
                return lowLandColor; // 低地
            else if (height < 15f)
                return midLandColor; // 中地
            else if (height < 23f)
                return highLandColor; // 高地
            else if (height < 30f)
                return mountainColor; // 山地
            else
                return snowColor; // 雪地
        }

        #endregion


        #region serialize

        // obsolete
        public void SerializeTerrainMesh(StreamWriter writer) {
            //int totalLength = ;   // TODO : 测试一下加上这个东西后能优化多少时间？
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"mesh:{curLODLevel}");

            //Vector3[] vertexs = new Vector3[1];
            //Vector3[] outofMeshVertexs = new Vector3[1];
            //Vector3[] normals = new Vector3[1];
            //Vector2[] uvs = new Vector2[1];
            //Color[] colors = new Color[1];

            for (int i = 0; i < vertexs.Length; i++) {
                stringBuilder.AppendLine($"v:{vertexs[i].ToStringFixed()}");
            }
            for (int i = 0; i < outofMeshVertexs.Length; i++) {
                stringBuilder.AppendLine($"ov:{vertexs[i].ToStringFixed()}");
            }
            for (int i = 0; i < normals.Length; i++) {
                stringBuilder.AppendLine($"n:{normals[i].ToStringFixed()}");
            }
            for (int i = 0; i < uvs.Length; i++) {
                stringBuilder.AppendLine($"uv:{uvs[i].ToStringFixed()}");
            }
            for (int i = 0; i < colors.Length; i++) {
                stringBuilder.AppendLine($"c:{colors[i].ToStringFixedRGB()}");
            }

            //int[,] vertexIndiceMap = new int[1, 1];         // map the (x, y) to index in vertexs/outofMeshVertexs
            for (int i = 0; i < vertexIndiceMap.GetLength(0); i++) {
                for(int j = 0; j < vertexIndiceMap.GetLength(1); j++) {
                    stringBuilder.AppendLine($"i:{i},{j},{vertexIndiceMap[i, j]}");
                }
            }

            //int[] triangles = new int[1];
            //int[] outOfMeshTriangles = new int[1];
            for (int i = 0; i < triangles.Length; i += 3) {
                stringBuilder.AppendLine($"t:{triangles[i]},{triangles[i + 1]},{triangles[i + 2]}");
            }
            for (int i = 0; i < outOfMeshTriangles.Length; i += 3) {
                stringBuilder.AppendLine($"ot:{outOfMeshTriangles[i]},{outOfMeshTriangles[i + 1]},{outOfMeshTriangles[i + 2]}");
            }
            writer.WriteLine(stringBuilder.ToString());
        }

        public void WriteToBinary(BinaryWriter writer) {
            //Vector3[] vertexs = new Vector3[1];
            //Vector3[] outofMeshVertexs = new Vector3[1];
            //Vector3[] normals = new Vector3[1];
            //Vector2[] uvs = new Vector2[1];
            //Color[] colors = new Color[1];

            writer.Write(vertexs.Length);
            for (int i = 0; i < vertexs.Length; i++) {
                writer.Write(vertexs[i].x); writer.Write(vertexs[i].y); writer.Write(vertexs[i].z);
            }
            writer.Write(outofMeshVertexs.Length);
            for (int i = 0; i < outofMeshVertexs.Length; i++) {
                writer.Write(outofMeshVertexs[i].x); writer.Write(outofMeshVertexs[i].y); writer.Write(outofMeshVertexs[i].z);
            }
            //for (int i = 0; i < normals.Length; i++) {
            //    writer.Write(normals[i].x); writer.Write(normals[i].y); writer.Write(normals[i].z);
            //}
            writer.Write(uvs.Length);
            for (int i = 0; i < uvs.Length; i++) {
                writer.Write(uvs[i].x); writer.Write(uvs[i].y);
            }
            //for (int i = 0; i < colors.Length; i++) {
            //    writer.Write(colors[i].r); writer.Write(colors[i].g); writer.Write(colors[i].b);
            //}

            // TODO : this var is used to fix lod seam, should not storage in file
            //int[,] vertexIndiceMap = new int[1, 1];         // map the (x, y) to index in vertexs/outofMeshVertexs
            writer.Write(vertexIndiceMap.GetLength(0));
            writer.Write(vertexIndiceMap.GetLength(1));
            for (int i = 0; i < vertexIndiceMap.GetLength(0); i++) {
                for (int j = 0; j < vertexIndiceMap.GetLength(1); j++) {
                    writer.Write(i); writer.Write(j); writer.Write(vertexIndiceMap[i, j]);
                }
            }

            //int[] triangles = new int[1];
            //int[] outOfMeshTriangles = new int[1];
            writer.Write(triangles.Length);
            for (int i = 0; i < triangles.Length; i += 3) {
                writer.Write(triangles[i]); writer.Write(triangles[i + 1]); writer.Write(triangles[i + 2]);
            }

            writer.Write(outOfMeshTriangles.Length);
            for (int i = 0; i < outOfMeshTriangles.Length; i += 3) {
                writer.Write(outOfMeshTriangles[i]); writer.Write(outOfMeshTriangles[i + 1]); writer.Write(outOfMeshTriangles[i + 2]);
            }
        }

        public void ReadFromBinary(BinaryReader reader) {
            // 
            // Vector3[] vertexs = new Vector3[1];
            // Vector3[] outofMeshVertexs = new Vector3[1];
            // Vector3[] normals = new Vector3[1];
            // Vector2[] uvs = new Vector2[1];
            // Color[] colors = new Color[1];
            //
            int vertLen = reader.ReadInt32();
            vertexs = new Vector3[vertLen];
            for (int i = 0; i < vertLen; i++) {
                vertexs[i].x = reader.ReadSingle(); vertexs[i].y = reader.ReadSingle(); vertexs[i].z = reader.ReadSingle();
            }

            int outofMeshVertexsLen = reader.ReadInt32();
            outofMeshVertexs = new Vector3[outofMeshVertexsLen];
            for (int i = 0; i < outofMeshVertexs.Length; i++) {
                outofMeshVertexs[i].x = reader.ReadSingle(); outofMeshVertexs[i].y = reader.ReadSingle(); outofMeshVertexs[i].z = reader.ReadSingle();
            }

            int uvsLen = reader.ReadInt32();
            uvs = new Vector2[uvsLen];
            for (int i = 0; i < uvs.Length; i++) {
                uvs[i].x = reader.ReadSingle(); uvs[i].y = reader.ReadSingle();
            }
            //for (int i = 0; i < colors.Length; i++) {
            //    writer.Write(colors[i].r); writer.Write(colors[i].g); writer.Write(colors[i].b);
            //}

            // TODO : this var is used to fix lod seam, should not storage in file
            //int[,] vertexIndiceMap = new int[1, 1];         // map the (x, y) to index in vertexs/outofMeshVertexs
            int w = reader.ReadInt32();
            int h = reader.ReadInt32();
            vertexIndiceMap = new int[w, h];
            for (int i = 0; i < w; i++) {
                for (int j = 0; j < h; j++) {
                    int _i = reader.ReadInt32(); int _j = reader.ReadInt32();
                    vertexIndiceMap[i, j] = reader.ReadInt32();
                }
            }

            //int[] triangles = new int[1];
            //int[] outOfMeshTriangles = new int[1];
            int trianglesLen = reader.ReadInt32();
            triangles = new int[trianglesLen];
            for (int i = 0; i < triangles.Length; i += 3) {
                triangles[i] = reader.ReadInt32(); triangles[i + 1] = reader.ReadInt32(); triangles[i + 2] = reader.ReadInt32();
            }
            int outOfMeshTrianglesLen = reader.ReadInt32();
            outOfMeshTriangles = new int[outOfMeshTrianglesLen];
            for (int i = 0; i < outOfMeshTriangles.Length; i += 3) {
                outOfMeshTriangles[i] = reader.ReadInt32(); outOfMeshTriangles[i + 1] = reader.ReadInt32(); outOfMeshTriangles[i + 2] = reader.ReadInt32();
            }
        }


        #endregion

    }

}
