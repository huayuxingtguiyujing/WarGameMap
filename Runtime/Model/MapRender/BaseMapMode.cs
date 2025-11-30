using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public abstract class BaseMapMode
    {

        // TODO : 到这里放置 MapRender 需要用到的 Material
        public BaseMapMode() { }

        public abstract string GetMapModeName();


        public abstract void EnterMapMode();

        public abstract void UpdateMapMode();

        public abstract void ExitMapMode();

    }
}
