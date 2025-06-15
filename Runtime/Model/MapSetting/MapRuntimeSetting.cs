using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public enum LODSwitchMethod {
        Height, Distance
    }

    public class MapRuntimeSetting : MapSettingSO {
        public override string MapSettingName => "TerrainRuntimeSet_Default.asset";

        public override string MapSettingDescription => "TerrainRuntimeSet, but Runtime";

        [LabelText("LOD0 ����")]
        [Tooltip("�������������л�Ϊ������")]
        public float MeshFadeDistance = 500.0f;

        [LabelText("LOD�л���ʽ")]
        public LODSwitchMethod lodSwitchMethod = LODSwitchMethod.Height;

        [LabelText("���cluster��Ŀ")]
        [Tooltip("�������ڴ��cluster mesh���������Ŀ�󣬿�ʼ����ж��")]
        public float MaxClusterNum = 12;

        [LabelText("AOI ��Χ")]
        [Tooltip("��������� AOI ��Χ�ڵ����� cluster")]
        public int AOIScope = 1;

    }
}
