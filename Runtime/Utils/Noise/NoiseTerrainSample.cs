using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static LZ.WarGameMap.Runtime.FastNoiseLite;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;
using System;

namespace LZ.WarGameMap.Runtime
{
    public class NoiseTerrainSample : MonoBehaviour
    {
        [Header("Mountain Setting")]
        [SerializeField] Texture2D moutainTexture;
        [SerializeField] Color MoutainColor;
        [SerializeField] Color PlainColor;

        [Header("Terrain Setting")]
        [SerializeField] int terrainSize = 512;
        [SerializeField] float baseHeight = 1;
        [SerializeField, Range(1, 50)] float heightFix = 10;
        [SerializeField, Range(0.01f, 10)] float elevation = 1.0f;
        [SerializeField, Range(1, 100)] int interuptInstence = 2;

        [Header("Noise General Setting")]
        [SerializeField] NoiseType noiseType = NoiseType.Perlin;
        [SerializeField] int randomSeed = 1227;
        [SerializeField] float frequency = 0.010f;

        [Header("Noise Fractal Setting")]
        [SerializeField] FractalType fractalType = FractalType.FBm;
        [SerializeField] int octaves = 3;
        [SerializeField] float lacunarity = 2.0f;
        [SerializeField] float gain = 0.5f;
        [SerializeField] float weightedStrength = 0;
        [SerializeField] float pingpongStrength = 0;

        FastNoiseLite fastNoiseLite;
        FastNoiseLite interuptNoiseLite;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        Mesh mesh;

        List<Vector3> vertex = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();

        private void Awake()
        {
            CreateTerrain();
        }

        private void OnDestroy()
        {
            ClearTerrain();
        }

        // Call this to generate a noise terrain
        [Button("Create Terrain")]
        public void CreateTerrain()
        {
            InitCfg();
            GenNoise();
            GenTerrain();
            RecaculateNormals();
            SetMesh();
            Debug.Log($"terrain gen over, size : {terrainSize}");
        }

        [Button("Clear Terrain")]
        public void ClearTerrain()
        {
            vertex.Clear();
            triangles.Clear();
            normals.Clear();
            if (mesh != null)
            {
#if UNITY_EDITOR
                GameObject.DestroyImmediate(mesh);
                SceneView.RepaintAll();
#else
                GameObject.Destroy(mesh);
#endif
            }
        }

        private void InitCfg()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
        }

        private void GenNoise()
        {
            fastNoiseLite = new FastNoiseLite(randomSeed);
            fastNoiseLite.SetNoiseType(noiseType);
            fastNoiseLite.SetFrequency(frequency);

            fastNoiseLite.SetFractalType(fractalType);
            fastNoiseLite.SetFractalOctaves(octaves);
            fastNoiseLite.SetFractalLacunarity(lacunarity);
            fastNoiseLite.SetFractalGain(gain);
            fastNoiseLite.SetFractalWeightedStrength(weightedStrength);
            fastNoiseLite.SetFractalPingPongStrength(pingpongStrength);

            interuptNoiseLite = new FastNoiseLite(501);
            interuptNoiseLite.SetNoiseType(NoiseType.Perlin);
            interuptNoiseLite.SetFrequency(0.01f);
        }

        private void GenTerrain()
        {
            mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;

            vertex = new List<Vector3>(terrainSize * terrainSize);
            normals = new List<Vector3>(terrainSize * terrainSize);
            Vector3 upNormal = new Vector3(0, 1, 0);
            for (int i = 0; i < terrainSize; i++)
            {
                for (int j = 0; j < terrainSize; j++)
                {
                    Vector2Int interuptedIdx = InterpretWithNoise(i, j);
                    Color color = moutainTexture.GetPixel(interuptedIdx.x, interuptedIdx.y);
                    float ratio = MathUtil.ColorInverseLerp(PlainColor, MoutainColor, color);

                    float noise = fastNoiseLite.GetNoise(i, j) * ratio + baseHeight;
                    noise = Mathf.Pow(noise, elevation);
                    float height = noise * heightFix;

                    vertex.Add(new Vector3(i, height, j));
                    normals.Add(upNormal);
                }
            }

            triangles = new List<int>((terrainSize - 1) * (terrainSize - 1) * 2);
            for (int i = 0; i < terrainSize - 1; i++)
            {
                for (int j = 0; j < terrainSize - 1; j++)
                {
                    int curIdx = i * terrainSize + j;
                    int rightIdx = i * terrainSize + j + 1;
                    int upIdx = (i + 1) * terrainSize + j;
                    int upRightIdx = (i + 1) * terrainSize + j + 1;

                    triangles.Add(curIdx);
                    triangles.Add(rightIdx);
                    triangles.Add(upRightIdx);
                    triangles.Add(curIdx);
                    triangles.Add(upRightIdx);
                    triangles.Add(upIdx);
                }
            }

            mesh.SetVertices(vertex);
            mesh.SetTriangles(triangles, 0);
        }

        private Vector2Int InterpretWithNoise(int i, int j)
        {
            Vector2Int pos = new Vector2Int(i, j);
            float noise = interuptNoiseLite.GetNoise(i, j);
            pos.x = Mathf.Clamp(pos.x + (int)(noise * interuptInstence), 0, terrainSize - 1);
            pos.y = Mathf.Clamp(pos.y + (int)(noise * interuptInstence), 0, terrainSize - 1);
            return pos;
        }

        private void RecaculateNormals()
        {
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int indexA = triangles[i];
                int indexB = triangles[i + 1];
                int indexC = triangles[i + 2];

                Vector3 pointA = vertex[indexA];
                Vector3 pointB = vertex[indexB];
                Vector3 pointC = vertex[indexC];

                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                Vector3 faceNormal = Vector3.Cross(sideAB, sideAC);

                normals[indexA] += faceNormal;
                normals[indexB] += faceNormal;
                normals[indexC] += faceNormal;
            }

            for (int i = 0; i < normals.Count; i++)
            {
                normals[i] = normals[i].normalized;
            }
            mesh.SetNormals(normals);
        }

        private void SetMesh()
        {
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

    }
}
