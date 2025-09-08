using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    // Hexmap �༭�����࣬�ṩ���� :
    // (1) ֧�ֶ� scene ���� brush�������һ������
    // (2) ֧���� scene �в鿴��ͼ terrain/hexgrids ��Ϣ�ģ��̳и���
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


        #region ͿˢHexmap����

        protected class BrushHexmapSetting
        {
            public bool enableBrush = true;           // ����Ϳˢ
            public bool enableKeyCode = false;        // ����ʹ�ÿ�ݼ�
            //public bool enableBrush;    //
        }

        protected abstract BrushHexmapSetting GetBrushSetting();


        [FoldoutGroup("ͿˢHexmap����", -8)]
        [LabelText("����ͿˢHex")]
        [OnValueChanged("EnableBrushValueChanged")]
        public bool enableBrush;

        [FoldoutGroup("ͿˢHexmap����")]
        [LabelText("HexͿˢ��Χ")]
        [Range(1, 100)]
        public int brushScope;

        [FoldoutGroup("ͿˢHexmap����")]
        [LabelText("HexͿˢCS")]
        public ComputeShader paintRTShader;         // Use "Utils/PaintRTPixels.compute"

        [FoldoutGroup("ͿˢHexmap����")]
        [LabelText("HexͿˢMat")]
        public Material hexBrushMat;                // Use "WarGameMap/Terrain/ShowTex/HexGridShader"

        [FoldoutGroup("ͿˢHexmap����")]
        [LabelText("HexͿˢ��ɫ")]
        public Color brushColor;

        private void EnableBrushValueChanged()
        {
            if (enableBrush)
            {
                // TODO : ÿ���л���ʱ�� �ǲ���Ӧ�ùҸ�ɶ�ص���
            }
        }

        [FoldoutGroup("ͿˢHexmap����")]
        [Button("��ʼ��Hex��ͼ��", ButtonSizes.Medium)]
        private void BuildHexGridMap()
        {
            if(hexmapDataTexManager == null)
            {
                Debug.LogError("hexmapDataTexManager is null!");
                return;
            }

            // TODO : ����� 3000*3000 ���ĵ�ͼ��Mesh���ݻᵽ800MB��Ҫ��̬���أ�
            HexCtor.InitHexConsRectangle_Once(hexBrushMat);
            
            hexmapDataTexManager.InitHexmapDataTexture(hexSet.mapWidth, hexSet.mapHeight, 1, Vector3.zero, 
                EditorSceneManager.mapScene.hexTextureParentObj, hexBrushMat, paintRTShader, true);

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

        [FoldoutGroup("ͿˢHexmap����")]
        [Button("���Hex��ͼ��", ButtonSizes.Medium)]
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

        private int scopeModify_Little = 1;
        private int scopeModify_Big = 5;

        protected override void OnKeyCodeQ()
        {
            brushScope = Mathf.Max(1, brushScope - scopeModify_Big);
            LogCurBrushScope();
        }

        protected override void OnKeyCodeE()
        {
            brushScope = Mathf.Min(100, brushScope + scopeModify_Big);
            LogCurBrushScope();
        }

        protected override void OnKeyCodeA()
        {
            brushScope = Mathf.Max(1, brushScope - scopeModify_Little);
            LogCurBrushScope();
        }

        protected override void OnKeyCodeD()
        {
            brushScope = Mathf.Min(100, brushScope + scopeModify_Little);
            LogCurBrushScope();
        }

        private void LogCurBrushScope()
        {
            Debug.Log($"cur brushScope : {brushScope}");
        }

        protected override void OnKeyCodeAlphaNum(int num)
        {
            base.OnKeyCodeAlphaNum(num); Debug.Log(num);
        }


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
            if (hexmapDataTexManager == null)
            {
                Debug.LogError("hexmapDataTexManager is null!");
                return;
            }

            // Get hex grids offset coord by worldPos
            Vector2 pos = new Vector2(worldPos.x, worldPos.z);
            Vector2Int offsetHexPos = HexHelper.AxialToOffset(HexHelper.PixelToAxialHex(pos, hexSet.hexGridSize));
            List<Vector2Int> offsetHexList = HexHelper.GetOffsetHexNeighbour_Scope(offsetHexPos, brushScope);

            Debug.Log($"now paint grid num : {offsetHexList.Count}");
            for (int i = offsetHexList.Count - 1; i >= 0; i--)
            {
                if (!EnablePaintHex(offsetHexList[i]))
                {
                    offsetHexList.RemoveAt(i);
                }
            }
            Debug.Log($"now enable paint grid num : {offsetHexList.Count}");

            // TODO : ������ܣ����䲻ͬ��Ϳˢ��Χ
            //hexmapDataTexManager.PaintHexDataTexture_RectScope(offsetHexPos.TransToXZ(), brushScope, brushColor);
            hexmapDataTexManager.PaintHexDataTexture_Scope(offsetHexList, brushColor);
            PaintHexRTEvent(offsetHexList);
        }

        // Call it to get enable paint pixel
        protected virtual bool EnablePaintHex(Vector2Int offsetHexPos) { return true; }

        // Call it when paint hexRT
        protected virtual void PaintHexRTEvent(List<Vector2Int> offsetHexList) { }

        // Call it when click init hex grid map
        protected virtual Color PaintHexGridWhenLoad(int index) { return Color.white; }


        public override void Enable()
        {
            base.Enable();
            // TODO : ��ȡ brush setting������Ϳˢ����
        }

        public override void Disable() 
        { 
            base.Disable();

        }

    }
}
