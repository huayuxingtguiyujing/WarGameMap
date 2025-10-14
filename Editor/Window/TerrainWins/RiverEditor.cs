using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.Rendering;
using ReadOnlyAttribute = Sirenix.OdinInspector.ReadOnlyAttribute;
using Unity.Mathematics;
using System.ComponentModel;

namespace LZ.WarGameMap.MapEditor
{
    public struct PaintRiverTextureJob : IJobParallelFor
    {
        [ReadOnly] public int rvTexSize;
        [ReadOnly] public Color fillColor;
        [ReadOnly] public Color brushColor;
        [NativeDisableParallelForRestriction]
        [ReadOnly] public NativeHashSet<int2> paintedPixels;

        [WriteOnly] public NativeArray<Color> datas;

        public void Execute(int index)
        {
            int i = index / rvTexSize;
            int j = index % rvTexSize;
            int2 curIdx = new int2 { x = i, y = j };
            if(paintedPixels.Contains(curIdx))
            {
                datas[index] = brushColor;
            }
            else
            {
                datas[index] = fillColor;
            }
        }
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


        #region ��ǰ�����༭

        [Serializable]
        public class PaintRiverRTData : IDisposable {

            [HorizontalGroup("PaintRiverRTData"), LabelText("RT����"), ReadOnly]
            public GameObject paintRTObj;

            MeshRenderer renderer;

            [HorizontalGroup("PaintRiverRTData"), LabelText("RT�ʲ�"), ReadOnly]
            public RenderTexture renderTexture;

            [HorizontalGroup("PaintRiverRTData"), LabelText("RT���"), ReadOnly]
            public Vector2Int rtClusterIdx;

            public PaintRiverRTData(GameObject paintRTObj, MeshRenderer renderer, RenderTexture renderTexture, Vector2Int rtClusterIdx) {
                this.paintRTObj = paintRTObj;
                this.renderer = renderer;
                this.renderTexture = renderTexture;
                this.rtClusterIdx = rtClusterIdx;
            }

            // Update renderTexture's position
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

        public enum PaintMode
        {
            SetStart,
            Paint,
            Erase
        }

        //[FoldoutGroup("�����༭")]
        //[LabelText("���Ʒ�Χ")]
        //[Tooltip("Ӧ����3~5֮��")]
        //[MaxValue(10)]    // public 
        // NOTE : ������������Կ���Ϳˢ��Χ�ģ����������ƺ�̫�鷳�����������Ҳû��Ҫ�棬����Ϳһ�����Ӿͺ���...
        // NOTE : �����Ŀ��Ҫ����ȥ����
        ushort paintScope = 0;

        [FoldoutGroup("�����༭")]
        [LabelText("����ģʽ")]
        [OnValueChanged("OnPaintModeChanged")]
        public PaintMode paintMode = PaintMode.Paint;

        Color paintRiverColor = RiverPaintColor.riverColor;       // ��Ҫ���׵ظ������Ⱑ��������Ӱ������ж��ģ�����

        Color bgColor = RiverPaintColor.noRvColor;

        private void OnPaintModeChanged()
        {
            switch (paintMode)
            {
                case PaintMode.SetStart:
                    paintScope = 0;
                    paintRiverColor = RiverPaintColor.rvStartColor;
                    break;
                case PaintMode.Paint:
                    paintScope = 0;
                    paintRiverColor = RiverPaintColor.riverColor;
                    break;
                case PaintMode.Erase:
                    paintScope = 8;
                    paintRiverColor = RiverPaintColor.noRvColor;
                    break;
            }
        }

        [FoldoutGroup("�����༭")]
        [LabelText("��ǰ�༭ ����ID")]
        public ushort curEditRiverID = 9999;

        [FoldoutGroup("�����༭")]
        [LabelText("����-RT ����")]
        public Material paintRTMat;

        [FoldoutGroup("�����༭")]
        [LabelText("�������ڵ�")]
        [Tooltip("��ʵ�����������Ϊprefab ����")]
        public GameObject signObj;

        [FoldoutGroup("�����༭")]
        [LabelText("����-RT �б�")]
        public List<PaintRiverRTData> paintRTDatas;

        Dictionary<Vector2Int, PaintRiverRTData> paintRTDatasDict;


        BezierCurveEditor curBezierCurveEditor;

