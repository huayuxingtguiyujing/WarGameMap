using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public interface CSVInterface
    {
        public string Serialize();

        public void Deserialize(string lineData);
    }
}
