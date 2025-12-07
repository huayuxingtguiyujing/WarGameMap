using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    public class TerrainMeshData : IBinarySerializer, IDisposable {

        public int tileIdxX { get; private set; }
        public int tileIdxY { get; private set; }
        public int curLODLevel { get; private set; }

        List<Vector3> vertexs = new List<Vector3>();
        List<Vector3> outofMeshVertexs = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        int[,] vertexIndiceMap = new int[1, 1];         // map the (x, y) to index in vertexs/outofMeshVertexs

        List<int> triangles = new List<int>();
        int[] outOfMeshTriangles = new int[1];

        private List<Vector3> fixedVertexs = new List<Vector3>();
        private List<Vector3> fixedOutMeshVertexs = new List<Vector3>();

        List<int> edgeVertIdxs;                             // storage the vertex indice on egde

        private int triangleIndex = 0;
        private int outOfMeshTriangleIndex = 0;
        private int vertexPerLine;
        private int vertexPerLineFixed;
        private int gridNumPerLine;

        MeshWrapper meshWrapper = new MeshWrapper();


        public bool IsInit { get; private set; }

        public void InitMeshData(int tileIdxX, int tileIdxY, int lodLevel, int gridNumPerLine, int gridNumPerLineFixed, int vertexPerLine, int vertexPerLineFixed) {
            this.tileIdxX = tileIdxX;
            this.tileIdxY = tileIdxY;

            this.curLODLevel = lodLevel;
            this.vertexPerLine = vertexPerLine;
            this.vertexPerLineFixed = vertexPerLineFixed;
            this.gridNumPerLine = gridNumPerLine;

            int vertNum = vertexPerLine * vertexPerLine;
            vertexs = new List<Vector3>(vertNum);
            vertexs.FillInList(vertNum);
            outofMeshVertexs = new List<Vector3>(vertexPerLine * 4 + 4);
            outofMeshVertexs.FillInList(vertexPerLine * 4 + 4);
            normals = new List<Vector3>(vertNum);
            normals.FillInList(vertNum);
            uvs = new List<Vector2>(vertNum);
            uvs.FillInList(vertNum);
            colors = new List<Color>(vertNum);
            colors.FillInList(vertNum);

            vertexIndiceMap = new int[vertexPerLineFixed, vertexPerLineFixed];

            triangles = new List<int>(gridNumPerLine * gridNumPerLine * 2 * 3);
            triangles.FillInList(gridNumPerLine * gridNumPerLine * 2 * 3);
            outOfMeshTriangles = new int[(gridNumPerLine + 1) * 4 * 2 * 3];

            triangleIndex = 0;
            outOfMeshTriangleIndex = 0;

            IsInit = false;
        }

        public void CopyMeshData(int lodLevel, TerrainMeshData other)
        {
            this.tileIdxX = tileIdxX;
            this.tileIdxY = tileIdxY;

            this.curLODLevel = lodLevel;
            this.vertexPerLine = other.vertexPerLine;
            this.vertexPerLineFixed = other.vertexPerLineFixed;
            this.gridNumPerLine = other.gridNumPerLine;

            //int sizeOfVec2 = 2 * sizeof(float);
            //int sizeOfVec3 = 3 * sizeof(float);     // sizeof(Vector3) need unsafe
            //int sizeOfColor = 4 * sizeof(float);

            vertexs = new List<Vector3>(other.vertexs);
            // new Vector3[vertexPerLine * vertexPerLine];
            //Array.Copy(other.vertexs, vertexs, vertexs.Length);

            outofMeshVertexs = new List<Vector3>(other.outofMeshVertexs);
            //new Vector3[vertexPerLine * 4 + 4];
            //Array.Copy(other.outofMeshVertexs, outofMeshVertexs, outofMeshVertexs.Length);

            normals = new List<Vector3>(other.normals);
            //normals = new Vector3[vertexPerLine * vertexPerLine];
            //Array.Copy(other.normals, normals, normals.Length);

            uvs = new List<Vector2>(other.uvs);
            //uvs = new Vector2[vertexPerLine * vertexPerLine];
            //Array.Copy(other.uvs, uvs, uvs.Length);

            colors = new List<Color>(other.colors);
            //colors = new Color[vertexPerLine * vertexPerLine];
            //Array.Copy(other.colors, colors, colors.Length);
            //Buffer.BlockCopy(other.colors, 0, colors, 0, colors.Length * sizeOfColor);

            vertexIndiceMap = new int[vertexPerLineFixed, vertexPerLineFixed];              // 2维
            Buffer.BlockCopy(other.vertexIndiceMap, 0, vertexIndiceMap, 0, vertexIndiceMap.Length * sizeof(int));

            triangles = new List<int>(other.triangles);
            //triangles = new int[gridNumPerLine * gridNumPerLine * 2 * 3];
            //Buffer.BlockCopy(other.triangles, 0, triangles, 0, triangles.Length * sizeof(int));

            outOfMeshTriangles = new int[(gridNumPerLine + 1) * 4 * 2 * 3];
            Buffer.BlockCopy(other.outOfMeshTriangles, 0, outOfMeshTriangles, 0, outOfMeshTriangles.Length * sizeof(int));

            triangleIndex = 0;
            outOfMeshTriangleIndex = 0;

            SetInited();  // already complete while copy other meshdata
        }

        public void SetInited()
        {
            IsInit = true;
        }

        public void Dispose() {
            meshWrapper.Dispose();
        }


        #region Add vertex; geometry handle

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
            if (a < 0 || b < 0 || c < 0) 
            {
                if (outOfMeshTriangleIndex + 1 > outOfMeshTriangles.Length - 1) {
                    DebugUtility.LogError(string.Format("triangle idx : {0}, {1} !", i, j));
                    DebugUtility.LogError(string.Format("out of bound! cur idx : {0}, cur a : {1}, cur b : {2}, cur c : {3}, length : {4}", outOfMeshTriangleIndex, a, b, c, outOfMeshTriangles.Length));
                }
                outOfMeshTriangles[outOfMeshTriangleIndex] = a;
                outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
                outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
                outOfMeshTriangleIndex += 3;
            } 
            else 
            {
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

            int triangleCount = triangles.Count / 3;
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

            for (int i = 0; i < normals.Count; i++) {
                normals[i].Normalize();
            }
        }

        public void RecaculateNormal_Mesh() {

            int vertCnt = meshWrapper.GetVertNum();

            NativeArray<Vector3> newNormals = new NativeArray<Vector3>(vertCnt, Allocator.TempJob);

            NativeArray<Vector3> vertexs_job = new NativeArray<Vector3>(meshWrapper.GetVertex().ToArray(), Allocator.TempJob);
            NativeArray<int> triangles_job = new NativeArray<int>(meshWrapper.GetTriangles().ToArray(), Allocator.TempJob);

            NativeArray<Vector3> outofMeshVertexs_job = new NativeArray<Vector3>(outofMeshVertexs.ToArray(), Allocator.TempJob);
            NativeArray<int> outofMeshTriangles_job = new NativeArray<int>(outOfMeshTriangles, Allocator.TempJob);

            // caculate inner triangle normals by job
            int triangleCount = meshWrapper.GetTriangles().Count / 3;
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

            meshWrapper.SetNormals(newNormals.ToList());

            newNormals.Dispose();
            vertexs_job.Dispose();
            triangles_job.Dispose();
            outofMeshVertexs_job.Dispose();
            outofMeshTriangles_job.Dispose();
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
            if (-indexA - 1 >= outofMeshVertexs.Count || -indexB - 1 >= outofMeshVertexs.Count || -indexC - 1 >= outofMeshVertexs.Count) {
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

        public void ApplyRiverEffect(HeightDataManager heightDataManager, RiverDataManager riverDataManager)
        {
            int len = vertexs.Count;
            for (int i = 0; i < len; i++)
            {
                Vector3 point = vertexs[i];
                riverDataManager.SampleRiverRatio(point, out bool IsEffectByRiver, out float offsetDown, out Vector2Int bindWorldPos);
                // TODO : 绑定不好搞哇
                if (IsEffectByRiver)
                {
                    //float bindTargetHeight = vertexs[bindTargetIdxInVert].y;
                    //float bindTargetHeight = heightDataManager.SampleFromHeightData(bindWorldPos.TransToXZ());
                    vertexs[i] = new Vector3(point.x, point.y - offsetDown, point.z);
                }
            }

            int outLen = outofMeshVertexs.Count;
            for (int i = 0; i < outLen; i++)
            {
                Vector3 point = outofMeshVertexs[i];
                riverDataManager.SampleRiverRatio(point, out bool IsEffectByRiver, out float offsetDown, out Vector2Int bindWorldPos);
                if (IsEffectByRiver)
                {
                    outofMeshVertexs[i] = new Vector3(point.x, point.y - offsetDown, point.z);
                }
            }
        }

        public void BuildOriginMesh()
        {
            if (!meshWrapper.IsValid)
            {
                DebugUtility.LogError("not valid meshWrapper!", DebugPriority.High);
            }
            string meshName = GetMeshName();
            meshWrapper.BuildMesh(meshName);
        }

        // this function build a terrain tile mesh (tiled mesh)
        public void BuildOriginMeshWrapper() {
            RecaculateNormal_Origin();

            meshWrapper = new MeshWrapper(GetMeshName(), vertexs.ToList(), triangles.ToList(), normals.ToList(), uvs.ToList(), colors.ToList());
        }

        #endregion

        public void SetMeshWrapper(MeshWrapper meshWrapper) {
            this.meshWrapper = meshWrapper;
        }

        #region Mesh data get/set

        public List<int> GetOutOfMeshTris() {
            return outOfMeshTriangles.ToList();
        }

        public void SetOutOfMeshTris(List<int> outOfMeshTriangles) {
            this.outOfMeshTriangles = outOfMeshTriangles.ToArray();
        }

        public void UpdateEdgeVertInfoByOrigin() {
            if(edgeVertIdxs != null) {
                return;
            }

            edgeVertIdxs = new List<int>(); 

            // firstly, we caculate the contribute of the outOfVert to the edgeNormals
            int borderTriangleCount = outOfMeshTriangles.Length / 3;
            Vector3[] rawNormals = new Vector3[normals.Count];
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
            }
            // start with 2, because vert[1] has been added in top part
            for (int i = 2; i < height - 2; i++) {
                edgeVertIdxs.Add(vertexIndiceMap[1, i]);
                edgeVertIdxs.Add(vertexIndiceMap[width - 2, i]);
            }
        }

        public List<int> GetEdgeVertInfo() {
            return this.edgeVertIdxs;
        }

        public void SetEdgeVertInfo(List<int> edgeVertIdxs) {
            this.edgeVertIdxs = edgeVertIdxs;
        }

        public MeshWrapper GetMeshWrapper()
        {
            return meshWrapper;
        }

        public Mesh GetMesh_LODHeight() {
            return meshWrapper.GetMesh();
        }

        public Mesh GetMesh_LODDistance(int tileIdxX, int tileIdxY, int fixDirection) {
            Mesh mesh = new Mesh();
            mesh.name = string.Format("TerrainMesh_LOD{0}_Idx{1}_{2}", curLODLevel, tileIdxX, tileIdxY);

            // fix the lod seam
            fixedVertexs = new List<Vector3>(vertexs);
            fixedOutMeshVertexs = new List<Vector3>(outofMeshVertexs);
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

            mesh.vertices = fixedVertexs.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.colors = colors.ToArray();

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

        public int GetVertNum()
        {
            return vertexs.Count;
        }

        public string GetMeshName()
        {
            return string.Format("TerrainMesh_LOD{0}_Idx{1}_{2}", curLODLevel, tileIdxX, tileIdxY);
        }

        #endregion


        #region Set landform (color) data

        public void InitLandform() {
            int len = vertexs.Count;
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


        #region Serialize

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

            for (int i = 0; i < vertexs.Count; i++) {
                stringBuilder.AppendLine($"v:{vertexs[i].ToStringFixed()}");
            }
            for (int i = 0; i < outofMeshVertexs.Count; i++) {
                stringBuilder.AppendLine($"ov:{vertexs[i].ToStringFixed()}");
            }
            for (int i = 0; i < normals.Count; i++) {
                stringBuilder.AppendLine($"n:{normals[i].ToStringFixed()}");
            }
            for (int i = 0; i < uvs.Count; i++) {
                stringBuilder.AppendLine($"uv:{uvs[i].ToStringFixed()}");
            }
            for (int i = 0; i < colors.Count; i++) {
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
            for (int i = 0; i < triangles.Count; i += 3) {
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

            writer.Write(vertexs.Count);
            for (int i = 0; i < vertexs.Count; i++) {
                writer.Write(vertexs[i].x); writer.Write(vertexs[i].y); writer.Write(vertexs[i].z);
            }
            writer.Write(outofMeshVertexs.Count);
            for (int i = 0; i < outofMeshVertexs.Count; i++) {
                writer.Write(outofMeshVertexs[i].x); writer.Write(outofMeshVertexs[i].y); writer.Write(outofMeshVertexs[i].z);
            }
            //for (int i = 0; i < normals.Length; i++) {
            //    writer.Write(normals[i].x); writer.Write(normals[i].y); writer.Write(normals[i].z);
            //}
            writer.Write(uvs.Count);
            for (int i = 0; i < uvs.Count; i++) {
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
            writer.Write(triangles.Count);
            for (int i = 0; i < triangles.Count; i += 3) {
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
            vertexs = new List<Vector3>(vertLen);
            vertexs.FillInList(vertLen);
            for (int i = 0; i < vertLen; i++) {
                vertexs[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            int outofMeshVertexsLen = reader.ReadInt32();
            outofMeshVertexs = new List<Vector3>(outofMeshVertexsLen);
            outofMeshVertexs.FillInList(outofMeshVertexsLen);
            for (int i = 0; i < outofMeshVertexs.Count; i++) {
                outofMeshVertexs[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }

            int uvsLen = reader.ReadInt32();
            uvs = new List<Vector2>(uvsLen);
            vertexs.FillInList(vertLen);
            for (int i = 0; i < uvs.Count; i++) {
                uvs[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }

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
            triangles = new List<int>(trianglesLen);
            for (int i = 0; i < triangles.Count; i += 3) {
                triangles[i] = reader.ReadInt32(); triangles[i + 1] = reader.ReadInt32(); triangles[i + 2] = reader.ReadInt32();
            }
            int outOfMeshTrianglesLen = reader.ReadInt32();
            outOfMeshTriangles = new int[outOfMeshTrianglesLen];
            for (int i = 0; i < outOfMeshTriangles.Length; i += 3) {
                outOfMeshTriangles[i] = reader.ReadInt32(); outOfMeshTriangles[i + 1] = reader.ReadInt32(); outOfMeshTriangles[i + 2] = reader.ReadInt32();
            }
        }


        #endregion


        #region Paint In Editor

        // TODO : 验证
        public List<Vector3> GetPointsInScope(Vector3 pos, float scope)
        {
            Vector2 posXZ = new Vector2(pos.x, pos.z);
            List<Vector3> pointList = new List<Vector3>(16);
            for(int i = 0; i < vertexs.Count; i++)
            {
                Vector3 point = vertexs[i];
                Vector2 pointXZ = new Vector2(point.x, point.z);
                float dist = Vector2.Distance(posXZ, pointXZ);
                if(dist <= scope)
                {
                    pointList.Add(point);
                }
            }
            return pointList;
        }

        // TODO : 验证
        public void UpdatePaintPoints(List<Vector3> newPoints, List<Vector2Int> pointInTileIdx)
        {
            for (int i = 0; i < pointInTileIdx.Count; i++)
            {
                Vector2Int pointInTile = pointInTileIdx[i];
                int idx = GetIndiceInMap(pointInTile.x, pointInTile.y);
                if (idx < 0)
                {
                    outofMeshVertexs[-idx - 1] = newPoints[i];
                }
                else
                {
                    vertexs[idx] = newPoints[i];
                }
            }
            meshWrapper.SetVertex(vertexs);
        }

        #endregion

    }

}
