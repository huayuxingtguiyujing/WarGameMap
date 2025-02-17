using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public interface IBinarySerializer
    {

        public abstract void WriteToBinary(BinaryWriter writer);

        public abstract void ReadFromBinary(BinaryReader reader);

    }
}
