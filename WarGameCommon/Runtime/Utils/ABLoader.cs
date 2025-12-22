using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameCommon
{
    public class ABLoader : Singleton<ABLoader>
    {
        public ABLoader() { }


        public AssetBundle LoadABFile(string path) {
            return AssetBundle.LoadFromFile(path);
        }

    }
}
