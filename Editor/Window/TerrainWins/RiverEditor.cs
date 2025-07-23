using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Rendering;
using System.Security.Cryptography;
using static LZ.WarGameMap.MapEditor.RiverEditor;
using static Codice.Client.BaseCommands.BranchExplorer.Layout.BrExLayout;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;
using System.ComponentModel;
//using LZ.WarGameCommon;

namespace LZ.WarGameMap.MapEditor
{

    public enum RiverDataFlow 
    { 
        RiverTexture,       // 以纹理 (pixel) 的形式 保存河流数据       
        RiverBezier,        // 以贝塞尔曲线的形式    保存河流数据
    }

    public class RiverEditor : BrushMapEditor 
    {
        public override string EditorName => MapEditorEnum.RiverEditor;

        Transform RiverDataParentTrans;

        Transform RiverDatasTrans;

        Transform RiverPaintRTsTrans;

        Transform RiverBezierParentTrans;

        static class RiverEditorNames {
            public static string RiverDatas = "RiverDatas";
            public static string RiverPaintRTs = "RiverPaintRT";
            public static string RiverBezierParent = "RiverBezierParent";
        }

        protected override void InitEditor() {
            base.InitEditor();
            InitMapSetting();

            RiverDataParentTrans = EditorSceneManager.mapScene.riverDataParentObj.transform;

            // create other trans
            if(RiverDatasTrans == null) {
                RiverDatasTrans = RiverDataParentTrans.Find(RiverEditorNames.RiverDatas);
                if(RiverDatasTrans == null) {
                    GameObject go = new GameObject(RiverEditorNames.RiverDatas);
                    RiverDatasTrans = go.transform;
                }
            }
            RiverDatasTrans.parent = RiverDataParentTrans;

            if (RiverPaintRTsTrans == null) {
                RiverPaintRTsTrans = RiverDataParentTrans.Find(RiverEditorNames.RiverPaintRTs);
                if (RiverPaintRTsTrans == null) {
                    GameObject go = new GameObject(RiverEditorNames.RiverPaintRTs);
                    RiverPaintRTsTrans = go.transform;
                }
            }
            RiverPaintRTsTrans.parent = RiverDataParentTrans;

            if (RiverBezierParentTrans == null) {
                RiverBezierParentTrans = RiverDataParentTrans.Find(RiverEditorNames.RiverBezierParent);
                if (RiverBezierParentTrans == null) {
                    GameObject go = new GameObject(RiverEditorNames.RiverBezierParent);
                    RiverBezierParentTrans = go.transform;
                }
            }
            RiverBezierParentTrans.parent = RiverDataParentTrans;

            // init mapRiverData, it will always bind with cur TerrainSettingSO and HexSettingSO
            FindOrCreateSO<MapRiverData>(ref mapRiverData, MapStoreEnum.RiverDataPath, "MapRiverData_Default.asset");
            mapRiverData.InitMapRiverData(terSet, hexSet);

            editRiverDatas = new List<EditingRiverData>();
        }

        static RenderTexturePool RTPool;


        #region 当前河流编辑


        [Serializable]
        public class PaintRiverRTData : IDisposable {

            public GameObject paintRTObj;

            MeshRenderer renderer;

            public Material paintRTMat;

            public RenderTexture renderTexture;

            public Vector2Int rtClusterIdx;

            public PaintRiverRTData(GameObject paintRTObj, MeshRenderer renderer, RenderTexture renderTexture, Material paintRTMat, Vector2Int rtClusterIdx) {
                this.paintRTObj = paintRTObj;
                this.renderer = renderer;
                this.renderTexture = renderTexture;
                //this.paintRTMat = paintRTMat;
                this.rtClusterIdx = rtClusterIdx;
            }

            // update renderTexture's position
            public void UpdateRTData(Vector2Int rtClusterIdx, int clusterSize) {
                this.rtClusterIdx = rtClusterIdx;
                paintRTObj.transform.position = new Vector3(
                    rtClusterIdx.x * clusterSize, 0, rtClusterIdx.y * clusterSize
                );
            }

