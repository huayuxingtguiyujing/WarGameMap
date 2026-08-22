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
using System.Security.Cryptography;
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

            paintRTMat = EditorSceneManager.TerMaterial;
            signObj = EditorSceneManager.SignObj;

            InitRvTransforms();

            // init mapRiverData, it will always bind with cur TerrainSettingSO and HexSettingSO
            FindOrCreateSO<MapRiverData>(ref mapRiverData, MapStoreEnum.RiverDataPath, "MapRiverData_Default.asset");
            mapRiverData.InitMapRiverData(terSet, hexSet);

            editRiverDatas = new List<EditingRiverData>();

            LoadAllRiver();
        }

        // TODO : Move to sceneManager
        private void InitRvTransforms()
        {
            RiverDataParentTrans = EditorSceneManager.mapScene.riverDataParentObj.transform;

            // create other trans
            if (RiverDatasTrans == null)
            {
                RiverDatasTrans = RiverDataParentTrans.Find(RiverEditorNames.RiverDatas);
                if (RiverDatasTrans == null)
                {
                    GameObject go = new GameObject(RiverEditorNames.RiverDatas);
                    RiverDatasTrans = go.transform;
                }
            }
            RiverDatasTrans.parent = RiverDataParentTrans;

            if (RiverPaintRTsTrans == null)
            {
                RiverPaintRTsTrans = RiverDataParentTrans.Find(RiverEditorNames.RiverPaintRTs);
                if (RiverPaintRTsTrans == null)
                {
                    GameObject go = new GameObject(RiverEditorNames.RiverPaintRTs);
                    RiverPaintRTsTrans = go.transform;
                }
            }
            RiverPaintRTsTrans.parent = RiverDataParentTrans;

            if (RiverBezierParentTrans == null)
            {
                RiverBezierParentTrans = RiverDataParentTrans.Find(RiverEditorNames.RiverBezierParent);
                if (RiverBezierParentTrans == null)
                {
                    GameObject go = new GameObject(RiverEditorNames.RiverBezierParent);
                    RiverBezierParentTrans = go.transform;
                }
            }
            RiverBezierParentTrans.parent = RiverDataParentTrans;
        }

        //[FoldoutGroup("河流数据")]
        //[Button("加载河流数据", ButtonSizes.Medium)]
        private void LoadAllRiver()
        {
            Debug.Log("load river will override all editing data!");

            for (int i = 0; i < editRiverDatas.Count; i++)
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


        static RenderTexturePool RTPool;


        #region 河流编辑（新）

        [Serializable]
        public class PaintRiverRTData : IDisposable
        {

            [HorizontalGroup("PaintRiverRTData"), LabelText("RT物体"), ReadOnly]
            public GameObject paintRTObj;

            MeshRenderer renderer;

            [HorizontalGroup("PaintRiverRTData"), LabelText("RT资产"), ReadOnly]
            public RenderTexture renderTexture;

            [HorizontalGroup("PaintRiverRTData"), LabelText("RT序号"), ReadOnly]
            public Vector2Int rtClusterIdx;

            public PaintRiverRTData(GameObject paintRTObj, MeshRenderer renderer, RenderTexture renderTexture, Vector2Int rtClusterIdx)
            {
                this.paintRTObj = paintRTObj;
                this.renderer = renderer;
                this.renderTexture = renderTexture;
                this.rtClusterIdx = rtClusterIdx;
            }

            // Update renderTexture's position
            public void UpdateRTData(Vector2Int rtClusterIdx, int clusterSize)
            {
                this.rtClusterIdx = rtClusterIdx;
                paintRTObj.transform.position = new Vector3(
                    rtClusterIdx.x * clusterSize, 0, rtClusterIdx.y * clusterSize
                );
            }

            public void PaintRTData(Texture2D painter, Vector2Int paintPos, int paintScope)
            {
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

            // This function will fill texture with white
            public void ResetRTData(Texture2D tmp)
            {
                Graphics.CopyTexture(tmp, 0, 0, 0, 0, renderTexture.width, renderTexture.height, renderTexture, 0, 0, 0, 0);
            }

            public void Dispose()
            {
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

        [FoldoutGroup("河流编辑")]
        [ShowIf("showAllField")]
        [LabelText("绘制范围")]
        [ReadOnly]
        [Range(0, 50)]
        public ushort paintScope = 0;

        [FoldoutGroup("河流编辑")]
        [LabelText("绘制模式")]
        [OnValueChanged("OnPaintModeChanged")]
        public PaintMode paintMode = PaintMode.Paint;

        Color paintRiverColor = RiverPaintColor.riverColor;       // 不要轻易地改这玩意啊！！！会影响绘制判定的！！！

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
                    paintScope = 4;
                    paintRiverColor = RiverPaintColor.riverColor;
                    break;
                case PaintMode.Erase:
                    paintScope = 8;
                    paintRiverColor = RiverPaintColor.noRvColor;
                    break;
            }
        }


        [FoldoutGroup("河流编辑")]
        [ShowIf("showAllField")]
        [LabelText("当前编辑 河流ID"), ReadOnly]
        public ushort curEditRiverID = 9999;

        // TODO : 直接从 Editor Scene Manager 里获取
        [FoldoutGroup("河流编辑")]
        [ShowIf("showAllField")]
        [LabelText("绘制-RT 材质")]
        public Material paintRTMat;

        // TODO : 直接从 Editor Scene Manager 里获取
        [FoldoutGroup("河流编辑")]
        [ShowIf("showAllField")]
        [LabelText("贝塞尔节点")]
        [Tooltip("其实随便拉个球，作为prefab 就行")]
        public GameObject signObj;

        [FoldoutGroup("河流编辑")]
        [LabelText("河流编辑地块 Idx 左下角")]
        public Vector2Int riverEditStartIdx;

        [FoldoutGroup("河流编辑")]
        [LabelText("河流编辑地块 Idx 右上角")]
        public Vector2Int riverEditEndIdx;

        [FoldoutGroup("河流编辑")]
        [ShowIf("showAllField")]
        [LabelText("河流编辑地块 RT 实例"), ReadOnly]
        public List<PaintRiverRTData> paintRTDatas = new List<PaintRiverRTData>();

        Dictionary<Vector2Int, PaintRiverRTData> paintRTDatasDict = new Dictionary<Vector2Int, PaintRiverRTData>();

        [Obsolete]
        BezierCurveEditor curBezierCurveEditor;


        [FoldoutGroup("河流编辑", 0)]
        [Button("生成编辑地块", ButtonSizes.Medium)]
        private void GenRiverEditCluster()
        {
            if (riverEditEndIdx.y < riverEditStartIdx.y || riverEditEndIdx.x < riverEditStartIdx.x)
            {
                Debug.LogError($"Wrong index : left down idx {riverEditStartIdx}, right up idx {riverEditEndIdx}");
                return;
            }

            int width = riverEditEndIdx.x - riverEditStartIdx.x + 1;
            int height = riverEditEndIdx.y - riverEditStartIdx.y + 1;
            List<Vector2Int> clusterIdxs = new List<Vector2Int>(width * height);

            for(int i = 0; i < width; i++)
            {
                for(int j = 0; j < height; j++)
                {
                    clusterIdxs.Add(new Vector2Int(riverEditStartIdx.x + i, riverEditStartIdx.y + j));
                }
            }

            CreatePaintRiverRTData_ClusterIdxs(clusterIdxs);
        }

        private void CreatePaintRiverRTData_ClusterIdxs(List<Vector2Int> clusterIdxs)
        {
            // NOTE : 原本的计划 paint-river-texture 是 clusterSize 的 1/4
            // NOTE : 目前 showTexSize 与 clusterSize 大小一致，所以 terSet.paintRTSizeScale 字段可以移除
            int clusterSize = terSet.clusterSize;
            int showTexSize = clusterSize / terSet.paintRTSizeScale;

            RenderTextureDescriptor desc = new RenderTextureDescriptor()
            {
                width = showTexSize,
                height = showTexSize,
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                volumeDepth = 1,
                msaaSamples = 1,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D
            };
            Texture2D tmp = new Texture2D(showTexSize, showTexSize, TextureFormat.RGBA32, false);

            // 生成所有地块的 Paint-RT，保证 所有 riverdata 的数据一并被加载上去
            mapRiverData.UpdateMapRiverData();
            foreach (var clusterIdx in clusterIdxs)
            {
                // 获取到在该地块存在的所有 RiverData
                List<RiverData> riverDatasInCls = mapRiverData.GetClsExistRiverData(clusterIdx);
                if (riverDatasInCls == null)
                {
                    riverDatasInCls = new List<RiverData>();
                }

                // 获取到所有 RiverData 的河流格子（pixel）数据
                List<Vector2Int> paintedPixel = new List<Vector2Int>(clusterSize * 5);
                for (int i = 0; i < riverDatasInCls.Count; i++)
                {
                    RiverData riverData = riverDatasInCls[i];
                    ushort riverID = riverData.riverID;
                    riverData.UpdateClusterPixelDict(clusterSize, terSet.paintRTSizeScale);
                    List<Vector2Int> pixelInRiver = riverData.GetPaintedClsPixles(clusterIdx);
                    foreach (var pixel in pixelInRiver)
                    {
                        paintedPixel.Add(pixel);
                    }
                }

                // NOTE : paintedPixel's data scope : [0, showTexSize], [0, showTexSize]
                NativeHashSet<int2> paintedPixelsSets = new NativeHashSet<int2>(paintedPixel.Count, Allocator.TempJob);
                for (int j = 0; j < paintedPixel.Count; j++)
                {
                    paintedPixelsSets.Add(new int2(paintedPixel[j].x, paintedPixel[j].y));
                }

                NativeArray<Color> colors = new NativeArray<Color>(showTexSize * showTexSize, Allocator.TempJob);
                PaintRiverTextureJob paintRiverTextureJob = new PaintRiverTextureJob()
                {
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

                RenderTexture renderTexture = RTPool.Get(desc);
                Graphics.CopyTexture(tmp, 0, 0, 0, 0, showTexSize, showTexSize, renderTexture, 0, 0, 0, 0);
                CreatePaintRiverRTData(clusterIdx, renderTexture, clusterSize, paintRTMat);
            }

            Debug.Log($"Gen river RT over, num : {clusterIdxs.Count}");
            GameObject.DestroyImmediate(tmp);

            // Set the rt's position!!
            foreach (var pair in paintRTDatasDict)
            {
                pair.Value.UpdateRTData(pair.Key, clusterSize);
            }

            Tools.current = Tool.None;
            SceneView.RepaintAll();
        }

        private void CreatePaintRiverRTData(Vector2Int clusterIdx, RenderTexture renderTexture, int clusterSize, Material paintRiverMat)
        {
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


        [FoldoutGroup("河流编辑", 0)]
        [Button("清空编辑地块", ButtonSizes.Medium)]
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

        [Obsolete]
        //[FoldoutGroup("河流编辑", 0)]
        //[Button("同步曲线场景节点到曲线", ButtonSizes.Medium)]
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


        #region 河流数据

        [Serializable]
        [InlineProperty]
        public class EditingRiverData : IDisposable 
        {
            [HorizontalGroup("PaintRiverRTData"), LabelText("河流ID")]
            public ushort riverID;

            [HorizontalGroup("PaintRiverRTData"), LabelText("河流名称")]
            public string riverName;

            [HideInInspector]
            [HorizontalGroup("PaintRiverRTData"), LabelText("河流obj"), ReadOnly]
            public GameObject riverDataObj;

            // TODO : 隐藏这个字段
            [LabelText("河流起点")]
            [InfoBox("未正确设置起点！请选中河流，切换绘制模式为 SetStart 并点击地块来设置", InfoMessageType.Error, visibleIfMemberName: "IsRiverStartInvalid")]
            public RiveStartData riverStart = MapRiverData.UnvalidRvStart;

            private bool IsRiverStartInvalid => !riverStart.IsValid();

            [ReadOnly]
            [LabelText("河流存在的地块")]
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
                // this.EditStartEvent = editStartEvent;
                this.GenCurveEvent = genCurveEvent;
                this.SaveRiverEvent = saveEvent;
                this.DeleteRiverEvent = deleteEvent;
            }

            [HorizontalGroup("RiverBtns"), Button("选中河流")]
            private void ChooseRiver() {
                ChooseRiverEvent.Invoke(riverID, this);
            }

            // [HorizontalGroup("RiverBtns"), Button("编辑起点（暂时没用）")]
            // private void EditStart() {
            //     EditStartEvent.Invoke(riverID, this);
            // }

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
                //existTerrainClusterIDs.Clear();
            }
        }

        [FoldoutGroup("河流数据")]
        [LabelText("河流数据")]
        public MapRiverData mapRiverData;

        [Space(10), FoldoutGroup("河流数据")]
        [LabelText("当前编辑的河流")]
        [Tooltip("仅存在于Editor中的数据，如果不保存则会在关闭时销毁")]
        public List<EditingRiverData> editRiverDatas;

        Dictionary<ushort, EditingRiverData> editRiverDataDict;


        [FoldoutGroup("河流数据", 0)]
        [Button("添加河流数据", ButtonSizes.Medium)]
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
            // 警告：如果你没有保存就选择了另一条河流编辑数据，那么之前的编辑会失效
            ResetEditingRiverData();
            GenRiverEditCluster();

            if (paintRTDatas == null) {
                paintRTDatas = new List<PaintRiverRTData>();
            }
            if (paintRTDatasDict == null) {
                paintRTDatasDict = new Dictionary<Vector2Int, PaintRiverRTData>();
            }
            
            // If this river is choosed, load all the exist cluster ID's Texture
            curEditRiverID = riverData.riverID;

            // NOTE : 不要隐藏 其他地块，全部展示就好
            // We will hide other cluster fristly
            //int clusterNum = riverData.existTerrainClusterIDs.Count;
            // HashSet<Vector2Int> shouldShowCluster = new HashSet<Vector2Int>();
            // for (int i = 0; i < clusterNum; i++) 
            // {
            //     shouldShowCluster.Add(riverData.existTerrainClusterIDs[i]);
            // }
            // int paintRTCount = paintRTDatas.Count;
            // for (int i = paintRTCount - 1; i >= 0; i--) 
            // {
            //     if (!shouldShowCluster.Contains(paintRTDatas[i].rtClusterIdx)) 
            //     {
            //         paintRTDatas[i].Dispose();
            //         paintRTDatas.RemoveAt(i);
            //     }
            // }

            // Set the paint-river-texture as 1/4 of clusterSize
            int clusterSize = terSet.clusterSize;
            int showTexSize = clusterSize / terSet.paintRTSizeScale;

            RenderTextureDescriptor desc = new RenderTextureDescriptor() {
                width = showTexSize, height = showTexSize,
                graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm,
                volumeDepth = 1, msaaSamples = 1, dimension = UnityEngine.Rendering.TextureDimension.Tex2D
            };
            Texture2D tmp = new Texture2D(showTexSize, showTexSize, TextureFormat.RGBA32, false);

            // Create and init all paint texture, load all painted data
            mapRiverData.UpdateMapRiverData();
            RiverData runtimingRvData = mapRiverData.GetRiverData(riverID);
            bool hasSavedThisRvData = runtimingRvData != null;
            if (hasSavedThisRvData)
            {
                runtimingRvData.UpdateClusterPixelDict(terSet.clusterSize, terSet.paintRTSizeScale);
            }
            int hasPaintedPixelNum = 0;

            // 先创建纯白纹理用于 Reset
            Texture2D whiteTex = new Texture2D(showTexSize, showTexSize, TextureFormat.RGBA32, false);
            Color[] whiteColors = new Color[showTexSize * showTexSize];
            Array.Fill(whiteColors, Color.white);
            whiteTex.SetPixels(whiteColors);
            whiteTex.Apply();

            // 选中一个河流时会 洗掉其他河流的影响
            foreach (var rvRTDatas in paintRTDatasDict)
            {
                rvRTDatas.Value.ResetRTData(whiteTex);
            }

            // TODO : 需要改动-优化，因为新的riverdata必不会存在 existTerrainClusterIDs 字段，这样就没法刷新编辑数据了（得思考办法
            int clusterNum = riverData.existTerrainClusterIDs.Count;
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

                // if cluster exist we still create a new RTData
                Vector2Int clusterIdx = riverData.existTerrainClusterIDs[i];
                if (paintRTDatasDict.ContainsKey(clusterIdx)) 
                {
                    paintRTDatasDict[clusterIdx].ResetRTData(whiteTex);
                    paintRTDatasDict[clusterIdx].ResetRTData(tmp);
                } 
                else 
                {
                    RenderTexture renderTexture = RTPool.Get(desc);
                    Graphics.CopyTexture(tmp, 0, 0, 0, 0, showTexSize, showTexSize, renderTexture, 0, 0, 0, 0);
                    CreatePaintRiverRTData(clusterIdx, renderTexture, clusterSize, paintRTMat);
                }
            }
            Debug.Log($"choose river id : {riverID}, expected add {hasPaintedPixelNum} painted pixels, saved this RvData {hasSavedThisRvData}");
            GameObject.DestroyImmediate(tmp);
            GameObject.DestroyImmediate(whiteTex);

            // TODO : you should set the rt's position!!
            foreach (var pair in paintRTDatasDict) {
                pair.Value.UpdateRTData(pair.Key, clusterSize);
            }

            Selection.activeGameObject = riverData.riverDataObj;
            Tools.current = Tool.None;
            SceneView.RepaintAll();

        }

        [Obsolete]
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

            if (!riverData.riverStart.IsValid())
            {
                Debug.LogError("河流起点未设置！请先切换绘制模式为 SetStart，在场景中点击设置起点");
                return;
            }

            List<Vector2Int> brushedPixelPoss = GetBrushedPixels();

            // TODO : 需要验证 贝塞尔曲线 的拟合程度
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
            bezierCurveEditor.InitCurveEditer(bezierCurve, terSet.paintRTSizeScale);
            go.transform.SetParent(RiverBezierParentTrans);
            return bezierCurveEditor;
        }

        // 保存河流数据的逻辑
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

            // 从 brushedPixels 自动推算河流存在的地块 cluster ID
            HashSet<Vector2Int> autoClusterIDs = new HashSet<Vector2Int>();
            foreach (var pixel in brushedPixels)
            {
                Vector2Int clusterID = new Vector2Int(pixel.x / terSet.clusterSize, pixel.y / terSet.clusterSize);
                autoClusterIDs.Add(clusterID);
            }

            // 真正保存河流数据
            RiverData saveRiverData = new RiverData(riverData.riverID, riverData.riverName, curBezierCurveEditor.Curve, brushedPixels, new List<Vector2Int>(autoClusterIDs));
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
            Debug.Log("本操作仅删除Editor下的河流，如需彻底删除请到MapData中手动删");
            ResetEditingRiverData();
        }

        // // TODO : 要刷新 scene
        // [FoldoutGroup("河流数据")]
        // [Button("保存河流数据（暂时没用）", ButtonSizes.Medium)]
        // private void SaveAllRiver() {
        //     // TODO : save all to mapRiverData
        //     // TODO : 要在这个方法里面去生成贝塞尔曲线吗？
        //     foreach (var riverData in editRiverDatas)
        //     {
        //         //SaveRiverEvent(riverData.)
        //     }
        // }

        #endregion


        #region 河流纹理

        [FoldoutGroup("河流纹理")]
        [Button("保存河流数据", ButtonSizes.Medium), ReadOnly]
        public string riverTexturePath = MapStoreEnum.RiverTexDataPath;

        [FoldoutGroup("河流纹理")]
        [Button("导出河流数据为纹理", ButtonSizes.Medium)]
        [Tooltip("根据选择的RiverWorkFlow决定如何导出")]
        public void GenRiverTexture()
        {
            //int texSize = terSet.clusterSize * terSet.terrainSize.x / terSet.paintRTSizeScale;
            Texture2D riverTex = mapRiverData.GenRiverTexture(terSet);

            string texName = GetRiverTexName();
            TextureUtility.SaveTextureAsAsset(riverTexturePath, texName, riverTex);
            GameObject.DestroyImmediate(riverTex);
        }

        private string GetRiverTexName()
        {
            int texSize = terSet.clusterSize * terSet.terrainSize.x / terSet.paintRTSizeScale;
            return string.Format("RvTexture_{0}_{1}x{1}_{2}", terSet.paintRTSizeScale, texSize, UnityEngine.Random.Range(0, 120));
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

        protected override void HandleSceneDraw(Event e) {
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
