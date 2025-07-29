using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    // 需要对scene进行brush，或需要查看scene中地图信息的，继承该类
    public abstract class BrushMapEditor : BaseMapEditor {

        protected HexSettingSO hexSet;

        protected TerrainSettingSO terSet;

        protected MapRuntimeSetting mapSet;

        protected override void InitMapSetting() {
            base.InitMapSetting();
            mapSet = EditorSceneManager.mapSet;
            FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");

            terSet = EditorSceneManager.terSet;
            FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");

            hexSet = EditorSceneManager.hexSet;
            FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");
        }

        #region 地图信息配置

        [FoldoutGroup("配置scene", -10)]
        [LabelText("展示Terrain")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showTerrainScene;

        [FoldoutGroup("配置scene", -10)]
        [LabelText("展示Hex")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showHexScene;

        [FoldoutGroup("配置scene", -10)]
        [LabelText("地形生成方式")]
        [OnValueChanged("TerMeshGenValueChanged")]
        public TerMeshGenMethod genMethod;

        private void ShowSceneValueChanged() {
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
        }

        private void TerMeshGenValueChanged() {
            // TODO : 让 EditorSceneManager 改变生成方式
        }

        #endregion


        #region 涂刷 Hex 纹理

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("允许涂刷Hex")]
        [OnValueChanged("EnableBrushValueChanged")]
        public bool enableBrush;

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("涂刷范围")]
        public int brushScope = 5;

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("涂刷颜色")]
        public Color brushColor = Color.blue;

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("纹理尺寸")]
        public int hexTexScale = 1;

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("纹理偏移")]
        public Vector3 hexTextureOffset = new Vector3(0, 0, 0);

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("涂刷画板所用的材质")]
        public Material hexmapTexMaterial;

        [FoldoutGroup("涂刷Hexmap纹理", -9)]
        [LabelText("HexData纹理路径")]
        public string hexmapDataPath = MapStoreEnum.TerrainHexmapDataPath;

        HexmapDataTexManager hexmapDataTexManager = new HexmapDataTexManager();

        private void EnableBrushValueChanged() {
            if (enableBrush) {
                // if start brush, then close the hex scene and ter scene
                showTerrainScene = false;
                showHexScene = false;
                sceneManager.UpdateSceneView(showTerrainScene, showHexScene);

            }

            if (hexmapDataTexManager == null || hexmapDataTexManager.IsInit == false) {
                Debug.LogError("hexmapData tex manager not init!");
                return;
            }
            hexmapDataTexManager.ShowHideTexture(enableBrush);
        }

        [FoldoutGroup("涂刷Hexmap纹理")]
        [Button("初始化Hex数据纹理", ButtonSizes.Medium)]
        private void InitHexmapDataTexture() {
            // TODO : generate a texture to storage the data of hex map;
            hexSet = EditorSceneManager.hexSet;
            terSet = EditorSceneManager.terSet;
            hexmapDataTexManager.InitHexmapDataTexture(hexSet.mapWidth, hexSet.mapHeight, hexTexScale, hexTextureOffset, 
                EditorSceneManager.mapScene.hexTextureParentObj, hexmapTexMaterial);
        }

        [FoldoutGroup("涂刷Hexmap纹理")]
        [Button("读取Hex数据纹理", ButtonSizes.Medium)]
        private void LoadHexmapDataTexture() {
            string hexmapImportPath = EditorUtility.OpenFilePanel("Import Hexmap Data Texture", "", "");
            if (hexmapImportPath == "") {
                Debug.LogError("you do not get the Hexmap texture");
                return;
            }

            hexmapImportPath = AssetsUtility.TransToAssetPath(hexmapImportPath);
            Texture2D hexmapTex = AssetDatabase.LoadAssetAtPath<Texture2D>(hexmapImportPath);
            if (hexmapTex == null) {
                Debug.LogError(string.Format("can not load Hexmap texture from this path: {0}", hexmapImportPath));
                return;
            }

            hexmapDataTexManager.InitHexmapDataTexture(hexmapTex, hexTexScale, hexTextureOffset,
                EditorSceneManager.mapScene.hexTextureParentObj, hexmapTexMaterial);
        }

        [FoldoutGroup("涂刷Hexmap纹理")]
        [Button("存储Hex数据纹理", ButtonSizes.Medium)]
        private void SaveHexmapDataTexture() {

            // TODO : 这里是不是有问题？真的存到了吗？

            RenderTexture rt = hexmapDataTexManager.GetHexDataTexture();
            TextureFormat fmt = rt.format == RenderTextureFormat.ARGBHalf ? TextureFormat.RGBAHalf :
                                rt.format == RenderTextureFormat.ARGBFloat ? TextureFormat.RGBAFloat :
                                TextureFormat.RGBA32;
            Texture2D tex = new Texture2D(rt.width, rt.height, fmt, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply(false, false);

            DateTime dateTime = DateTime.Now;
            string texName = string.Format("hexmapDataTexture_{0}x{1}_{2}", rt.width, rt.height, dateTime.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(hexmapDataPath, texName, tex);

            GameObject.DestroyImmediate(tex);
        }

        [FoldoutGroup("涂刷Hexmap纹理")]
        [Button("清空Hex数据纹理", ButtonSizes.Medium)]
        private void ClearHexmapDataTexture() {
            // TODO : clear it! but do not delete it!
            hexmapDataTexManager.Dispose();

            Debug.Log("hexmap Data Texture cleared");
        }

        #endregion


        public override void Enable() {
            base.Enable();
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
        }

        public override void Disable() { 
            base.Disable();
            sceneManager.UpdateSceneView(false, false);
            lockSceneView = false;
        }

        protected override void OnMouseDown(Event e) {
            if (!enableBrush) {
                return;
            }

            // only valid when lock scene view
            Vector3 worldPos = GetMousePosToScene(e);
            hexmapDataTexManager.PaintHexDataTexture(worldPos, brushScope, brushColor);
            SceneView.RepaintAll();
        }

        protected override void OnMouseDrag(Event e) {
            if (!enableBrush) {
                return;
            }

            // only valid when lock scene view
            Vector3 worldPos = GetMousePosToScene(e);
            hexmapDataTexManager.PaintHexDataTexture(worldPos, brushScope, brushColor);
            SceneView.RepaintAll();
        }

    }
}