            public void PaintRTData(Texture2D painter, Vector2Int paintPos, int paintScope) {
                Vector2Int paintStart = new Vector2Int(
                    Mathf.Clamp(paintPos.x - paintScope, 0, renderTexture.width - painter.width), 
                    Mathf.Clamp(paintPos.y - paintScope, 0, renderTexture.height - painter.height));
                painter.SetPixel(0, 0, Color.green);
                RenderTexture.active = renderTexture;
                Graphics.CopyTexture(
                    painter, 0, 0,
                    0, 0, painter.width, painter.height,
                    renderTexture, 0, 0,
                    paintStart.x, paintStart.y
                );

                var mpb = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(mpb);
                mpb.SetTexture("_MainTex", renderTexture);
                renderer.SetPropertyBlock(mpb);

                RenderTexture.active = null;
            }

            // this function will fill texture with white
            public void ResetRTData(Texture2D tmp) {
                Graphics.CopyTexture(tmp, 0, 0, 0, 0, renderTexture.width, renderTexture.height, renderTexture, 0, 0, 0, 0);
            }

            public void Dispose() {
                // return rt, destory the go
                RTPool.Release(renderTexture);
                renderTexture = null;
                DestroyImmediate(paintRTObj);
            }
        }


        [FoldoutGroup("河流编辑")]
        [LabelText("绘制范围")]
        [Tooltip("应当在3~5之间")]
        [MaxValue(10)]
        public ushort paintScope = 3;

        [FoldoutGroup("河流编辑")]
        [LabelText("绘制颜色")]
        public Color paintColor = Color.blue;       // 不要轻易地改这玩意啊！！！会影响绘制判定的！！！

        [FoldoutGroup("河流编辑")]
        [LabelText("背景颜色")]
        public Color bgColor = Color.white;

        [FoldoutGroup("河流编辑")]
        [LabelText("当前编辑 河流ID")]
        public ushort curEditRiverID = 9999;

        [FoldoutGroup("河流编辑")]
        [LabelText("RT相比cluster的缩放比例")]
        public ushort paintRTSizeScale = 4;

        [FoldoutGroup("河流编辑")]
        [LabelText("绘制-RT 材质")]
        public Material paintRTMat;

        [FoldoutGroup("河流编辑")]
        [LabelText("贝塞尔节点")]
        [Tooltip("其实随便拉个球，作为prefab 就行")]
        public GameObject signObj;

        [FoldoutGroup("河流编辑")]
        [LabelText("绘制-RT 列表")]
        public List<PaintRiverRTData> paintRTDatas;

        Dictionary<Vector2Int, PaintRiverRTData> paintRTDatasDict;

        //[FoldoutGroup("河流编辑")]
        //[LabelText("当前编辑河流的贝塞尔曲线")]
        //public 
            BezierCurve curHandleBezierCurve;


        [FoldoutGroup("河流编辑", 0)]
        [Button("重置编辑数据", ButtonSizes.Medium)]
        private void UpdateEditingRiverData() {
            if (paintRTDatas != null) {
                paintRTDatas.Clear();
            }
            if (paintRTDatasDict != null) {
                paintRTDatasDict.Clear();
            }
            // InitEditingRiverData
            foreach (var editingRiverData in editRiverDatas) {
                editingRiverData.InitEditingRiverData(ChooseRiverEvent, EditStartEvent,
                    GenCurveEvent, SaveCurveEvent, DeleteRiverEvent);
            }
            RiverPaintRTsTrans.ClearObjChildren();

            if (paintHelpTexture != null) {
                DestroyImmediate(paintHelpTexture);
            }
            curEditRiverID = 9999;

            curHandleBezierCurve = null;
        }

        #endregion


        #region 河流数据

        [Serializable]
        [InlineProperty]
        public class EditingRiverData : IDisposable {
            //Game
            public ushort riverID;

            public string riverName;

            public GameObject riverDataObj;

            [LabelText("河流存在的地块")]
            public List<Vector2Int> existTerrainClusterIDs;

            // TODO : 怎么真正地去存储河流的数据？？？不会还是要回归到贝塞尔曲线来吧？？？
            // TODO : 是不是应该 动态画纹理，然后每次切换地块纹理的时候把编辑的数据转为曲线，立刻放到EditRiverData里面？
            List<Vector2Int> riverDataPoints;

            Action<ushort, EditingRiverData> ChooseRiverEvent;
            Action<ushort, EditingRiverData> EditStartEvent;
            Action<ushort, EditingRiverData> GenCurveEvent;
            Action<ushort, EditingRiverData> SaveRiverEvent;
            Action<ushort, EditingRiverData> DeleteRiverEvent;

