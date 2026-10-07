using System.Threading.Tasks;

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
