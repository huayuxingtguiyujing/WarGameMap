using LZ.WarGameMap.Runtime;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using static LZ.WarGameMap.Runtime.FastNoiseLite;
using Debug = UnityEngine.Debug;

namespace NewAssembly
{
    public enum SDFPixelType
    {
        Edge, Inner, Outter, NotValid
    }

    public enum SDFLerpType
    {
        Linear, Power2, SmoothStep, Sqrt
    }

    public enum SDFBatchType
    {
        Outline, InnerContour, TransToCenter, NotValid
    }

    // A sample for SDF Texture generating
    // Using Jumping Flooding Algorithm, use job to accelerate process
    // Recommand compute shader rather than cpu ways
    public class SDFGenSample : MonoBehaviour
    {
        [Header("Show Setting")]
        [SerializeField] int meshWidth = 512;
        [SerializeField] int meshHeight = 512;
        [SerializeField] Material SDFShowMaterial;

        [Header("SDF setting")]
        [SerializeField] bool genSDFTexture         = true;
        [SerializeField] bool noOutterSDF           = true;
        [SerializeField] SDFLerpType sdfLerpType    = SDFLerpType.SmoothStep;
        [SerializeField] float SDFMaxDistance       = 20;                   // If distance is more than it, set bg color
        [SerializeField] Color sdfLerpColor         = Color.blue;

        [SerializeField] float outlineDistance = 5;
        [SerializeField] Color outlineColor         = Color.gray;
        [SerializeField] float innerContourDistance = 15;
        [SerializeField] Color innerContourColor    = Color.black;          // inner contour
        [SerializeField] Color backgroundColor      = Color.white;
        [SerializeField] Color edgeColor = Color.black;

        [Header("Noise General Setting")]
        [SerializeField, Range(0, 100)] float noiseInfluence = 30;
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

        [Header("SDF resource")]
        [SerializeField] Texture2D OriginTexture;
        [SerializeField] Texture2D EdgeTexture;
        [SerializeField] Texture2D SDFTexture;

        int width;
        int height;

        Color[] originColors;
        Vector4[] pixelDatas;

        static Vector2Int NoKeyPointVal = new Vector2Int(-1, -1);
        static Vector2Int[] NeighborIdx = new Vector2Int[8] 
        {
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), 
            new Vector2Int(-1, 0),                        new Vector2Int(1, 0), 
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
        };

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        Mesh mesh;

        FastNoiseLite SampleNoise;

        public void CreateSDFTexture()
        {
            if (OriginTexture == null)
            {
                Debug.LogError("you have no Origin Texture, can not gen SDF");
                return;
            }
            InitJFAInfo();
            ExecuteJFA();
            if (genSDFTexture)
            {
                GenSampleNoise();
                GenEdgeTexture();
                GenSDFTexture();
            }
        }

        #region Jumping Flooding Algorithm, Gen SDF