            public EditingRiverData(ushort riverID, GameObject riverDataObj,
                Action<ushort, EditingRiverData> chooseEvent, Action<ushort, EditingRiverData> editStartEvent,
                 Action<ushort, EditingRiverData> genCurveEvent, Action<ushort, EditingRiverData> saveEvent, Action<ushort, EditingRiverData> deleteEvent) {
                this.riverID = riverID;
                this.riverDataObj = riverDataObj;

                this.ChooseRiverEvent = chooseEvent;
                this.EditStartEvent = editStartEvent;
                this.GenCurveEvent = genCurveEvent;
                this.SaveRiverEvent = saveEvent;
                this.DeleteRiverEvent = deleteEvent;
            }

            public EditingRiverData(RiverData riverData, GameObject riverDataObj) {
                this.riverID = riverData.riverID;
                this.riverName = riverData.riverName;
                //this.curve = riverData.curve;
                this.riverDataObj = riverDataObj;
                this.existTerrainClusterIDs = riverData.existTerrainClusterIDs;
            }

            public void InitEditingRiverData(Action<ushort, EditingRiverData> chooseEvent, Action<ushort, EditingRiverData> editStartEvent,
                 Action<ushort, EditingRiverData> genCurveEvent, Action<ushort, EditingRiverData> saveEvent, Action<ushort, EditingRiverData> deleteEvent) {
                this.ChooseRiverEvent = chooseEvent;
                this.EditStartEvent = editStartEvent;
                this.GenCurveEvent = genCurveEvent;
                this.SaveRiverEvent = saveEvent;
                this.DeleteRiverEvent = deleteEvent;
            }

            [HorizontalGroup("RiverBtns"), Button("选中河流")]
            private void ChooseRiver() {
                ChooseRiverEvent.Invoke(riverID, this);
            }

            [HorizontalGroup("RiverBtns"), Button("编辑起点")]
            private void EditStart() {
                EditStartEvent.Invoke(riverID, this);
            }

            // NOTE : 开始我想使用 贝塞尔曲线 拟真河流 的制作流，但是发现了一个问题
            // 当河流有多段分支的时候，贝塞尔曲线也需要分段，这样的编辑对于河流密集的情况非常麻烦
            // 所以，为提醒后来者：若你确保河流不分段，可以生成贝塞尔曲线-保存河流
            // 若你觉得这套制作流麻烦，请直接使用保存纹理
            [HorizontalGroup("RiverBtns"), Button("生成河流曲线")]
            private void GenerateCurve()
            {
                GenCurveEvent.Invoke(riverID, this);
            }

            [HorizontalGroup("RiverBtns"), Button("保存河流曲线/纹理")]
            private void SaveRiver() {
                SaveRiverEvent.Invoke(riverID, this);
            }

            [HorizontalGroup("RiverBtns"), Button("删除河流")]
            private void DeleteRiver() {
                DeleteRiverEvent.Invoke(riverID, this);
            }

            public void Dispose() {
                DeleteRiverEvent = null;
                GenCurveEvent = null;
                SaveRiverEvent = null;
                EditStartEvent = null;
                ChooseRiverEvent = null;

                GameObject.DestroyImmediate(riverDataObj);
                existTerrainClusterIDs.Clear();
            }
        }

        [FoldoutGroup("河流数据")]
        [LabelText("当前河流数据制作流")]
        public RiverDataFlow CurRiverDataFlow = RiverDataFlow.RiverTexture;

        [FoldoutGroup("河流数据")]
        [LabelText("持久化河流容器")]
        public MapRiverData mapRiverData;

        [Space(10), FoldoutGroup("河流数据")]
        [LabelText("当前编辑的河流")]
        [Tooltip("仅存在于Editor中的数据，如果不保存则会在关闭时销毁")]
        public List<EditingRiverData> editRiverDatas;


        [FoldoutGroup("河流数据", 0)]
        [Button("添加河流数据", ButtonSizes.Medium)]
        private void AddNewRiver() {
            ushort riverID = mapRiverData.GetRiverID();
            GameObject riverObj = CreateRiverObj(riverID);

            EditingRiverData editingRiverData = new EditingRiverData(riverID, riverObj,
                ChooseRiverEvent, EditStartEvent, GenCurveEvent, SaveCurveEvent, DeleteRiverEvent);
            editRiverDatas.Add(editingRiverData);

            Debug.Log($"now you add a river, cur river cnt : {editRiverDatas.Count}");
        }

