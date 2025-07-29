using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    public class RiverMesh : IBinarySerializer, IDisposable
    {
        // 切线，Vertex，索引需要动态地构建
        int triIdx = 0;

        Vector3[] vertexs;
        Vector2[] tangents;
        int[] triangles;

        MeshFilter meshFiler;
        MeshRenderer renderer;

        Mesh mesh;

        public void InitRiverMesh(int borderVertNum, MeshFilter meshFiler, MeshRenderer renderer)
        {
            triIdx = 0;
            vertexs = new Vector3[borderVertNum * 2];
            tangents = new Vector2[borderVertNum * 2];
            triangles = new int[borderVertNum * 3 * 2];

            this.meshFiler = meshFiler;
            this.renderer = renderer;
        }

        public void SetBorderVert(int idx, Vector3 vert, Vector2 tangent, int offset)
        {
            vertexs[idx + offset] = vert;
            tangents[idx + offset] = tangent;
        }

        public void AddTriangle(int aIdx, int bIdx, int cIdx)
        {
            triangles[triIdx] = aIdx;
            triangles[triIdx + 1] = bIdx;
            triangles[triIdx + 2] = cIdx;
            triIdx += 3;
        }

        public void BuildOrightMesh()
        {
            if(mesh == null)
            {
                mesh = new Mesh();
            }
            else
            {
                GameObject.DestroyImmediate(mesh);
            }
            mesh.SetVertices(vertexs);
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
    }
}
