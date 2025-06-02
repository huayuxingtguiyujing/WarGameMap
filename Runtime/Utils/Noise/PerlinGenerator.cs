using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static UnityEditor.U2D.ScriptablePacker;
using static Unity.Mathematics.math;
using System;
using UnityEngine.UIElements;

namespace LZ.WarGameMap.Runtime
{

    // Unity 自带的 PerlinNoise 不是无缝的，请使用  PerlinNoise
    struct InitNoiseJob : IJobParallelFor {
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float scale;
        [ReadOnly] public float repeat;

        [WriteOnly] public NativeArray<float> noiseArray;

        public void Execute(int index) {
            int i = index / width;
            int j = index % height;
            float x = i * scale;
            float y = j * scale;
            noiseArray[index] = SeamlessPerlin.Generate(x, y, repeat);
            //noiseArray[index] = Mathf.PerlinNoise(x, y);
        }
    }

    struct FillPerlinTexJob : IJobParallelFor {
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public NativeArray<float> noiseArray;

        [WriteOnly] public NativeArray<Color> colorArray;
        
        public void Execute(int index) {
            colorArray[index] = Color.Lerp(new Color(0.8f, 0.8f, 0.8f), Color.white, noiseArray[index]);
        }
    }

    // 这个似乎不是无缝的，请使用  PerlinNoise
    public class PerlinGenerator {
        private int texWidth;
        private int texHeight;

        public Texture2D mapTex {  get; private set; }

        public const float noiseScale = 0.01f;

        public PerlinGenerator() { }

        public void GeneratePerlinNoise(int width, int height, float scale = 0.01f, float repeat = 512f) {
            this.texWidth = width;
            this.texHeight = height;

            if (scale <= 0) {
                scale = 0.0001f;
            }

            NativeArray<float> noiseArray = new NativeArray<float>(width * height, Allocator.TempJob);
            InitNoiseJob initNoiseJob = new InitNoiseJob {
                width = texWidth,
                height = texHeight,
                scale = scale,
                repeat = repeat,
                noiseArray = noiseArray,
            };
            JobHandle jobHandle1 = initNoiseJob.Schedule(width * height, 64);
            jobHandle1.Complete();

            NativeArray<Color> colorArray = new NativeArray<Color>(width * height, Allocator.TempJob);
            FillPerlinTexJob fillJob = new FillPerlinTexJob {
                width = texWidth,
                height = texHeight,
                noiseArray = noiseArray,
                colorArray = colorArray
            };
            JobHandle jobHandle2 = fillJob.Schedule(width * height, 64);
            jobHandle2.Complete();

            mapTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            mapTex.SetPixels(colorArray.ToArray());
            mapTex.Apply();

            noiseArray.Dispose();
            colorArray.Dispose();

            Debug.Log("Successfully generated seamless Perlin noise texture.");
        }

        public Vector4 SampleNoise(Vector3 position) {
            if (mapTex == null) {
                Debug.LogError("Noise map texture not generated.");
                return Vector4.zero;
            }
            float u = (position.x % texWidth) / texWidth;
            float v = (position.z % texHeight) / texHeight;
            Color color = mapTex.GetPixelBilinear(u, v);
            return new Vector4(color.r, color.g, color.b, color.a);
        }
    }

    public static class SeamlessPerlin {

        private static readonly int[] permutation = {
            151,160,137,91,90,15,
            131,13,201,95,96,53,194,233,7,225,
            140,36,103,30,69,142,8,99,37,240,
            21,10,23,190,6,148,247,120,234,75,
            0,26,197,62,94,252,219,203,117,35,
            11,32,57,177,33,88,237,149,56,87,
            174,20,125,136,171,168,68,175,74,165,
            71,134,139,48,27,166,77,146,158,231,
            83,111,229,122,60,211,133,230,220,105,
            92,41,55,46,245,40,244,102,143,54,
            65,25,63,161,1,216,80,73,209,76,
            132,187,208,89,18,169,200,196,135,130,
            116,188,159,86,164,100,109,198,173,186,
            3,64,52,217,226,250,124,123,5,202,
            38,147,118,126,255,82,85,212,207,206,
            59,227,47,16,58,17,182,189,28,42,
            223,183,170,213,119,248,152,2,44,154,
            163,70,221,153,101,155,167,43,172,9,
            129,22,39,253,19,98,108,110,79,113,
            224,232,178,185,112,104,218,246,97,228,
            251,34,242,193,238,210,144,12,191,179,
            162,241,81,51,145,235,249,14,239,107,
            49,192,214,31,181,199,106,157,184,84,
            204,176,115,121,50,45,127,4,150,254,
            138,236,205,93,222,114,67,29,24,72,
            243,141,128,195,78,66,215,61,156,180
        };

        private static readonly int[] p;

        static SeamlessPerlin() {
            p = new int[512];
            for (int i = 0; i < 256; i++) {
                p[i] = permutation[i];
                p[256 + i] = permutation[i];
            }
        }

