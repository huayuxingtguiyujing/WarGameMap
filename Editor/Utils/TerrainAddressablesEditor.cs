using System.IO;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    internal static class TerrainAddressablesEditor
    {
        public static void AddTerrainMeshToAB(string terrainMeshPath)
        {
            if (!Directory.Exists(terrainMeshPath))
            {
                Debug.LogError($"目录不存在: {terrainMeshPath}");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var group = settings.FindGroup(MapStoreEnum.TerrainMeshAssetGroupName);
            if (group == null)
                group = settings.CreateGroup(MapStoreEnum.TerrainMeshAssetGroupName, false, false, true, null);

            foreach (string filePath in Directory.GetFiles(terrainMeshPath, TerrainSettingSO.GetClusterFileSuffixName()))
            {
                string fileName = Path.GetFileName(filePath);
                if (!TerrainSettingSO.TryParseClusterFileName(fileName, out long longitude, out long latitude, out int lodLevel))
                    throw new InvalidDataException("Invalid cluster LOD filename: " + fileName);

                string label = TerrainSettingSO.GetClusterFileName(longitude, latitude, lodLevel);
                string guid = AssetDatabase.AssetPathToGUID(filePath.Replace('\\', '/'));
                var entry = settings.CreateOrMoveEntry(guid, group);
                entry.SetAddress(label);
                entry.SetLabel(label, true, true);
            }

            EditorUtility.SetDirty(settings);
        }
    }
}