        private void InitJFAInfo()
        {
            int edgeNum = 0, innerNum = 0, outterNum = 0;
            // if point is edge point   : (x, y, x, y)
            // if point is inner point  : (x, y, -1, -1)
            // if point is outter point : (-1, -1, x, y)
            width = OriginTexture.width;
            height = OriginTexture.height;
            pixelDatas = new Vector4[width * height];
            originColors = OriginTexture.GetPixels();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    SDFPixelType pixelType = GetPixleType(i, j);
                    int index = i * width + j;
                    switch (pixelType)
                    {
                        case SDFPixelType.Edge:
                            pixelDatas[index] = new Vector4(i, j, i, j); edgeNum++;
                            break;
                        case SDFPixelType.Inner:
                            pixelDatas[index] = new Vector4(i, j, -1, -1);  innerNum++;
                            break;
                        case SDFPixelType.Outter:
                            pixelDatas[index] = new Vector4(-1, -1, i, j);  outterNum++;
                            break;
                        case SDFPixelType.NotValid:
                            Debug.LogError($"find a not valid pixel : ({i}, {j})");
                            pixelDatas[index] = new Vector4(-1, -1, -1, -1);
                            break;
                    }
                }
            }
            Debug.Log($"init JFA info over, edge num : {edgeNum}, inner num : {innerNum}, outter num : {outterNum}");
        }

        private bool IsBackgroundColor(Color color)
        {
            return Mathf.Abs(color.r - backgroundColor.r) < 0.01f &&
                   Mathf.Abs(color.g - backgroundColor.g) < 0.01f &&
                   Mathf.Abs(color.b - backgroundColor.b) < 0.01f;
        }

        private SDFPixelType GetPixleType(int i, int j)
        {
            if (!CheckIndexValid(i, j))
            {
                return SDFPixelType.NotValid;
            }
            int index = i * width + j;
            if (originColors[index] == backgroundColor)
            {
                return SDFPixelType.Outter;
            }
            else
            {
                foreach (var neighbor in NeighborIdx)
                {
                    int neighborX = i + neighbor.x;
                    int neighborY = j + neighbor.y;
                    if (!CheckIndexValid(neighborX, neighborY))
                    {
                        continue;
                    }
                    int neighborIndex = neighborX * width + neighborY;
                    if (originColors[neighborIndex] == backgroundColor)
                    {
                        return SDFPixelType.Edge;
                    }
                }
                return SDFPixelType.Inner;
            }
        }

        private bool CheckIndexValid(int i, int j)
        {
            return (i >= 0 && i < width) && (j >= 0 && j < height);
        }

        struct JFAIterJob : IJobParallelFor
        {
            [ReadOnly] public int step;
            [ReadOnly] public int width;
            [ReadOnly] public int height;
            
            [NativeDisableParallelForRestriction] 
            public NativeArray<Vector4> OutputJFAPixelDatas;

            public void Execute(int index)
            {
                int i = index / width;
                int j = index % height;
                Vector2Int curIdx = new Vector2Int(i, j);

                foreach (var neighbor in SDFGenSample.NeighborIdx)
                {
                    Vector2Int sampleTarget = curIdx + neighbor * step;
                    if (!CheckIndexValid(sampleTarget)) continue;
                    SampleKeyPoint(curIdx, sampleTarget);
                }
            }

            private void SampleKeyPoint(Vector2Int curIdx, Vector2Int sampleIdx)
            {
                //int curIndex = curIdx.x * width + curIdx.y;
                //int sampleIndex = sampleIdx.x * width + sampleIdx.y;
                SDFPixelType curPixelType = GetPixleType(curIdx, out Vector2Int curKeyPoint);
                SDFPixelType samplePixelType = GetPixleType(sampleIdx, out Vector2Int sampleKeyPoint);
                if (curKeyPoint == SDFGenSample.NoKeyPointVal)
                {
                    UpdateKeyPoint(curPixelType, curIdx, sampleKeyPoint);
                }
                else if (sampleKeyPoint == SDFGenSample.NoKeyPointVal)
                {
                    // Nothing happen
                }
                else if (Vector2.Distance(curIdx, sampleKeyPoint) < Vector2.Distance(curIdx, curKeyPoint))
                {
                    UpdateKeyPoint(curPixelType, curIdx, sampleKeyPoint);
                }
            }

            private SDFPixelType GetPixleType(Vector2Int idx, out Vector2Int keyPointVal)
            {
                int index = idx.x * width + idx.y;
                Vector4 pixelData = OutputJFAPixelDatas[index];
                Vector2Int prefix = new Vector2Int((int)pixelData.x, (int)pixelData.y);
                Vector2Int suffix = new Vector2Int((int)pixelData.z, (int)pixelData.w);
                if (prefix == suffix)
                {
                    keyPointVal = prefix;
                    return SDFPixelType.Edge;
                }
                else if (prefix == idx)
                {
                    keyPointVal = suffix;
                    return SDFPixelType.Inner;
                }
                else if (suffix == idx)
                {
                    keyPointVal = prefix;
                    return SDFPixelType.Outter;
                }
                else
                {
                    throw new System.Exception($"find point pixel ({idx.x}, {idx.y}) data not valid, pixel data : {pixelData}");
                    //return SDFPixelType.NotValid;
                }
            }

            private void UpdateKeyPoint(SDFPixelType pixelType, Vector2Int idx, Vector2Int newKeyPointVal)
            {
                int index = idx.x * width + idx.y;
                switch (pixelType)
                {
                    // if point is edge point   : (x, y, x, y)
                    // if point is inner point  : (x, y, -1, -1)
                    // if point is outter point : (-1, -1, x, y)
                    case SDFPixelType.Inner:
                        OutputJFAPixelDatas[index] = new Vector4(idx.x, idx.y, newKeyPointVal.x, newKeyPointVal.y);
                        break;
                    case SDFPixelType.Outter:
                        OutputJFAPixelDatas[index] = new Vector4(newKeyPointVal.x, newKeyPointVal.y, idx.x, idx.y);
                        break;
                }
            }

            private bool CheckIndexValid(Vector2Int target)
            {
                return (target.x >= 0 && target.x < width) && (target.y >= 0 && target.y < height);
            }

        }

        private void ExecuteJFA()
        {
            int initStep = Mathf.Max(width / 2, height / 2);
            int step = initStep;
            Stopwatch sw = Stopwatch.StartNew();
            NativeArray<Vector4> OutputJFAPixelDatas = new NativeArray<Vector4>(pixelDatas, Allocator.Persistent);
            while (step >= 1)
            {
                JFAIterJob jFAIterJob = new JFAIterJob()
                {
                    step = step,
                    width = width,
                    height = height,
                    OutputJFAPixelDatas = OutputJFAPixelDatas,
                };
                JobHandle jFAHandle = jFAIterJob.Schedule(width * height, 64);
                jFAHandle.Complete();

                step /= 2;
            }
            OutputJFAPixelDatas.CopyTo(pixelDatas);
            OutputJFAPixelDatas.Dispose();
            sw.Stop();
            Debug.Log($"JFA iter over, init step : {initStep}, cost time : {sw.ElapsedMilliseconds}ms");
        }

        private void GenSampleNoise()
        {
            SampleNoise = new FastNoiseLite(randomSeed);
            SampleNoise.SetNoiseType(noiseType);
            SampleNoise.SetFrequency(frequency);

            SampleNoise.SetFractalType(fractalType);
            SampleNoise.SetFractalOctaves(octaves);
            SampleNoise.SetFractalLacunarity(lacunarity);
            SampleNoise.SetFractalGain(gain);
            SampleNoise.SetFractalWeightedStrength(weightedStrength);
            SampleNoise.SetFractalPingPongStrength(pingpongStrength);
        }

        private void GenEdgeTexture()
        {
            Stopwatch sw = Stopwatch.StartNew();
            EdgeTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            Color[] colors = EdgeTexture.GetPixels();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int index = i * width + j;
                    Vector2Int idx = new Vector2Int(i, j);
                    SDFPixelType pixelType = GetPixleType(idx, out Vector2Int keyPointVal);
                    if (pixelType == SDFPixelType.Edge)
                    {
                        colors[index] = edgeColor;
                    }
                    else
                    {
                        colors[index] = backgroundColor;
                    }
                }
            }
            EdgeTexture.SetPixels(colors);
            EdgeTexture.Apply();
            sw.Stop();  // sw.ElapsedMilliseconds

            Debug.Log($"gen Edge texture over, cost {sw.ElapsedMilliseconds}ms");
        }

        private void GenSDFTexture()
        {
            Stopwatch sw = Stopwatch.StartNew();
            //CheckAllSpreaded();

            SDFTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            Color[] colors = SDFTexture.GetPixels();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int index = i * width + j;
                    colors[index] = GetSDFColor(i, j);
                }
            }
            SDFTexture.SetPixels(colors);
            SDFTexture.Apply();
            sw.Stop();

            Debug.Log($"gen SDF texture over, cost {sw.ElapsedMilliseconds}ms");
        }

        private void CheckAllSpreaded()
        {
            int NoKeyPointNum = 0;
            for (int index = 0; index < width * height; index++)
            {
                int i = index / width;
                int j = index % width;
                Vector2Int curIdx = new Vector2Int(i, j);
                GetPixleType(curIdx, out Vector2Int keyPointVal);
                if (keyPointVal == NoKeyPointVal)
                {
                    NoKeyPointNum++;
                }
            }
            if (NoKeyPointNum > 0)
            {
                Debug.Log($"found No keypoint num : {NoKeyPointNum}");
            }
            else
            {
                Debug.Log("check over, No key point not found");
            }
        }

        private Color GetSDFColor(int i, int j)
        {
            Vector2Int idx = new Vector2Int(i, j);
            SDFPixelType pixelType = GetPixleType(idx, out Vector2Int keyPointVal);
            if (keyPointVal == NoKeyPointVal)
            {
                return backgroundColor;
            }
            
            // Get the distance to key point
            float distanceToKeyPoint = Vector2.Distance(idx, keyPointVal);
            SDFBatchType batchType = GetBatchType(pixelType, distanceToKeyPoint);
            return GetLerpColor(i, j, batchType, distanceToKeyPoint);
        }

        private SDFPixelType GetPixleType(Vector2Int idx, out Vector2Int keyPointVal)
        {
            int index = idx.x * width + idx.y;
            Vector4 pixelData = pixelDatas[index];
            Vector2Int prefix = new Vector2Int((int)pixelData.x, (int)pixelData.y);
            Vector2Int suffix = new Vector2Int((int)pixelData.z, (int)pixelData.w);
            if (prefix == suffix)
            {
                keyPointVal = prefix;
                return SDFPixelType.Edge;
            }
            else if (prefix == idx)
            {
                keyPointVal = suffix;
                return SDFPixelType.Inner;
            }
            else if (suffix == idx)
            {
                keyPointVal = prefix;
                return SDFPixelType.Outter;
            }
            else
            {
                throw new System.Exception($"find point pixel ({idx.x}, {idx.y}) data not valid, pixel data : {pixelData}");
            }
        }

        private SDFBatchType GetBatchType(SDFPixelType pixelType, float distanceToKeyPoint)
        {
            if (pixelType == SDFPixelType.Outter)
            {
                if (distanceToKeyPoint <= outlineDistance)
                {
                    return SDFBatchType.Outline;
                }
                else
                {
                    return SDFBatchType.NotValid;
                }
            }
            else
            {
                if (distanceToKeyPoint <= innerContourDistance)
                {
                    return SDFBatchType.InnerContour;
                }
                else if (distanceToKeyPoint <= SDFMaxDistance)
                {
                    return SDFBatchType.TransToCenter;
                }
                else
                {
                    return SDFBatchType.NotValid;
                }
            }
        }

        private Color GetLerpColor(int i, int j, SDFBatchType batchType, float distanceToKeyPoint)
        {
            // Init lerp data
            float lerpTargetDistance;
            Color keyPointColor, bgColor;
            switch (batchType)
            {
                case SDFBatchType.Outline:
                    lerpTargetDistance = outlineDistance;
                    keyPointColor = outlineColor;
                    bgColor = backgroundColor;
                    break;
                case SDFBatchType.InnerContour:
                    lerpTargetDistance = innerContourDistance;
                    keyPointColor = innerContourColor;
                    bgColor = sdfLerpColor;
                    break;
                case SDFBatchType.TransToCenter:
                    lerpTargetDistance = SDFMaxDistance;
                    keyPointColor = sdfLerpColor;
                    bgColor = backgroundColor;
                    break;
                //case SDFBatchType.NotValid:
                default:
                    lerpTargetDistance = SDFMaxDistance;
                    keyPointColor = backgroundColor;
                    bgColor = backgroundColor;
                    break;
            }

            // Get ratio to lerp target
            if (batchType == SDFBatchType.TransToCenter)
            {
                distanceToKeyPoint = InfluenceByNoise(i, j);
            }
            float ratio = Mathf.Clamp01(Mathf.InverseLerp(0, lerpTargetDistance, distanceToKeyPoint));
            Color finalColor = backgroundColor;
            switch (sdfLerpType)
            {
                case SDFLerpType.Linear:
                    finalColor = Color.Lerp(keyPointColor, bgColor, ratio);
                    break;
                case SDFLerpType.Power2:
                    ratio = ratio * ratio;
                    finalColor = Color.Lerp(keyPointColor, bgColor, ratio);
                    break;
                case SDFLerpType.SmoothStep:
                    ratio = ratio * ratio * (3f - 2f * ratio);
                    finalColor = Color.Lerp(keyPointColor, bgColor, ratio);
                    break;
                case SDFLerpType.Sqrt:
                    ratio = Mathf.Sqrt(ratio);
                    finalColor = Color.Lerp(keyPointColor, bgColor, ratio);
                    break;
            }
            return finalColor;
        }

        private float InfluenceByNoise(int i, int j)
        {
            float noise = SampleNoise.GetNoise(i, j) * noiseInfluence;
            i = (int)(i + noise) % width;
            j = (int)(j + noise) % height;
            i = Mathf.Clamp(i, 0, width);
            j = Mathf.Clamp(j, 0, height);

            // Return distance to new sample point
            Vector2Int idx = new Vector2Int(i, j);
            GetPixleType(idx, out Vector2Int keyPointVal);
            return Vector2.Distance(idx, keyPointVal);          
        }

        #endregion

        public void ClearSDFResource()
        {
#if UNITY_EDITOR
            if(EdgeTexture != null)
            {
                GameObject.DestroyImmediate(EdgeTexture);
            }
            if (SDFTexture != null)
            {
                GameObject.DestroyImmediate(SDFTexture);
            }
#else
            if(EdgeTexture != null)
            {
                GameObject.Destroy(EdgeTexture);
            }
            if(SDFTexture != null)
            {
                GameObject.Destroy(SDFTexture);
            }
#endif
            EdgeTexture = null;
            SDFTexture = null;
        }

        public void ShowSDFTexture()
        {
            ShowTexture(SDFTexture);
        }

        public void ShowEdgeTexture()
        {
            ShowTexture(EdgeTexture);
        }

        private void ShowTexture(Texture2D targetTexture)
        {
            if(targetTexture == null)
            {
                Debug.LogError("you have not gen targetTexture!");
                return;
            }
            InitShowComponent();

            Texture2D temp = new Texture2D(targetTexture.width, targetTexture.height, targetTexture.format, targetTexture.mipmapCount > 1);
            Graphics.CopyTexture(targetTexture, temp);
            RenderTexture rtData = GetRTTexture(temp, width, height);

            meshRenderer.sharedMaterial = SDFShowMaterial;
            var mpb = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetTexture("_MainTex", rtData);
            meshRenderer.SetPropertyBlock(mpb);

            SceneView.RepaintAll();
        }

        private void InitShowComponent()
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

            if (mesh != null)
            {
                GameObject.DestroyImmediate(mesh);
            }

            // Create a plane mesh to show texture
            mesh = new Mesh();
            Vector3[] vertexs = new Vector3[]
            {
                new Vector3 (0, 0, 0),                 new Vector3 (meshWidth, 0, 0),
                new Vector3(meshWidth, 0, meshHeight), new Vector3 (0, 0, meshHeight)
            };
            int[] indices = new int[]
            {
                0, 2, 1, 0, 3, 2
            };
            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            };
            mesh.SetVertices(vertexs);
            mesh.SetTriangles(indices, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

        private RenderTexture GetRTTexture(Texture2D originTexture, int width, int height)
        {
            RenderTexture targetRenderTexture = new RenderTexture(
                originTexture.width,
                originTexture.height,
                0, RenderTextureFormat.ARGB32);
            targetRenderTexture.Create();

            RenderTexture.active = targetRenderTexture;
            Graphics.Blit(originTexture, targetRenderTexture);
            RenderTexture.active = null;

            DestroyImmediate(originTexture);

            return targetRenderTexture;
        }

    }

}
