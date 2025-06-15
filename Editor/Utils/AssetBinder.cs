using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor {
    // TODO : 看看能不能写一个资产绑定器！！！

    [Serializable]
    public class AssetPathBinder {

        public UnityEngine.Object asset; // 拖入的资产
        public string assetPath; // 自动绑定的路径

#if UNITY_EDITOR
        private void OnValidate() {
            if (asset != null) {
                string path = AssetDatabase.GetAssetPath(asset);
                if (assetPath != path) {
                    assetPath = path;
                    //EditorUtility.SetDirty(this);
                }
            }
        }
#endif

    }
}
