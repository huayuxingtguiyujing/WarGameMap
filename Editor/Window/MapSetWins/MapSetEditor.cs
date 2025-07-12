using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    public class MapSetEditor : BaseMapEditor {
        public override string EditorName => MapEditorEnum.MapSetEditor;


        [FoldoutGroup("����scene")]
        [LabelText("��ͼRuntime����")]
        public MapRuntimeSetting mapSet;

        [FoldoutGroup("����scene")]
        [LabelText("��������")]
        public TerrainSettingSO terSet;     // �Ծ���Ҫ���ⲿ��������������޸�

        [FoldoutGroup("����scene")]
        [LabelText("��ͼHex����")]
        public HexSettingSO hexSet;

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

            FindOrCreateSO<MapRuntimeSetting>(ref mapSet, MapStoreEnum.WarGameMapSettingPath, "TerrainRuntimeSet_Default.asset");
            FindOrCreateSO<TerrainSettingSO>(ref terSet, MapStoreEnum.WarGameMapSettingPath, "TerrainSetting_Default.asset");
            FindOrCreateSO<HexSettingSO>(ref hexSet, MapStoreEnum.WarGameMapSettingPath, "HexSetting_Default.asset");

            base.InitEditor();
        }


        #region Terrain Scene/����

        [FoldoutGroup("Editor ��������")]
        [LabelText("Terrain���ɷ�ʽ")]
        public TerMeshGenMethod GenMethod;

        [FoldoutGroup("Editor ��������")]
        [LabelText("Terrain Material")]
        public List<HeightDataModel> heightDataModels;

        [FoldoutGroup("Editor ��������")]
        [LabelText("ter����")]
        public Material terMaterial;

        [FoldoutGroup("Editor ��������")]
        [LabelText("hex����")]
        public Material hexMaterial;

        [FoldoutGroup("Editor ��������")]
        [LabelText("Terrain Mesh ����")]    // serialized file data
        public TerrainMeshDataBinder terAssetBinder;

        [FoldoutGroup("Editor ��������")]
        [LabelText("Terrain Binder �ļ���·��")]
        public string terBinderPath = MapStoreEnum.WarGameMapEditObjPath;

        [FoldoutGroup("Editor ��������")]
        [LabelText("Terrain Mesh �ļ���·��")]
        public string clsMeshDataPath = MapStoreEnum.TerrainMeshSerializedPath;

        [FoldoutGroup("Editor ��������")]
        [Button("һ���������� Terrain Mesh ����", ButtonSizes.Medium)]
        private void ImportClusterMeshDatas() {
            FindOrCreateSO(ref terAssetBinder, terBinderPath, "TerrainMeshDataBinder.asset");

            // read mesh data from the path
            string[] filePaths = Directory.GetFiles(clsMeshDataPath, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta")).ToArray();
            terAssetBinder.LoadAsset(filePaths);
        }

        [FoldoutGroup("Editor ��������")]
        [Button("��� Ter Scene", ButtonSizes.Medium)]
        private void ClearTerScene() {
            EditorSceneManager.GetInstance().ClearTerScene();
        }

        [FoldoutGroup("Editor ��������")]
        [Button("��ʼ�� Ter Scene", ButtonSizes.Medium)]   // so that you can view the terrain cluster in scene
        private void InitSceneManagerTer() {

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            EditorSceneManager.GetInstance().LoadTerScene(5, terAssetBinder.MeshBinderList, heightDataModels, terMaterial);

            EditorSceneManager.GetInstance().LoadHexScene(hexMaterial);

            stopwatch.Stop();
            Debug.Log($"init scene manager ter scene successfully! cost {stopwatch.ElapsedMilliseconds} ms");
        }


        [FoldoutGroup("Editor ��������")]
        [Button("��� Hex Scene", ButtonSizes.Medium)]
        private void ClearHexScene() {
            EditorSceneManager.GetInstance().ClearHexScene();
        }

        [FoldoutGroup("Editor ��������")]
        [Button("��ʼ�� Hex Scene", ButtonSizes.Medium)]
        private void InitSceneManagerHex() {

        }

        #endregion


    }


}
