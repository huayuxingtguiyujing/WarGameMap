using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace LZ.WarGameMap.MapEditor
{
    // Hexmap 编辑器基类，提供功能 :
    // (1) 支持对 scene 进行 brush，会呈现一张纹理
    // (2) 支持在 scene 中查看地图 terrain/hexgrids 信息的，继承该类
    public abstract class BrushHexmapEditor : BaseMapEditor
    {
        protected HexSettingSO hexSet;

        protected TerrainSettingSO terSet;

        protected MapRuntimeSetting mapSet;

        //protected GridTerrainSO gridTerrainSO;

        protected HexmapConstructor HexCtor;

        protected HexmapDataTexManager hexmapDataTexManager;

        protected override void InitMapSetting()
        {
            HexCtor = EditorSceneManager.HexCtor;
            hexmapDataTexManager = new HexmapDataTexManager();

            base.InitMapSetting();
            mapSet = EditorSceneManager.MapSet;
            //FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");

            terSet = EditorSceneManager.TerSet;
            //FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");

            hexSet = EditorSceneManager.HexSet;
            //FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");

            //FindOrCreateSO<GridTerrainSO>(ref gridTerrainSO, MapStoreEnum.GamePlayGridTerrainDataPath, "GridTerrainSO_Default.asset");
            //gridTerrainSO.UpdateTerSO(hexSet.mapWidth, hexSet.mapHeight);
        }


        #region 涂刷Hexmap格子

        protected struct BrushHexmapSetting
        {
            public bool enableBrush;           // 允许涂刷
            public bool enableKeyCode;        // 允许使用快捷键    // TODO : use it
            public bool useTexCache;          // 开启 HexmapDataTexManager 的缓存
            public int texCacheNum;               // 缓存页数

            public BrushHexmapSetting(bool enableBrush, bool enableKeyCode, bool useTexCache, int texCacheNum)
            {
                this.enableBrush = enableBrush;
                this.enableKeyCode = enableKeyCode;
                this.useTexCache = useTexCache;
                this.texCacheNum = texCacheNum;
            }

            public static BrushHexmapSetting Default = new BrushHexmapSetting() { 
                enableBrush = true,
                enableKeyCode = false,
                useTexCache = false,
                texCacheNum = 1
            };
        }

        protected abstract BrushHexmapSetting GetBrushSetting();


        [FoldoutGroup("涂刷Hexmap格子", -8)]
        [LabelText("允许涂刷Hex")]
        [OnValueChanged("EnableBrushValueChanged")]
        public bool enableBrush;

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷范围")]
        [Range(0, 100)]
        public int brushScope;

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷CS")]
        public ComputeShader paintRTShader;         // Use "Utils/PaintRTPixels.compute"

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷Mat")]
        public Material hexBrushMat;                // Use "WarGameMap/Terrain/ShowTex/HexGridShader"

        [FoldoutGroup("涂刷Hexmap格子")]
        [LabelText("Hex涂刷颜色")]
        public Color brushColor;

        protected List<Color> brushCachePageColorList = new List<Color>();

        private void EnableBrushValueChanged()
        {
            if (enableBrush)
            {
                // TODO : 每次切换的时候 是不是应该挂个啥回调？
            }
        }

        [FoldoutGroup("涂刷Hexmap格子")]
        [Button("初始化Hex地图格", ButtonSizes.Medium)]
        [Tooltip("点击初始化后，会在sceneview中生成供涂刷的六边形格子地图，格子地图即GamePlay的地图")]
        private void BuildHexGridMap()
        {
            if(hexmapDataTexManager == null)
            {
                Debug.LogError("hexmapDataTexManager is null! you should init editor firstly");
                return;
            }

            BrushHexmapSetting brushSetting = GetBrushSetting();

            // TODO : 如果是 3000*3000 规格的地图，Mesh数据会到800MB，要动态加载？
            HexCtor.InitHexConsRectangle_Once(hexBrushMat);
            
            hexmapDataTexManager.InitHexmapDataTexture(hexSet.mapWidth, hexSet.mapHeight, 1, Vector3.zero, 
                EditorSceneManager.mapScene.hexTextureParentObj, hexBrushMat, paintRTShader, 
                true, brushSetting.useTexCache, brushSetting.texCacheNum, FilterMode.Point);

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
            PostBuildHexGridMap();
            Debug.Log($"build the hex grid map, width : {hexSet.mapWidth}, height : {hexSet.mapHeight}");
        }

        [FoldoutGroup("涂刷Hexmap格子")]
        [Button("清空Hex地图格", ButtonSizes.Medium)]
        private void ClearHexGridMap()
        {
            if(HexCtor != null)
            {
                HexCtor.ClearClusterObj();
            }
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

        protected void SetBrushCacheColor(Color[] brushCacheColor)
        {
            BrushHexmapSetting brushHexmapSetting = GetBrushSetting();
            Assert.AreEqual(brushHexmapSetting.texCacheNum, brushCacheColor.Length); 

            if (brushCachePageColorList == null || brushCachePageColorList.Count == 0)
            {
                brushCachePageColorList = new List<Color>(brushHexmapSetting.texCacheNum);
                for (int i = 0; i < brushCacheColor.Length; i++)
                {
                    brushCachePageColorList.Add(brushCacheColor[i]);
                }
            }
            else
            {
                for (int i = 0; i < brushCacheColor.Length; i++)
                {
                    brushCachePageColorList[i] = brushCacheColor[i];
                }
            }
        }

        // Call it when you need handle when BuildHexGridMap over
        protected virtual void PostBuildHexGridMap() { }

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

            //Debug.Log($"now paint grid num : {offsetHexList.Count}");
            for (int i = offsetHexList.Count - 1; i >= 0; i--)
            {
                if (!EnablePaintHex(offsetHexList[i]))
                {
                    offsetHexList.RemoveAt(i);
                }
                //DebugUtility.DebugGameObject("paintGo", offsetHexList[i].TransToXZ(), null);
            }
            //Debug.Log($"now enable paint grid num : {offsetHexList.Count}");

            // TODO : 提高性能，适配不同的涂刷范围
            //hexmapDataTexManager.PaintHexDataTexture_RectScope(offsetHexPos.TransToXZ(), brushScope, brushColor);
            hexmapDataTexManager.PaintHexDataTexture_Scope(offsetHexList, brushColor, brushCachePageColorList);
            PaintHexRTEvent(offsetHexList);
        }

        protected void PaintHexRT(List<Vector2Int> worldPoss, Color color)
        {
            for (int i = worldPoss.Count - 1; i >= 0; i--)
            {
                if (!EnablePaintHex(worldPoss[i]))
                {
                    worldPoss.RemoveAt(i);
                }
            }
            hexmapDataTexManager.PaintHexDataTexture_Scope(worldPoss, color, brushCachePageColorList);
        }

        // Call it to get enable paint pixel
        protected virtual bool EnablePaintHex(Vector2Int offsetHexPos) { return true; }

        // Call it when paint hexRT
        protected virtual void PaintHexRTEvent(List<Vector2Int> offsetHexList) { }

        // Call it when click init hex grid map
        protected virtual Color PaintHexGridWhenLoad(int index) { return Color.white; }

        protected void UpdateHexTexManager()
        {
            hexmapDataTexManager.UpdateHexManager();
        }

        public override void Enable()
        {
            base.Enable();
            // TODO : 读取 brush setting，调整涂刷配置
        }

        public override void Disable() 
        { 
            base.Disable();
            ClearHexGridMap();
        }
        
    }
}
