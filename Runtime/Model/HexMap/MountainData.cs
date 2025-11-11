using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using static LZ.WarGameMap.Runtime.FastNoiseLite;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    public class MountainData
    {

        [HorizontalGroup("MountainData"), LabelText("ID"), ReadOnly]
        public int MountainID;

        [HorizontalGroup("MountainData"), LabelText("名称")]
        public string MountainName;

        public List<Vector2Int> MountainGridList = new List<Vector2Int>();

        public MountainNoiseData MountainNoiseData = new MountainNoiseData();

        // Only for editing statu
        public HashSet<Vector2Int> MountainGridSets = new HashSet<Vector2Int>();
        
        // Call it in GridTerrainSO
        public MountainData(int moutainID)
        {
            MountainID = moutainID;
        }

        public bool IsMountainValid()
        {
            return MountainName != null;    //  && mountainGridList.Count > 0
        }

        // Call it before edit mountain
        public void UpdateMountainData()
        {
            if(MountainGridSets == null)
            {
                MountainGridSets = new HashSet<Vector2Int>();
            }
            MountainGridSets.Clear();
            foreach (var grid in MountainGridList)
            {
                MountainGridSets.Add(grid);
            }
        }

        public void AddMountainGrid(List<Vector2Int> newGrids)
        {
            foreach (var grid in newGrids)
            {
                if (!MountainGridSets.Contains(grid))
                {
                    MountainGridSets.Add(grid);
                }
            }
        }

        public void RemoveMountainGrid(List<Vector2Int> newGrids)
        {
            foreach (var grid in newGrids)
            {
                if (MountainGridSets.Contains(grid))
                {
                    MountainGridSets.Remove(grid);
                }
            }
        }

        // Only call save, grid edit res will be saved in data
        public void SaveMountainGrid()
        {
            MountainGridList.Clear();
            foreach (var grid in MountainGridSets)
            {
                MountainGridList.Add(grid);
            }
        }
        
    }

    [Serializable]
    // 请参考 : NoiseTerrainSample
    public class MountainNoiseData
    {
        [Header("山脉 设置")]
        public float baseHeight = 1.0f;
        public float heightFix = 10;

        [Header("山脉 采样 设置")]
        public float elevation = 1.0f;
        public int interuptInstence = 20;

        [Header("Noise General Setting")]
        public int randomSeed = 1227;
        public NoiseType noiseType = NoiseType.Perlin;
        public float frequency = 0.010f;

        [Header("Noise Fractal Setting")]
        public FractalType fractalType = FractalType.FBm;
        public int octaves = 3;
        public float lacunarity = 2.0f;
        public float gain = 0.5f;
        public float weightedStrength = 0;
        public float pingpongStrength = 0;

        [Header("Sample Noise General Setting")]
        public int interuptRandomSeed = 196;
        public NoiseType interuptNoiseType = NoiseType.Perlin;
        public float interuptFrequency = 0.030f;

        public FastNoiseLite GetNoiseDataLite()
        {
            FastNoiseLite fastNoiseLite = new FastNoiseLite(randomSeed);
            fastNoiseLite.SetNoiseType(noiseType);
            fastNoiseLite.SetFrequency(frequency);

            fastNoiseLite.SetFractalType(fractalType);
            fastNoiseLite.SetFractalOctaves(octaves);
            fastNoiseLite.SetFractalLacunarity(lacunarity);
            fastNoiseLite.SetFractalGain(gain);
            fastNoiseLite.SetFractalWeightedStrength(weightedStrength);
            fastNoiseLite.SetFractalPingPongStrength(pingpongStrength);
            return fastNoiseLite;
        }

        public FastNoiseLite GetSampleNoiseData()
        {
            FastNoiseLite interuptNoiseLite = new FastNoiseLite(interuptRandomSeed);
            interuptNoiseLite.SetNoiseType(interuptNoiseType);
            interuptNoiseLite.SetFrequency(interuptFrequency);
            return interuptNoiseLite;
        }
    }

}
