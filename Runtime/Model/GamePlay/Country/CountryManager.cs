using LZ.WarGameMap.Runtime.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    using StrToBoundDict = Dictionary<string, CountryBoundData>;
    public class CountryManager : IDisposable
    {
        CountrySO countrySO;

        // Each layer's country neighbor relation bitmap, true means 2 countrys are neighbor
        List<NativeArray<bool>> CountryNeighborMaps = new List<NativeArray<bool>>();

        // Each country's boundary data
        List<CountryBoundData> CountryBoundDatas = new List<CountryBoundData>();

        Dictionary<int, StrToBoundDict> LayerCountryBdDicts = new Dictionary<int, StrToBoundDict>();


        bool isInit = false;
        public bool IsInit { get { return isInit; } }

        public void InitCountryManager(CountrySO countrySO)
        {
            if (!countrySO.isUpdated)
            {
                Debug.LogError($"Setting country manager using an unvalid CountrySO");
                return;
            }
            this.countrySO = countrySO;
            int mapWidth = countrySO.mapWidth;
            int mapHeight = countrySO.mapHeight;
            int gridNum = countrySO.GridNum;

            // Init grid country index, record every layer's grids country index
            List<NativeList<uint>> GridLayerIdxs = new List<NativeList<uint>>(BaseCountryDatas.MaxLayerNum + 1);
            for(int i = 0;  i < GridLayerIdxs.Capacity; i++)
            {
                GridLayerIdxs.Add(new NativeList<uint>(gridNum, Allocator.Persistent));
            }
            foreach (var gridIndice in countrySO.GridCountryIndiceList)
            {
                GridLayerIdxs[0].Add(gridIndice.x);
                GridLayerIdxs[1].Add(gridIndice.y);
                GridLayerIdxs[2].Add(gridIndice.z);
                GridLayerIdxs[3].Add(gridIndice.w);
            }

            // Init country bitmap
            int totalBoundGridNum = 0;
            CountryNeighborMaps = new List<NativeArray<bool>>(BaseCountryDatas.MaxLayerNum + 1);
            Debug.Log($"Start init country neighbor bitmap : {BaseCountryDatas.MaxLayerNum + 1}, gridNum : {gridNum}, map width : {mapWidth}, map height : {mapHeight}");
            foreach (var layerCountryData in countrySO.LayerCountryDataList)
            {
                int curLayer = layerCountryData.LayerLevel;
                if (curLayer == BaseCountryDatas.RootLayerIndex) 
                { 
                    continue; 
                }
                NativeList<uint> curLayerGridIdxs = GridLayerIdxs[curLayer];

                // Country bitmap : neighbor relation
                int bitMapSize = layerCountryData.Count * layerCountryData.Count;
                NativeArray<bool> countryNeighborMap = new NativeArray<bool>(bitMapSize, Allocator.Persistent);
                CountryNeighborMaps.Add(countryNeighborMap);

                // TODO : ȫ������ native list
                // Gen country's boundary
                //List<GridBoundType> GridBoundTypes = new List<GridBoundType>(gridNum);
                NativeArray<GridBoundType> GridBoundTypes = new NativeArray<GridBoundType>(gridNum, Allocator.Persistent);
                
                FindCountryBoundJob findCountryBoundJob = new FindCountryBoundJob
                {
                    mapWidth = mapWidth,
                    mapHeight = mapHeight,
                    countryNum = layerCountryData.Count,
                    GridCountryIdxData = curLayerGridIdxs,
                    GridBoundTypes = GridBoundTypes,
                    CountryNeighborMap = countryNeighborMap,
                };
                JobHandle jobHandle = findCountryBoundJob.Schedule(gridNum, 32);
                jobHandle.Complete();

                // TODO : ������ ���� CountryBoundData �����������Ҫ��̬����
                // Construct country boundary data
                StrToBoundDict nameBoundDict = new StrToBoundDict();
                LayerCountryBdDicts.Add(curLayer, nameBoundDict);

                CountryData cacheData = null;
                CountryBoundData cacheBoundData = null;
                for (int i = 0; i < gridNum; i++)
                {
                    uint countryIdx = curLayerGridIdxs[i];
                    // If country index is not valid, continue
                    if(countryIdx == BaseCountryDatas.NotValidCountryIndex)
                    {
                        continue;
                    }

                    if(cacheData == null || cacheData.IndexInLayer != countryIdx)
                    {
                        cacheData = countrySO.GetCountryDataByIndex(curLayer, (int)countryIdx);
                        if (!nameBoundDict.ContainsKey(cacheData.CountryName))
                        {
                            cacheBoundData = new CountryBoundData(curLayer, cacheData.CountryName);
                            nameBoundDict.Add(cacheData.CountryName, cacheBoundData);
                            CountryBoundDatas.Add(cacheBoundData);
                        }
                        else
                        {
                            cacheBoundData = nameBoundDict[cacheData.CountryName];
                        }
                    }
                    
                    // Add boundary grid
                    GridBoundType boundType = GridBoundTypes[i];
                    if(boundType == GridBoundType.IsBoundary)
                    {
                        totalBoundGridNum++;
                        Vector2Int idx = new Vector2Int(i % mapWidth, i / mapWidth);    // ��ο� CountrySO - SetGridCountry() ����������
                        cacheBoundData.AddBoundGrid(idx);
                    }
                }
                Debug.Log($"Layer : {curLayer}, has bound grid num : {totalBoundGridNum}");
                totalBoundGridNum = 0;
                GridBoundTypes.Dispose();
            }

            // TODO : Ϊʲôֻ�� 4 �� boundNum����
            int boundNum = 0;
            foreach (var dictPair in LayerCountryBdDicts)
            {
                foreach (var nameBoundDict in dictPair.Value)
                {
                    nameBoundDict.Value.UpdateBound();
                    boundNum++;
                }
            }

            // Dispose all temp data
            foreach (var layerIdxs in GridLayerIdxs)
            {
                layerIdxs.Dispose();
            }

            Debug.Log($"Init country manager over! grid : {gridNum}, layer : {CountryNeighborMaps.Count}, bound num : {boundNum}, bound grid : {totalBoundGridNum}");
            isInit = true;
        }


        #region set country color bound

        public void UpdateCountryColor()
        {
            if (!isInit)
            {
                Debug.LogError("Country Manager not inited!");
                return;
            }

            int changedCountryColorNum = 0;
            foreach (var layerCountryData in countrySO.LayerCountryDataList)
            {
                int curLayer = layerCountryData.LayerLevel;
                if (curLayer == BaseCountryDatas.RootLayerIndex)
                {
                    continue;
                }
                changedCountryColorNum += UpdateCountryColor(curLayer);
            }
            Debug.Log($"update country color over, changed colors : {changedCountryColorNum}");
        }

        private int UpdateCountryColor(int layer)
        {
            NativeArray<bool> CountryNeighborMap = CountryNeighborMaps[layer];
            int layerCountryCount = (int)Mathf.Sqrt(CountryNeighborMap.Length);

            // Build a neighbor map by bit map
            List<List<uint>> neighborMap = new List<List<uint>>(layerCountryCount);
            for(int i = 0; i < layerCountryCount; i++)
            {
                List<uint> curNeighbor = new List<uint>(layerCountryCount / 4);
                for (int j = 0; j < layerCountryCount; j++)
                {
                    if (IsNeighbor(CountryNeighborMap, i, j, layerCountryCount))
                    {
                        curNeighbor.Add((uint)j);
                    }
                }
                neighborMap.Add(curNeighbor);
            }

            // Get country's color (this layer)x
            Color[] CountryColors = new Color[layerCountryCount];
            List<CountryData> countryDatas = new List<CountryData>(layerCountryCount);
            for(int i = 0; i < layerCountryCount; i++)
            {
                CountryData countryData = countrySO.GetCountryDataByIndex(layer, i);
                countryDatas.Add(countryData);
                CountryColors[i] = countryData.CountryColor;
            }

            // Greedy Algorithm : Graph Coloring, let neighbor's color won't be the same
            int changedColorNum = 0;
            HashSet<Color> neighborColors = new HashSet<Color>(layerCountryCount);
            for (int i = 0; i < layerCountryCount; i++)
            {
                neighborColors.Clear();
                bool shouldChangeCurColor = false;
                Color curColor = CountryColors[i];
                List<uint> curNeighbors = neighborMap[i];
                for (int j = 0; j < curNeighbors.Count; j++)
                {
                    Color neighborColor = CountryColors[(int)curNeighbors[j]];
                    neighborColors.Add(neighborColor);
                    if(neighborColor == curColor)
                    {
                        shouldChangeCurColor = true;
                    }
                }

                if (shouldChangeCurColor)
                {
                    CountryColors[i] = MapColorUtil.GetDifferentColor(neighborColors);
                    changedColorNum++;
                }
            }

            // Set CountryData's color
            for (int i = 0; i < layerCountryCount; i++)
            {
                countryDatas[i].SetCountryColor(CountryColors[i]);
            }
            return changedColorNum;
        }

        private bool IsNeighbor(NativeArray<bool> CountryNeighborMap, int curIndex, int neighborIndex, int layerCountryCount)
        {
            int index = curIndex * layerCountryCount + neighborIndex;
            return CountryNeighborMap[index];
        }

        #endregion

        #region

        #endregion

        // TODO : �����˱߽����ݺ��ǲ��ǿ��Ե���һ�ű߽�����ͼ��������shader�������򻮷ֵ�Ч����
        public void ExportBoundTexture()
        {

        }

        public void Dispose()
        {
            foreach (var map in CountryNeighborMaps)
            {
                map.Dispose();
            }
            CountryNeighborMaps.Clear();

            foreach (var country in CountryNeighborMaps)
            {

            }
        }

    }
}
