using System;
using UnityEditor;

namespace LZ.WarGameMap.MapEditor {
    // TODO : �����ܲ���дһ���ʲ�����������

    [Serializable]
    public class AssetPathBinder {

        public UnityEngine.Object asset; // ������ʲ�
        public string assetPath; // �Զ��󶨵�·��

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
