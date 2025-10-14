using LZ.WarGameMap.Runtime;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    // NOTE :
    // 这个类存在的意义是：在每次重新编译脚本后，将每个BaseManager的IsInit重新设置为false
    // 这样调用BaseManger的GetInstance时，可以再自动执行一次初始化逻辑
    [InitializeOnLoad]
    public class BaseManager_Editor
    {
        static BaseManager_Editor()
        {
            ResetBaseManagers();
            Debug.Log("Reset all manager's statu");
        }

        public static void ResetBaseManagers()
        {
            TaskManager.GetInstance().ResetManager();
            CoroutineManager.GetInstance().ResetManager();
        }
    }
}
