using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {

    public class MapRiverData : ScriptableObject
    {

        [LabelText("河流ID")]
        [Tooltip("不要修改这个字段，它代表了现在河流使用的ID")]
        public ushort RiverCount = 0;

        public ushort GetRiverID() {
            RiverCount++;
            return RiverCount;
        }

        public TerrainSettingSO bindTerSet;

        public HexSettingSO bindHexSet;


        [LabelText("河流数据列表")]
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
                // TODO : 还没写完，可能复制构函要做更多操作
                RiverDataDict[riverData.riverID].CopyRiverData(riverData);
                Debug.Log($"already contains river id : {riverData.riverID}, so copy it");
            } else {
                RiverDatas.Add(riverData);
            }
        }

    }
}
