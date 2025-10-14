using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class MountainEditor : BrushHexmapEditor 
    {
        public override string EditorName => MapEditorEnum.MountainEditor;

        static Color MountainColor = Color.yellow;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();

            FindOrCreateSO<GridTerrainSO>(ref gridTerrainSO, MapStoreEnum.GamePlayGridTerrainDataPath, "GridTerrainSO_Default.asset");
            gridTerrainSO.UpdateTerSO(hexSet.mapWidth, hexSet.mapHeight);

            Debug.Log("init moutain Editor over !");
        }

        protected override BrushHexmapSetting GetBrushSetting()
        {
            return BrushHexmapSetting.Default;
        }


        Dictionary<string, List<Vector2>> MountainBoundVectorDict = new Dictionary<string, List<Vector2>>();

        Dictionary<string, List<Vector2Int>> MountainNameBoundDict = new Dictionary<string, List<Vector2Int>>();

        protected override void PostBuildHexGridMap()
        {
            // 什么是山脉群？
            // 1.均由山脉格组成的连续格
            // 2.且大于4个格子

            // 能对山脉群进行什么操作？
            // 1.展示边界，在Editor中对山脉边界进行描边 
            // 2.将边界转为丘陵地带

            // 山脉编辑器还提供什么其他的功能？
            // 1.支持用不同笔刷调整地形高度（这个功能也许应该集成到 BrushMapEditor ）
            // 2.与 Gaea 联调

            // 需求拆分
            // 一.山脉编辑器
            // 1.支持通过点击选中正在编辑的山脉
            // 2.变量：lerpScope / showDebugData
            // 3.按钮：将选中山脉周围格转为丘陵
            // 4.按钮：刷新山脉数据（重新加载，然后刷新山脉边界范围，及边界格到非山脉地区的距离）
            // 二.笔刷工具（功能放到 BrushMapEditor）
            // 1.变量：不同的笔刷类型 （圆形、圆渐变_线性、圆渐变1、圆渐变2、噪声型）
            // 2.变量：scope / influenceFix / lockToChooseCluster
            // 3.按钮：保存到 Terrain

            // 典型的岛屿问题
            int mountainGridCount = 0;
            HashSet<Vector2Int> hasVisied = new HashSet<Vector2Int>(hexSet.mapWidth * hexSet.mapHeight);
            int mountainCount = 0;
            Dictionary<string, List<Vector2Int>> MountainNameGridDict = new Dictionary<string, List<Vector2Int>>();
            MountainNameBoundDict.Clear();
            for (int i = 0;  i < hexSet.mapWidth; i++)
            {
                for(int j = 0; j < hexSet.mapHeight; j++)
                {
                    Vector2Int offsetHex = new Vector2Int(i, j);
                    if (hasVisied.Contains(offsetHex))
                    {
                        continue;
                    }
                    hasVisied.Add(offsetHex);

                    // Island Problem Algorithm
                    bool isMountain = gridTerrainSO.GetGridIsMountain(offsetHex);
                    if (isMountain)
                    {
                        // Build mountain cluster
                        mountainCount++;
                        string mountainName = $"MountainCluster_{mountainCount}";
                        List<Vector2Int> mountainGrids = new List<Vector2Int>();
                        MountainNameGridDict.Add(mountainName, mountainGrids);

                        HashSet<Vector2Int> hasAddBound = new HashSet<Vector2Int>();
                        List<Vector2Int> mountainBoundGrids = new List<Vector2Int>();
                        MountainNameBoundDict.Add(mountainName, mountainBoundGrids);

                        Queue<Vector2Int> BFSQueue = new Queue<Vector2Int>();
                        BFSQueue.Enqueue(offsetHex);

                        while (BFSQueue.Count > 0)
                        {
                            mountainGridCount++;
                            Vector2Int curGrid = BFSQueue.Dequeue();
                            mountainGrids.Add(curGrid);

                            Vector2Int[] neighbour = HexHelper.GetOffsetHexNeighbour(curGrid);

                            foreach (var neighbor in neighbour)
                            {
                                Vector2Int cur = neighbor + curGrid;

                                // Neighbor is mountain, so curGrid is boundary grid
                                bool curIsMountain = gridTerrainSO.GetGridIsMountain(cur);
                                if (!curIsMountain && !hasAddBound.Contains(curGrid))
                                {
                                    hasAddBound.Add(curGrid);
                                    mountainBoundGrids.Add(curGrid);
                                }
                                else if(curIsMountain && !hasVisied.Contains(cur))
                                {
                                    hasVisied.Add(cur);
                                    BFSQueue.Enqueue(cur);
                                }
                            }
                        }
                    }
                }
            }

            // Use MountainNameGridDict to paint mountain data int hex map
            // Use MountainNameBoundDict to build boundary data, And draw Gizmos of boundary
            MountainBoundVectorDict.Clear();
            foreach (var pair in MountainNameGridDict)
            {
                string mountainName = pair.Key;
                List<Vector2Int> gridOffsetList = pair.Value;
                PaintHexRT(gridOffsetList, MountainColor);

                List<Vector2Int> boundList = MountainNameBoundDict[mountainName];
                List<Vector2> boundWorldPosList = MapBoundUtil.GetBoundPointList(hexSet.GetScreenLayout(), gridOffsetList, boundList);
                MountainBoundVectorDict.Add(mountainName, boundWorldPosList);
            }

            Debug.Log($"Mountain grid cnt : {mountainGridCount}, Mountain count : {MountainBoundVectorDict.Count}");
        }

        // TODO : 还有绘边界的必要吗？思考一下价值！！！
        private void DrawMountainBound()
        {
            if(MountainBoundVectorDict == null || MountainBoundVectorDict.Count == 0)
            {
                return;
            }

            //// DON DEL, Debug log mountain bound
            //foreach (var pair in MountainNameBoundDict)
            //{
            //    List<Vector2Int> boundPoss = pair.Value;
            //    int boundPosNum = boundPoss.Count;
            //    Color32 mountainBoundColor = Color.red;
            //    for (int i = 0; i < boundPosNum - 1; i++)
            //    {
            //        Vector3 preWorldPos = HexHelper.OffsetToWorld(hexSet.GetScreenLayout(), boundPoss[i]);
            //        Vector3 nextWorldPos = HexHelper.OffsetToWorld(hexSet.GetScreenLayout(), boundPoss[i + 1]);
            //        GizmosUtils.DrawLine(preWorldPos, nextWorldPos, mountainBoundColor);
            //    }
            //}

            foreach (var pair in MountainBoundVectorDict)
            {
                string mountainName = pair.Key;
                List<Vector2> boundWorldPoss = pair.Value;
                int boundPosNum = boundWorldPoss.Count;
                //Color32 mountainBoundColor = MapColorUtil.GetRandomColor(mountainName);
                Color32 mountainBoundColor = Color.red;

                for (int i = 0; i < boundPosNum; i++)
                {
                    if (i == boundPosNum - 1)
                    {
                        GizmosUtils.DrawLine(boundWorldPoss[i].TransToXZ(), boundWorldPoss[0].TransToXZ(), mountainBoundColor);
                    }
                    else
                    {
                        GizmosUtils.DrawLine(boundWorldPoss[i].TransToXZ(), boundWorldPoss[i + 1].TransToXZ(), mountainBoundColor);
                    }
                }
            }
        }

        #region 山脉编辑

        [FoldoutGroup("山脉编辑")]
        [LabelText("展示山脉边界")]
        [OnValueChanged("UpdateBoundGizmos")]
        public bool showMountainBoundGizmos;

        [FoldoutGroup("山脉编辑")]
        [LabelText("格子地形源数据SO")]
        public GridTerrainSO gridTerrainSO;

        private void UpdateBoundGizmos()
        {
            if (showMountainBoundGizmos)
            {
                // Register boundary gizmos data
                GizmosCtrl.GetInstance().RegisterGizmoEvent(DrawMountainBound);
            }
            else
            {
                GizmosCtrl.GetInstance().UnregisterGizmoEvent(DrawMountainBound);
            }
        }

        [FoldoutGroup("山脉编辑")]
        [Button("初始化 CountryManager", ButtonSizes.Medium)]
        private void InitCountryManager()
        {
            //HexCtor.InitCountry(countrySO);
        }


        #endregion

    }
}
