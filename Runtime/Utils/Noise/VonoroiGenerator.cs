using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class VonoroiNoise : IDisposable {
        // TODO  :  直接搬运 catlike的代码！！！！

        public int Resolution;
        public int RandomSeed;
        public int PointCount;
        public int FBMIteration;

        public VonoroiNoise(int resolution, int randomSeed, int pointCount, int fBMIteration = 1) {
            Resolution = resolution;
            RandomSeed = randomSeed;
            PointCount = pointCount;
            FBMIteration = fBMIteration;
        }

        public void Dispose() {

        }

        public Vector4 SampleNoise(Vector3 position) {
            int x = (int)position.x;
            int y = (int)position.z;
            //int id = y * Resolution + x;
            Vector2 uv = new Vector2((float)x / Resolution, (float)y / Resolution);

            float value = 0f;
            float strength = 1f;
            float totalStrength = 0f;
            int iterations = Mathf.Max(1, FBMIteration);

            for (int i = 0; i < iterations; i++) {
                float scale = Mathf.Pow(2, i);
                Vector2 scaledUV = uv * scale;
                Vector2[] fPoints = GeneratePoints(PointCount, RandomSeed + i * 997); // 用不同种子保证变化

                float v = GetVoronoiValue(scaledUV, fPoints);
                value += v * strength;

                totalStrength += strength;
                strength /= 2f;
            }

            value /= totalStrength;
            value = Mathf.Clamp01(value); // 归一化
            //colors[id] = new Color(value, value, value, 1f);
            return new Vector4(value, value, value, 1f);
        }

        //public Color[] SampleNoise() {
        //    int total = Resolution * Resolution;
        //    Color[] colors = new Color[total];
        //    Vector2[] points = GeneratePoints(PointCount, RandomSeed);

        //    for (int y = 0; y < Resolution; y++) {
        //        for (int x = 0; x < Resolution; x++) {
        //            int id = y * Resolution + x;
        //            Vector2 uv = new Vector2((float)x / Resolution, (float)y / Resolution);

        //            float value = 0f;
        //            float strength = 1f;
        //            float totalStrength = 0f;
        //            int iterations = Mathf.Max(1, FBMIteration);

        //            for (int i = 0; i < iterations; i++) {
        //                float scale = Mathf.Pow(2, i);
        //                Vector2 scaledUV = uv * scale;
        //                Vector2[] fPoints = GeneratePoints(PointCount, RandomSeed + i * 997); // 用不同种子保证变化

        //                float v = GetVoronoiValue(scaledUV, fPoints);
        //                value += v * strength;

        //                totalStrength += strength;
        //                strength /= 2f;
        //            }

        //            value /= totalStrength;
        //            value = Mathf.Clamp01(value); // 归一化
        //            colors[id] = new Color(value, value, value, 1f);
        //        }
        //    }

        //    return colors;
        //}


        private Vector2[] GeneratePoints(int count, int seed) {
            UnityEngine.Random.InitState(seed);
            Vector2[] points = new Vector2[count];
            for (int i = 0; i < count; i++) {
                points[i] = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
            }
            return points;
        }

        private float GetVoronoiValue(Vector2 uv, Vector2[] points) {
            float minDist = float.MaxValue;
            foreach (var point in points) {
                float dist = Vector2.Distance(uv, point);
                if (dist < minDist)
                    minDist = dist;
            }
            return minDist;
        }

    }
}
