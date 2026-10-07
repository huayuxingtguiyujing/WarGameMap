using LZ.WarGameCommon;
using System.Threading.Tasks;
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