        private GameObject CreateRiverObj(ushort riverID) {
            GameObject riverObj = new GameObject($"River{riverID}");
            riverObj.transform.parent = RiverDatasTrans;
            return riverObj;
        }

        private void ChooseRiverEvent(ushort riverID, EditingRiverData riverData) {
            // 警告：如果你没有保存就选择了另一条河流编辑数据，那么之前的编辑会失效

            if (paintRTDatas == null) {
                paintRTDatas = new List<PaintRiverRTData>();
            }
            if (paintRTDatasDict == null) {
                paintRTDatasDict = new Dictionary<Vector2Int, PaintRiverRTData>();
            }

            // if this river is choosed, load all the exist cluster ID's Texture
            curEditRiverID = riverData.riverID;

            // we will hide other cluster fristly
            int clusterNum = riverData.existTerrainClusterIDs.Count;
            HashSet<Vector2Int> shouldShowCluster = new HashSet<Vector2Int>();
            for (int i = 0; i < clusterNum; i++) {
                shouldShowCluster.Add(riverData.existTerrainClusterIDs[i]);
            }
            int paintRTCount = paintRTDatas.Count;
            for (int i = paintRTCount - 1; i >= 0; i--) {
                if (!shouldShowCluster.Contains(paintRTDatas[i].rtClusterIdx)) {
                    paintRTDatas[i].Dispose();
                    paintRTDatas.RemoveAt(i);
                }
            }

            // we set the paint-river-texture as 1/4 of clusterSize
            int clusterSize = terSet.clusterSize;
            int showTexSize = clusterSize / paintRTSizeScale;

            RenderTextureDescriptor desc = new RenderTextureDescriptor() {
                width = showTexSize, height = showTexSize,
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                volumeDepth = 1, msaaSamples = 1, dimension = UnityEngine.Rendering.TextureDimension.Tex2D
            };
            Texture2D tmp = new Texture2D(showTexSize, showTexSize, TextureFormat.RGBA32, false);

            // create and init all paint texture
            for (int i = 0; i < clusterNum; i++) {

                NativeArray<Color> colors = new NativeArray<Color>(showTexSize * showTexSize, Allocator.TempJob);
                PaintHexDataTextureJob paintHexDataTextureJob = new PaintHexDataTextureJob() {
                    fillColor = bgColor,
                    datas = colors,
                };
                JobHandle job = paintHexDataTextureJob.Schedule(showTexSize * showTexSize, 16);
                job.Complete();
                tmp.SetPixels(colors.ToArray());
                tmp.Apply();
                colors.Dispose();

                // if cluster exist we do not need create a new RTData
                Vector2Int clusterIdx = riverData.existTerrainClusterIDs[i];
                if (paintRTDatasDict.ContainsKey(clusterIdx)) {
                    paintRTDatasDict[clusterIdx].ResetRTData(tmp);
                } else {
                    RenderTexture renderTexture = RTPool.Get(desc);
                    Graphics.CopyTexture(tmp, 0, 0, 0, 0, showTexSize, showTexSize, renderTexture, 0, 0, 0, 0);
                    CreatePaintRiverRTData(clusterIdx, renderTexture, clusterSize, paintRTMat);
                }
            }
            GameObject.DestroyImmediate(tmp);

            // TODO : you should set the rt's position!!
            foreach (var pair in paintRTDatasDict) {
                pair.Value.UpdateRTData(pair.Key, clusterSize);
            }

            Selection.activeGameObject = riverData.riverDataObj;
            Tools.current = Tool.None;
            SceneView.RepaintAll();

            // TODO : debug it!!!
        }

        private void CreatePaintRiverRTData(Vector2Int clusterIdx, RenderTexture renderTexture, int clusterSize, Material paintRiverMat) {
            GameObject paintRiverParentObj = new GameObject($"paintRiver{paintRTDatas.Count}");
            paintRiverParentObj.transform.parent = RiverPaintRTsTrans.transform;
            MeshFilter meshFilter = paintRiverParentObj.AddComponent<MeshFilter>();
            MeshRenderer renderer = paintRiverParentObj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = paintRiverMat;

            // create the mesh
            Vector3[] vertexs = new Vector3[4] {
                new Vector3(0, 0, 0), new Vector3(0, 0, clusterSize),
                new Vector3(clusterSize, 0, clusterSize), new Vector3(clusterSize, 0, 0)
            };
            int[] triangles = new int[] {
                0, 1, 2, 0, 2, 3
            };
            Vector2[] uvs = new Vector2[4] {
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
            };
            Mesh mesh = new Mesh();
            mesh.vertices = vertexs;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            meshFilter.sharedMesh = mesh;

            var block = new MaterialPropertyBlock();
            block.SetTexture("_MainTex", renderTexture);
            block.SetFloat("_GridNum", clusterSize / paintRTSizeScale);
            renderer.SetPropertyBlock(block);

            PaintRiverRTData paintRiverRTData = new PaintRiverRTData(paintRiverParentObj, renderer, renderTexture, paintRTMat, clusterIdx);

            paintRTDatas.Add(paintRiverRTData);
            paintRTDatasDict.Add(clusterIdx, paintRiverRTData);
        }


