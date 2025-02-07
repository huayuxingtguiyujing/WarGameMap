using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;
//using 

namespace LZ.WarGameMap.MapEditor
{

    // NOTE: WARNING: fileName ����� EditorName һ��
    //[CreateAssetMenu(fileName = "����༭", menuName = "��ͼ/�༭��/����༭��")]
    [Serializable]
    public class HexmapEditor : BaseMapEditor {


        public override string EditorName => MapEditorEnum.HexMapEditor;


        public static class TerrainColorEnum {
            public static Color RiverColor = Color.white;
            public static Color SeaColor = Color.white;

            public static Color PlainColor = Color.white;
            public static Color HillColor = Color.white;
            public static Color MountainColor = Color.white;

        }

        GameObject mapRootObj;
        GameObject hexClusterParentObj;
        GameObject hexSignParentObj;

        HexmapConstructor HexCtor;

        [FoldoutGroup("���õ�ͼScene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("����: û�г�ʼ��Scene")]
        public string warningMessage = "������ť��ʼ��!";

        [FoldoutGroup("���õ�ͼScene", -1)]
        [Button("��ʼ����ͼ Scene")]
        protected override void InitEditor() {
            if (mapRootObj == null) {
                mapRootObj = GameObject.Find(MapSceneEnum.MapRootName);
                if (mapRootObj == null) {
                    mapRootObj = new GameObject(MapSceneEnum.MapRootName);
                }
            }
            if (hexClusterParentObj == null) {
                hexClusterParentObj = GameObject.Find(MapSceneEnum.HexClusterParentName);
                if (hexClusterParentObj == null) {
                    hexClusterParentObj = new GameObject(MapSceneEnum.HexClusterParentName);
                }
            }
            hexClusterParentObj.transform.parent = mapRootObj.transform;

            if (hexSignParentObj == null) {
                hexSignParentObj = GameObject.Find(MapSceneEnum.HexSignParentName);
                if (hexSignParentObj == null) {
                    hexSignParentObj = new GameObject(MapSceneEnum.HexSignParentName);
                }
            }
            hexSignParentObj.transform.parent = mapRootObj.transform;

            // TODO: �Ժ���������Ҳ��Ҫ�õ� HexConstructor, ��Ҫ�ŵ������ط�������������
            HexCtor = mapRootObj.GetComponent<HexmapConstructor>();
            if (HexCtor == null) {
                HexCtor = mapRootObj.AddComponent<HexmapConstructor>();
            }
            HexCtor.SetMapPrefab(hexClusterParentObj.transform, hexSignParentObj.transform);
            notInitScene = false;
        }


        #region init map

        [FoldoutGroup("��ʼ����ͼ")]
        [LabelText("���ο��")]
        public Vector2 LeftAndRight = new Vector2(0, 10);
        [FoldoutGroup("��ʼ����ͼ")]
        [LabelText("���θ߶�")]
        public Vector2 TopAndBottom = new Vector2(0, 10);

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("���ƾ�������")]
        private void DrawRectangleGrid() {
            HexCtor.InitHexConsRectangle(
                (int)TopAndBottom.x, (int)TopAndBottom.y,
                (int)LeftAndRight.x, (int)LeftAndRight.y
            );
        }

        [FoldoutGroup("��ʼ����ͼ")]
        [LabelText("ƽ���ı��ο��")]
        public Vector2 Q1AndQ2 = new Vector2(0, 10);
        [FoldoutGroup("��ʼ����ͼ")]
        [LabelText("ƽ���ı��θ߶�")]
        public Vector2 Q3AndQ4 = new Vector2(0, 10);

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("����ƽ���ı�������")]
        private void DrawParallelogramGrid() {
            HexCtor.InitHexConsParallelogram(
                (int)Q1AndQ2.x, (int)Q1AndQ2.y,
                (int)Q3AndQ4.x, (int)Q3AndQ4.y
            );
        }

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("��յ�ͼ")]
        private void ClearHexMap() {
            hexClusterParentObj.ClearObjChildren();
        }

        [FoldoutGroup("��ʼ����ͼ")]
        [Button("��ձ��")]
        private void ClearHexSigns() {
            hexSignParentObj.ClearObjChildren();
        }

        //[FoldoutGroup("��ʼ����ͼ")]
        //[Button("������ͼ�Ĳ���")]
        //private void BuildQuadTree() {
            //HexCtor.BuildQuadTreeMap();
        //}
        //[FoldoutGroup("��ʼ����ͼ")]
        //[OnValueChanged("ShowHidQuadTree")]
        //public bool ShowQuadTree = true;
        //private void ShowHidQuadTree() {
            //HexCtor.ShowQuadTreeGizmos(ShowQuadTree);
        //}

        #endregion


        #region test function

        [FoldoutGroup("����")]
        [LabelText("��������")]
        public Vector2Int gridIndex = new Vector2Int(0, 0);

        [FoldoutGroup("����")]
        [Button("��ȡ�ھ�")]
        private void TestGetNeighbors() {
            HexCtor.GetMapGridNeighbor(gridIndex.x, gridIndex.y);
        }

        [FoldoutGroup("����")]
        [Button("��ȡ����")]
        private void TestGetCoordinates() {
            HexCtor.GetMapGridTest(gridIndex.x, gridIndex.y);
        }

        #endregion

        #region draw map

        [FoldoutGroup("����")]
        public bool OpenBrush = false;

        [FoldoutGroup("����")]
        public int BrushScope = 3;

        [FoldoutGroup("����")]
        public Color32 BrushColor = TerrainColorEnum.RiverColor;

        [FoldoutGroup("����")]
        public Color32 SignColor = Color.green;

        #endregion


        private MapGrid curMapGrid;

        private List<MapGrid> curGridsInScope;


        public override void Enable() {
            base.Enable();
            //GizmosCtrl.GetInstance().RegisterGizmoEvent(ShowCurChooseGrid);
        }

        public override void Disable() {
            //GizmosCtrl.GetInstance().UnregisterGizmoEvent(ShowCurChooseGrid);
            base.Disable();
        }

        protected override void OnMouseDrag(Event e) {

            if (curGridsInScope == null) {
                return;
            }
            DetectCurChooseGrid(e);

            HexCtor.SetMapGridColor(curGridsInScope, BrushColor);
        }

        protected override void OnMouseMove(Event e) {
            base.OnMouseMove(e);
            
            if(HexCtor == null) {
                return;
            }

            DetectCurChooseGrid(e);
        }

        private void DetectCurChooseGrid(Event e) {
            Vector2 CurPos = GetMousePos(e);
            MapGrid grid = HexCtor.GetClosestMapGrid(CurPos);
            if (grid != null) {
                if (curMapGrid != grid) {
                    curMapGrid = grid;
                    curGridsInScope = HexCtor.GetMapGrid_HexScope(curMapGrid.mapIdx.x, curMapGrid.mapIdx.y, BrushScope);
                }
            }
        }

        private void ShowCurChooseGrid() {
            if (curMapGrid == null) {
                return;
            }

            if(curGridsInScope == null) {
                return;
            }

            foreach (var grid in curGridsInScope)
            {
                GizmosUtils.DrawCube(grid.Position, SignColor);
            }

        }


    }
}
