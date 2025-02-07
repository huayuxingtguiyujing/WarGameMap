using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class HeightDataManager
    {

        //  自觉一点，不要改动 HeightDataModel 里的东西
        List<HeightDataModel> heightDataModels;

        int srcWidth;
        int srcHeight;

        Vector3Int terrainClusterSize;
        int terrainClusterWidth;
        int terrainClusterHeight;


        internal enum GetHeightDir {
            Center, Up, Down, Left, Right
        }

        class SampleCache {
            int longitude; 
            int latitude;

            HeightData centerHeightData;

            HeightData upHeightData;
            HeightData downHeightData;
            HeightData leftHeightData;
            HeightData rightHeightData;

            internal bool IsValid { get; private set; }

            internal SampleCache() {
                longitude = -1;
                latitude = -1;
                centerHeightData = null;
                IsValid = false;
            }

            internal void InitSampleCache(int longitude, int latitude, HeightData heightData, 
                HeightData upHeightData, HeightData downHeightData, HeightData leftHeightData, HeightData rightHeightData) {
                this.longitude = longitude;
                this.latitude = latitude;
                this.centerHeightData = heightData;
                this.upHeightData = upHeightData;
                this.downHeightData = downHeightData;
                this.leftHeightData = leftHeightData;
                this.rightHeightData = rightHeightData;

                IsValid = true;
            }

            internal bool IsDirty(int longitude, int latitude) {
                return longitude != this.longitude || latitude != this.latitude;
            }

            internal HeightData GetHeightData(GetHeightDir dir) {
                switch (dir) {
                    case GetHeightDir.Center:
                        return centerHeightData;
                    case GetHeightDir.Up:
                        return upHeightData;
                    case GetHeightDir.Down:
                        return downHeightData;
                    case GetHeightDir.Left:
                        return leftHeightData;
                    case GetHeightDir.Right:
                        return rightHeightData;
                    default:
                        return centerHeightData;
                }
                
            }

        }

        SampleCache sampleCache;

        public HeightDataManager() {
            heightDataModels = new List<HeightDataModel>();
        }

        public void InitHeightDataManager(List<HeightDataModel> heightDataModels, Vector3Int terrainClusterSize) {
            this.heightDataModels = heightDataModels;

            srcWidth = heightDataModels[0].singleHeightFileSize;
            srcHeight = heightDataModels[0].singleHeightFileSize;

            this.terrainClusterSize = terrainClusterSize;
            terrainClusterWidth = terrainClusterSize.x;
            terrainClusterHeight = terrainClusterSize.z;
        }


        #region sample height

        public float SampleFromHeightData(Vector2Int startLongitudeLatitude, Vector3 vertPos) {
            int startLongitude = startLongitudeLatitude.x;
            int startLatitude = startLongitudeLatitude.y;

            Vector3 clusterStartPoint = Vector3.zero;

            int longitude = Mathf.FloorToInt(vertPos.x) / terrainClusterWidth;
            int latitude = Mathf.FloorToInt(vertPos.z) / terrainClusterHeight;

            clusterStartPoint.x += longitude * terrainClusterWidth;
            clusterStartPoint.z += latitude * terrainClusterHeight;

            longitude += startLongitude;
            latitude += startLatitude;

            return SampleFromHeightData(longitude, latitude, vertPos, clusterStartPoint);
        }

        public float SampleFromHeightData(int longitude, int latitude, Vector3 vertPos, Vector3 clusterStartPoint) {

            if (sampleCache == null) {
                sampleCache = new SampleCache();
            }

            if (sampleCache.IsDirty(longitude, latitude)) {
                CacheSampleHandle(longitude, latitude);
            }

            HeightData heightData = sampleCache.GetHeightData(0);
            if (heightData != null) {

                // fixed the vert, because exist cluster offset!
                vertPos.x -= clusterStartPoint.x;
                vertPos.z -= clusterStartPoint.z;
                // resample the size of height map
                float sx = vertPos.x / terrainClusterWidth * srcWidth;
                float sy = vertPos.z / terrainClusterHeight * srcHeight;

                int x0 = Mathf.FloorToInt(sx); //Mathf.Clamp(, 0, srcWidth - 1);
                int x1 = x0 + 1; // Mathf.Min(, srcWidth - 1);
                int y0 = Mathf.FloorToInt(sy); // Mathf.Clamp(, 0, srcHeight - 1); ;
                int y1 = y0 + 1; // Mathf.Min(, srcHeight - 1);

                float q00 = GetHeightVal(longitude, latitude, x0, y0, heightData);
                float q01 = GetHeightVal(longitude, latitude, x0, y1, heightData);
                float q10 = GetHeightVal(longitude, latitude, x1, y0, heightData);
                float q11 = GetHeightVal(longitude, latitude, x1, y1, heightData);

                float rx0 = Mathf.Lerp(q00, q10, sx - x0);
                float rx1 = Mathf.Lerp(q01, q11, sx - x0);

                // caculate the height by the data given
                float h = Mathf.Lerp(rx0, rx1, sy - y0) * terrainClusterSize.y;
                float fixed_h = Mathf.Clamp(h, 0, 50);

                return fixed_h;
            }

            return 0;
        }

        private float GetHeightVal(int longitude, int latitude, int x, int y, HeightData heightData) {
            // if x y not in this scope, find in other HeightData
            if (x < 0) {
                x += srcWidth;
                HeightData left = sampleCache.GetHeightData(GetHeightDir.Left);
                if (left != null) {
                    return GetHeightVal(longitude - 1, latitude, x, y, left);
                }
                x = 0;
            } else if (x > srcWidth - 1) {
                x -= srcWidth;
                HeightData right = sampleCache.GetHeightData(GetHeightDir.Right);
                if (right != null) {
                    return GetHeightVal(longitude + 1, latitude, x, y, right);
                }
                x = srcWidth - 1;
            }

            if (y < 0) {
                y += srcHeight;
                HeightData down = sampleCache.GetHeightData(GetHeightDir.Down);
                if (down != null) {
                    return GetHeightVal(longitude, latitude - 1, x, y, down);
                }
                y = 0;
            } else if (y > srcHeight - 1) {
                y -= srcHeight;
                HeightData up = sampleCache.GetHeightData(GetHeightDir.Up);
                if (up != null) {
                    return GetHeightVal(longitude, latitude + 1, x, y, up);
                }
                y = srcHeight - 1;
            }

            // in scope, so find the HeightData and return value
            return heightData[x, y];
        }

        private HeightData CacheSampleHandle(int longitude, int latitude) {
            HeightData heightData = null;

            HeightData upHeightData = null;
            HeightData downHeightData = null;
            HeightData leftHeightData = null;
            HeightData rightHeightData = null;

            foreach (var model in heightDataModels) {
                if (model.ExistHeightData(longitude, latitude)) {
                    heightData = model.GetHeightData(longitude, latitude);
                }

                // TODO : 搞错方位了......
                if (model.ExistHeightData(longitude - 1, latitude)) {
                    //Debug.Log($"NOTE : init left height data, {longitude - 1}, {latitude}");
                    leftHeightData = model.GetHeightData(longitude - 1, latitude);
                }
                if (model.ExistHeightData(longitude + 1, latitude)) {
                    //Debug.Log($"NOTE : init right height data, {longitude + 1}, {latitude}");
                    rightHeightData = model.GetHeightData(longitude + 1, latitude);
                }
                if (model.ExistHeightData(longitude, latitude - 1)) {
                    //Debug.Log($"NOTE : init down height data, {longitude}, {latitude - 1}");
                    downHeightData = model.GetHeightData(longitude, latitude - 1);
                }
                if (model.ExistHeightData(longitude, latitude + 1)) {
                    //Debug.Log($"NOTE : init up height data, {longitude}, {latitude + 1}");
                    upHeightData = model.GetHeightData(longitude, latitude + 1);
                }
            }

            // set the sample data ( longitude and latitude )
            sampleCache.InitSampleCache(longitude, latitude, heightData,
                upHeightData, downHeightData, leftHeightData, rightHeightData);
            return heightData;
        }

        #endregion
    }
}
