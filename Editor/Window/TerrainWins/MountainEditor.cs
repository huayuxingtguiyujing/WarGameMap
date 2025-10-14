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
            // ʲô��ɽ��Ⱥ��
            // 1.����ɽ������ɵ�������
            // 2.�Ҵ���4������

            // �ܶ�ɽ��Ⱥ����ʲô������
            // 1.չʾ�߽磬��Editor�ж�ɽ���߽������� 
            // 2.���߽�תΪ����ش�

            // ɽ���༭�����ṩʲô�����Ĺ��ܣ�
            // 1.֧���ò�ͬ��ˢ�������θ߶ȣ��������Ҳ��Ӧ�ü��ɵ� BrushMapEditor ��
            // 2.�� Gaea ����

            // ������
            // һ.ɽ���༭��
            // 1.֧��ͨ�����ѡ�����ڱ༭��ɽ��
            // 2.������lerpScope / showDebugData
            // 3.��ť����ѡ��ɽ����Χ��תΪ����
            // 4.��ť��ˢ��ɽ�����ݣ����¼��أ�Ȼ��ˢ��ɽ���߽緶Χ�����߽�񵽷�ɽ�������ľ��룩
            // ��.��ˢ���ߣ����ܷŵ� BrushMapEditor��
            // 1.��������ͬ�ı�ˢ���� ��Բ�Ρ�Բ����_���ԡ�Բ����1��Բ����2�������ͣ�
            // 2.������scope / influenceFix / lockToChooseCluster
            // 3.��ť�����浽 Terrain

            // ���͵ĵ�������
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

        // TODO : ���л�߽�ı�Ҫ��˼��һ�¼�ֵ������
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

        #region ɽ���༭

        [FoldoutGroup("ɽ���༭")]
        [LabelText("չʾɽ���߽�")]
        [OnValueChanged("UpdateBoundGizmos")]
        public bool showMountainBoundGizmos;

        [FoldoutGroup("ɽ���༭")]
        [LabelText("���ӵ���Դ����SO")]
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

        [FoldoutGroup("ɽ���༭")]
        [Button("��ʼ�� CountryManager", ButtonSizes.Medium)]
        private void InitCountryManager()
        {
            //HexCtor.InitCountry(countrySO);
        }


        #endregion

    }
}
