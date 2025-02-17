using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static UnityEditor.U2D.ScriptablePacker;

namespace LZ.WarGameMap.Runtime
{

    struct InitNoiseJob : IJobParallelFor {
        [ReadOnly] public int width;
        [ReadOnly] public int height;
        [ReadOnly] public float scale;

        [WriteOnly] public NativeArray<float> noiseArray;

        public void Execute(int index) {
            int i = index / width;
            int j = index % height;
            float x = i * scale;
            float y = j * scale;
            noiseArray[index] = Mathf.PerlinNoise(x, y);
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

    public class PerlinGenerator {

        private int texWidth;
        private int texHeight;

        public Texture2D mapTex {  get; private set; }

        public const float noiseScale = 0.01f;

        public PerlinGenerator() { }


        public void GeneratePerlinNoise(int width, int height, float scale = 0.1f) {
            this.texWidth = width;
            this.texHeight = height;

            if (scale <= 0) {
                scale = 0.0001f;
            }
            scale = scale * Mathf.PI / 3;

            NativeArray<float> noiseArray = new NativeArray<float>(width * height, Allocator.TempJob);
            InitNoiseJob initNoiseJob = new InitNoiseJob {
                width = texWidth,
                height = texHeight,
                scale = scale,
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

            Debug.Log("successfully generate perlin map Tex");
        }


        public Vector4 SampleNosie(Vector3 position) {
            if (mapTex == null) {
                Debug.LogError("noise map texture Î´Éú³É");
                return Vector4.zero;
            }
            return mapTex.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
        }
    }
}
