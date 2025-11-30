using Sirenix.OdinInspector;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public enum LODSwitchMethod 
    {
        Height, Distance
    }

    public class MapRuntimeSetting : MapSettingSO 
    {
        public override string MapSettingName => "TerrainRuntimeSet_Default.asset";

        public override string MapSettingDescription => "TerrainRuntimeSet, but Runtime";

        [LabelText("LOD0 距离")]
        [Tooltip("超出这个距离后，切换为纯纹理")]
        public float MeshFadeDistance = 500.0f;

        [LabelText("LOD切换方式")]
        public LODSwitchMethod lodSwitchMethod = LODSwitchMethod.Height;

        [LabelText("最大cluster数目")]
        [Tooltip("已载入内存的cluster mesh超出这个数目后，开始进行卸载")]
        public float MaxClusterNum = 12;

        [LabelText("是否使用AOI")]
        public bool UseAOI = true;

        [LabelText("AOI 范围")]
        [Tooltip("加载摄像机 AOI 范围内的所有 cluster")]
        public int AOIScope = 1;

    }
}
