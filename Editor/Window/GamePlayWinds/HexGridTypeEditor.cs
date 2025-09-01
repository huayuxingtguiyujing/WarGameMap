using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public class HexGridTypeEditor : BrushHexmapEditor
    {
        public override string EditorName => MapEditorEnum.HexGridTypeEditor;

        protected override void InitEditor()
        {
            base.InitEditor();
            InitMapSetting();
            LoadTerrainType();
            LoadHexMapSO();
        }

        //SerializedObject serializedGridTerrainSO;
        GridTerrainSO gridTerrainSO;

        List<GridTerrainLayer> layerList;
        List<GridTerrainType> terrainTypeList;

        private void LoadTerrainType()
        {
            gridTerrainSO = GridTerrainSO.GetInstance();
            gridTerrainSO.UpdateTerSO();

            TerrainLayersList.Clear();
            TerrainTypesList.Clear();

            // load base list and etc
            foreach (var layer in gridTerrainSO.Base_GridLayerList)
            {
                TerrainLayersList.Add(layer.CopyObject());
            }
            foreach (var layer in gridTerrainSO.GridLayerList)
            {
                TerrainLayersList.Add(layer.CopyObject());
            }

            foreach (var type in gridTerrainSO.Base_GridTypeList)
            {
                TerrainTypesList.Add(type.CopyObject());
            }
            foreach (var type in gridTerrainSO.GridTypeList)
            {
                TerrainTypesList.Add(type.CopyObject());
            }
            //Debug.Log($"load terrain types over, TerrainLayersList : {TerrainLayersList.Count}, TerrainTypesList : {TerrainTypesList.Count}");
        }

        private void LoadHexMapSO()
        {
            string soName = $"RawHexMap_{hexSet.mapWidth}x{hexSet.mapHeight}_{UnityEngine.Random.Range(0, 100)}.asset";
            string RawHexPath = exportHexMapDataPath + $"/{soName}";
            if (hexMapSO == null)
            {
                hexMapSO = AssetDatabase.LoadAssetAtPath<HexMapSO>(RawHexPath);
                if (hexMapSO == null)
                {
                    AssetDatabase.CreateAsset(hexMapSO, RawHexPath);
                    Debug.Log($"successfully create Hex Map, path : {RawHexPath}");
                }
            }
        }

        public override void Destory()
        {
            base.Destory();
        }


        #region ���ӵ��α༭

        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("�༭ʹ�õĵ���")]
        [ValueDropdown("GetTerrainTypesList")]
        [OnValueChanged("OnCurGriTerrainChanged")]
        public string CurGridTerrainName;

        GridTerrainType CurGriTerrain;

        private IEnumerable<ValueDropdownItem<string>> GetTerrainTypesList()
        {
            List<ValueDropdownItem<string>> dropDownItemList = new List<ValueDropdownItem<string>>();
            foreach (var terrainType in TerrainTypesList)
            {
                dropDownItemList.Add(new ValueDropdownItem<string>(terrainType.terrainTypeChineseName, terrainType.terrainTypeName));
            }
            return dropDownItemList;
        }

        private void OnCurGriTerrainChanged()
        {
            if (notInitScene)
            {
                return;
            }
            CurGriTerrain = gridTerrainSO.GetTerrainType(CurGridTerrainName);
            SetBrushColor(CurGriTerrain.terrainEditColor);
            Debug.Log($"now you choose terrain type : {CurGriTerrain.terrainTypeName}");
        }


        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("���β㼶")] 
        public List<GridTerrainLayer> TerrainLayersList = new List<GridTerrainLayer>();

        [FoldoutGroup("���ӵ������ݱ༭")]
        [LabelText("�����б�")]
        public List<GridTerrainType> TerrainTypesList = new List<GridTerrainType>();

        [FoldoutGroup("���ӵ������ݱ༭")]
        [Button("�����������", ButtonSizes.Medium)]
        private void SaveTerrainTypeDatas()
        {
            gridTerrainSO.SaveGridTerSO(TerrainLayersList, TerrainTypesList);
        }

        #endregion

        #region ���ӵ������ݵ���

        // MapStoreEnum.TerrainHexmapGridDataPath

        [FoldoutGroup("���ӵ������ݵ���")]
        [LabelText("��ǰ��������")]
        public HexMapSO hexMapSO;

        [FoldoutGroup("���ӵ������ݵ���")]
        [LabelText("����λ��")]
        public string exportHexMapDataPath = MapStoreEnum.TerrainHexmapGridDataPath;

        [FoldoutGroup("���ӵ������ݵ���")]
        [Button("������ӱ༭���", ButtonSizes.Medium)]
        private void SaveGridEdit()
        {
            
        }

        private void TransTextureToGrid()
        {
            // TODO : 
        }

        #endregion

        protected override void PaintHexRTEvent(List<Vector2Int> offsetHexList)
        {
            base.PaintHexRTEvent(offsetHexList);
            if (hexMapSO == null)
            {
                Debug.LogError("hexMapSO is null");
                return;
            }
            if(CurGriTerrain == null)
            {
                Debug.Log("CurGriTerrain is null");
                return;
            }

            byte idx = (byte)GetIdxByCurGridTerrain();
            hexMapSO.UpdateGridTerrainData(offsetHexList, idx);
        }

        private int GetIdxByCurGridTerrain()
        {
            for(int i = 0; i < TerrainTypesList.Count; i++)
            {
                if (CurGriTerrain.terrainTypeName == TerrainTypesList[i].terrainTypeName)
                {
                    return i;
                }

            }
            return 0;
        }

        private GridTerrainType GetGridTerrainTypeByIdx(int i)
        {
            return TerrainTypesList[i];
        }

    }
}
