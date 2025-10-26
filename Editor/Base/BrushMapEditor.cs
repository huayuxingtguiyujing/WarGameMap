using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    // �༭�����࣬�ṩ���� :
    // (1) ֧�ֶ� scene ���� brush�������һ������
    // (2) ֧���� scene �в鿴��ͼ terrain/hexgrids ��Ϣ�ģ��̳и���
    public abstract class BrushMapEditor : BaseMapEditor {

        protected HexSettingSO hexSet;

        protected TerrainSettingSO terSet;

        protected MapRuntimeSetting mapSet;

        protected override void InitMapSetting() {
            base.InitMapSetting();
            mapSet = EditorSceneManager.MapSet;
            //FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");

            terSet = EditorSceneManager.TerSet;
            //FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");

            hexSet = EditorSceneManager.HexSet;
            //FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");
        }

        #region ��ͼ��Ϣ����

        [FoldoutGroup("����scene", -10)]
        [LabelText("չʾTerrain")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showTerrainScene;

        [FoldoutGroup("����scene", -10)]
        [LabelText("չʾHex")]
        [OnValueChanged("ShowSceneValueChanged")]
        public bool showHexScene;

        [FoldoutGroup("����scene", -10)]
        [LabelText("�������ɷ�ʽ")]
        [OnValueChanged("TerMeshGenValueChanged")]
        public TerMeshGenMethod genMethod;

        private void ShowSceneValueChanged() {
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
        }

        private void TerMeshGenValueChanged() {
            // TODO : �� EditorSceneManager �ı����ɷ�ʽ
        }

        #endregion


        #region Ϳˢ Hex ����

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("����ͿˢHex")]
        [OnValueChanged("EnableBrushValueChanged")]
        public bool enableBrush;

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("Ϳˢ��Χ")]
        public int brushScope = 5;

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("Ϳˢ��ɫ")]
        public Color brushColor = Color.blue;

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("����ߴ�")]
        public int hexTexScale = 1;

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("����ƫ��")]
        public Vector3 hexTextureOffset = new Vector3(0, 0, 0);

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("Ϳˢ�������õĲ���")]
        public Material hexmapTexMaterial;

        [FoldoutGroup("ͿˢHexmap����", -9)]
        [LabelText("HexData����·��"), ReadOnly]
        public string hexmapDataPath = MapStoreEnum.TerrainHexmapDataPath;

        HexmapDataTexManager hexmapDataTexManager = new HexmapDataTexManager();

        private void EnableBrushValueChanged() {
            if (enableBrush) {
                // if start brush, then close the hex scene and ter scene
                showTerrainScene = false;
                showHexScene = false;
                sceneManager.UpdateSceneView(showTerrainScene, showHexScene);
            }
            ShowHideTexture(enableBrush);
        }

        [FoldoutGroup("ͿˢHexmap����")]
        [Button("��ʼ��Hex��������", ButtonSizes.Medium)]
        private void InitHexmapDataTexture() {
            // TODO : generate a texture to storage the data of hex map;
            hexSet = EditorSceneManager.HexSet;
            terSet = EditorSceneManager.TerSet;
            hexmapDataTexManager.InitHexmapDataTexture(hexSet.mapWidth, hexSet.mapHeight, hexTexScale, hexTextureOffset, 
                EditorSceneManager.mapScene.hexTextureParentObj, hexmapTexMaterial, null);
        }

        [FoldoutGroup("ͿˢHexmap����")]
        [Button("��ȡHex��������", ButtonSizes.Medium)]
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
                EditorSceneManager.mapScene.hexTextureParentObj, hexmapTexMaterial, null);
        }

        [FoldoutGroup("ͿˢHexmap����")]
        [Button("�洢Hex��������", ButtonSizes.Medium)]
        private void SaveHexmapDataTexture() {

            // TODO : �����ǲ��������⣿��Ĵ浽����

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

        [FoldoutGroup("ͿˢHexmap����")]
        [Button("���Hex��������", ButtonSizes.Medium)]
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
            if (enableBrush) 
            {
                ShowHideTexture(false);     // if open, close it
            }
            lockSceneView = false;
        }

        private void ShowHideTexture(bool enableBrush)
        {
            if (hexmapDataTexManager == null)
            {
                DebugUtility.LogError("hexmapData tex manager is null");
                return;
            }
            hexmapDataTexManager.ShowHideTexture(enableBrush);
        }

        protected override void OnMouseDown(Event e) {
            if (!enableBrush) {
                return;
            }

            //Debug.Log(22222);

            // only valid when lock scene view
            Vector3 worldPos = GetMousePosToScene(e);
            hexmapDataTexManager.PaintHexDataTexture_RectScope(worldPos, brushScope, brushColor);
            SceneView.RepaintAll();
        }

        protected override void OnMouseDrag(Event e) {
            if (!enableBrush) {
                return;
            }

            //Debug.Log(333333);

            // only valid when lock scene view
            Vector3 worldPos = GetMousePosToScene(e);
            hexmapDataTexManager.PaintHexDataTexture_RectScope(worldPos, brushScope, brushColor);
            SceneView.RepaintAll();
        }

    }
}
