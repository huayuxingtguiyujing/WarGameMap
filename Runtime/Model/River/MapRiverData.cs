using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {

    public class MapRiverData : ScriptableObject
    {

        [LabelText("����ID")]
        [Tooltip("��Ҫ�޸�����ֶΣ������������ں���ʹ�õ�ID")]
        public ushort RiverCount = 0;

        public ushort GetRiverID() {
            RiverCount++;
            return RiverCount;
        }

        public TerrainSettingSO bindTerSet;

        public HexSettingSO bindHexSet;


        [LabelText("���������б�")]
        public List<RiverData> RiverDatas = new List<RiverData>();

        Dictionary<ushort, RiverData> RiverDataDict = new Dictionary<ushort, RiverData>();

        public void InitMapRiverData(TerrainSettingSO bindTerSet, HexSettingSO bindHexSet) {
            this.bindTerSet = bindTerSet;
            this.bindHexSet = bindHexSet;
        }

        public void UpdateMapRiverData() {
            RiverDataDict.Clear();
            foreach (var riverData in RiverDatas)
            {
                RiverDataDict.Add(riverData.riverID, riverData);
            }
        }

        public void AddRiverData(RiverData riverData) {
            UpdateMapRiverData();
            if (RiverDataDict.ContainsKey(riverData.riverID)) {
                // TODO : ��ûд�꣬���ܸ��ƹ���Ҫ���������
                RiverDataDict[riverData.riverID].CopyRiverData(riverData);
                Debug.Log($"already contains river id : {riverData.riverID}, so copy it");
            } else {
                RiverDatas.Add(riverData);
            }
        }

    }
}
