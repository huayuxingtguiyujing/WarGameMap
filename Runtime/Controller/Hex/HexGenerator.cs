using LZ.WarGameMap.Runtime.HexStruct;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class HexGenerator {
        

        public HexGenerator() {
            hexagonIdxDic = new Dictionary<Vector2Int, Hexagon>();
            hexagonNumDic = new Dictionary<uint, Hexagon>();
        }

        // NOTE: hexagon 中存储的是偏移坐标，此处使用轴向坐标作为 Key
        // 轴向坐标 - Hexagon 的映射(纯数据)
        private Dictionary<Vector2Int, Hexagon> hexagonIdxDic;

        // ID - Hexagon 的映射
        private Dictionary<uint, Hexagon> hexagonNumDic;

        public Dictionary<Vector2Int, Hexagon> HexagonIdxDic {
            get => hexagonIdxDic;
            private set => hexagonIdxDic = value;
        }

        //public Dictionary<uint, Hexagon> HexagonNumDic {
        //    get => hexagonNumDic;
        //    private set => hexagonNumDic = value;
        //}


        #region 不同的生成方法
        public void ClearHexagon() {
            hexagonIdxDic.Clear();
            hexagonNumDic.Clear();
        }

        /// <summary>
        /// 生成平行四边形状 的六边形集体
        /// </summary>
        /// <param name="q1">q方向的起点</param>
        /// <param name="q2">q方向的终点</param>
        /// <param name="r1">r方向的起点</param>
        /// <param name="r2">r方向的终点</param>
        public void GenerateParallelogram(int q1, int q2, int r1, int r2) {
            uint count = 0;
            for (int q = q1; q <= q2; q++) {
                for (int r = r1; r <= r2; r++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexagonIdxDic.Add((Vector2Int)hexagon, hexagon);
                    hexagonNumDic.Add((uint)count, hexagon);
                    count++;
                }
            }
        }

        /// <summary>
        /// 生成 三角形状 的六边形集体
        /// </summary>
        /// <param name="mapSize">地图尺寸</param>
        public void GenerateTriangle(int mapSize) {
            uint count = 0;
            for (int q = 0; q <= mapSize; q++) {
                for (int r = 0; r <= mapSize - q; r++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexagonIdxDic.Add((Vector2Int)hexagon, hexagon);
                    hexagonNumDic.Add((uint)count, hexagon);
                    count++;
                }
            }
        }

        /// <summary>
        /// 生成 六边形状 的六边形集体
        /// </summary>
        /// <param name="mapSize">地图尺寸</param>
        public void GenerateHexagon(int mapSize) {
            uint count = 0;
            for (int q = -mapSize; q <= mapSize; q++) {
                int r1 = Mathf.Max(-mapSize, -q - mapSize);
                int r2 = Mathf.Min(mapSize, -q + mapSize);
                for (int r = r1; r <= r2; r++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexagonIdxDic.Add((Vector2Int)hexagon, hexagon);
                    hexagonNumDic.Add((uint)count, hexagon);
                    count++;
                }
            }
        }

        /// <summary>
        /// 生成 矩形形状 的六边形集体
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void GenerateRectangle(int top, int bottom, int left, int right) {
            uint count = 0;
            int i = 0, j = 0;
            for (int r = top; r < bottom; r++) { // pointy top
                //执行横向的偏移
                int r_offset = (int)Mathf.Floor(r / 2); // or r>>1
                for (int q = left - r_offset; q < right - r_offset; q++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexagonIdxDic.Add(new Vector2Int(j, i) , hexagon);
                    //hexagonNumDic.Add((uint)count, hexagon);  // NOTE : ID - hex 暂时不再使用
                    count++;
                    j++;
                }
                i++;
                j = 0;
            }
        }

        public static Dictionary<Vector2Int, Hexagon> GenerateRectangleHexData(int down, int up, int left, int right) {
            int i = 0, j = 0;
            Dictionary<Vector2Int, Hexagon> hexagonIdxDic = new Dictionary<Vector2Int, Hexagon>();
            for (int r = down; r < up; r++) { // pointy top
                //执行横向的偏移
                int r_offset = (int)Mathf.Floor(r / 2); // or r>>1
                for (int q = left - r_offset; q < right - r_offset; q++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexagonIdxDic.Add(new Vector2Int(j, i), hexagon);
                    j++;
                }
                i++;
                j = 0;
            }
            return hexagonIdxDic;
        }

        #endregion

        public List<Hexagon> GetHexagonsScope(Hexagon center, int scope) {
            List<Hexagon> hexs = new List<Hexagon>();
            for (int q = -scope; q <= scope; q++) {
                // NOTE: Q and R is axis coord
                int r1 = Mathf.Max(-scope, -center.q - scope);
                int r2 = Mathf.Min(scope, -center.r + scope);
                for (int r = r1; r <= r2; r++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexs.Add(hexagon);
                }
            }
            return hexs;
        }


    }
}