        private void EditStartEvent(ushort riverID, EditingRiverData riverData) 
        {
            // TODO : 这个功能要将切换 BrushColor设置 为 Start 的颜色
            // 然后把涂刷范围设置为0（只准刷一格，（起点只能有一个））
        }


        private void GenCurveEvent(ushort riverID, EditingRiverData riverData)
        {
            if (curEditRiverID != riverID)
            {
                Debug.LogError("please choose this river data before save it (only with which we can get river Edit data)");
                return;
            }

            List<Vector2Int> brushedPixelPoss = GetBrushedPixels();

            // TODO : 需要验证 贝塞尔曲线 的拟合程度
            BezierCurve bezierCurve = BezierCurve.FitCurve(brushedPixelPoss, 10);
            //TransBezierCurve(brushedPixelPoss, 10, brushedPixelPoss[0]);

            curHandleBezierCurve = bezierCurve;
        }

        private List<Vector2Int> GetBrushedPixels()
        {
            if (paintRTDatas == null || paintRTDatas.Count <= 0)
            {
                Debug.LogError("paint RT Datas is null, there is no paint data!");
                return null;
            }

            RenderTextureFormat rtFmt = paintRTDatas[0].renderTexture.format;
            TextureFormat fmt = rtFmt == RenderTextureFormat.ARGBHalf ? TextureFormat.RGBAHalf :
                                rtFmt == RenderTextureFormat.ARGBFloat ? TextureFormat.RGBAFloat :
                                TextureFormat.RGBA32;

            int texSize = terSet.clusterSize / paintRTSizeScale;
            Texture2D tmp = new Texture2D(texSize, texSize, fmt, false);
            List<Vector2Int> brushedPixelPoss = new List<Vector2Int>();

            // TODO : 将 startPos 作为第一个元素加入进去，这样才能确定河流的起点，河流起点会作为贝塞尔曲线的起点

            foreach (var paintRTData in paintRTDatas)
            {
                Vector2Int clusterStartPos = paintRTData.rtClusterIdx * terSet.clusterSize;
                RenderTexture.active = paintRTData.renderTexture;
                tmp.ReadPixels(new Rect(0, 0, texSize, texSize), 0, 0);
                tmp.Apply();

                Color[] colors = tmp.GetPixels();
                for (int i = 0; i < texSize; i++)
                {
                    for (int j = 0; j < texSize; j++)
                    {
                        int index = (texSize - 1 - j) * texSize + i;    //
                        if (colors[index] != bgColor)
                        {
                            Vector2Int curPixelPos = new Vector2Int(i, j) * paintRTSizeScale + clusterStartPos;
                            brushedPixelPoss.Add(curPixelPos);
                        }
                    }
                }
            }
            GameObject.DestroyImmediate(tmp);
            Debug.Log($"find painted pixel num : {brushedPixelPoss.Count}");
            if (brushedPixelPoss.Count <= 0)
            {
                Debug.LogError($"you do not paint!");
                return null;
            }
            return brushedPixelPoss;
        }


        private void SaveCurveEvent(ushort riverID, EditingRiverData riverData) 
        {
            if(curEditRiverID != riverID) {
                Debug.LogError("please choose this river data before save it (only with which we can get river Edit data)");
                return;
            }

            if (CurRiverDataFlow == RiverDataFlow.RiverTexture)
            {
                SaveRiverTexture(riverID, riverData);
            }
            else if (CurRiverDataFlow == RiverDataFlow.RiverBezier)
            {
                SaveRiverCurve(riverID, riverData);
            }
        }

