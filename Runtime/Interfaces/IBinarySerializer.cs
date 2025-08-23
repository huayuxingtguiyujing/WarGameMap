using System.IO;

namespace LZ.WarGameMap.Runtime
{
    public interface IBinarySerializer
    {

        public abstract void WriteToBinary(BinaryWriter writer);

        public abstract void ReadFromBinary(BinaryReader reader);

    }
}
