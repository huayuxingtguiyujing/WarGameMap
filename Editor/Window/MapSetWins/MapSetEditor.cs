using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class MapSetEditor : BaseMapEditor {
        public override string EditorName => MapEditorEnum.MapSetEditor;




        [FoldoutGroup("����scene")]
        [LabelText("��������")]
        public TerrainSettingSO terSet;

        [FoldoutGroup("����scene")]
        [LabelText("��ͼHex����")]
        public HexSettingSO hexSet;

        [FoldoutGroup("����scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("����: û�г�ʼ��Scene")]
        public string warningMessage = "������ť��ʼ��!";


        public static MapSetEditor Instance { get; private set; }

        public static TerrainSettingSO GetTerSet() {
            return Instance.terSet;
        }

        public static HexSettingSO GetHexSet() {
            return Instance.hexSet;
        }


        [FoldoutGroup("����scene", -1)]
        [Button("��ʼ����������")]
        protected override void InitEditor() {
            Instance = this;

            if (terSet == null) {
                string terrainSettingPath = MapStoreEnum.WarGameMapSettingPath + "/TerrainSetting_Default.asset";
                terSet = AssetDatabase.LoadAssetAtPath<TerrainSettingSO>(terrainSettingPath);
                if (terSet == null) {
                    // create it !
                    terSet = CreateInstance<TerrainSettingSO>();
                    AssetDatabase.CreateAsset(terSet, terrainSettingPath);
                    Debug.Log($"successfully create Terrain Setting, path : {terrainSettingPath}");
                }
            }

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

            notInitScene = false;
        }


    }
}
