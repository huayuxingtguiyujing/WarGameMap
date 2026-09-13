using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
                bool parsed = TerrainSettingSO.TryParseClusterFileName(fileName, out long longitude, out long latitude, out int lodLevel);
                if (!parsed)
                {
                    throw new InvalidDataException("Invalid cluster LOD filename: " + fileName);
                }
                string label = TerrainSettingSO.GetClusterFileName(longitude, latitude, lodLevel);
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
        public async Task<byte[]> ReadClusterBytesAsync(long longitude, long latitude, int lodLevel)
        {
            string assetName = TerrainSettingSO.GetClusterFileName(longitude, latitude, lodLevel);
            TextAsset ta = await ABLoader.GetInstance().LoadTextAssetAsync(assetName);
            return ta.bytes;
        }

#endregion

    }
}
