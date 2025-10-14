using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    
    [Obsolete]
    [Serializable]
    public class HexMapSO : ScriptableObject {

        #region get/set ·½·¨

        public GridTerrainData GetTerrainData(List<Vector2Int> offsetHex) {
            // TODO : offset coord
            return null;
        }

        public void SetDirty()
        {
            //IsDirty = true;
        }

        #endregion

    }
}
