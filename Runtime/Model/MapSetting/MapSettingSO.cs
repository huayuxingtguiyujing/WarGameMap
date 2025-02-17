using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public abstract class MapSettingSO : ScriptableObject
    {

        public abstract string MapSettingType { get; }

        public abstract string MapSettingDescription { get; }

    }
}
