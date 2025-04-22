using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class NormalDataManager
    {

        Texture2D normalTex;

        int clusterSize;
        Vector3 clusterStartPoint;

        public NormalDataManager(Texture2D normalTex, int clusterSize, Vector3 clusterStartPoint) {
            this.normalTex = normalTex;
            this.clusterSize = clusterSize;
            this.clusterStartPoint = clusterStartPoint;
        }

        //public Vector3 SampleNormalFromTexture(Vector3 vertPos) {
        //    // TODO : Íê³ÉËü£¡
        //}

    }
}
