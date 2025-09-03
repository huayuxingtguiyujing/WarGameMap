using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    // 编辑器基类，提供功能 :
    // (1) 支持对 scene 进行 brush，会呈现一张纹理
    // (2) 支持在 scene 中查看地图 terrain/hexgrids 信息的，继承该类
    public abstract class BrushHexmapEditor : BaseMapEditor
    {
        protected HexSettingSO hexSet;

        protected TerrainSettingSO terSet;

        protected MapRuntimeSetting mapSet;

        protected HexmapConstructor HexCtor;

        protected HexmapDataTexManager hexmapDataTexManager;

        protected override void InitMapSetting()
        {
            HexCtor = EditorSceneManager.HexCtor;
            hexmapDataTexManager = new HexmapDataTexManager();

            base.InitMapSetting();
            mapSet = EditorSceneManager.mapSet;
            FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");

            terSet = EditorSceneManager.terSet;
            FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");

            hexSet = EditorSceneManager.hexSet;
            FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");
        }


        #region 涂刷Hexmap格子

        [FoldoutGroup("涂刷Hexmap格子", -8)]
        [LabelText("允许涂刷Hex")]
        [OnValueChanged("EnableBrushValueChanged")]
        public bool enableBrush;

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷范围")]
        [Range(1, 100)]
        public int brushScope;

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷Mat")]
        public Material hexBrushMat;        // Use "WarGameMap/Terrain/ShowTex/HexGridShader"

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷颜色")]
        public Color brushColor;

        private void EnableBrushValueChanged()
        {
            if (enableBrush)
            {
                // TODO : 每次切换的时候 是不是应该挂个啥回调？
            }
        }

        [FoldoutGroup("涂刷Hexmap格子")]
        [Button("初始化Hex地图格", ButtonSizes.Medium)]
        private void BuildHexGridMap()
        {
            if(hexmapDataTexManager == null)
            {
                Debug.LogError("hexmapDataTexManager is null!");
                return;
            }

            HexCtor.InitHexConsRectangle_Once(hexBrushMat);
            hexmapDataTexManager.InitHexmapDataTexture(hexSet.mapWidth, hexSet.mapHeight, 1, Vector3.zero, 
                EditorSceneManager.mapScene.hexTextureParentObj, hexBrushMat, true);

            List<Color> colors = new List<Color>(hexSet.mapWidth * hexSet.mapHeight);
            for(int i = 0; i < hexSet.mapWidth; i++)
            {
                for(int j = 0;  j < hexSet.mapHeight; j++)
                {
                    colors.Add(new Color());
                }
            }

            Parallel.ForEach(colors, (item, state, index) => 
            {
                colors[(int)index] = PaintHexGridWhenLoad((int)index);
            });
            hexmapDataTexManager.SetRTPixel(colors);
            Debug.Log($"build the hex grid map, width : {hexSet.mapWidth}, height : {hexSet.mapHeight}");
        }

        [FoldoutGroup("涂刷Hexmap格子")]
        [Button("清空Hex地图格", ButtonSizes.Medium)]
        private void ClearHexGridMap()
        {
            HexCtor.ClearClusterObj();
            if(hexmapDataTexManager != null)
            {
                hexmapDataTexManager.Dispose();
            }
            Debug.Log("clear the hex grid map");
        }

        protected void SetBrushColor(Color brushColor)
        {
            this.brushColor = brushColor;
        }

        #endregion


        protected override void OnMouseDown(Event e)
        {
            Vector3 worldPos = GetMousePosToScene(e);
            PaintHexRT(worldPos);
            SceneView.RepaintAll();
        }

        protected override void OnMouseDrag(Event e)
        {
            Vector3 worldPos = GetMousePosToScene(e);
            PaintHexRT(worldPos);
            SceneView.RepaintAll();
        }

        private void PaintHexRT(Vector3 worldPos)
        {
            // Get hex grids offset coord by worldPos
            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            Vector2Int offsetHexPos = HexHelper.AxialToOffset(HexHelper.PixelToAxialHex(pos, hexSet.hexGridSize));
            List<Vector2Int> offsetHexList = HexHelper.GetOffsetHexNeighbour_Scope(offsetHexPos, brushScope);

            // TODO : 提高性能，适配不同的涂刷范围
            hexmapDataTexManager.PaintHexDataTexture_RectScope(offsetHexPos.TransToXZ(), brushScope, brushColor);

            PaintHexRTEvent(offsetHexList);
        }

        // Call it when paint hexRT
        protected virtual void PaintHexRTEvent(List<Vector2Int> offsetHexList)
        {

        }

        // Call it when click init hex grid map
        protected virtual Color PaintHexGridWhenLoad(int index)
        {
            int i = index / hexSet.mapWidth;
            int j = index % hexSet.mapHeight;
            return Color.white;
        }

    }
}
