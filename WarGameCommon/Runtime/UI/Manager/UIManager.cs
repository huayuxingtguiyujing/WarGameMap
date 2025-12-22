

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LZ.WarGameCommon {


    /// <summary>
    /// UI 框架的管理器
    /// 
    /// NOTE: 如何使用？
    /// 1.将下面的 UIManager 放到场景的一个 root 对象
    /// 2.将你要加载的 ui 面板逻辑类继承另一个文件夹下的 UIWidget / UIPanel，前者适合于小组件，后者用于一整个面板
    /// 3.把 ui panel 放到指定的 prefab 目录之下 （通过 InitUIManager(string panelPath) 来设置这个路径）
    /// 4.直接调用下面的 LoadPanel 等方法加载 ui 即可
    /// 
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        
        public static UIManager Instance;

        AssetBundle UIPanels;

        private void Awake() {
            Instance = this;
        }

        private void OnDestroy() {
            //卸载所有加载的AB包，如果参数为True，则同时将AB包加载的资源一并卸载
            AssetBundle.UnloadAllAssetBundles(false);
        }


        private Dictionary<string, UIWidget> UIDict = new ();

        private string basePanelPath;

        #region 外部接口

        public void InitUIManager(string panelPath) {
            basePanelPath = panelPath;
            UIPanels = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/uipanel");
        }

        public bool IsPanelLoaded(string panelName) {
            if (UIDict.ContainsKey(panelName)) {
                return UIDict[panelName];
            }
            return false;
        }

        public UIWidget LoadPanel(string panelName) {
            GameObject rootCanvas = GameObject.FindWithTag("RootCanvas");
            return LoadPanel(panelName, rootCanvas);
        }

        public UIWidget LoadPanel(string panelName, GameObject rootCanvas) {
            // 如果已经加载 则放到顶部
            if (UIDict.ContainsKey(panelName)) {
                UIDict[panelName].OnResume();
                return UIDict[panelName];
            }

            //string tail = ".prefab";
            //string panelPath = basePanelPath + panelName + tail;

            // 未加载 则加载之
            GameObject uiObj = LoadUIPrefab(panelName);
            //GameObject uiObj = await LoadUIPrefabAsync(panelPath);
            if (uiObj != null) {
                uiObj.transform.parent = rootCanvas.transform;
                UIWidget widget = uiObj.GetComponent<UIWidget>();
                widget.OnEnter();
                UIDict.Add(panelName, widget);
                Debug.Log("加载成功: " + panelName);
                return widget;
            }
            Debug.LogError("加载失败: " + panelName);
            return null;
        }

        public void UnloadPanel(string panelName) {
            // 这里不会执行panel的销毁 需要在外部进行回收
            if (UIDict.ContainsKey(panelName)) {
                UIWidget panel = UIDict[panelName];
                panel.OnExit();
                UIDict.Remove(panelName);
            }
        }

        public void ClearAllPanel() {
            // 此处会强行销毁 panel 的 object
            foreach (var keyValue in UIDict)
            {
                UIDict[keyValue.Key].OnExit();
                GameObject.Destroy(keyValue.Value);
            }
            UIDict.Clear();
        }


        #endregion

        private GameObject LoadUIPrefab(string panelName) {
            var panelPrefab = UIPanels.LoadAsset(panelName);
            GameObject obj = Instantiate(panelPrefab) as GameObject;
            obj.transform.localScale = Vector3.one;
            return obj;
        }

        private async Task<GameObject> LoadUIPrefabAsync(string prefabPath) {
            // 使用addressable  Assets/Prefab/UI/Panel/ArmyDetail.prefab
            GameObject prefabObj = await Addressables.LoadAssetAsync<GameObject>(prefabPath).Task;
            return prefabObj;
        }



    }
}
