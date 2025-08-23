using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class BaseManager : MonoBehaviour
    {
        public bool IsInit = false;     //

        public static ChildManager GetInstance<ChildManager>() where ChildManager : BaseManager
        {
            BaseManager manager;
            string managerName = typeof(ChildManager).Name;
            GameObject go = GameObject.Find(managerName);
            if (go == null)
            {
                go = new GameObject(managerName);
                //go.hideFlags = HideFlags.HideAndDontSave; // do not set hide
            }
            go.transform.parent = null;

            manager = go.GetComponent<ChildManager>();
            if (manager == null)
            {
                manager = go.AddComponent<ChildManager>();
            }
            Debug.Log($"get manager : {managerName}, {manager != null}");
            return (ChildManager)manager;
        }

        public virtual void InitManager() { IsInit = true; }

    }
}
