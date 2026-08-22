using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{

    // 本类仅使用于加载 Terrain Mesh Data，然后传给 TerrainConstructor
    // TerrainConstructor 利用它来获取到 Terrain
    public class TerrainLoader
    {
        public TerrainLoader()
        {
        }
#if UNITY_EDITOR
        #region Editor 将 TerrainMesh 加入AB资产

        public void AddTerrainMeshToAB(string terrainMeshPath) {

            if (!Directory.Exists(terrainMeshPath))
            {
                Debug.LogError($"目录不存在: {terrainMeshPath}");
                return;
            }

            foreach (string filePath in Directory.GetFiles(terrainMeshPath, TerrainSettingSO.GetClusterFileSuffixName()))
            {
                string fileName = Path.GetFileName(filePath);
                TerrainSettingSO.TryParseClusterFileName(fileName, out long longitude, out long latitude);
                string label = TerrainSettingSO.GetClusterFileName(longitude, latitude);
                ABLoader.GetInstance().AddBinToGroup(filePath, label, MapStoreEnum.TerrainMeshAssetGroupName);
            }

            ABLoader.GetInstance().RefreshABGroup();
        }


        #endregion
#endif

        #region Runtime 加载/卸载地块


        public async void  InitTerrainMeshAB()
        {
            await ABLoader.GetInstance().InitABLoader();
        }


        /// <summary>
        /// 读取一个 cluster 的全部 TerrainMeshData,Runtime
        /// </summary>
        public async Task LoadClusterAsync(long longitude, long latitude, TerrainCluster cluster)
        {
            byte[] bytes = await ReadClusterBytesAsync(longitude, latitude);

            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                // 读 tile 总数 + 全部 tile size 数据，方便后面用多线程同时起任务
                int tileCount = reader.ReadInt32();
                var tileSizeList = new int[tileCount];
                for (int i = 0; i < tileCount; i++)
                    tileSizeList[i] = reader.ReadInt32();

                // 一次读完整块数据
                byte[] dataBytes = reader.ReadBytes((int)(ms.Length - ms.Position));

                // 计算每个 tile 的 offset
                var offsets = new int[tileCount];
                int curOff = 0;
                for (int i = 0; i < tileCount; i++)
                {
                    offsets[i] = curOff;
                    curOff += tileSizeList[i];
                }
                
                // 并行解析每个 tile
                int index = 0;
                var tasks = new Task[tileCount];
                TDList<TerrainTile> tiles = cluster.TileList;
                foreach (var tile in tiles) {
                    int idx = index;   // 捕获循环变量
                    index++;
                    tasks[idx] = Task.Run(() =>
                    {
                        var segment = new ArraySegment<byte>(dataBytes, offsets[idx], tileSizeList[idx]);
                        ParseTile(tile, segment);
                    });
                }
                await Task.WhenAll(tasks);
            }
        }

        public async Task<byte[]> ReadClusterBytesAsync(long longitude, long latitude)
        {
            string assetName = TerrainSettingSO.GetClusterFileName(longitude, latitude);
            TextAsset ta = await ABLoader.GetInstance().LoadTextAssetAsync(assetName);
            return ta.bytes;
        }

        private void ParseTile(TerrainTile tile, ArraySegment<byte> data)
        {
            // 每个 tile 独立 reader，互不干扰
            using (var ms = new MemoryStream(data.Array, data.Offset, data.Count, writable: false))
            using (var reader = new BinaryReader(ms))
            {
                // 这里取 cluster 的第 tileIdx 个 tile 回填（注意与写入顺序一致）
                tile.ReadFromBinary(reader);

                TerrainMeshData[] meshDatas = tile.GetLODMeshes();
                int curlodLevel = 0;
                foreach (var terrainMesh in meshDatas)
                {
                    try {
                        terrainMesh.ReadFromBinary(reader);
                    } catch (System.IO.EndOfStreamException) {
                        Debug.LogError($"cluster bin 数据不足, 缺失 LOD{curlodLevel}");
                        break;
                    }
                    curlodLevel++;
                }
            }
        }
#endregion

    }
}
