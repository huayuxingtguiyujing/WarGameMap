using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // TODO: ���ع����� 20 ���ֵ�����Ҫ����
    public enum MapTerrainType {
        Plain,      // ƽԭ
        Hill,       // ����
        Mountain,   // ɽ��
        //
        //
        //
        //
        //
    }


    public static class MapTerrainEnum {

        //public static Vector3Int ClusterSize = new Vector3Int(1024, 1000, 1024);

        public const int ClusterSize = 1024;

        public const int TileSize = 256;

    }

}