        [FoldoutGroup("�����༭", 0)]
        [Button("���ñ༭����", ButtonSizes.Medium)]
        private void ResetEditingRiverData() {
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

            if(curBezierCurveEditor != null)
            {
                curBezierCurveEditor.Dispose();
            }
            curBezierCurveEditor = null;
            RiverBezierParentTrans.ClearObjChildren();

            //RiverDataParentTrans.ClearObjChildren();
            UpdateEditingRvDataDict();
        }

        private void UpdateEditingRvDataDict()
        {
            if(editRiverDataDict == null)
            {
                editRiverDataDict = new Dictionary<ushort, EditingRiverData>();
            }
            editRiverDataDict.Clear();
            if(editRiverDatas != null)
            {
                foreach (var editRvData in editRiverDatas)
                {
                    editRiverDataDict.Add(editRvData.riverID, editRvData);
                }
            }
        }

        [FoldoutGroup("�����༭", 0)]
        [Button("ͬ�����߳����ڵ㵽����", ButtonSizes.Medium)]
        private void SyncToBezierCurve()
        {
            if(curBezierCurveEditor == null)
            {
                Debug.LogError("bezier curve editor is null!");
                return;
            }
            curBezierCurveEditor.SyncToBezierCurve();
        }

        #endregion


        #region ��������

        [Serializable]
        [InlineProperty]
        public class EditingRiverData : IDisposable 
        {
            [HorizontalGroup("PaintRiverRTData"), LabelText("����ID")]
            public ushort riverID;

            [HorizontalGroup("PaintRiverRTData"), LabelText("��������")]
            public string riverName;

            [HorizontalGroup("PaintRiverRTData"), LabelText("����obj"), ReadOnly]
            public GameObject riverDataObj;

            [LabelText("�������")]
            public RiveStartData riverStart = MapRiverData.UnvalidRvStart;

            [LabelText("�������ڵĵؿ�")]
            public List<Vector2Int> existTerrainClusterIDs;

            Action<ushort, EditingRiverData> ChooseRiverEvent;
            Action<ushort, EditingRiverData> EditStartEvent;
            Action<ushort, EditingRiverData> GenCurveEvent;
            Action<ushort, EditingRiverData> SaveRiverEvent;
            Action<ushort, EditingRiverData> DeleteRiverEvent;

            public EditingRiverData(ushort riverID, GameObject riverDataObj, RiveStartData riverStart,
                Action<ushort, EditingRiverData> chooseEvent, Action<ushort, EditingRiverData> editStartEvent,
                 Action<ushort, EditingRiverData> genCurveEvent, Action<ushort, EditingRiverData> saveEvent, Action<ushort, EditingRiverData> deleteEvent) {
                this.riverID = riverID;
                this.riverDataObj = riverDataObj;
                this.riverStart = riverStart;

                this.ChooseRiverEvent = chooseEvent;
                this.EditStartEvent = editStartEvent;
                this.GenCurveEvent = genCurveEvent;
                this.SaveRiverEvent = saveEvent;
                this.DeleteRiverEvent = deleteEvent;
            }

