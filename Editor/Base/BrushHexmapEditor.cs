using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    // �༭�����࣬�ṩ���� :
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

        [FoldoutGroup("ͿˢHexmap����", -8)]
        [LabelText("����ͿˢHex")]
        [OnValueChanged("EnableBrushValueChanged")]
        public bool enableBrush;

        [FoldoutGroup("ͿˢHexmap����")]
        [LabelText("HexͿˢ��Χ")]
        [Range(1, 100)]
        public int brushScope;

        [FoldoutGroup("ͿˢHexmap����")]
        [LabelText("HexͿˢMat")]
        public Material hexBrushMat;        // Use "WarGameMap/Terrain/ShowTex/HexGridShader"

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

            // TODO : ������ܣ����䲻ͬ��Ϳˢ��Χ
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
