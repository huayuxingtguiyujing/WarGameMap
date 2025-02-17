using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public interface ISerializer
    {

        public abstract void SerializeObject();

        public abstract void UnserializeObject();

    }
}
