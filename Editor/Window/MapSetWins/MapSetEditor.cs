using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;
using LZ.WarGameMap.Runtime.Model;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.VersionControl;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    public class MapSetEditor : BaseMapEditor {
        public override string EditorName => MapEditorEnum.MapSetEditor;


        [FoldoutGroup("配置scene")]
        [LabelText("地图Runtime配置")]
        public MapRuntimeSetting mapSet;

        [FoldoutGroup("配置scene")]
        [LabelText("地形配置")]
        public TerrainSettingSO terSet;     // 自觉不要在外部对这个东西进行修改

        [FoldoutGroup("配置scene")]
        [LabelText("地图Hex配置")]
        public HexSettingSO hexSet;

        [FoldoutGroup("配置scene")]
        [LabelText("格子地形数据")]
        public GridTerrainSO gridTerrainSO;

        [FoldoutGroup("配置scene")]
        [LabelText("区域数据")]
        public CountrySO countrySO;

        protected override void InitEditor() {
            //if (terSet == null) {
            //    string terrainSettingPath = MapStoreEnum.WarGameMapSettingPath + ;
            //    terSet = AssetDatabase.LoadAssetAtPath<TerrainSettingSO>(terrainSettingPath);
            //    if (terSet == null) {
            //        terSet = CreateInstance<TerrainSettingSO>();
            //        AssetDatabase.CreateAsset(terSet, terrainSettingPath);
            //        Debug.Log($"successfully create Terrain Setting, path : {terrainSettingPath}");
            //    }
            //}
            //if (hexSet == null) {
            //    string hexSettingPath = MapStoreEnum.WarGameMapSettingPath + ;
            //    hexSet = AssetDatabase.LoadAssetAtPath<HexSettingSO>(hexSettingPath);
            //    if (hexSet == null) {       // create it !
            //        hexSet = CreateInstance<HexSettingSO>();
            //        AssetDatabase.CreateAsset(hexSet, hexSettingPath);
            //        Debug.Log($"successfully create Hex Setting, path : {hexSettingPath}");
            //    }
            //}

            //FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");
            //FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");
            //FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");
            mapSet = EditorSceneManager.MapSet;
            terSet = EditorSceneManager.TerSet;
            hexSet = EditorSceneManager.HexSet;
            gridTerrainSO = EditorSceneManager.GridTerrainSO;
            countrySO = EditorSceneManager.CountrySO;

            EditorSceneManager.GetInstance().LoadMapRenderer(mainMaterial, terrainLandformMat, riverMaterial);

            base.InitEditor();
        }


        #region Terrain Scene/数据

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain生成方式")]
        public TerMeshGenMethod GenMethod;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Material")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("地图主材质")]
        public Material mainMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("地貌材质")]
        public Material terrainLandformMat;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("河流材质")]
        public Material riverMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("ter材质-用于编辑")]
        public Material terMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("hex材质-用于编辑")]
        public Material hexMaterial;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Mesh 数据")]    // serialized file data
        public TerrainMeshDataBinder terAssetBinder;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Binder 文件夹路径")]
        public string terBinderPath = MapStoreEnum.WarGameMapEditObjPath;

        [FoldoutGroup("Editor 场景配置")]
        [LabelText("Terrain Mesh 文件夹路径")]
        public string clsMeshDataPath = MapStoreEnum.TerrainMeshSerializedPath;

        [FoldoutGroup("Editor 场景配置")]
        [Button("一键导入所有 Terrain Mesh 数据", ButtonSizes.Medium)]
        private void ImportClusterMeshDatas() {
            FindOrCreateSO(ref terAssetBinder, terBinderPath, "TerrainMeshDataBinder.asset");

            // read mesh data from the path
            string[] filePaths = Directory.GetFiles(clsMeshDataPath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta")).ToArray();
            terAssetBinder.LoadAsset(filePaths);
        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("清空 Ter Scene", ButtonSizes.Medium)]
        private void ClearTerScene() {
            EditorSceneManager.GetInstance().ClearTerScene();
        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("初始化 Ter Scene", ButtonSizes.Medium)]   // so that you can view the terrain cluster in scene
        private void InitSceneManagerTer() {

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            EditorSceneManager.GetInstance().LoadTerScene(5, terAssetBinder.MeshBinderList, heightDataModels, terMaterial);

            EditorSceneManager.GetInstance().LoadHexScene(hexMaterial);

            EditorSceneManager.GetInstance().LoadMapRenderer(mainMaterial, terrainLandformMat, riverMaterial);

            stopwatch.Stop();
            Debug.Log($"init scene manager ter scene successfully! cost {stopwatch.ElapsedMilliseconds} ms");
        }


        [FoldoutGroup("Editor 场景配置")]
        [Button("清空 Hex Scene", ButtonSizes.Medium)]
        private void ClearHexScene() {
            EditorSceneManager.GetInstance().ClearHexScene();
        }

        [FoldoutGroup("Editor 场景配置")]
        [Button("初始化 Hex Scene", ButtonSizes.Medium)]
        private void InitSceneManagerHex() {

        }

        #endregion

        
        #region 渲染设置

        [FoldoutGroup("渲染 设置")]
        [Button("test", ButtonSizes.Medium)]
        private void SetRenderTest()
        {
            // TODO : 要在这里集中地管理 Render 资产
        }

        #endregion

    }
}
