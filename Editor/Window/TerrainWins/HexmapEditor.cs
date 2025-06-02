using Codice.Client.BaseCommands.BranchExplorer.ExplorerData;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.HexStruct;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.UIElements;
using static Codice.Client.Common.Servers.RecentlyUsedServers;
using static LZ.WarGameMap.Runtime.HexHelper;
using Random = UnityEngine.Random;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

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
        //GameObject hexSignParentObj;

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

            //if (hexSignParentObj == null) {
            //    hexSignParentObj = GameObject.Find(MapSceneEnum.HexSignParentName);
            //    if (hexSignParentObj == null) {
            //        hexSignParentObj = new GameObject(MapSceneEnum.HexSignParentName);
            //    }
            //}
            //hexSignParentObj.transform.parent = mapRootObj.transform;
            hexClusterParentObj.transform.position = new Vector3(0, hexHeight, 0);

            // TODO: 以后其他界面也需要用到 HexConstructor, 需要放到其他地方（开个单例）
            HexCtor = mapRootObj.GetComponent<HexmapConstructor>();
            if (HexCtor == null) {
                HexCtor = mapRootObj.AddComponent<HexmapConstructor>();
            }

            InitMapSetting();
            HexCtor.SetHexSetting(hexSet, hexClusterParentObj.transform, hexMaterial);
            notInitScene = false;
        }
        
        protected override void InitMapSetting() {
            base.InitMapSetting();
            if (hexSet == null) {
                string hexSettingPath = MapStoreEnum.WarGameMapSettingPath + "/HexSetting_Default.asset";
                hexSet = AssetDatabase.LoadAssetAtPath<HexSettingSO>(hexSettingPath);
                if (hexSet == null) {
                    // create it !
                    hexSet = CreateInstance<HexSettingSO>();
                    AssetDatabase.CreateAsset(hexSet, hexSettingPath);
                    Debug.Log($"successfully create Hex Setting, path : {hexSettingPath}");
                }
            }
        }

        #region init map

        [FoldoutGroup("初始化地图", -1)]
        [LabelText("地图配置")]
        public HexSettingSO hexSet;

        [FoldoutGroup("初始化地图")]
        [LabelText("地图高度")]
        public float hexHeight = 30;

        [FoldoutGroup("初始化地图")]
        [LabelText("地图材质")]
        public Material hexMaterial;

        [FoldoutGroup("初始化地图")]
        [Button("绘制矩形网格", ButtonSizes.Medium)]
        private void DrawRectangleGrid() {
            HexCtor.InitHexConsRectangle();
        }

        [FoldoutGroup("初始化地图")]
        [Button("刷新地图", ButtonSizes.Medium)]
        private void UpdateHexMap() {
            // set the height
            
            // update the material
        }

        [FoldoutGroup("初始化地图")]
        [Button("清空地图", ButtonSizes.Medium)]
        private void ClearHexMap() {
            hexClusterParentObj.ClearObjChildren();
        }

        #endregion


        #region generate RawHexMap

        [FoldoutGroup("生成RawHexMap")]
        [LabelText("当前使用的高度图数据")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("生成RawHexMap")]
        [LabelText("当前操作Hex地图对象")]
        public RawHexMapSO rawHexMapSO;

        [FoldoutGroup("生成RawHexMap")]
        [LabelText("当前Hex地图对象纹理")]
        public Texture2D rawHexMapTexture;

        [FoldoutGroup("生成RawHexMap")]
        [LabelText("起始经纬度")]
        public Vector2Int startLongitudeLatitude = new Vector2Int(109, 32);

        [FoldoutGroup("生成RawHexMap")]
        [LabelText("导出位置")]
        public string exportHexMapSOPath = MapStoreEnum.TerrainHexMapPath;

        [FoldoutGroup("生成RawHexMap")]
        [Button("生成RawHexMapSO", ButtonSizes.Medium)]
        private void GenerateRawHexMap() {

            HeightDataManager heightDataManager = new HeightDataManager();
            heightDataManager.InitHeightDataManager(heightDataModels, MapTerrainEnum.ClusterSize);

            rawHexMapSO = CreateInstance<RawHexMapSO>();
            rawHexMapSO.InitRawHexMap(hexSet.mapWidth, hexSet.mapHeight);
            HexCtor.GenerateRawHexMap(startLongitudeLatitude, rawHexMapSO, heightDataManager);

            // 通过 HexMapSO 还原 Terrain 时
            //      对当前 vertex 取得对应所属 Hex（以及对应的所有邻居节点）
            //      根据当前的 Hex type 设置 vert 的 baseHeight
            //      按照 hill 参数，概率性地调整高度（有一定概率按照邻居节点的参数进行调整）

            // 难点：hex 贴边 shader 要放到还原后的 Terrain 上面
            // 难点：混合大世界地图...（可以在 HexMapSO 生成的时候做索引/混合贴图吗？（在边缘应用噪声？））
        }

        [FoldoutGroup("生成RawHexMap")]
        [Button("生成RawHexMap纹理", ButtonSizes.Medium)]
        private void GenerateRawHexTexture() {
            if(rawHexMapSO == null) {
                Debug.LogError("rawHexMapSO is null!");
                return;
            } 

            //if(rawHexMapTexture != null) {
            //    rawHexMapTexture.
            //}

            rawHexMapTexture = new Texture2D(rawHexMapSO.width, rawHexMapSO.height);

            foreach (var gridTerrainData in rawHexMapSO.HexMapGridTersList) {
                Vector2Int pos = gridTerrainData.GetHexPos();
                //Vector2Int pos = gridTerrainData.hexagon;
                Color color = gridTerrainData.GetTerrainColor();

                // TODO : 下面的生成步骤还是有问题！没有照顾到 hex 坐标的特性
                //Vector2Int fixed_pos = new Vector2Int(rawHexMapTexture.width - pos.x, rawHexMapTexture.height - pos.y);
                //Vector2Int fixed_pos = new Vector2Int(pos.x, rawHexMapTexture.height - pos.y);
                //Vector2Int fixed_pos = new Vector2Int(rawHexMapTexture.width - pos.x, pos.y);
                //Vector2Int fixed_pos = new Vector2Int(pos.y, pos.x);
                Vector2Int fixed_pos = new Vector2Int(pos.x, pos.y);
                // NOTE : ExportFlipVertically true
                rawHexMapTexture.SetPixel(fixed_pos.x, fixed_pos.y, color);
                //rawHexMapTexture.SetPixel(pos.x, pos.y, color);
            }

            //for(int i = 0;  i < rawHexMapTexture.width; i ++) { 
            //    for(int j = 0; j < rawHexMapTexture.height; j ++) {
            //        //rawHexMapTexture.SetPixel(i, j, );
            //    }
            //}
            Debug.Log($"generate hex texture : {rawHexMapTexture.width}x{rawHexMapTexture.height}");
        }

        [FoldoutGroup("生成RawHexMap")]
        [Button("保存RawHexMapSO", ButtonSizes.Medium)]
        private void SaveRawHexMap() {

            CheckExportPath();
            string soName = $"RawHexMap_{rawHexMapSO.width}x{rawHexMapSO.height}_{UnityEngine.Random.Range(0, 100)}.asset";
            string RawHexPath = exportHexMapSOPath + $"/{soName}";
            AssetDatabase.CreateAsset(rawHexMapSO, RawHexPath);
            Debug.Log($"successfully create Hex Map, path : {RawHexPath}");
        }

        [FoldoutGroup("生成RawHexMap")]
        [Button("保存RawHexMap纹理", ButtonSizes.Medium)]
        private void SaveRawHexTexture() {
            if (rawHexMapTexture == null) {
                Debug.LogError("rawHexMapTexture is null!");
                return;
            }

            CheckExportPath();
            string textureName = $"hexTexture_{rawHexMapTexture.width}x{rawHexMapTexture.height}_{UnityEngine.Random.Range(0, 100)}";
            TextureUtility.GetInstance().SaveTextureAsAsset(exportHexMapSOPath, textureName, rawHexMapTexture);

            //string path = exportHexMapSOPath + $"/{textureName}";
            //AssetDatabase.CreateAsset(rawHexMapTexture, path);
            //Debug.Log($"successfully create Hex Map, path : {path}");
        }

        private void CheckExportPath() {
            string mapSOFolerName = AssetsUtility.GetInstance().GetFolderFromPath(exportHexMapSOPath);
            if (!AssetDatabase.IsValidFolder(exportHexMapSOPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.TerrainRootPath, mapSOFolerName);
            }
        }

        #endregion



        #region hexTexture Construct
        // NOTE : 此处输出六边形网格纹理，是为了构建 类似 文明那样的 六边形 Terrain 地表

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("分辨率")]
        public int OuputTexResolution = 1024;

        [FoldoutGroup("六边形纹理构建/纹理设置")]
        [LabelText("颜色")]
        public Color hexEdgeColor = new Color(1, 1, 1, 0.5f);

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("当前操作的地貌纹理")]
        public Texture2D curHexLandformTex;

        [FoldoutGroup("六边形纹理构建")]
        [LabelText("贴图导出位置")]
        public string hexLandformTexImportPath = MapStoreEnum.HexLandformTexOutputPath;

        // TODO : 总目标：按照文明6的地貌方式生成混合地貌，基本步骤如下：
        // step1 : 通过随机数种子得到每个Hex格子的地形数据
        // step2 : 准备对应地形的纹理（至少16张，这些纹理与地形其实不是一一对应的，可能会进行叠加）
        // step3 : 根据catlike的方案做初版的 hex 混合地貌，即边缘插值与周围格子进行混合，中心不做混合
        // step4 : （要调研）通过噪声等方式对边缘混合地带进行处理，得到更为自然和美观的地貌
        // step5 : （要调研）做更多的地貌效果，了解贴花原理，写shader......
        struct ExportHexLandFormJob : IJobParallelFor {
            [ReadOnly] public int OuputTextureResolution;
            [ReadOnly] public int hexGridSize;
            [ReadOnly] public Layout layout;

            [ReadOnly] public NativeHashMap<Vector2Int, int> tempTerrainType;
            [WriteOnly] public NativeArray<Color> colors;

            public void Execute(int index) {
                int j = index / OuputTextureResolution;
                int i = index % OuputTextureResolution;

                Vector2 pos = new Vector2(i, j);
                Hexagon hex = HexHelper.PixelToAxialHex(pos, hexGridSize);
                HexAreaPointData hexData = HexHelper.GetPointHexArea(pos, hex, layout, 0.7f);

                // NOTE : 这里的方位是错误的，会对称，需要查证为什么？
                HandleHexPointData(index, ref hexData, hex);
            }

            private void HandleHexPointData(int idx, ref HexAreaPointData hexData, Hexagon hex) {
                // get this hexGrid's type type
                int terrainType = -1;
                Vector2Int OffsetHex = HexHelper.AxialToOffset(hex);
                if (tempTerrainType.ContainsKey(OffsetHex)) {
                    terrainType = tempTerrainType[OffsetHex];
                }

                if (hexData.insideInnerHex) {
                    colors[idx] = GetColorByType_Test(terrainType);
                } else {
                    //colors[idx] = Color.white;
                    // not inner hex, so need lerp and mix
                    colors[idx] = GetColorByDirection(hexData.hexAreaDir);

                    /*switch (hexData.hexAreaDir) {
                        case HexDirection.NW:
                            break;
                        case HexDirection.NE:
                            break;
                        case HexDirection.E:
                            break;
                        case HexDirection.SE:
                            break;
                        case HexDirection.SW:
                            break;
                        case HexDirection.W:
                            break;
                        case HexDirection.None:
                            break;
                    }*/
                }

                //BlenderWithNeighborHex(idx, ref hexData);
            }

            private void BlenderWithNeighborHex(int idx, ref HexAreaPointData hexData) {
                switch (hexData.outHexAreaEnum) {
                    case OutHexAreaEnum.Edge:
                        colors[idx] = Color.red;
                        break;
                    case OutHexAreaEnum.LeftCorner:
                        colors[idx] = Color.blue;
                        break;
                    case OutHexAreaEnum.RightCorner:
                        colors[idx] = Color.green;
                        break;
                }
            }

            private Color GetColorByType_Test(int type) {
                //Debug.Log("trriger it!");
                // NOTE : 这个方法目前是占位符
                switch (type) {
                    case 0:
                        return new Color(0.20f, 0.60f, 0.20f); // 浅绿色 #339933
                    case 1:
                        return new Color(0.78f, 0.65f, 0.35f); // 黄绿色 #C8A55A
                    case 2:
                        return new Color(0.62f, 0.49f, 0.34f); // 浅棕色 #9E7E57
                    case 3:
                        return new Color(0.85f, 0.85f, 0.85f); // 浅灰色 #D8D8D8;
                    default:
                        return Color.white;
                }
            }
        
            private Color GetColorByDirection(HexDirection direction) {
                switch (direction) {
                    case HexDirection.NW:
                        return Color.red;
                    case HexDirection.NE:
                        return Color.green;
                    case HexDirection.E:
                        return Color.blue;
                    case HexDirection.SE:
                        return Color.gray;
                    case HexDirection.SW:
                        return Color.cyan;
                    case HexDirection.W:
                        return Color.yellow;
                    default:
                        return Color.white;
                }
            }

        }

        [FoldoutGroup("六边形纹理构建")]
        [Button("导出六边形地貌纹理图", ButtonSizes.Medium)]
        private void ExportHexLandFormTex() {
            curHexLandformTex = new Texture2D(OuputTexResolution, OuputTexResolution);
            HexGenerator hexGenerator = new HexGenerator();
            hexGenerator.GenerateRectangle(0, 50, 0, 50);

            // 暂时使用 这个hashmap 记录所有 hex 省份的地形类型
            Layout layout = hexSet.GetScreenLayout();
            Color[] colors = curHexLandformTex.GetPixels();

            NativeArray<Color> nativeColors = new NativeArray<Color>(colors, Allocator.TempJob);
            NativeHashMap<Vector2Int, int> tempTerrainType = new NativeHashMap<Vector2Int, int>(
                hexGenerator.HexagonIdxDic.Count, Allocator.TempJob
            );
            foreach (var pair in hexGenerator.HexagonIdxDic)
            {
                int type = Random.Range(0, 3);
                tempTerrainType.Add(pair.Key, type);
            }

            ExportHexLandFormJob exportJob = new ExportHexLandFormJob {
                OuputTextureResolution = OuputTexResolution,
                hexGridSize = hexSet.hexGridSize,
                layout = layout,
                tempTerrainType = tempTerrainType,
                colors = nativeColors,
            };
            JobHandle jobHandle1 = exportJob.Schedule(OuputTexResolution * OuputTexResolution, 64);
            jobHandle1.Complete();

            curHexLandformTex.SetPixels(nativeColors.ToArray());
            curHexLandformTex.Apply();

            nativeColors.Dispose();
            tempTerrainType.Dispose();

        }


        private Color SampleFromTex(int i, int j, Texture2D tex) {
            return tex.GetPixel(i, j);
        }

        [FoldoutGroup("六边形纹理构建")]
        [Button("保存六边形地貌纹理图", ButtonSizes.Medium)]
        private void SaveHexLandFormTex() {
            string texName = string.Format("landform_{0}x{0}_{1}", OuputTexResolution, DateTime.Now.Ticks);
            TextureUtility.GetInstance().SaveTextureAsAsset(hexLandformTexImportPath, texName, curHexLandformTex);
        }


        public List<Vector2Int> GetLinePoints(Vector2Int A, Vector2Int B) {
            List<Vector2Int> points = new List<Vector2Int>();

            int dx = Mathf.Abs(B.x - A.x);
            int dy = Mathf.Abs(B.y - A.y);
            int sx = A.x < B.x ? 1 : -1;
            int sy = A.y < B.y ? 1 : -1;
            int err = dx - dy;

            int x = A.x, y = A.y;

            while (true) {
                points.Add(new Vector2Int(x, y));
                if (x == B.x && y == B.y) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }

            return points;
        }

        private Color GetColorByLineDistance(Vector2 point, Vector2 A, Vector2 B, float maxDistance = 2) {
            float distance = DistanceToSegment(point, A, B);
            if (distance > maxDistance)
                return Color.white;

            float intensity = 1f - (distance / maxDistance);
            return new Color(1f, intensity, intensity);
        }

        private float DistanceToSegment(Vector2 P, Vector2 A, Vector2 B) {
            Vector2 AB = B - A;
            Vector2 AP = P - A;

            float abSquared = AB.sqrMagnitude;
            if (abSquared == 0f) {  // A B重合
                return AP.magnitude;
            } 

            float t = Mathf.Clamp01(Vector2.Dot(AP, AB) / abSquared);
            Vector2 closest = A + t * AB;
            return Vector2.Distance(P, closest);
        }

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

            //HexCtor.SetMapGridColor(curGridsInScope, BrushColor);
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
                //if (curMapGrid != grid) {
                //    curMapGrid = grid;
                //    curGridsInScope = HexCtor.GetMapGrid_HexScope(curMapGrid.mapIdx.x, curMapGrid.mapIdx.y, BrushScope);
                //}
            }
        }


    }
}
