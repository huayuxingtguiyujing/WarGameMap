using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {
    [Serializable]
    [CreateAssetMenu(fileName = "TerrainSetting_Default", menuName = "WarGameMap/Set/TerrainSetting", order = 2)]
    public class TerrainSettingSO : MapSettingSO {

        public override string MapSettingType {
            get {
                return "TerrainSetting";
            }
        }

        public override string MapSettingDescription {
            get {
                return "setting for game map terrain, include lod levels, cluster size, tile size; and note terrain is not gameplay only to show";
            }
        }

        [LabelText("LOD总层级数")]
        public int LODLevel = 5;

        [LabelText("Terrain大小")]
        [Tooltip("大地图规模，表示共有多少个cluster，它不必是2的倍数")]
        public Vector3Int terrainSize = new Vector3Int(10, 0, 10);

        [LabelText("cluster大小")]
        [Tooltip("cluster规模，y轴代表对高度数据的放大操作")]
        public int clusterSize = MapTerrainEnum.ClusterSize;

        [LabelText("地块大小")]
        public int tileSize = MapTerrainEnum.TileSize;

        public TerrainSetting GetTerrainSetting() {
            return new TerrainSetting(
                LODLevel, terrainSize, clusterSize, tileSize    
            );
        }

    }

    public class TerrainSetting : IBinarySerializer {

        public int LODLevel { get; private set; }

        public Vector3Int terrainSize { get; private set; }

        public int clusterSize { get; private set; }

        public int tileSize { get; private set; }

        public TerrainSetting(){ }

        public TerrainSetting(int lODLevel, Vector3Int terrainSize, int clusterSize, int tileSize) {
            LODLevel = lODLevel;
            this.terrainSize = terrainSize;
            this.clusterSize = clusterSize;
            this.tileSize = tileSize;
        }

        public override string ToString() {
            return $"{LODLevel},{terrainSize.ToStringFixed()},{clusterSize},{tileSize}";
        }

        public void WriteToBinary(BinaryWriter writer) {
            writer.Write(LODLevel);
            writer.Write(terrainSize.ToStringFixed());
            writer.Write(clusterSize);
            writer.Write(tileSize);
        }

        public void ReadFromBinary(BinaryReader reader) {
            LODLevel = reader.ReadInt32();
            terrainSize = reader.ReadString().ToVector3Int();
            clusterSize = reader.ReadInt32();
            tileSize = reader.ReadInt32();
        }

    }
}
