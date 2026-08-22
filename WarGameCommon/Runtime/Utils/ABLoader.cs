using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
#endif

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LZ.WarGameCommon
{
    public class ABLoader : Singleton<ABLoader>
    {
        public ABLoader() { }

        // TODO : 计划在跑通 runtime 游戏加载流程时，回来验证 runtime ab 包功能
        // 
        // 使用示例:
        // ABLoader.GetInstance().InitABLoader();          // 调用这个来加载 ab 包资产
        // ABLoader.GetInstance().LoadTextAssetAsync();    // 调用这个来加载地块，使用地块名称即可
        // 
        // 
        // Use it to load all ab resource
        public async Task InitABLoader()
        {
            await Addressables.InitializeAsync().Task;
        }


#if UNITY_EDITOR
        public void AddBinToGroup(string binPath, string label, string groupName) 
        {
            // Get Ab setting and Ab group
            var settings = AddressableAssetSettingsDefaultObject.Settings;  // 需确认
            var group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, null);
            }

            // remove old asset
            var oldEntry = settings.FindAssetEntry(label);
            if (oldEntry != null)
            {
                settings.RemoveAssetEntry(label, false);   // 或 group.RemoveAssetEntry(oldEntry)
            }
        
            // Add asset to group
            string guid = AssetDatabase.AssetPathToGUID(binPath);
            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.SetAddress(label);
            entry.SetLabel(label, true);
        }

        public void RefreshABGroup()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;  // 需确认
            EditorUtility.SetDirty(settings);
        }

#endif

        public AssetBundle LoadABFile(string path) 
        {
            return AssetBundle.LoadFromFile(path);
        }

        // return .bytes
        // You can load cluster by it
        public async Task<TextAsset> LoadTextAssetAsync(string key)
        {
            AsyncOperationHandle<TextAsset> handle = Addressables.LoadAssetAsync<TextAsset>(key);
            await handle.Task;
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"Addressables 加载失败: {key}");
                return null;
            }
            return handle.Result;
        }

    }
}
