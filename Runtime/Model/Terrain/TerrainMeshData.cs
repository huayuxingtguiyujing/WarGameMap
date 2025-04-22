using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    // TODO : 地貌的修改也得到这里，到时候怎么搞？是不是应该拆出来？
    public class TerrainMeshData : IBinarySerializer {

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

        private int triangleIndex = 0;
        private int outOfMeshTriangleIndex = 0;
        private int vertexPerLine;
        private int vertexPerLineFixed;

        public void InitMeshData(int gridNumPerLine, int gridNumPerLineFixed, int vertexPerLine, int vertexPerLineFixed) {
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
                triangles[triangleIndex] = a;
                triangles[triangleIndex + 1] = b;
                triangles[triangleIndex + 2] = c;
                triangleIndex += 3;
            }
        }

        // 此处代码参考：Procedural-Landmass-Generation-master\Proc Gen E21
        public void RecaculateNormal() {

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

        // TODO: 这是一个冗余的函数，要改！; TODO : 也许可以改成 JobSystem ？
        public void RecaculateBorderNormal() {
            // TODO: 这个方法仅会重新计算边缘顶点的法线
            // TODO: 
            int triangleCount = triangles.Length / 3;
            for (int i = 0; i < triangleCount; i++) {
                int normalTriangleIndex = i * 3;
                int vertexIndexA = triangles[normalTriangleIndex];
                int vertexIndexB = triangles[normalTriangleIndex + 1];
                int vertexIndexC = triangles[normalTriangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
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

                Vector3 triangleNormal = SurfaceNormalFromIndices_Fixed(vertexIndexA, vertexIndexB, vertexIndexC);
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

        private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
            // 算三个点构成的三角型的叉乘
            Vector3 pointA = (indexA < 0) ? outofMeshVertexs[-indexA - 1] : vertexs[indexA];
            Vector3 pointB = (indexB < 0) ? outofMeshVertexs[-indexB - 1] : vertexs[indexB];
            Vector3 pointC = (indexC < 0) ? outofMeshVertexs[-indexC - 1] : vertexs[indexC];

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

        #endregion


        #region mesh data get/set

        public void GetEdgeVertInfo(ref List<int> edgeVertIdxs, ref List<Vector3> edgeRawNormals) {
            edgeVertIdxs = new List<int>();
            edgeRawNormals = new List<Vector3>();

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


        public Mesh GetMesh(int tileIdxX, int tileIdxY, int fixDirection) {
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

        // GPT 提供：湿度、温度、海拔混合公式
        private Color CalculateTerrainColor(float temperature, float humidity, float height) {
            float mexHeight = 40;
            float snowLine = 40;

            Color baseColor = Color.green;
            baseColor.r += Mathf.Clamp(temperature - 20, 0, 10) * 0.05f;    // 温度影响
            baseColor.g += humidity * 0.1f;                                 // 湿度影响
            baseColor *= 1.0f - (height / mexHeight);                       // 海拔影响
            if (height > snowLine) {
                baseColor = Color.Lerp(baseColor, Color.white, (height - snowLine) / (mexHeight - snowLine)); // 高山雪地
            }
            return baseColor;
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