        private void SaveRiverTexture(ushort riverID, EditingRiverData riverData)
        {
            List<Vector2Int> brushedPixels = GetBrushedPixels();
            RiverData saveRiverData = new RiverData(riverData.riverID, riverData.riverName, null, brushedPixels, riverData.existTerrainClusterIDs);
            mapRiverData.AddRiverData(saveRiverData);
            Debug.Log($"save river! id : {riverData.riverID}, name : {riverData.riverName}, generate river pixel num : {brushedPixels.Count}");
        }

        private void SaveRiverCurve(ushort riverID, EditingRiverData riverData)
        {
            if (curHandleBezierCurve == null)
            {
                Debug.LogError("you should generate curve for this river firstly");
                return;
            }

            RiverData saveRiverData = new RiverData(riverData.riverID, riverData.riverName, curHandleBezierCurve, null, riverData.existTerrainClusterIDs);
            mapRiverData.AddRiverData(saveRiverData);
            DrawBezierInScene(curHandleBezierCurve, riverData.riverName);
            Debug.Log($"save river! id : {riverData.riverID}, name : {riverData.riverName}, generate river bezier curve node num : {curHandleBezierCurve.Count}");
        }

        [Obsolete]
        private BezierCurve TransBezierCurve(List<Vector2Int> pixels, int nodeInterval, Vector2Int startPos) {
            BezierCurve curve = new BezierCurve();
            if (pixels == null || pixels.Count < 2)
                return curve;

            // 找到 startPos 在 pixels 中的最近索引
            int startIndex = 0;
            float minDist = float.MaxValue;
            for (int i = 0; i < pixels.Count; i++) {
                float dist = Vector2Int.Distance(pixels[i], startPos);
                if (dist < minDist) {
                    minDist = dist;
                    startIndex = i;
                }
            }

            // 保证顺序向前推进
            List<Vector2Int> orderedPixels = pixels.GetRange(startIndex, pixels.Count - startIndex);

            // 创建 BezierNodes 每隔 nodeInterval 点
            for (int i = 0; i < orderedPixels.Count; i += nodeInterval) {
                Vector2Int current = orderedPixels[i];
                Vector3 position = new Vector3(current.x, 0f, current.y);

                // 尝试计算前后方向向量作为 handle
                Vector3 handleIn = Vector3.zero;
                Vector3 handleOut = Vector3.zero;

                // 前一个点
                if (i - nodeInterval >= 0) {
                    Vector2Int prev = orderedPixels[i - nodeInterval];
                    Vector3 inDir = (position - new Vector3(prev.x, prev.y, 0)).normalized;
                    handleIn = -inDir * (nodeInterval / 2f);
                }

                // 后一个点
                if (i + nodeInterval < orderedPixels.Count) {
                    Vector2Int next = orderedPixels[i + nodeInterval];
                    Vector3 outDir = (new Vector3(next.x, 0, next.y) - position).normalized;
                    handleOut = outDir * (nodeInterval / 2f);
                }

                BezierNode node = new BezierNode(position, handleIn, handleOut);
                curve.Add(node);
            }

            return curve;
        }

        private void DrawBezierInScene(BezierCurve bezierCurve, string riverName) {

            GameObject bezierSignParent = new GameObject($"bezeierCurve_{riverName}");
            bezierSignParent.transform.parent = RiverBezierParentTrans;

            for(int i = 0; i < bezierCurve.Nodes.Count; i++) {
                GameObject signGo = Instantiate(signObj, bezierSignParent.transform);
                signGo.transform.position = bezierCurve.Nodes[i].position;
                signGo.name = $"bezierNode_{i}";
            }
            // the draw logic function is below this class code
        }


        private void DeleteRiverEvent(ushort riverID, EditingRiverData riverData) {
            riverData.Dispose();
            for (int i = editRiverDatas.Count - 1; i >=0; i--) {
                if (editRiverDatas[i].riverID == riverID) {
                    editRiverDatas.Remove(editRiverDatas[i]);
                    break;
                }
            }

            UpdateEditingRiverData();
        }


        [FoldoutGroup("河流数据")]
        [Button("加载河流数据", ButtonSizes.Medium)]
        private void LoadAllRiver() {
            Debug.Log("load river will override all editing data!");

            foreach (var riverData in mapRiverData.RiverDatas)
            {
                GameObject riverObj = CreateRiverObj(riverData.riverID);
                EditingRiverData editingRiverData = new EditingRiverData(riverData, riverObj);
                editingRiverData.InitEditingRiverData(ChooseRiverEvent, EditStartEvent, GenCurveEvent, SaveCurveEvent, DeleteRiverEvent);
                editRiverDatas.Add(editingRiverData);
            }

            Debug.Log($"now you load all rivers, cur river cnt : {editRiverDatas.Count}");
        }

