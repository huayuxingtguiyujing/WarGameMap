using CodiceApp;
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

        [LabelText("LOD总层级数")]
        public int LODLevel = 5;

        [LabelText("Terrain大小")]
        [Tooltip("大地图规模，表示共有多少个cluster，它不必是2的倍数")]
        public Vector3Int terrainSize = new Vector3Int(10, 0, 10);

        [LabelText("起始地块经纬度")]
        [Tooltip("左下角的地块的经纬度")]
        public Vector2Int startLL;

        [LabelText("cluster大小")]
        [Tooltip("cluster规模，y轴代表对高度数据的放大操作")]
        public int clusterSize = MapTerrainEnum.ClusterSize;

        [LabelText("地块大小")]
        public int tileSize = MapTerrainEnum.TileSize;

        // river setting
        [LabelText("河流编辑数据相比大地图的缩放")]
        public ushort paintRTSizeScale = 4;     // only editor

        [LabelText("河道最大沉降")]
        public int riverDownOffset = 5;

        [LabelText("非河流颜色（纹理存储）")]
        public Color noRiverColor = Color.white;

        [LabelText("河流颜色（纹理存储）")]
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