            public EditingRiverData(RiverData riverData, GameObject riverDataObj, RiveStartData riverStart) {
                this.riverID = riverData.riverID;
                this.riverName = riverData.riverName;
                this.riverStart = riverStart;
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

            [HorizontalGroup("RiverBtns"), Button("ѡ�к���")]
            private void ChooseRiver() {
                ChooseRiverEvent.Invoke(riverID, this);
            }

            [HorizontalGroup("RiverBtns"), Button("�༭��㣨��ʱû�ã�")]
            private void EditStart() {
                EditStartEvent.Invoke(riverID, this);
            }

            // NOTE : ��ʼ����ʹ�� ���������� ������� �������������Ƿ�����һ������
            // �������ж�η�֧��ʱ�򣬱���������Ҳ��Ҫ�ֶΣ������ı༭���ں����ܼ�������ǳ��鷳
            // ���ԣ�Ϊ���Ѻ����ߣ�����ȷ���������ֶΣ��������ɱ���������-�������
            // ������������������鷳����ֱ��ʹ�ñ�������
            [HorizontalGroup("RiverBtns"), Button("���ɺ�������")]
            private void GenerateCurve()
            {
                GenCurveEvent.Invoke(riverID, this);
            }

            [HorizontalGroup("RiverBtns"), Button("�����������/����")]
            private void SaveRiver() {
                SaveRiverEvent.Invoke(riverID, this);
            }

            [HorizontalGroup("RiverBtns"), Button("ɾ������")]
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

        [FoldoutGroup("��������")]
        [LabelText("��ǰ��������������")]
        public RiverDataFlow CurRiverDataFlow = RiverDataFlow.Texture;

        [FoldoutGroup("��������")]
        [LabelText("�־û���������")]
        public MapRiverData mapRiverData;

        [Space(10), FoldoutGroup("��������")]
        [LabelText("��ǰ�༭�ĺ���")]
        [Tooltip("��������Editor�е����ݣ��������������ڹر�ʱ����")]
        public List<EditingRiverData> editRiverDatas;

        Dictionary<ushort, EditingRiverData> editRiverDataDict;


        [FoldoutGroup("��������", 0)]
        [Button("��Ӻ�������", ButtonSizes.Medium)]
        private void AddNewRiver() {
            ushort riverID = mapRiverData.GetRiverID();
            GameObject riverObj = CreateRiverObj(riverID);

            EditingRiverData editingRiverData = new EditingRiverData(riverID, riverObj, MapRiverData.UnvalidRvStart,
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
            // ���棺�����û�б����ѡ������һ�������༭���ݣ���ô֮ǰ�ı༭��ʧЧ
            ResetEditingRiverData();

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
            int showTexSize = clusterSize / terSet.paintRTSizeScale;

            RenderTextureDescriptor desc = new RenderTextureDescriptor() {
                width = showTexSize, height = showTexSize,
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                volumeDepth = 1, msaaSamples = 1, dimension = UnityEngine.Rendering.TextureDimension.Tex2D
            };
            Texture2D tmp = new Texture2D(showTexSize, showTexSize, TextureFormat.RGBA32, false);

            // create and init all paint texture, load all painted data
            mapRiverData.UpdateMapRiverData();
            RiverData runtimingRvData = mapRiverData.GetRiverData(riverID);
            bool hasSavedThisRvData = runtimingRvData != null;
            if (hasSavedThisRvData)
            {
                runtimingRvData.UpdateClusterPixelDict(terSet.clusterSize, terSet.paintRTSizeScale);
            }
            int hasPaintedPixelNum = 0;
            for (int i = 0; i < clusterNum; i++) 
            {
                // get the pixel that has been painted. they existed in mapRiverData-RiverData, not EditingRiverData
                Vector2Int curClsID = riverData.existTerrainClusterIDs[i];
                List<Vector2Int> paintedPixel;
                if (hasSavedThisRvData)
                {
                    paintedPixel = runtimingRvData.GetPaintedClsPixles(curClsID);
                }
                else
                {
                    paintedPixel = new List<Vector2Int>() { new Vector2Int(-1, -1) };
                }
                // NOTE : paintedPixel's data scope : [0, showTexSize], [0, showTexSize]
                NativeHashSet<int2> paintedPixelsSets = new NativeHashSet<int2>(paintedPixel.Count, Allocator.TempJob);
                for (int j = 0; j < paintedPixel.Count; j++)
                {
                    paintedPixelsSets.Add(new int2(paintedPixel[j].x, paintedPixel[j].y));
                }
                hasPaintedPixelNum += paintedPixel.Count;

                NativeArray<Color> colors = new NativeArray<Color>(showTexSize * showTexSize, Allocator.TempJob);
                PaintRiverTextureJob paintRiverTextureJob = new PaintRiverTextureJob() {
                    rvTexSize = showTexSize,
                    brushColor = RiverPaintColor.riverColor,
                    fillColor = RiverPaintColor.noRvColor,
                    paintedPixels = paintedPixelsSets,
                    datas = colors,
                };
                JobHandle job = paintRiverTextureJob.Schedule(showTexSize * showTexSize, 16);
                job.Complete();
                tmp.SetPixels(colors.ToArray());
                tmp.Apply();
                colors.Dispose();
                paintedPixelsSets.Dispose();

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
            Debug.Log($"choose river id : {riverID}, expected add {hasPaintedPixelNum} painted pixels, saved this RvData {hasSavedThisRvData}");
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
            block.SetFloat("_GridNum", clusterSize / terSet.paintRTSizeScale);
            renderer.SetPropertyBlock(block);

            PaintRiverRTData paintRiverRTData = new PaintRiverRTData(paintRiverParentObj, renderer, renderTexture, clusterIdx);

            paintRTDatas.Add(paintRiverRTData);
            paintRTDatasDict.Add(clusterIdx, paintRiverRTData);
        }

        [Obsolete]
        private void EditStartEvent(ushort riverID, EditingRiverData riverData) 
        {
            // TODO : �������Ҫ���л� BrushColor���� Ϊ Start ����ɫ
            // Ȼ���Ϳˢ��Χ����Ϊ0��ֻ׼ˢһ�񣬣����ֻ����һ������
        }


        private void GenCurveEvent(ushort riverID, EditingRiverData riverData)
        {
            if (curEditRiverID != riverID)
            {
                Debug.LogError("please choose this river data before save it (only with which we can get river Edit data)");
                return;
            }

            List<Vector2Int> brushedPixelPoss = GetBrushedPixels();

            // TODO : ��Ҫ��֤ ���������� ����ϳ̶�
            //BezierCurve bezierCurve = BezierCurve.GenCurve(brushedPixelPoss, riverData.riverStart, terSet);
            BezierCurve bezierCurve = BezierCurve.FitCurve(brushedPixelPoss, riverData.riverStart, terSet);
            curBezierCurveEditor = CreateBezierEditor(bezierCurve);
            Debug.Log($"gen over, curve node num : {bezierCurve.Count}, pixel num {brushedPixelPoss.Count}");
        }

        // NOTE : this function return worldPos not editing pixelPos
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

            int texSize = terSet.clusterSize / terSet.paintRTSizeScale;
            Texture2D tmp = new Texture2D(texSize, texSize, fmt, false);
            int brushedPixelNum = 0;
            List<Vector2Int> brushedPixelPoss = new List<Vector2Int>();

            foreach (var paintRTData in paintRTDatas)
            {
                //Vector2Int clusterStartPos = paintRTData.rtClusterIdx * terSet.clusterSize;
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
                            //Vector2Int curPixelPos = new Vector2Int(i, j) * terSet.paintRTSizeScale + clusterStartPos;
                            Vector2Int curPixelPos = MapRiverData.GetMapPosByRvEditPixel(new Vector2Int(i, j), 
                                terSet.clusterSize, terSet.paintRTSizeScale, paintRTData.rtClusterIdx);
                            brushedPixelPoss.Add(curPixelPos);
                            brushedPixelNum++;
                        }
                    }
                }
            }
            GameObject.DestroyImmediate(tmp);
            Debug.Log($"find painted pixel num : {brushedPixelNum}");
            if (brushedPixelNum <= 0)
            {
                Debug.LogError($"you do not paint!");
            }
            return brushedPixelPoss;
        }