        [FoldoutGroup("河流数据")]
        [Button("保存河流数据", ButtonSizes.Medium)]
        private void SaveAllRiver() {
            // TODO : save all to mapRiverData

            // TODO : 要在这个方法里面去生成贝塞尔曲线吗？
            foreach (var riverData in editRiverDatas)
            {
                //SaveRiverEvent(riverData.)
            }

        }

        #endregion


        #region 河流纹理

        [FoldoutGroup("河流纹理")]
        [Button("保存河流数据", ButtonSizes.Medium)]
        public string riverTexturePath = MapStoreEnum.RiverTexDataPath;

        struct GenRiverTexJob : IJobParallelFor
        {
            [ReadOnly] public Color brushColor;
            [ReadOnly] public int riverTexSize;
            [ReadOnly] public int paintRTSizeScale;
            [ReadOnly] public NativeArray<Vector2Int> riverPixels;

            [NativeDisableParallelForRestriction]
            [WriteOnly] public NativeArray<Color> riverTexData;

            public void Execute(int index)
            {
                Vector2Int pixelPos = riverPixels[index] / paintRTSizeScale;
                int idx = pixelPos.y * riverTexSize + pixelPos.x;
                riverTexData[idx] = Color.blue; // gray
            }
        }


        [FoldoutGroup("河流纹理")]
        [Button("导出河流数据为纹理", ButtonSizes.Medium)]
        public void GenRiverTexture()
        {
            int texSize = terSet.clusterSize * terSet.terrainSize.x / paintRTSizeScale;
            Texture2D riverTex = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            NativeArray<Color> riverTexData = new NativeArray<Color>(texSize * texSize, Allocator.Persistent);
            for (int i = 0; i < riverTexData.Length; i ++)
            {
                riverTexData[i] = Color.white;
            }
            foreach (var riverData in mapRiverData.RiverDatas)
            {
                int riverPixelSize = riverData.pixels.Count;
                NativeArray<Vector2Int> riverPixels = new NativeArray<Vector2Int>(riverPixelSize, Allocator.TempJob);
                //riverPixels.CopyTo(riverData.pixels.ToArray());
                for (int i = 0; i < riverPixelSize; i++) 
                {
                    riverPixels[i] = riverData.pixels[i];
                }

                Debug.Log(riverPixelSize);

                GenRiverTexJob genRiverTexJob = new GenRiverTexJob()
                {
                    brushColor = brushColor,
                    riverTexSize = texSize,
                    paintRTSizeScale = paintRTSizeScale,
                    riverPixels = riverPixels,
                    riverTexData = riverTexData
                };
                JobHandle jobHandle = genRiverTexJob.Schedule(riverPixelSize, 16);
                jobHandle.Complete();

                //foreach (var pixelPos in riverPixels)
                //{
                //    Vector2Int fixedPos = pixelPos / paintRTSizeScale;
                //    int idx = fixedPos.y * texSize + fixedPos.x;
                //    riverTexData[idx] = Color.blue; // gray
                //}

                riverPixels.Dispose();
            }
            riverTex.SetPixels(riverTexData.ToArray());
            riverTex.Apply();
            riverTexData.Dispose();

            string texName = GetRiverTexName();
            TextureUtility.GetInstance().SaveTextureAsAsset(riverTexturePath, texName, riverTex);
            GameObject.DestroyImmediate(riverTex);
        }

        private string GetRiverTexName()
        {
            int texSize = terSet.clusterSize * terSet.terrainSize.x / paintRTSizeScale;
            return string.Format("RvTexture_{0}_{1}x{1}_{2}", paintRTSizeScale, texSize, UnityEngine.Random.Range(0, 120));
        }

        #endregion


        Texture2D paintHelpTexture;

        public override void Enable() {
            base.Enable();

            if (RTPool == null) {
                RTPool = new RenderTexturePool();
            }

            BezierNode node;
        }

        public override void Destory() {
            base.Destory();

            if(paintHelpTexture != null) {
                GameObject.DestroyImmediate(paintHelpTexture);
            }

            // TODO : destory all river data;
            if (RTPool != null) {
                RTPool.Dispose();
            }
            RTPool = null;
        }

