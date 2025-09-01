using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    // 编辑器的 Root，用于初始化编辑器配置
    public class RootMapEditor : OdinMenuEditorWindow {

        [MenuItem("GameMap/InitMapEditor")]
        private static void InitMapEditor() {

            if (!AssetDatabase.IsValidFolder(MapStoreEnum.WarGameMapRootPath)) {
                string folderName = AssetsUtility.GetInstance().GetFolderFromPath(MapStoreEnum.WarGameMapRootPath);
                AssetDatabase.CreateFolder(MapStoreEnum.WarGameMapRootPath, folderName);
                Debug.Log(string.Format("create file folder : {0}", MapStoreEnum.MapWindowPath));
            }

            string mapsetPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.MapSetClass;
            string terrainPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.TerrainClass;
            string decoratePath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.DecorateClass;
            string gameplayPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.GamePlayClass;
            string toolPath = MapStoreEnum.MapWindowPath + "/" + MapEditorClass.ToolClass;
            if (!AssetDatabase.IsValidFolder(mapsetPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.MapSetClass);
            }
            if (!AssetDatabase.IsValidFolder(terrainPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.TerrainClass);
            }
            if (!AssetDatabase.IsValidFolder(decoratePath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.DecorateClass);
            }
            if (!AssetDatabase.IsValidFolder(gameplayPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.GamePlayClass);
            }
            if (!AssetDatabase.IsValidFolder(toolPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.MapWindowPath, MapEditorClass.ToolClass);
            }

            // get HashSet(window objs name) in folders
            HashSet<string> mapsetFileNames = AssetsUtility.GetInstance().GetFileNames(mapsetPath);
            HashSet<string> terrainFileNames = AssetsUtility.GetInstance().GetFileNames(terrainPath);
            HashSet<string> decorateFileNames = AssetsUtility.GetInstance().GetFileNames(decoratePath);
            HashSet<string> gameplayFileNames = AssetsUtility.GetInstance().GetFileNames(gameplayPath);
            HashSet<string> toolFileNames = AssetsUtility.GetInstance().GetFileNames(toolPath);

            if (!mapsetFileNames.Contains(MapEditorEnum.MapSetEditor)) {
                CreateWindowObj<MapSetEditor>(MapEditorClass.MapSetClass);
            }

            // create terrain windows 
            if (!terrainFileNames.Contains(MapEditorEnum.TerrainEditor)) {
                CreateWindowObj<TerrainEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.HexMapEditor)) {
                CreateWindowObj<HexmapEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.LandformEditor)) {
                CreateWindowObj<LandformEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.HeightMapEditor)) {
                CreateWindowObj<HeightMapEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.RiverEditor)) {
                CreateWindowObj<RiverEditor>(MapEditorClass.TerrainClass);
            }
            if (!terrainFileNames.Contains(MapEditorEnum.MountainEditor)) {
                CreateWindowObj<MountainEditor>(MapEditorClass.TerrainClass);
            }

            // 装饰编辑
            if (!decorateFileNames.Contains(MapEditorEnum.PlantEditor)) 
            {
                CreateWindowObj<PlantCoverEditor>(MapEditorClass.DecorateClass);
            }
            if (!decorateFileNames.Contains(MapEditorEnum.WarFogEditor))
            {
                CreateWindowObj<WarFogEditor>(MapEditorClass.DecorateClass);
            }

            // gameplay
            if (!gameplayFileNames.Contains(MapEditorEnum.HexGridTypeEditor))
            {
                CreateWindowObj<HexGridTypeEditor>(MapEditorClass.GamePlayClass);
            }
            if (!gameplayFileNames.Contains(MapEditorEnum.CountryEditor))
            {
                CreateWindowObj<CountryEditor>(MapEditorClass.GamePlayClass);
            }
            if (!gameplayFileNames.Contains(MapEditorEnum.FactionEditor))
            {
                CreateWindowObj<FactionEditor>(MapEditorClass.GamePlayClass);
            }
            if (!gameplayFileNames.Contains(MapEditorEnum.PeopleEditor))
            {
                CreateWindowObj<PeopleEditor>(MapEditorClass.GamePlayClass);
            }
            if (!gameplayFileNames.Contains(MapEditorEnum.ResourceEditor))
            {
                CreateWindowObj<ResourceEditor>(MapEditorClass.GamePlayClass);
            }

            // common tools
            if (!toolFileNames.Contains(MapEditorEnum.TextureToolEditor)) {
                CreateWindowObj<TextureToolEditor>(MapEditorClass.ToolClass);
            }
            if (!toolFileNames.Contains(MapEditorEnum.NoiseToolEditor)) {
                CreateWindowObj<NoiseToolEditor>(MapEditorClass.ToolClass);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("成功初始化 MapEditor, 现在可以打开编辑器!");
        }

        static string GetWinAssetPath(string winClass, string winAssetName, bool addAsset = true) {
            if (addAsset) {
                return MapStoreEnum.MapWindowPath + "/" + winClass + "/" + winAssetName + ".asset";
            } else {
                return MapStoreEnum.MapWindowPath + "/" + winClass + "/" + winAssetName;
            }
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

        static void GetWindowObj<MapEditor>(string windowClass) where MapEditor : BaseMapEditor {

        }


        [MenuItem("GameMap/OpenMapEditor")]
        static void OpenMapEditor() {
            RootMapEditor window = GetWindow<RootMapEditor>("MapEditor");
            window.minSize = new Vector2(720, 320);
            window.Show();
        }


        GameObject rootMapEditorObj;

        private OdinMenuItem curSelected;

        protected override OdinMenuTree BuildMenuTree() {
            var tree = new OdinMenuTree();
            tree.DefaultMenuStyle = OdinMenuStyle.TreeViewStyle;
            tree.AddAllAssetsAtPath(MapEditorClass.MapSetClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.MapSetClass, typeof(BaseMapEditor), true);
            //tree.AddAllAssetsAtPath(MapEditorClass.TerrainClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.TerrainClass, typeof(BaseMapEditor), true);
            
            var terrainWinGroup = new OdinMenuItem(tree, MapEditorClass.TerrainClass, null);
            tree.MenuItems.Add(terrainWinGroup);
            AddWinEditorAsMenuItem(tree, terrainWinGroup, MapEditorClass.TerrainClass, MapEditorEnum.TerrainEditor);
            AddWinEditorAsMenuItem(tree, terrainWinGroup, MapEditorClass.TerrainClass, MapEditorEnum.HexMapEditor);
            AddWinEditorAsMenuItem(tree, terrainWinGroup, MapEditorClass.TerrainClass, MapEditorEnum.LandformEditor);
            AddWinEditorAsMenuItem(tree, terrainWinGroup, MapEditorClass.TerrainClass, MapEditorEnum.HeightMapEditor);
            AddWinEditorAsMenuItem(tree, terrainWinGroup, MapEditorClass.TerrainClass, MapEditorEnum.RiverEditor);
            AddWinEditorAsMenuItem(tree, terrainWinGroup, MapEditorClass.TerrainClass, MapEditorEnum.MountainEditor);

            tree.AddAllAssetsAtPath(MapEditorClass.DecorateClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.DecorateClass, typeof(BaseMapEditor), true);
            
            var gamePlayWinGroup = new OdinMenuItem(tree, MapEditorClass.GamePlayClass, null);
            tree.MenuItems.Add(gamePlayWinGroup);
            AddWinEditorAsMenuItem(tree, gamePlayWinGroup, MapEditorClass.GamePlayClass, MapEditorEnum.HexGridTypeEditor);
            AddWinEditorAsMenuItem(tree, gamePlayWinGroup, MapEditorClass.GamePlayClass, MapEditorEnum.CountryEditor);
            AddWinEditorAsMenuItem(tree, gamePlayWinGroup, MapEditorClass.GamePlayClass, MapEditorEnum.FactionEditor);
            AddWinEditorAsMenuItem(tree, gamePlayWinGroup, MapEditorClass.GamePlayClass, MapEditorEnum.PeopleEditor);
            AddWinEditorAsMenuItem(tree, gamePlayWinGroup, MapEditorClass.GamePlayClass, MapEditorEnum.ResourceEditor);

            tree.AddAllAssetsAtPath(MapEditorClass.ToolClass, MapStoreEnum.MapWindowPath + "/" + MapEditorClass.ToolClass, typeof(BaseMapEditor), true);

            return tree;
        }

        static void AddWinEditorAsMenuItem(OdinMenuTree tree, OdinMenuItem terrainWinGroup, string winClassName,  string winEditorName) {
            string winEditorMenuPath = MapStoreEnum.MapWindowPath + "/" + winClassName + "/" + winEditorName;
            string winEditorAssetPath = MapStoreEnum.MapWindowPath + "/" + winClassName + "/" + winEditorName + ".asset";
            BaseMapEditor winEditorObj = (BaseMapEditor)AssetDatabase.LoadAssetAtPath(winEditorAssetPath, typeof(BaseMapEditor));
            var terrainWin = new OdinMenuItem(tree, winEditorName, winEditorObj);
            terrainWinGroup.ChildMenuItems.Add(terrainWin);
        }

        #region behaviors


        protected override void OnEnable() {
            base.OnEnable();
            //UnityEditorManager.RegisterUpdate(EditorSceneManager.GetInstance().UpdateSceneHex);
            //UnityEditorManager.RegisterUpdate(EditorSceneManager.GetInstance().UpdateSceneTer);

            EditorSceneManager.GetInstance();
        }

        protected override void OnDisable() {
            base.OnDisable();

            GizmosCtrl.GetInstance().UnregisterGizmosAll();
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
                        if (lastMapEditor != null) {
                            lastMapEditor.Disable();
                        }
                        //Debug.Log($"{curSelected.Name} disable");
                    }

                    curSelected = selected;
                    BaseMapEditor curMapEditor = curSelected.Value as BaseMapEditor;
                    if (curMapEditor != null) {
                        curMapEditor.Enable();
                    }
                    //Debug.Log($"{curSelected.Name} enable");
                }
            }
        }

        // TODO : 怎么全局禁用

        protected override void OnDestroy() {
            // OdinMenuEditorWindow will call this function when it is closed

            if (curSelected != null) {
                BaseMapEditor editor = curSelected.Value as BaseMapEditor;
                if (editor != null) {
                    editor.Disable();
                }
            }

            if (this.MenuTree.Selection.Count > 0) {
                foreach (var window in MenuTree.Selection) {
                    BaseMapEditor editor = window.Value as BaseMapEditor;
                    editor.Destory();
                }
            }

            if (rootMapEditorObj != null) {
                DestroyImmediate(rootMapEditorObj);
            }

            // destroy Gizmos
            //GizmosCtrl.GetInstance().UnregisterGizmosAll();
            GizmosCtrl.GetInstance().Dispose();
            EditorSceneManager.Dispose();
            base.OnDestroy();
        }

        #endregion
    }
}