        private BezierCurveEditor CreateBezierEditor(BezierCurve bezierCurve)
        {
            GameObject go = new GameObject("curEditor");
            BezierCurveEditor bezierCurveEditor = go.AddComponent<BezierCurveEditor>();
            bezierCurveEditor.InitCurveEditer(bezierCurve, terSet.paintRTSizeScale / 2);
            go.transform.SetParent(RiverBezierParentTrans);
            return bezierCurveEditor;
        }


        private void SaveCurveEvent(ushort riverID, EditingRiverData riverData) 
        {
            if(curEditRiverID != riverID) {
                Debug.LogError("please choose this river data before save it (only with which we can get river Edit data)");
                return;
            }

            // Texture should be saved, and curve should be saved too
            if (curBezierCurveEditor == null)
            {
                Debug.LogError("you should generate curve for this river firstly");
                return;
            }

            List<Vector2Int> brushedPixels = GetBrushedPixels();
            RiverData saveRiverData = new RiverData(riverData.riverID, riverData.riverName, curBezierCurveEditor.Curve, brushedPixels, riverData.existTerrainClusterIDs);
            saveRiverData.riverStart = riverData.riverStart;
            mapRiverData.AddRiverData(saveRiverData);

            //DrawBezierInScene(curBezierCurveEditor.Curve, riverData.riverName);
            Debug.Log($"save river! id : {riverData.riverID}, name : {riverData.riverName}, generate river bezier curve node num : {curBezierCurveEditor.Curve.Count}, river pixel num : {brushedPixels.Count}");

            EditorUtility.SetDirty(mapRiverData);
            AssetDatabase.Refresh();
        }

        private void DeleteRiverEvent(ushort riverID, EditingRiverData riverData) {
            riverData.Dispose();
            for (int i = editRiverDatas.Count - 1; i >=0; i--) {
                if (editRiverDatas[i].riverID == riverID) {
                    editRiverDatas.Remove(editRiverDatas[i]);
                    break;
                }
            }

            ResetEditingRiverData();
        }