        public override void Disable() {
            base.Disable();

            // TODO : release all data ?

            if (paintHelpTexture != null) {
                GameObject.DestroyImmediate(paintHelpTexture);
            }

            if (RTPool != null) {
                RTPool.Dispose();
            }
            RTPool = null;
        }

        protected override void OnMouseDown(Event e) {
            if (!enableBrush) {
                return;
            }

            // only valid when lock scene view
            Vector3 worldPos = GetMousePosToScene(e);
            PaintRiverRT(worldPos);
            SceneView.RepaintAll();
        }

        protected override void OnMouseDrag(Event e) {
            if (!enableBrush) {
                return;
            }

            // only valid when lock scene view
            Vector3 worldPos = GetMousePosToScene(e);
            PaintRiverRT(worldPos);
            SceneView.RepaintAll();
        }

        protected override void HandleSceneDraw() {
            if(curHandleBezierCurve == null) {
                return;
            }

            int count = curHandleBezierCurve.Nodes.Count;
            if (count < 2) {
                return;
            }
            
            for(int i = 0; i < count - 1; i++) {
                Vector3 startPoint = curHandleBezierCurve.Nodes[i].position;
                Vector3 startTangent = curHandleBezierCurve.Nodes[i].handleIn;
                Vector3 endPoint = curHandleBezierCurve.Nodes[i + 1].position;
                Vector3 endTangent = curHandleBezierCurve.Nodes[i].handleOut;
                Handles.DrawBezier(startPoint, endPoint,
                    startTangent, endTangent, Color.blue, null, 2f);
            }
        }

        private void PaintRiverRT(Vector3 worldPos) {
            int clusterSize = terSet.clusterSize;
            Vector3Int FixedWorldPos = new Vector3Int((int)worldPos.x, 0, (int)worldPos.z);

            int clusterIdxX = FixedWorldPos.x / clusterSize;
            int clusterIdxY = FixedWorldPos.z / clusterSize;

            HashSet<Vector2Int> curShowRTIdx = new HashSet<Vector2Int>();
            foreach (var paintRTData in paintRTDatas)
            {
                curShowRTIdx.Add(paintRTData.rtClusterIdx);
            }

            Vector2Int curPaintClsIdx = new Vector2Int(clusterIdxX, clusterIdxY);
            if (!curShowRTIdx.Contains(curPaintClsIdx)) {
                return;
            }

            // if paint scope has been changed, then renew the texture
            int curSize = paintScope * 2 + 1;
            if (paintHelpTexture == null) {
                CreatePaintTexture(curSize);
            } else {
                int size = paintHelpTexture.width;
                if (size != curSize) {
                    CreatePaintTexture(curSize);
                }
            }
            
            // if paint position is in cur Cluster
            int rtSize = clusterSize / paintRTSizeScale;
            float innerRatioX = (float)(FixedWorldPos.x % clusterSize) / clusterSize;
            float innerRatioY = (float)(FixedWorldPos.z % clusterSize) / clusterSize;

            int paintX = Mathf.Clamp((int)(innerRatioX * rtSize), 0, rtSize - 1);
            int paintY = Mathf.Clamp((int)(innerRatioY * rtSize), 0, rtSize - 1);       // curSize

            Vector2Int paintPos = new Vector2Int(paintX, paintY);
            if (paintRTDatasDict.ContainsKey(curPaintClsIdx)) {
                paintRTDatasDict[curPaintClsIdx].PaintRTData(paintHelpTexture, paintPos, paintScope);
            } else {
                Debug.LogError($"dic do not contains : {curPaintClsIdx}");
            }
            
            Debug.Log($"world pos : {worldPos}, paint cluster : {curPaintClsIdx}, paint area : {curSize}x{curSize}, paint pos : {paintPos}, paint ratio : ({innerRatioX},{innerRatioY}), paint target RT size : {rtSize}");
        }

        private void CreatePaintTexture(int curSize) {
            if (paintHelpTexture != null) {
                GameObject.DestroyImmediate(paintHelpTexture);
            }
            paintHelpTexture = new Texture2D(curSize, curSize, TextureFormat.RGBA32, false, false);
            Color[] colors = new Color[curSize * curSize];
            for (int i = 0; i < curSize * curSize; i++) {
                colors[i] = paintColor;
            }
            paintHelpTexture.SetPixels(colors);
            paintHelpTexture.Apply();
        }

    }
}
