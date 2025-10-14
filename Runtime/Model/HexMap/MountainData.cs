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

        [HorizontalGroup("MountainData"), LabelText("Ãû³Æ")]
        public string MountainName;

        List<Vector2Int> mountainGridList = new List<Vector2Int>();
        public List<Vector2Int> MountainGridList => mountainGridList;

        MountainNoiseData mountainNoiseData = new MountainNoiseData();
        public MountainNoiseData MountainNoiseData => mountainNoiseData;

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
            foreach (var grid in mountainGridList)
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
        public void SaveMountainGrid(string MountainName)
        {
            mountainGridList.Clear();
            foreach (var grid in MountainGridSets)
            {
                mountainGridList.Add(grid);
            }
        }
        
    }

    [Serializable]
    // Çë²Î¿¼ : NoiseTerrainSample
    public class MountainNoiseData
    {
        [Header("Mountain Setting")]
        public float heightFix = 10;
        public int interuptRandomSeed = 196;
        public float elevation = 1.0f;
        public int interuptInstence = 2;

        [Header("Noise General Setting")]
        public NoiseType noiseType = NoiseType.Perlin;
        public int randomSeed = 1227;
        public float frequency = 0.010f;

        [Header("Noise Fractal Setting")]
        public FractalType fractalType = FractalType.FBm;
        public int octaves = 3;
        public float lacunarity = 2.0f;
        public float gain = 0.5f;
        public float weightedStrength = 0;
        public float pingpongStrength = 0;

    }

}
