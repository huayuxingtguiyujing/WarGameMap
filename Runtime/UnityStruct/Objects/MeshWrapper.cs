using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LZ.WarGameMap.Runtime
{
    public class MeshWrapper : UnityObjectBase, IDisposable
    {
        List<Vector3> vertexs = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> colors = new List<Color>();

        Mesh mesh;
        public string meshName;

        public MeshWrapper()
        {
            IsValid = false;
        }

        public MeshWrapper(string meshName, List<Vector3> vertexs, List<int> triangles, List<Vector3> normals, List<Vector2> uvs, List<Color> colors)
        {
            this.vertexs = vertexs;
            this.triangles = triangles;
            this.normals = normals;
            this.uvs = uvs;
            this.colors = colors;
            IsValid = true;
        }


        #region Set Method

        public int GetVertNum() { return vertexs.Count; }

        public List<Vector3> GetVertex() { return vertexs; }

        public void SetVertex(List<Vector3> vertexs) { this.vertexs = vertexs; }

        public List<int> GetTriangles() { return triangles; }

        public void SetTriangles(List<int> triangles) { this.triangles = triangles; }

        public void SetNormals(List<Vector3> normals) { this.normals = normals; }

        public void SetUVs(List<Vector2> uvs) { this.uvs = uvs; }

        #endregion


        #region UnityEngine.Mesh 

        public Mesh GetMesh()
        {
            if (!IsValid)
            {
                DebugUtility.Log($"meshWrapper is not valid", DebugPriority.High);
                return null;
            }
            if (mesh == null)
            {
                BuildMesh(this.meshName);
            }
            return mesh;
        }

        public Mesh BuildMesh(string meshName)
        {
            this.meshName = meshName;
            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = meshName;

                if (vertexs.Count >= UInt16.MaxValue)
                {
                    mesh.indexFormat = IndexFormat.UInt32;
                }
            }

            mesh.vertices = vertexs.ToArray();
            if (normals != null)
            {
                mesh.normals = normals.ToArray();
            }
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            if (colors != null)
            {
                mesh.colors = colors.ToArray();
            }

            mesh.RecalculateBounds();
            //mesh.RecalculateNormals();
            return mesh;
        }

        public void SetMesh(Mesh mesh)
        {
            if (this.mesh != null)
            {
                GameObject.DestroyImmediate(this.mesh);
            }
            this.mesh = mesh;
        }

        #endregion

        public void Dispose()
        {
#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(mesh);
#else
            UnityEngine.Object.Destroy(tileMesh);
#endif
        }
    

    }
}
