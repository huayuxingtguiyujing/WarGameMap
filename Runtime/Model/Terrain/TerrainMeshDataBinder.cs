using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace LZ.WarGameMap.Runtime
{
    [Serializable]
    public class TerrainMeshDataBinder : ScriptableObject
    {

        [Serializable]
        public class MeshAssetBinder {
            public UnityEngine.Object meshAsset;
            public string assetPath;

            public MeshAssetBinder(UnityEngine.Object meshAsset, string assetPath) {
                this.meshAsset = meshAsset;
                this.assetPath = assetPath;
            }
        }

        // serialized file data
        // 当前使用的 Terrain Mesh 数据
        public List<MeshAssetBinder> MeshBinderList;

        public void LoadAsset(string[] filePaths) {
            MeshBinderList = new List<MeshAssetBinder>();

            foreach (var filePath in filePaths)
            {
                // maybe it will be useful
                string meshFileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                string fixedFilePath = AssetsUtility.FixFilePath(filePath);
                UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fixedFilePath);
                MeshBinderList.Add(new MeshAssetBinder(asset, filePath));
            }
        }

#if UNITY_EDITOR
        //private void OnValidate() {   // too costlt no need
        //    foreach (var binder in MeshBinderList)
        //    {
        //        if (binder.meshAsset != null) {
        //            string path = AssetDatabase.GetAssetPath(binder.meshAsset);
        //            if (binder.assetPath != path) {
        //                binder.assetPath = path;
        //            }
        //        }
        //    }
        //}
#endif

    }
}
