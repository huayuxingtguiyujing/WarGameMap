
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public enum GridBoundType
    {
        NotValid = 0,       // Sea or river or other grid type that can not be country
        IsBoundary = 1, 
        NoBoundary = 2, 
    }

    public struct FindCountryBoundJob : IJobParallelFor
    {
        [ReadOnly] public int mapWidth;
        [ReadOnly] public int mapHeight;
        [ReadOnly] public int countryNum;
        [ReadOnly] public NativeList<uint> GridCountryIdxData;            // Grid's country data, a index  

        [NativeDisableParallelForRestriction][WriteOnly] 
        public NativeArray<GridBoundType> GridBoundTypes;      // Is grid bound grid? 
        [NativeDisableParallelForRestriction][WriteOnly] 
        public NativeArray<bool> CountryNeighborMap;           // Record neighbor relation (If true

        public void Execute(int index)
        {
            int x = index / mapWidth;
            int y = index % mapWidth;
            Vector2Int pos = new Vector2Int(x, y);
            Vector2Int[] neighbour = HexHelper.GetOffsetHexNeighbour(pos);

            bool isBoundFlag = false;
            uint curIdx = GridCountryIdxData[index];
            foreach (var neighbor in neighbour)
            {
                int idx = (pos.x + neighbor.x) * mapWidth + (pos.y + neighbor.y);
                if (!CheckValidIdx(idx))
                {
                    continue;
                }
                // If neighbor gris's country idx not = to cur grid idx, the grid is boundary grid
                uint neighborIdx = GridCountryIdxData[idx];
                if (neighborIdx != curIdx)
                {
                    isBoundFlag = true;
                    SetNeighborRelation(curIdx, neighborIdx);
                }
            }

            if (isBoundFlag)
            {
                GridBoundTypes[index] = GridBoundType.IsBoundary;
            }
            else
            {
                GridBoundTypes[index] = GridBoundType.NoBoundary;
            }
        }

        private bool CheckValidIdx(int idx)
        {
            return idx >= 0 && idx < GridCountryIdxData.Length;
        }

        private void SetNeighborRelation(uint curIdx, uint neighborIdx)
        {
            uint offset = (uint)(curIdx * countryNum);
            int index = (int)(offset + neighborIdx);
            if (index >= CountryNeighborMap.Length)
            {
                return;
            }
            CountryNeighborMap[index] = true;
        }

    }

    public class CountryBoundData
    {
        // Foreign Keys - CountryData
        public int Layer { get; private set; }
        public string CountryName { get; private set; }


        public List<Vector2Int> boundGrids = new List<Vector2Int>(8);
        public Vector2 boundCenter { get; private set; }

        public CountryBoundData(int layer, string countryName)
        {
            Layer = layer;
            CountryName = countryName;
        }

        public void AddBoundGrid(Vector2Int idx)
        {
            boundGrids.Add(idx);
        }

        public void UpdateBound()
        {
            // Caculate bound center
            boundCenter = boundGrids.Average();
        }

    }
}
