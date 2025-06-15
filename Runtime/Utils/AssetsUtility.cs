
using LZ.WarGameCommon;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Path = System.IO.Path;

namespace LZ.WarGameMap.Runtime
{
    public class AssetsUtility : Singleton<AssetsUtility>
    {
        public AssetsUtility() { }

        public string CombinedPath(string filePath, string fileName) {
            if (!Directory.Exists(filePath)) {
                Directory.CreateDirectory(filePath);
            }
            string fullPath = Path.Combine(filePath, fileName);
            fullPath = fullPath.Replace("\\", "/");
            return fullPath;
        }

        public static string TransToAssetPath(string fullFilePath) {
            return "Assets" + fullFilePath.Substring(Application.dataPath.Length);
        }

        public static string AssetToFullPath(string assetilePath) {
            string tmp = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            return tmp + "/" + assetilePath;
        }

        public static string FixFilePath(string filePath) {
            return filePath.Replace("\\", "/");
        }

        public string GetFolderFromPath(string filePath) {
            string trimmedPath = filePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string lastFolderName = Path.GetFileName(trimmedPath);
            return lastFolderName;
        }


        #region get file infos from folder

        public HashSet<string> GetFileNames(string folderPath) {
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            HashSet<string> terrainFileNames = new HashSet<string>();
            foreach (var guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                terrainFileNames.Add(fileName);
            }
            return terrainFileNames;
        }

        #endregion


        #region save load asset
        public void SaveAssets<T>(string path, string assetName, T asset) where T : UnityEngine.Object {
            if (assetName == null) {
                return;
            }

            string objPath = path + assetName;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<T>(objPath);
            if (existingAsset != null) {
                EditorUtility.SetDirty(asset);
            } else {
                AssetDatabase.CreateAsset(asset, objPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public T LoadAssets<T>(string path, string assetName) where T : UnityEngine.Object {
            if (assetName == null) {
                return null;
            }

            string objPath = path + assetName;
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir)) {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(objPath);
        }

        #endregion
    }
}
