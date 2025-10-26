using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {
    public static class MapTerrainEnum
    {
        public const int ClusterSize = 512;

        public const int TileSize = 128;
    }

    [Serializable]
    [CreateAssetMenu(fileName = "TerrainSetting_Default", menuName = "WarGameMap/Set/TerrainSetting", order = 2)]
    public class TerrainSettingSO : MapSettingSO {

        public override string MapSettingName {
            get {
                return "TerrainSetting_Default.asset";
            }
        }

        public override string MapSettingDescription {
            get {
                return "setting for game map terrain, include lod levels, cluster size, tile size; and note terrain is not gameplay only to show";
            }
        }

        [LabelText("LOD�ܲ㼶��")]
        public int LODLevel = 3;

        // recommend : 
        //  0 : 0.1
        //  1 : 0.2
        //  2 : 0.35
        //  3 : 0.6
        //  4 : 1.0
        [LabelText("LOD�����ļ򻯳̶�")]
        public List<float> LODLevelSimplifyTarget = new List<float>() { 
            0.1f, 0.2f, 0.35f, 0.6f, 1.0f
        };  

        public float GetSimplifyTarget(int lodLevel)
        {
            if(lodLevel < 0)
            {
                Debug.LogError($"why lodLevel is less than 0? : {lodLevel}");
                return 1.0f;
            }
            lodLevel = Mathf.Min(lodLevel, LODLevelSimplifyTarget.Count - 1);
            return LODLevelSimplifyTarget[lodLevel];
        }


        [LabelText("Terrain��С")]
        [Tooltip("���ͼ��ģ����ʾ���ж��ٸ�cluster����������2�ı���")]
        public Vector3Int terrainSize = new Vector3Int(10, 0, 10);

        [LabelText("��ʼ�ؿ龭γ��")]
        [Tooltip("���½ǵĵؿ�ľ�γ��")]
        public Vector2Int startLL;

        [LabelText("cluster��С")]
        [Tooltip("cluster��ģ��y�����Ը߶����ݵķŴ����")]
        public int clusterSize = MapTerrainEnum.ClusterSize;

        [LabelText("cluster-�������С")]
        [Tooltip("cluster�������ģ��������������ʱ���п����չ")]
        public int fixedClusterSize = MapTerrainEnum.ClusterSize + 20;

        [LabelText("�ؿ��С")]
        public int tileSize = MapTerrainEnum.TileSize;

        public int GetTileNumClsPerLine()
        {
            return clusterSize / tileSize;
        }

        // river setting
        [LabelText("�����༭������ȴ��ͼ������")]
        public ushort paintRTSizeScale = 4;     // only editor

        [LabelText("�ӵ�������")]
        public int riverDownOffset = 15;

        [LabelText("�Ǻ�����ɫ������洢��")]
        public Color noRiverColor = Color.white;

        [LabelText("������ɫ������洢��")]
        public Color riverColor = Color.blue;


        public TerrainSetting GetTerrainSetting() {
            return new TerrainSetting(
                LODLevel, terrainSize, startLL, clusterSize, tileSize, 
                riverDownOffset, noRiverColor, riverColor
            );
        }

    }

    [Serializable]
    public class TerrainSetting : IBinarySerializer {

        public int LODLevel { get; private set; }

        public Vector3Int terrainSize { get; private set; }

        public Vector2Int startLL;

        public int clusterSize { get; private set; }

        public int tileSize { get; private set; }

        // river set
        public int riverDownOffset { get; private set; }

        public Color noRiverColor { get; private set; }

        public Color riverColor { get; private set; }

        public TerrainSetting(){ }

        public TerrainSetting(int lODLevel, Vector3Int terrainSize, Vector2Int startLongitudeAndLatitude, 
            int clusterSize, int tileSize, int riverDownOffset, Color noRiverColor, Color riverColor) {
            LODLevel = lODLevel;
            this.terrainSize = terrainSize;
            this.startLL = startLongitudeAndLatitude;
            this.clusterSize = clusterSize;
            this.tileSize = tileSize;
            this.riverDownOffset = riverDownOffset;
            this.noRiverColor = noRiverColor;
            this.riverColor = riverColor;
        }

        public void WriteToBinary(BinaryWriter writer) {
            writer.Write(LODLevel);
            writer.Write(terrainSize.ToStringFixed());
            writer.Write(startLL.ToStringFixed());
            writer.Write(clusterSize);
            writer.Write(tileSize);

            writer.Write(riverDownOffset);
            writer.Write(noRiverColor.ToStringFixedRGBA());
            writer.Write(riverColor.ToStringFixedRGBA());
            //public float riverDownOffset { get; private set; }
            //public Color noRiverColor { get; private set; }
            //public Color riverColor { get; private set; }
        }

        public void ReadFromBinary(BinaryReader reader) {
            LODLevel = reader.ReadInt32();
            terrainSize = reader.ReadString().ToVector3Int();
            startLL = reader.ReadString().ToVector2Int();
            clusterSize = reader.ReadInt32();
            tileSize = reader.ReadInt32();

            riverDownOffset = reader.ReadInt32();
            noRiverColor = reader.ReadString().ToColorRGBA();
            riverColor = reader.ReadString().ToColorRGBA();
        }

        // TODO : update them
        public override string ToString() {
            return $"{LODLevel},{terrainSize.ToStringFixed()},{startLL.ToStringFixed()},{clusterSize},{tileSize}";
        }

        public static bool operator ==(TerrainSetting x, TerrainSetting y) {
            return (x.LODLevel == y.LODLevel)
                && (x.terrainSize == y.terrainSize)
                && (x.startLL == y.startLL)
                && (x.clusterSize == y.clusterSize)
                && (x.tileSize == y.tileSize);
        }

        public static bool operator !=(TerrainSetting x, TerrainSetting y) {
            return !(x == y);
        }

    }
}