        public static float Generate(float x, float y, float repeat = 256f) {
            // Adjust coordinates to ensure seamless tiling
            x = x % repeat;
            y = y % repeat;

            int xi = (int)x & 255;
            int yi = (int)y & 255;

            float xf = x - (int)x;
            float yf = y - (int)y;

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = p[p[xi] + yi];
            int ab = p[p[xi] + yi + 1];
            int ba = p[p[xi + 1] + yi];
            int bb = p[p[xi + 1] + yi + 1];

            float x1, x2;
            x1 = Mathf.Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            x2 = Mathf.Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return (Mathf.Lerp(x1, x2, v) + 1f) / 2f; // Normalize to [0,1]
        }

        private static float Fade(float t) {
            // 6t^5 - 15t^4 + 10t^3
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private static float Grad(int hash, float x, float y) {
            int h = hash & 7; // Convert low 3 bits of hash code
            float u = h < 4 ? x : y;
            float v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
   
    }


    public class PerlinNoise : IDisposable {

        struct Pixel {
            public int2 coord;

            public float2 lerp;

            public float2 cornerVector0;
            public float2 cornerVector1;
            public float2 cornerVector2;
            public float2 cornerVector3;

            public float2 constantVector0;
            public float2 constantVector1;
            public float2 constantVector2;
            public float2 constantVector3;
        };

        private int Resolution;
        private float Frequency;

        private bool IsTilable;
        private float RandomSeed;
        private float2 Evolution;
        private int FBMIteration;

        private NativeArray<float4> colors;
        private NativeArray<float2> noiseConstVector;

        public PerlinNoise(int resolution, float frequency, bool isTilable, float randomSeed, Vector2 evolution, int fBMIteration = 0) {
            Resolution = resolution;
            Frequency = frequency;
            IsTilable = isTilable;
            RandomSeed = randomSeed;
            Evolution = float2(evolution.x, evolution.y);
            FBMIteration = fBMIteration;
            colors = new NativeArray<float4>(Resolution * Resolution, Allocator.Persistent);
            noiseConstVector = new NativeArray<float2>();
        }

        public void Dispose() {
            colors.Dispose();
            noiseConstVector.Dispose();
        }

        public float SampleNoise(Vector3 position) {
            int2 id = int2((int)position.x, (int)position.z);
            int colorIndex = id.x;
            colorIndex += Resolution * id.y;

            if (colorIndex >= Resolution * Resolution) {
                return 0;
            }

            float noise = GetNoiseValue(id, Frequency, RandomSeed);

            if (FBMIteration > 1) {
                noise /= FBMIteration;
            }
            //noise /= 1.5f;

            // 分形噪声
            float currentTile = Frequency;
            float currentStrength = 1;
            for (int iii = 0; iii < FBMIteration; iii++) {
                currentTile *= 2;
                currentStrength /= 2;
                if (currentTile >= Resolution) {
                    break;
                }
                noise += GetNoiseValue(id, currentTile, RandomSeed + currentTile) * currentStrength;
                //noise /= 1.5f;
            }

            colors[colorIndex] = float4(noise, noise, noise, 1);
            return noise;
            //return new Vector4(noise, noise, noise, 1);
        }

        private float GetNoiseValue(int2 id, float tile, float randomSeed) {
            int blockNumber = (int)ceil(tile);

            int blockSize = (int)ceil((float)Resolution / blockNumber);

            Pixel pixel = new Pixel();
            pixel.coord = int2((int)id.x, (int)id.y);

            int2 blockCoord = PixelCoordToBlockCoord(blockSize, pixel.coord);
            int2 blockMin = GetBlockMin(blockSize, blockCoord);
            int2 blockMax = GetBlockMax(blockSize, blockCoord);

            pixel.lerp.x = (pixel.coord.x - blockMin.x) / (blockSize - 1.0f);
            pixel.lerp.y = (pixel.coord.y - blockMin.y) / (blockSize - 1.0f);

            // m k d
            pixel.cornerVector0 = pixel.coord - float2(blockMin.x, blockMin.y);
            pixel.cornerVector1 = pixel.coord - float2(blockMin.x, blockMax.y);
            pixel.cornerVector2 = pixel.coord - float2(blockMax.x, blockMax.y);
            pixel.cornerVector3 = pixel.coord - float2(blockMax.x, blockMin.y);

            pixel.cornerVector0 /= blockSize;
            pixel.cornerVector1 /= blockSize;
            pixel.cornerVector2 /= blockSize;
            pixel.cornerVector3 /= blockSize;

            // 
            pixel.constantVector0 = GetConstantVector(blockNumber, blockCoord + int2(0, 0), randomSeed);
            pixel.constantVector1 = GetConstantVector(blockNumber, blockCoord + int2(0, 1), randomSeed);
            pixel.constantVector2 = GetConstantVector(blockNumber, blockCoord + int2(1, 1), randomSeed);
            pixel.constantVector3 = GetConstantVector(blockNumber, blockCoord + int2(1, 0), randomSeed);


            float dot0 = dot(pixel.cornerVector0, pixel.constantVector0);
            float dot1 = dot(pixel.cornerVector1, pixel.constantVector1);
            float dot2 = dot(pixel.cornerVector2, pixel.constantVector2);
            float dot3 = dot(pixel.cornerVector3, pixel.constantVector3);

            float dotA = PerlinNoiseLerp(dot0, dot3, pixel.lerp.x);
            float dotB = PerlinNoiseLerp(dot1, dot2, pixel.lerp.x);
            float dotC = PerlinNoiseLerp(dotA, dotB, pixel.lerp.y);

            float noise = 0;

            noise = dotC;
            noise = (noise + 1.0f) / 2.0f;

            return noise;
        }

        private float PerlinNoiseLerp(float l, float r, float t) {
            t = ((6 * t - 15) * t + 10) * t * t * t;
            return lerp(l, r, t);
        }

        private float2 GetConstantVector(int blockNumber, int2 blockCoord, float randomSeed) {
            if (IsTilable) {
                // 如果是无缝的贴图，则循环
                if (blockCoord.x == blockNumber) {
                    blockCoord.x = 0;
                }

                if (blockCoord.y == blockNumber) {
                    blockCoord.y = 0;
                }
            }

            //float2 vec = GetRandom2To2_Raw(blockCoord + Evolution, length(blockCoord) * randomSeed);//GetRandom2To2_Tileable
            float2 vec = GetRandom2To2_Raw_Hash(blockCoord + Evolution, length(blockCoord) * randomSeed);//GetRandom2To2_Tileable
            //float2 vec = GetRandom2To2_Tileable(blockCoord + Evolution, length(blockCoord) * randomSeed);//
            vec = normalize(vec);
            return vec;
        }


        #region perlin math

        private int2 GetBlockMin(int blockSize, int2 blockCoord) {
            return blockCoord * blockSize;
        }

        private int2 GetBlockMax(int blockSize, int2 blockCoord) {
            return blockCoord * blockSize + blockSize;
        }

        private int2 PixelCoordToBlockCoord(int blockSize, int2 pixelCoord) {
            return (int2)floor((float2)pixelCoord / blockSize);
        }

        // 生成随机向量
        private static readonly Vector2[] Gradients = new Vector2[]
        {
            new Vector2( 1, 0), new Vector2(-1, 0),
            new Vector2( 0, 1), new Vector2( 0,-1),
            new Vector2( 1, 1).normalized, new Vector2(-1, 1).normalized,
            new Vector2( 1,-1).normalized, new Vector2(-1,-1).normalized
        };

        private int TileableHash(int x, int y, int seed, int tileSize) {
            x = x % tileSize; if (x < 0) x += tileSize;
            y = y % tileSize; if (y < 0) y += tileSize;
            int h = x * 374761393 + y * 668265263 + seed * 144665517;
            h = (h ^ (h >> 13)) * 1274126177;
            return h & 7;
        }

        public float2 GetRandom2To2_Tileable(Vector2 param, float randomSeed, int tileSize = 64) {
            int x = Mathf.FloorToInt(param.x * 1000f);
            int y = Mathf.FloorToInt(param.y * 1000f);
            int seed = Mathf.FloorToInt(randomSeed * 1000f);
            int index = TileableHash(x, y, seed, tileSize);
            return float2(Gradients[index].x, Gradients[index].y);
        }

        private float2 GetRandom2To2_Raw(float2 param, float randomSeed) {
            // 生成一个确定性的伪随机 float2 向量
            float len = length(param);
            float2 value;
            value.x = len + 58.12f + 79.52f * randomSeed;
            value.y = len + 96.53f + 36.95f * randomSeed;
            value = sin(value);
            value = fmod(value, 1f);  // 保持与 % 1 的语义一致
            return value;
        }

        private float2 GetRandom2To2_Raw_Hash(float2 param, float randomSeed) {
            // 使用简单 hash 方法来生成伪随机 gradient 向量
            int x = Mathf.FloorToInt(param.x * 1000f);
            int y = Mathf.FloorToInt(param.y * 1000f);
            int seed = Mathf.FloorToInt(randomSeed * 1000f);

            int index = RawHash(x, y, seed) & 7; // 取 0~7 作为 Gradient 索引
            return float2(Gradients[index].x, Gradients[index].y);
        }

        private int RawHash(int x, int y, int seed) {
            unchecked {
                int h = x * 374761393 + y * 668265263 + seed * 144665517;
                h = (h ^ (h >> 13)) * 1274126177;
                return h;
            }
        }

        private float2 GetRandom2To2_Remapped(float2 param, float randomSeed) {
            float2 value = GetRandom2To2_Raw(param, randomSeed);
            return (value + 1f) * 0.5f;
        }


        #endregion
    }


}
