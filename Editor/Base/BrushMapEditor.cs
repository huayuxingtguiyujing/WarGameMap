using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using static LZ.WarGameMap.Runtime.GizmosCtrl;

namespace LZ.WarGameMap.MapEditor
{
    // 编辑器基类，提供功能 :
    // (1) 支持对 scene 进行 brush，会呈现一张纹理
    // (2) 支持在 scene 中查看地图 terrain/hexgrids 信息的，继承该类
    public abstract class BrushMapEditor : BaseMapEditor {

        protected HexSettingSO hexSet;

        protected TerrainSettingSO terSet;

        protected MapRuntimeSetting mapSet;

        protected TerrainConstructor terrainCtor;

        protected override void InitMapSetting() {
            base.InitMapSetting();
            mapSet = EditorSceneManager.MapSet;
            terSet = EditorSceneManager.TerSet;
            hexSet = EditorSceneManager.HexSet;
            terrainCtor = EditorSceneManager.TerrainCtor;
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
        [LabelText("HexData纹理路径"), ReadOnly]
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

        [FoldoutGroup("涂刷Hexmap纹理")]
        [Button("初始化Hex数据纹理", ButtonSizes.Medium)]
        private void InitHexmapDataTexture() {
            // TODO : generate a texture to storage the data of hex map;
            hexSet = EditorSceneManager.HexSet;
            terSet = EditorSceneManager.TerSet;
            hexmapDataTexManager.InitHexmapDataTexture(hexSet.mapWidth, hexSet.mapHeight, hexTexScale, hexTextureOffset, 
                EditorSceneManager.mapScene.hexTextureParentObj, hexmapTexMaterial, null);
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
                EditorSceneManager.mapScene.hexTextureParentObj, hexmapTexMaterial, null);
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
            TextureUtility.SaveTextureAsAsset(hexmapDataPath, texName, tex);

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


        // TODO : 笔刷涂抹效果
        #region 笔刷 编辑地图

        class PaintTerrainEnums
        {
            public static Color BrushTerColor = Color.yellow;
        }

        [FoldoutGroup("笔刷 编辑地图")]
        [LabelText("开启涂刷")]
        [OnValueChanged("OnEnablePaintChanged")]
        public bool enablePaintTerrain = false;

        [FoldoutGroup("笔刷 编辑地图")]
        [LabelText("涂刷范围"), Range(0, 200)]
        public float brushTerrainScope = 10;

        [FoldoutGroup("笔刷 编辑地图")]
        [LabelText("涂刷力度"), Range(0, 100)]
        public float brushTerrainStrength = 0.5f;

        BaseBrushTool CurBrushTool = new CircleBrushTool();

        [FoldoutGroup("笔刷 编辑地图")]
        [LabelText("当前笔刷")]
        [OnValueChanged("OnCurBrushTypeChanged")]
        public BrushToolType CurBrushType;

        Vector3 lastPaintCenter = new Vector3();

        List<Vector3> paintTargets = new List<Vector3>();

        private void OnEnablePaintChanged()
        {
            enableBtnEvent = enablePaintTerrain;
            if (enablePaintTerrain)
            {
                //GizmosCtrl.GetInstance().RegisterGizmoEvent(ShowPaintScope_UnityAction);
            }
            else
            {
                //GizmosCtrl.GetInstance().UnregisterGizmoEvent(ShowPaintScope_UnityAction);
            }
        }

        private void OnCurBrushTypeChanged()
        {
            switch (CurBrushType)
            {
                case BrushToolType.Circle:
                    CurBrushTool = new CircleBrushTool();
                    break;
                case BrushToolType.CircleLinearLerped:
                    CurBrushTool = new CircleLinearLerpBrushTool();
                    break;
                case BrushToolType.CircleSoothStep1:
                    break;
                case BrushToolType.CircleSoothStep2:
                    break;
                case BrushToolType.CircleNoise:
                    break;
            }
        }

        private void ShowPaintScope_UnityAction()
        {
            ShowPaintScope(Input.mousePosition);
        }

        private void ShowPaintScope(Event e)
        {
            ShowPaintScope(e.mousePosition);
        }

        // TODO : 测试它
        private void ShowPaintScope(Vector3 mousePos)
        {
            if (!enablePaintTerrain)
            {
                return;
            }

            // 获取到鼠标在scene的位置，然后映射到 y=0 的平面上
            Vector3 paintCenter = GetMousePosIny0(mousePos);

            Vector3 paintScopeNormal = new Vector3(0, 1, 0);
            Handles.color = PaintTerrainEnums.BrushTerColor;
            Handles.DrawWireDisc(paintCenter, paintScopeNormal, brushTerrainScope);

            //GizmosUtils.DrawScope(paintCenter, brushTerrainScope, PaintTerrainEnums.BrushTerColor);
        }

        // TODO : 测试它
        private void UpdatePaintTargets(Event e)
        {
            if (!enablePaintTerrain)
            {
                return;
            }

            if (CurBrushTool == null)
            {
                return;
            }

            if (!terrainCtor.IsGen)
            {
                return;
            }

            // 获取到鼠标在scene的位置，然后映射到 y=0 的平面上
            Vector3 paintCenter = GetMousePosIny0(e);
            if (lastPaintCenter == paintCenter)
            {
                return;
            }
            lastPaintCenter = paintCenter;
            paintTargets.Clear();

            // 判断所在的区域是否超过当前展示的 terrain 的范围，如果超过则 return
            Vector2Int mapRightUp = terrainCtor.GetCurMapRightUp();
            Vector2Int mapLeftUp = terrainCtor.GetCurMapLeftDown();
            if (paintCenter.x < mapLeftUp.x || paintCenter.x > mapRightUp.x
                || paintCenter.y < mapLeftUp.y || paintCenter.y > mapRightUp.y)
            {
                return;
            }

            // Refresh paint targets
            paintTargets = terrainCtor.GetPaintTargets(paintCenter, brushTerrainScope);
            Debug.Log($"Paint over, point num : {paintTargets.Count}");
        }

        // TODO : 测试它
        private void HandlePaintProcess(Event e)
        {
            if (!enablePaintTerrain)
            {
                return;
            }

            if (CurBrushTool == null)
            {
                return;
            }

            if (paintTargets == null || paintTargets.Count == 0)
            {
                return;
            }

            Vector3 paintCenter = GetMousePosIny0(e);
            CurBrushTool.Brush(brushTerrainStrength, brushScope, paintCenter, paintTargets);
            terrainCtor.UpdatePaintVerts(paintTargets);
        }


        #endregion


        public override void Enable() {
            base.Enable();
            sceneManager.UpdateSceneView(showTerrainScene, showHexScene);

            //UnityEditorManager.RegisterUpdate(ShowPaintScope_UnityAction);
            //GizmoDrawEventHandler handler = new GizmoDrawEventHandler(ShowPaintScope_UnityAction);
            //GizmosCtrl.GetInstance().RegisterGizmoEvent(ShowPaintScope_UnityAction);
        }

        public override void Disable() { 
            base.Disable();
            sceneManager.UpdateSceneView(false, false);
            if (enableBrush) 
            {
                ShowHideTexture(false);     // if open, close it
            }
            lockSceneView = false;

            //UnityEditorManager.UnregisterUpdate(ShowPaintScope_UnityAction);
            //GizmosCtrl.GetInstance().UnregisterGizmoEvent(ShowPaintScope_UnityAction);
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

        protected override void OnMouseMove(Event e)
        {
            //ShowPaintScope(e);
        }

        protected override void OnMouseDown(Event e) {
            if (enableBrush)
            {
                // only valid when lock scene view
                Vector3 worldPos = GetMousePosToScene(e);
                hexmapDataTexManager.PaintHexDataTexture_RectScope(worldPos, brushScope, brushColor);
                SceneView.RepaintAll();
            }

            UpdatePaintTargets(e);
            HandlePaintProcess(e);
        }

        protected override void OnMouseDrag(Event e) {
            if (enableBrush) 
            {
                // only valid when lock scene view
                Vector3 worldPos = GetMousePosToScene(e);
                hexmapDataTexManager.PaintHexDataTexture_RectScope(worldPos, brushScope, brushColor);
                SceneView.RepaintAll();
            }

            UpdatePaintTargets(e);
            HandlePaintProcess(e);
        }

        protected override void HandleSceneDraw(Event e)
        {
            ShowPaintScope(e);
        }

    }
}
