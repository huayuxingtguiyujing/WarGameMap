using LZ.WarGameMap.Runtime;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    // NOTE :
    // �������ڵ������ǣ���ÿ�����±���ű��󣬽�ÿ��BaseManager��IsInit��������Ϊfalse
    // ��������BaseManger��GetInstanceʱ���������Զ�ִ��һ�γ�ʼ���߼�
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
