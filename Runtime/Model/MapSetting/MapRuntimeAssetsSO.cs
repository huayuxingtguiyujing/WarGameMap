using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LZ.WarGameMap.Runtime
{
    public enum MapRuntimeAssetRole
    {
        TerrainSetting, RuntimeSetting, HexSetting, GridTerrain, Country,
        LandformMaterial, AlbedoArray, NormalArray, RegionTexture, RegionSDFShader
    }

    [Serializable]
    public class MapRuntimeAssetEntry
    {
        public MapRuntimeAssetRole role;
        public AssetReference asset;
    }

    [Serializable]
    public class MapRuntimeClusterEntry
    {
        public int idxX, idxY, longitude, latitude;
        public int lodLevel;
        public AssetReference asset;
    }

    // Edited through MapSetEditor's ordinary object fields. No Editor dependencies.
    public class MapRuntimeAssetsSO : ScriptableObject
    {
        public List<MapRuntimeAssetEntry> assets = new List<MapRuntimeAssetEntry>();
        public List<AssetReference> heightDataModels = new List<AssetReference>();
        public string terrainDirectory = Enums.MapStoreEnum.TerrainMeshAssetPath;
        // Regenerated only by the explicit Addressables update button.
        public List<MapRuntimeClusterEntry> clusters = new List<MapRuntimeClusterEntry>();
    }
}
