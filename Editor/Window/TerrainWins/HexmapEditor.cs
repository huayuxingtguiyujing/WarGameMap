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

    // NOTE: WARNING: fileName 必须和 EditorName 一样
    //[CreateAssetMenu(fileName = "网格编辑", menuName = "地图/编辑器/网格编辑器")]
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

        [FoldoutGroup("配置地图Scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: 没有初始化Scene")]
        public string warningMessage = "请点击按钮初始化!";

        [FoldoutGroup("配置地图Scene", -1)]
        [Button("初始化地图 Scene")]
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

            // TODO: 以后其他界面也需要用到 HexConstructor, 需要放到其他地方（开个单例）
            HexCtor = mapRootObj.GetComponent<HexmapConstructor>();
            if (HexCtor == null) {
                HexCtor = mapRootObj.AddComponent<HexmapConstructor>();
            }
            HexCtor.SetMapPrefab(hexClusterParentObj.transform, hexSignParentObj.transform);
            notInitScene = false;
        }


        #region init map

        [FoldoutGroup("初始化地图")]
        [LabelText("矩形宽度")]
        public Vector2 LeftAndRight = new Vector2(0, 10);
        [FoldoutGroup("初始化地图")]
        [LabelText("矩形高度")]
        public Vector2 TopAndBottom = new Vector2(0, 10);

        [FoldoutGroup("初始化地图")]
        [Button("绘制矩形网格")]
        private void DrawRectangleGrid() {
            HexCtor.InitHexConsRectangle(
                (int)TopAndBottom.x, (int)TopAndBottom.y,
                (int)LeftAndRight.x, (int)LeftAndRight.y
            );
        }

        [FoldoutGroup("初始化地图")]
        [LabelText("平行四边形宽度")]
        public Vector2 Q1AndQ2 = new Vector2(0, 10);
        [FoldoutGroup("初始化地图")]
        [LabelText("平行四边形高度")]
        public Vector2 Q3AndQ4 = new Vector2(0, 10);

        [FoldoutGroup("初始化地图")]
        [Button("绘制平行四边形网格")]
        private void DrawParallelogramGrid() {
            HexCtor.InitHexConsParallelogram(
                (int)Q1AndQ2.x, (int)Q1AndQ2.y,
                (int)Q3AndQ4.x, (int)Q3AndQ4.y
            );
        }

        [FoldoutGroup("初始化地图")]
        [Button("清空地图")]
        private void ClearHexMap() {
            hexClusterParentObj.ClearObjChildren();
        }

        [FoldoutGroup("初始化地图")]
        [Button("清空标记")]
        private void ClearHexSigns() {
            hexSignParentObj.ClearObjChildren();
        }

        //[FoldoutGroup("初始化地图")]
        //[Button("构建地图四叉树")]
        //private void BuildQuadTree() {
            //HexCtor.BuildQuadTreeMap();
        //}
        //[FoldoutGroup("初始化地图")]
        //[OnValueChanged("ShowHidQuadTree")]
        //public bool ShowQuadTree = true;
        //private void ShowHidQuadTree() {
            //HexCtor.ShowQuadTreeGizmos(ShowQuadTree);
        //}

        #endregion


        #region test function

        [FoldoutGroup("测试")]
        [LabelText("网格坐标")]
        public Vector2Int gridIndex = new Vector2Int(0, 0);

        [FoldoutGroup("测试")]
        [Button("获取邻居")]
        private void TestGetNeighbors() {
            HexCtor.GetMapGridNeighbor(gridIndex.x, gridIndex.y);
        }

        [FoldoutGroup("测试")]
        [Button("获取坐标")]
        private void TestGetCoordinates() {
            HexCtor.GetMapGridTest(gridIndex.x, gridIndex.y);
        }

        #endregion

        #region draw map

        [FoldoutGroup("绘制")]
        public bool OpenBrush = false;

        [FoldoutGroup("绘制")]
        public int BrushScope = 3;

        [FoldoutGroup("绘制")]
        public Color32 BrushColor = TerrainColorEnum.RiverColor;

        [FoldoutGroup("绘制")]
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
