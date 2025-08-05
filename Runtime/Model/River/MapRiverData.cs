using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {
    using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;
    using RvClusterPixelDict = Dictionary<Vector2Int, List<Vector2Int>>;

    public enum RiverDataFlow
    {
        Texture,       // 以纹理 (pixel) 的形式 保存河流数据       
        Bezier,        // 以贝塞尔曲线的形式    保存河流数据
    }

    public static class RiverPaintColor
    {
        public static Color noRvColor = Color.white;

        public static Color riverColor = Color.blue;

        public static Color rvStartColor = Color.green;
    }

    public class MapRiverData : ScriptableObject
    {

        [LabelText("河流ID")]
        [Tooltip("不要修改这个字段，它代表了现在河流使用的ID")]
        public ushort RiverCount = 0;

        public ushort GetRiverID() {
            RiverCount++;
            return RiverCount;
        }

        public TerrainSettingSO bindTerSet;

        public HexSettingSO bindHexSet;


        [LabelText("河流数据列表")]
        public List<RiverData> RiverDatas = new List<RiverData>();

        Dictionary<ushort, RiverData> RiverDataDict = new Dictionary<ushort, RiverData>();


        Dictionary<Vector2Int, List<RiverData>> clusterExistRiverDict;      // Key : terrain cluster index, Value : riverdata exists in terrain cluster
        public Dictionary<Vector2Int, List<RiverData>> ClusterExistRiverDict { get => clusterExistRiverDict; }


        public void InitMapRiverData(TerrainSettingSO bindTerSet, HexSettingSO bindHexSet) {
            this.bindTerSet = bindTerSet;
            this.bindHexSet = bindHexSet;
        }

        public void UpdateMapRiverData() {
            RiverDataDict.Clear();
            foreach (var riverData in RiverDatas)
            {
                RiverDataDict.Add(riverData.riverID, riverData);
            }
        }

        public void AddRiverData(RiverData riverData) {
            UpdateMapRiverData();
            if (RiverDataDict.ContainsKey(riverData.riverID)) {
                // TODO : 还没写完，可能复制构函要做更多操作
                RiverDataDict[riverData.riverID].CopyRiverData(riverData);
                Debug.Log($"already contains river id : {riverData.riverID}, so copy it");
            } else {
                RiverDatas.Add(riverData);
            }
        }

        public RiverData GetRiverData(ushort riverID)
        {
            if (RiverDataDict.ContainsKey(riverID))
            {
                return RiverDataDict[riverID];
            }
            return null;
        }


        public void UpdateClsExistRiverDict()
        {
            if(clusterExistRiverDict != null)
            {
                clusterExistRiverDict.Clear();
            }
            clusterExistRiverDict = new Dictionary<Vector2Int, List<RiverData>>();
            foreach (var river in RiverDatas)
            {
                foreach (var clusterID in river.existTerrainClusterIDs)
                {
                    if (clusterExistRiverDict.ContainsKey(clusterID))
                    {
                        clusterExistRiverDict[clusterID].Add(river);
                    }
                    else
                    {
                        clusterExistRiverDict.Add(clusterID, new List<RiverData>() { river });
                    }
                }

            }
        }

        public List<RiverData> GetClsExistRiverData(Vector2Int clusterID)
        {
            if (clusterExistRiverDict.ContainsKey(clusterID))
            {
                return clusterExistRiverDict[clusterID];
            }
            else
            {
                return new List<RiverData>();
            }
        }


        #region generate river data

        struct GenRiverTexJob : IJobParallelFor
        {
            [ReadOnly] public Color brushColor;
            [ReadOnly] public int riverTexSize;
            [ReadOnly] public int paintRTSizeScale;
            [ReadOnly] public NativeArray<Vector2Int> riverPixels;

            [NativeDisableParallelForRestriction]
            [WriteOnly] public NativeArray<Color> riverTexData;

            public void Execute(int index)
            {
                Vector2Int pixelPos = riverPixels[index] / paintRTSizeScale;
                int idx = pixelPos.y * riverTexSize + pixelPos.x;
                riverTexData[idx] = Color.blue; // gray
            }
        }

        public Texture2D GenRiverTexture(RiverDataFlow CurRiverDataFlow, TerrainSettingSO terSet)
        {
            int texSize = terSet.clusterSize * terSet.terrainSize.x / terSet.paintRTSizeScale;
            Texture2D riverTex = new Texture2D(texSize, texSize, TextureFormat.RGB24, false);
            NativeArray<Color> riverTexData = new NativeArray<Color>(texSize * texSize, Allocator.Persistent);
            for (int i = 0; i < riverTexData.Length; i++)
            {
                riverTexData[i] = Color.white;
            }
            foreach (var riverData in RiverDatas)
            {
                if (CurRiverDataFlow == RiverDataFlow.Texture)
                {
                    GenRiverTexture_RvTex(texSize, ref riverTexData, riverData, terSet.paintRTSizeScale);
                }
                else if (CurRiverDataFlow == RiverDataFlow.Bezier)
                {
                    GenRiverTexture_RvCurve(texSize, ref riverTexData, riverData, terSet.paintRTSizeScale);
                }
            }
            riverTex.SetPixels(riverTexData.ToArray());
            riverTex.Apply();
            riverTexData.Dispose();
            return riverTex;
        }

        private void GenRiverTexture_RvTex(int texSize, ref NativeArray<Color> riverTexData, RiverData riverData, int paintRTSizeScale)
        {
            List<Vector2Int> pixelsData = riverData.pixels;//  GetBrushedPixels();
            int riverPixelSize = pixelsData.Count;
            NativeArray<Vector2Int> riverPixels = new NativeArray<Vector2Int>(riverPixelSize, Allocator.TempJob);
            //riverPixels.CopyTo(riverData.pixels.ToArray());
            for (int i = 0; i < riverPixelSize; i++)
            {
                riverPixels[i] = pixelsData[i];
            }

            Debug.Log(riverPixelSize);

            GenRiverTexJob genRiverTexJob = new GenRiverTexJob()
            {
                brushColor = RiverPaintColor.riverColor,
                riverTexSize = texSize,
                paintRTSizeScale = paintRTSizeScale,
                riverPixels = riverPixels,
                riverTexData = riverTexData
            };
            JobHandle jobHandle = genRiverTexJob.Schedule(riverPixelSize, 16);
            jobHandle.Complete();

            riverPixels.Dispose();
        }

        private void GenRiverTexture_RvCurve(int texSize, ref NativeArray<Color> riverTexData, RiverData riverData, int paintRTSizeScale)
        {
            if (riverData.curve == null)
            {
                Debug.LogError($"cur workflow, but riverData {riverData.riverID} has no curve");
                return;
            }

            int maxIter = 10000;
            int iterTime = 0;
            bool isFinal = false;
            riverData.curve.InitGetDistanceCache();
            while (!isFinal && iterTime < maxIter)
            {
                riverData.curve.GetPointAtDistance(iterTime, out Vector3 point, out Vector3 tangent, out isFinal);
                //int idx = (int)point.y * texSize + (int)point.x;
                //riverTexData[idx] = Color.blue;

                // paint via tangent
                int scope = 15;
                Vector3 normal = new Vector3(-tangent.z, 0, tangent.x).normalized;
                for (int i = -scope; i <= scope; i++)
                {
                    if (i == 0 && scope % 2 == 0)
                    {
                        continue;
                    }
                    Vector3 sample = (point + normal * i * 0.5f) / paintRTSizeScale;

                    float ratio = 1 - 0.8f * Mathf.Abs(i) / scope;
                    int idx_sample_pixel = (int)sample.z * texSize + (int)sample.x;
                    riverTexData[idx_sample_pixel] = Color.Lerp(RiverPaintColor.noRvColor, RiverPaintColor.riverColor, ratio);
                }

                iterTime++;
            }
            //Debug.Log(iterTime);
        }

        #endregion


        public static Vector2Int GetMapPosByRvEditPixel(Vector2Int rvEditPixel, int clusterSize, int paintRTSizeScale, Vector2Int clusterID)
        {
            Vector2Int clusterStartPos = clusterID * clusterSize;
            Vector2Int curPixelPos = rvEditPixel * paintRTSizeScale + clusterStartPos;
            return curPixelPos;
        }

        public static Vector2Int GetRvEditPixelByMapPos(Vector2Int mapPosPixel, int clusterSize, int paintRTSizeScale, out Vector2Int clusterID)
        {
            clusterID = new Vector2Int(mapPosPixel.x / clusterSize, mapPosPixel.y / clusterSize);    // 为什么是反的？？？
            Vector2Int inclsPos = new Vector2Int(mapPosPixel.y % clusterSize, mapPosPixel.x % clusterSize);
            Vector2Int rvPixelPos = inclsPos / paintRTSizeScale;
            return rvPixelPos;
        }


        public static RiveStartData UnvalidRvStart = new RiveStartData(new Vector2Int(-1, -1), new Vector2Int(-1, -1));

    }
}
