using LZ.WarGameMap.Runtime;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Sirenix.OdinInspector.Editor;
//using Sirenix.Utilities.Editor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{


    /// <summary>
    /// 编辑器的 Root，用于初始化编辑器配置
    /// </summary>
    public class RootMapEditor : OdinMenuEditorWindow {

        


        [MenuItem("GameMap/InitMapEditor")]
        private static void InitMapEditor() {

            if (!AssetDatabase.IsValidFolder(MapStoreEnum.WarGameMapRootPath)) {
                string folderName = AssetsUtility.GetInstance().GetFolderFromPath(MapStoreEnum.WarGameMapRootPath);
                AssetDatabase.CreateFolder(MapStoreEnum.WarGameMapRootPath, folderName);
                Debug.Log(string.Format("create file folder : {0}", MapStoreEnum.MapWindowPath));
            }

            string terrainPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.TerrainClass;
            string buildingPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.BuildingsClass;
            string decoratePath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.DecorateClass;
            string gameplayPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.GamePlayClass;
            if (!AssetDatabase.IsValidFolder(terrainPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.TerrainClass);
            }
            if (!AssetDatabase.IsValidFolder(buildingPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.BuildingsClass);
            }
            if (!AssetDatabase.IsValidFolder(decoratePath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.DecorateClass);
            }
            if (!AssetDatabase.IsValidFolder(gameplayPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.GamePlayClass);
            }

            // get HashSet(window objs name) in folders
            HashSet<string> terrainFileNames = GetFileNames(terrainPath);
            HashSet<string> buildingFileNames = GetFileNames(buildingPath);
            HashSet<string> decorateFileNames = GetFileNames(decoratePath);
            HashSet<string> gameplayFileNames = GetFileNames(gameplayPath);

            // create every terrain windows 
            if (!terrainFileNames.Contains(MapEditorEnum.TerrainEditor)) {
                CreateWindowObj<TerrainEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.LandformEditor)) {
                CreateWindowObj<LandformEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.HeightMapEditor)) {
                CreateWindowObj<HeightMapEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.HexMapEditor)) {
                CreateWindowObj<HexmapEditor>(MapEditorClass.TerrainClass);
            }

            // TODO : river edit

            if (!decorateFileNames.Contains(MapEditorEnum.PlantEditor)) {
                CreateWindowObj<PlantCoverEditor>(MapEditorClass.DecorateClass);
            }


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("成功初始化 MapEditor, 现在可以打开编辑器!");
        }

        static HashSet<string> GetFileNames(string folderPath) {
            string[] guids = AssetDatabase.FindAssets("", new[] { folderPath });

            HashSet<string> terrainFileNames = new HashSet<string>();
            foreach (var guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                terrainFileNames.Add(fileName);
            }
            return terrainFileNames;
        }

        static void CreateWindowObj<MapEditor>(string windowClass) where MapEditor : BaseMapEditor {
            string rootWindowPath = MapStoreEnum.MapWindowPath + "/" + windowClass + "/";
            MapEditor asset = ScriptableObject.CreateInstance<MapEditor>();
            asset.name = asset.EditorName + ".asset";
            string path = rootWindowPath + asset.name;
            Debug.Log("create path: " + path);
            AssetDatabase.CreateAsset(asset, path);
        }


        [MenuItem("GameMap/OpenMapEditor")]
        private static void OpenMapEditor() {
            RootMapEditor window = GetWindow<RootMapEditor>("MapEditor");
            window.minSize = new Vector2(720, 320);
            window.Show();
        }


        GameObject rootMapEditorObj;

        private OdinMenuItem curSelected;

        protected override OdinMenuTree BuildMenuTree() {
            var tree = new OdinMenuTree();
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
            tree.AddAllAssetsAtPath(MapEditorClass.TerrainClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.TerrainClass, typeof(BaseMapEditor), true);
            tree.AddAllAssetsAtPath(MapEditorClass.BuildingsClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.BuildingsClass, typeof(BaseMapEditor), true);
            tree.AddAllAssetsAtPath(MapEditorClass.DecorateClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.DecorateClass, typeof(BaseMapEditor), true);
            tree.AddAllAssetsAtPath(MapEditorClass.GamePlayClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.GamePlayClass, typeof(BaseMapEditor), true);
            return tree;
        }

        private void Awake() {
            if(rootMapEditorObj == null) {
                rootMapEditorObj = new GameObject();
                rootMapEditorObj.name = "RootMapObj";
            }
        }

        protected override void OnImGUI() {
            base.OnImGUI();

            // 获取当前选中的菜单项
            if (this.MenuTree.Selection.Count > 0) {
                
                var selected = this.MenuTree.Selection[0];
                if (curSelected != selected) {
                    // triger the Disable and Enable
                    if (curSelected != null) {
                        BaseMapEditor lastMapEditor = curSelected.Value as BaseMapEditor;
                        lastMapEditor.Disable();
                        //Debug.Log($"{curSelected.Name} disable");
                    }
                    curSelected = selected;
                    BaseMapEditor curMapEditor = curSelected.Value as BaseMapEditor;
                    curMapEditor.Enable();
                    //Debug.Log($"{curSelected.Name} enable");
                }
            }
        }

        protected override void OnDestroy() {
            if (curSelected != null) {
                BaseMapEditor editor = curSelected.Value as BaseMapEditor;
                editor.Disable();
            }

            if (this.MenuTree.Selection.Count > 0) {
                foreach (var window in MenuTree.Selection)
                {
                    BaseMapEditor editor = window.Value as BaseMapEditor;
                    editor.Destory();
                }
            }

            if(rootMapEditorObj != null) {
                DestroyImmediate(rootMapEditorObj);
            }

            // destroy Gizmos
            GizmosCtrl.GetInstance().Dispose();

            base.OnDestroy();
        }
    }


}
