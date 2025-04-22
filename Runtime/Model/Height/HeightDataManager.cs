using LZ.WarGameCommon;
using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static LZ.WarGameMap.Runtime.RawHexMapSO;

namespace LZ.WarGameMap.Runtime
{
    public class HeightDataManager
    {

        //  自觉一点，不要改动 HeightDataModel 里的东西
        List<HeightDataModel> heightDataModels;
        public List<HeightDataModel> HeightDataModels { get { return heightDataModels; } }

        // needed when generate terrain by hexMapSO
        HexSettingSO HexSet;
        RawHexMapSO RawHexMap;

        int srcWidth;
        int srcHeight;

        int terrainClusterSize;
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

        public void InitHeightDataManager(List<HeightDataModel> heightDataModels, int terrainClusterSize) {
            this.heightDataModels = heightDataModels;

            srcWidth = heightDataModels[0].singleHeightFileSize;
            srcHeight = heightDataModels[0].singleHeightFileSize;

            this.terrainClusterSize = terrainClusterSize;
            terrainClusterWidth = terrainClusterSize;
            terrainClusterHeight = terrainClusterSize;
        }


        #region sample height

        public float SampleFromHeightData(Vector2Int startLongitudeLatitude, Vector3 vertPos) {
            Vector3 clusterStartPoint = Vector3.zero;
            Vector2Int longAndLat = FixVertPosInCluster(startLongitudeLatitude, vertPos, ref clusterStartPoint);
            return SampleFromHeightData(longAndLat.x, longAndLat.y, vertPos, clusterStartPoint);
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

                int x0 = Mathf.FloorToInt(sx);
                int x1 = x0 + 1;
                int y0 = Mathf.FloorToInt(sy);
                int y1 = y0 + 1;

                float q00 = GetHeightVal(longitude, latitude, x0, y0, heightData);
                float q01 = GetHeightVal(longitude, latitude, x0, y1, heightData);
                float q10 = GetHeightVal(longitude, latitude, x1, y0, heightData);
                float q11 = GetHeightVal(longitude, latitude, x1, y1, heightData);

                float rx0 = Mathf.Lerp(q00, q10, sx - x0);
                float rx1 = Mathf.Lerp(q01, q11, sx - x0);

                // caculate the height by the data given
                float h = Mathf.Lerp(rx0, rx1, sy - y0) * terrainClusterSize;
                float fixed_h = Mathf.Clamp(h, 0, 50);
                return fixed_h;
            }

            return 0;
        }

        public TDList<float> SampleScopeFromHeightData(Vector2Int startLongitudeLatitude, Vector3 vertPos, int scope) {
            Vector3 clusterStartPoint = Vector3.zero;
            Vector2Int longAndLat = FixVertPosInCluster(startLongitudeLatitude, vertPos, ref clusterStartPoint);
            int longitude = longAndLat.x;
            int latitude = longAndLat.y;

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

                int x0 = Mathf.FloorToInt(sx);
                int y0 = Mathf.FloorToInt(sy);

                int startX = Mathf.Max(x0 - scope, 0);
                int endX = Mathf.Min(x0 + scope, srcWidth - 1);
                int startY = Mathf.Max(y0 - scope, 0);
                int endY = Mathf.Min(y0 + scope, srcHeight - 1);

                TDList<float> heights = new TDList<float>(endX - startX + 1, endY - startY + 1);

                for (int i = startX; i <= endX; i++) {
                    for(int j = startY; j <= endY; j++) {
                        heights[i - startX, j - startY] = GetHeightVal(longitude, latitude, i, j, heightData);
                    }
                }
                return heights;
            }
            TDList<float> ans = new TDList<float>(1, 1);
            ans[0, 0] = -1;
            return ans;
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

        #region sample height, but byHexMap

        public void InitHexSet(HexSettingSO HexSet, RawHexMapSO RawHexMap) {
            this.HexSet = HexSet;
            this.RawHexMap = RawHexMap;
        }

        int cnt = 10;
        public float SampleFromHexMap(Vector3 vertPos) {
            if (HexSet == null || RawHexMap == null) {
                return 0;
            }
            // get hex grid idx by vertPos
            Vector2 pos = new Vector2(vertPos.x, vertPos.z);
            Vector2Int offsetHexPos = HexHelper.AxialToOffset(HexHelper.PixelToAxialHex(pos, HexSet.hexGridSize));

            // get the average height and other data
            GridTerrainData terrainData = RawHexMap.GetTerrainData(offsetHexPos);
            if(terrainData == null) {
                //if(cnt > 0) {
                //    Debug.Log($"{pos}, {offsetHexPos} not found");
                //    cnt--;
                //}
                return -10;
            }

            float height = terrainData.baseHeight;

            float fix = 0;
            //float fix = terrainData.hillHeightFix * 0.2f * UnityEngine.Random.Range(-2, 3);

            // return the height

            return height + fix;
        }

        #endregion


        #region sample normal

        public Vector3 SampleNormalFromData(Vector2Int startLongitudeLatitude, Vector3 vertPos) {
            Vector3 clusterStartPoint = Vector3.zero;
            Vector2Int longAndLat = FixVertPosInCluster(startLongitudeLatitude, vertPos, ref clusterStartPoint);
            
            return SampleNormalFromData(longAndLat.x, longAndLat.y, vertPos, clusterStartPoint);
        }

        public Vector3 SampleNormalFromData(int longitude, int latitude, Vector3 vertPos, Vector3 clusterStartPoint) {
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

                int x0 = Mathf.FloorToInt(sx);
                int x1 = x0 + 1;
                int y0 = Mathf.FloorToInt(sy);
                int y1 = y0 + 1;

                float scale = 1000;
                float q00 = GetHeightVal(longitude, latitude, x0, y0, heightData) * scale;
                float q01 = GetHeightVal(longitude, latitude, x0, y1, heightData) * scale;
                float q10 = GetHeightVal(longitude, latitude, x1, y0, heightData) * scale;
                float q11 = GetHeightVal(longitude, latitude, x1, y1, heightData) * scale;

                // 计算梯度（Sobel 算子）
                Vector3 normal = new Vector3(q00 - q10, 4.0f, q01 - q11);
                return normal.normalized;
            }
            return new Vector3(0, 1, 0);
        }

        #endregion

        private Vector2Int FixVertPosInCluster(Vector2Int startLongitudeLatitude, Vector3 vertPos, ref Vector3 clusterStartPoint) {
            int startLongitude = startLongitudeLatitude.x;
            int startLatitude = startLongitudeLatitude.y;

            clusterStartPoint = Vector3.zero;

            // get the start point of cluster's which the pos on
            int longitude = Mathf.FloorToInt(vertPos.x) / terrainClusterWidth;
            int latitude = Mathf.FloorToInt(vertPos.z) / terrainClusterHeight;

            clusterStartPoint.x += longitude * terrainClusterWidth;
            clusterStartPoint.z += latitude * terrainClusterHeight;

            longitude += startLongitude;
            latitude += startLatitude;

            // get the cluster idx(longitude, latitude) of the position
            return new Vector2Int(longitude, latitude);
        }


    }
}
