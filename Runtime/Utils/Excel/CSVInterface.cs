
namespace LZ.WarGameMap.Runtime
{
    public interface CSVInterface
    {
        public string Serialize();

        public void Deserialize(string lineData);
    }
}
