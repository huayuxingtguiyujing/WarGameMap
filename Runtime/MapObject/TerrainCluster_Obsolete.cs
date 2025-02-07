using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static log4net.Appender.ColoredConsoleAppender;

namespace LZ.WarGameMap.Runtime
{

    // TODO : obsolete

    /// <summary>
    /// 地形图 的 分块 单位
    /// </summary>
    public class TerrainCluster_Obsolete : MonoBehaviour
    {

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        Mesh heightMesh;


        private float xzbias = 0, ybias = 0;
        public int Size = 4097;

        private Vector3[] vertices;
        private Vector3[] norms;
        private Color[] colors;

        private Vector2[] uv1;
        private Vector2[] uv2;
        private int[] triangles;

        int width;
        int height;

        GameObject signPrefab;
        Transform signTrans;

        //RenderTexture renderTexture;


        public void SetPrefab(GameObject signPrefab, Transform signTrans) {
            this.signPrefab = signPrefab;
            this.signTrans = signTrans;
        }

        public void InitHeightCluster(int width, int height, int gridSize, Vector3 startPoint) {
            this.width = width;
            this.height = height;

            heightMesh = new Mesh();
            heightMesh.name = "HeightCluster";
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = heightMesh;

            vertices = new Vector3[(width + 1) * (height + 1)];
            colors = new Color[(width + 1) * (height + 1)];
            norms = new Vector3[(width + 1) * (height + 1)];
            uv1 = new Vector2[(width + 1) * (height + 1)];
            triangles = new int[ width * height * 2 * 3];

            // set vertexs, idxs, uv
            for(int i = 0; i <= height; i++) {
                for(int j = 0;  j <= width; j++) {
                    int idx = i * (width + 1) + j;
                    int pos_x = j * gridSize;
                    int pos_y = i * gridSize;

                    float uv_x = j / width;
                    float uv_y = i / height;

                    // NOTE: the start point can add an offset to map tile
                    vertices[idx] = new Vector3(pos_x, 0, pos_y) + startPoint;
                    norms[idx] = new Vector3(0, 1, 0);
                    //colors[idx] = new Color(1, 1, 1, 1);
                    uv1[idx] = new Vector2(uv_x, uv_y);

                    //CreateSignObj(vertices[idx]);
                }
            }

            // set triangles, the sequence is set triangles in the rightDown, and then set leftUp
            int curGridIdx = 0;
            for (int i = 0; i < triangles.Length; i += 6) {
                int cur_w = curGridIdx % width;
                int cur_h = curGridIdx / width;
                int next_h = cur_h + 1;

                triangles[i] = cur_h * (width + 1) + cur_w;
                triangles[i + 1] = next_h * (width + 1) + cur_w + 1;
                triangles[i + 2] = cur_h * (width + 1) + cur_w + 1;

                triangles[i + 3] = cur_h * (width + 1) + cur_w;
                triangles[i + 4] = next_h * (width + 1) + cur_w;
                triangles[i + 5] = next_h * (width + 1) + cur_w + 1;

                string d1 = string.Format("{0} triangle1: {1}, {2}, {3}; ", curGridIdx, triangles[i], triangles[i + 1], triangles[i + 2]);
                string d2 = string.Format("{0} triangle2: {1}, {2}, {3}; ", curGridIdx, triangles[i + 3], triangles[i + 4], triangles[i + 5]);
                //Debug.Log(d1);
                //Debug.Log(d2);
                
                curGridIdx++;
            }

            SetClusterMesh();
        }

        internal void ClearClusterMesh() {
            heightMesh.Clear();
            vertices = new Vector3[1];
            triangles = new int[1];
            colors = new Color[1];
            uv1 = new Vector2[1];
            heightMesh.vertices = vertices.ToArray();
            heightMesh.triangles = triangles.ToArray();
            heightMesh.colors = colors.ToArray();
            heightMesh.uv = uv1;
            heightMesh.RecalculateNormals();
        }

        public void SetClusterMesh() {
            heightMesh.vertices = vertices;
            heightMesh.triangles = triangles;
            //heightMesh.colors = colors;
            heightMesh.uv = uv1;
            heightMesh.RecalculateNormals();
        }

        private void CreateSignObj(Vector3 pos) {
            GameObject go = Instantiate(signPrefab, signTrans);
            go.transform.position = pos;
        }

        public void SetHeights(float[,] heights) {
            int srcWsidth = heights.GetLength(0);
            int dstHeight = heights.GetLength(1);

            // resample the size of height map
            for (int i = 0; i <= height; i++) {
                for (int j = 0; j <= width; j++) {
                    int idx = i * (width + 1) + j;

                    float sx = i * (float)(srcWsidth - 1) / height;
                    float sy = j * (float)(dstHeight - 1) / width;

                    int x0 = Mathf.FloorToInt(sx);
                    int x1 = Mathf.Min(x0 + 1, srcWsidth - 1);
                    int y0 = Mathf.FloorToInt(sy);
                    int y1 = Mathf.Min(y0 + 1, dstHeight - 1);

                    float q00 = heights[x0, y0];
                    float q01 = heights[x0, y1];
                    float q10 = heights[x1, y0];
                    float q11 = heights[x1, y1];

                    float rx0 = Mathf.Lerp(q00, q10, sx - x0);
                    float rx1 = Mathf.Lerp(q01, q11, sx - x0);

                    // caculate the height by the data given
                    Vector3 rec = vertices[idx];
                    float h = Mathf.Lerp(rx0, rx1, sy - y0) * 200;
                    float fixed_h = Mathf.Clamp(h, 0, 50);
                    vertices[idx] = new Vector3(rec.x, h, rec.z);
                }
            }

            SetClusterMesh();
        }

    }
}
