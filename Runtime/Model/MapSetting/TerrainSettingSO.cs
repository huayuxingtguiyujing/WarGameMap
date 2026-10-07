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
                return "Setting for game map terrain, include lod levels, cluster size, tile size; and note terrain is not gameplay only to show";
            }
        }

        [Header("LOD Setting")]
        [LabelText("LOD总层级数")]
        public int LODLevel = 3;

        // recommend : 
        //  0 : 0.1
        //  1 : 0.2
        //  2 : 0.35
        //  3 : 0.6
        //  4 : 1.0
        [LabelText("LOD各级的简化程度")]
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


        [Header("Terrain Setting")]
        [LabelText("Terrain大小")]
        [Tooltip("大地图规模，表示共有多少个cluster，它不必是2的倍数")]
        public Vector3Int terrainSize = new Vector3Int(10, 0, 10);

        [LabelText("起始地块经纬度")]
        [Tooltip("左下角的地块的经纬度")]
        public Vector2Int startLL;

        [LabelText("cluster大小")]
        [Tooltip("cluster规模，y轴代表对高度数据的放大操作")]
        public int clusterSize = MapTerrainEnum.ClusterSize;

        [LabelText("cluster-修正后大小")]
        [Tooltip("cluster修正后规模，用于生成纹理时进行宽度扩展")]
        public int fixedClusterSize = MapTerrainEnum.ClusterSize + 20;

        [LabelText("地块大小")]
        public int tileSize = MapTerrainEnum.TileSize;

        [LabelText("高度缩放")]
        [Tooltip("归一化高度 [0,1] 转世界单位的倍率，默认 500 以维持现有观感")]
        public float heightScale = 100f;

        public int GetTileNumClsPerLine()
        {
            return clusterSize / tileSize;
        }

        public int GetTilePointNumMaxLOD()
        {
            return tileSize * tileSize;
        }

        public int GetClusterPointNumMaxLOD()
        {
            return clusterSize * clusterSize;
        }


        #region Camera Setting

        [Header("Camera Setting")]
        [LabelText("摄像机初始位置")]
        public Vector3 cameraInitialPosition = new Vector3(0f, 100f, 0f);

        [LabelText("摄像机初始倾斜角度")]
        [Tooltip("从竖直向下朝世界 +Z 偏转的角度，15度对应 Unity Euler X=75度")]
        public float cameraInitialTiltAngle = 15f;

        #endregion

        // River setting
        [Header("River Setting")]
        [LabelText("河流编辑数据相比大地图的缩放")]
        public ushort paintRTSizeScale = 1;     // only editor

        [LabelText("河道最大沉降")]
        public int riverDownOffset = 15;

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

        #region Terrain Cluster File Name
        public static string GetClusterFileSuffixName()
        {
            return "*_LOD*_terrain_cluster.bytes";
        }

        public static string GetClusterFileName(long longitude, long latitude, int lodLevel)
        {
            return $"{longitude}_{latitude}_LOD{lodLevel}_terrain_cluster.bytes";
        }

        public static bool TryParseClusterFileName(string fileName, out long longitude, out long latitude, out int lodLevel)
        {
            longitude = 0;
            latitude = 0;
            lodLevel = -1;
            string name = Path.GetFileNameWithoutExtension(fileName);
            const string suffix = "_terrain_cluster";
            bool hasSuffix = name.EndsWith(suffix, StringComparison.Ordinal);
            if (!hasSuffix)
            {
                return false;
            }
            string core = name.Substring(0, name.Length - suffix.Length);
            string[] parts = core.Split('_');
            if (parts.Length != 3)
            {
                return false;
            }
            bool hasLodPrefix = parts[2].StartsWith("LOD", StringComparison.Ordinal);
            if (!hasLodPrefix)
            {
                return false;
            }
            bool parsedLongitude = long.TryParse(parts[0], out longitude);
            bool parsedLatitude = long.TryParse(parts[1], out latitude);
            bool parsedLod = int.TryParse(parts[2].Substring(3), out lodLevel);
            bool validLod = parsedLod && lodLevel >= 0;
            return parsedLongitude && parsedLatitude && validLod;
        }
        #endregion
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