        [FoldoutGroup("��������")]
        [Button("���غ�������", ButtonSizes.Medium)]
        private void LoadAllRiver() {
            Debug.Log("load river will override all editing data!");

            for(int i = 0; i < editRiverDatas.Count; i++)
            {
                editRiverDatas[i].Dispose();
            }
            editRiverDatas.Clear();

            foreach (var riverData in mapRiverData.RiverDatas)
            {
                GameObject riverObj = CreateRiverObj(riverData.riverID);
                EditingRiverData editingRiverData = new EditingRiverData(riverData, riverObj, riverData.riverStart);
                editingRiverData.InitEditingRiverData(ChooseRiverEvent, EditStartEvent, GenCurveEvent, SaveCurveEvent, DeleteRiverEvent);
                editRiverDatas.Add(editingRiverData);
            }

            UpdateEditingRvDataDict();

            Debug.Log($"now you load all rivers, cur river cnt : {editRiverDatas.Count}");
        }

        [FoldoutGroup("��������")]
        [Button("����������ݣ���ʱû�ã�", ButtonSizes.Medium)]
        private void SaveAllRiver() {
            // TODO : save all to mapRiverData

            // TODO : Ҫ�������������ȥ���ɱ�����������
            foreach (var riverData in editRiverDatas)
            {
                //SaveRiverEvent(riverData.)
            }

        }

        #endregion


        #region ��������

        [FoldoutGroup("��������")]
        [Button("�����������", ButtonSizes.Medium), ReadOnly]
        public string riverTexturePath = MapStoreEnum.RiverTexDataPath;

        


        [FoldoutGroup("��������")]
        [Button("������������Ϊ����", ButtonSizes.Medium)]
        [Tooltip("����ѡ���RiverWorkFlow������ε���")]
        public void GenRiverTexture()
        {
            int texSize = terSet.clusterSize * terSet.terrainSize.x / terSet.paintRTSizeScale;
            Texture2D riverTex = mapRiverData.GenRiverTexture(CurRiverDataFlow, terSet);

            string texName = GetRiverTexName();
            TextureUtility.GetInstance().SaveTextureAsAsset(riverTexturePath, texName, riverTex);
            GameObject.DestroyImmediate(riverTex);
        }

        private string GetRiverTexName()
        {
            int texSize = terSet.clusterSize * terSet.terrainSize.x / terSet.paintRTSizeScale;
            return string.Format("RvTexture_{0}_{1}x{1}_{2}_{3}", terSet.paintRTSizeScale, texSize, CurRiverDataFlow.ToString(), UnityEngine.Random.Range(0, 120));
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
            if(curBezierCurveEditor == null) {
                return;
            }// curBezierCurveEditor

            int count = curBezierCurveEditor.Curve.Count;
            if (count < 2) {
                return;
            }
            
            for(int i = 0; i < count - 1; i++) {
                Vector3 startPoint = curBezierCurveEditor.Curve.Nodes[i].position;
                Vector3 startTangent = curBezierCurveEditor.Curve.Nodes[i].handleOut;
                Vector3 endPoint = curBezierCurveEditor.Curve.Nodes[i + 1].position;
                Vector3 endTangent = curBezierCurveEditor.Curve.Nodes[i + 1].handleIn;
                Handles.DrawBezier(startPoint, endPoint,
                    startTangent, endTangent, Color.blue, null, 2f);
            }
        }

        private PaintMode preMode;

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
            if (!curShowRTIdx.Contains(curPaintClsIdx)) 
            {
                return;
            }

            // if paint scope has been changed, then renew the texture
            int curSize = paintScope * 2 + 1;
            bool paintModeChanged = preMode != paintMode;
            if (paintHelpTexture == null || paintModeChanged) 
            {
                CreatePaintTexture(curSize);
            } 
            else 
            {
                int size = paintHelpTexture.width;
                if (size != curSize) {
                    CreatePaintTexture(curSize);
                }
            }
            preMode = paintMode;

            // if paint position is in cur Cluster
            int rtSize = clusterSize / terSet.paintRTSizeScale;
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


            if (paintMode == PaintMode.SetStart)
            {
                editRiverDataDict[curEditRiverID].riverStart = new RiveStartData(curPaintClsIdx, paintPos);
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
                colors[i] = paintRiverColor;
            }
            paintHelpTexture.SetPixels(colors);
            paintHelpTexture.Apply();
        }

    }
}
