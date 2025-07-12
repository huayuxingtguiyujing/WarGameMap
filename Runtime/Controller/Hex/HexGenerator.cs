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

        // NOTE: hexagon �д洢����ƫ�����꣬�˴�ʹ������������Ϊ Key
        // �������� - Hexagon ��ӳ��(������)
        private Dictionary<Vector2Int, Hexagon> hexagonIdxDic;

        // ID - Hexagon ��ӳ��
        private Dictionary<uint, Hexagon> hexagonNumDic;

        public Dictionary<Vector2Int, Hexagon> HexagonIdxDic {
            get => hexagonIdxDic;
            private set => hexagonIdxDic = value;
        }

        //public Dictionary<uint, Hexagon> HexagonNumDic {
        //    get => hexagonNumDic;
        //    private set => hexagonNumDic = value;
        //}


        #region ��ͬ�����ɷ���
        public void ClearHexagon() {
            hexagonIdxDic.Clear();
            hexagonNumDic.Clear();
        }

        /// <summary>
        /// ����ƽ���ı���״ �������μ���
        /// </summary>
        /// <param name="q1">q��������</param>
        /// <param name="q2">q������յ�</param>
        /// <param name="r1">r��������</param>
        /// <param name="r2">r������յ�</param>
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
        /// ���� ������״ �������μ���
        /// </summary>
        /// <param name="mapSize">��ͼ�ߴ�</param>
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
        /// ���� ������״ �������μ���
        /// </summary>
        /// <param name="mapSize">��ͼ�ߴ�</param>
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
        /// ���� ������״ �������μ���
        /// </summary>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void GenerateRectangle(int top, int bottom, int left, int right) {
            uint count = 0;
            int i = 0, j = 0;
            for (int r = top; r < bottom; r++) { // pointy top
                //ִ�к����ƫ��
                int r_offset = (int)Mathf.Floor(r / 2); // or r>>1
                for (int q = left - r_offset; q < right - r_offset; q++) {
                    Hexagon hexagon = new Hexagon(q, r, -q - r);
                    hexagonIdxDic.Add(new Vector2Int(j, i) , hexagon);
                    //hexagonNumDic.Add((uint)count, hexagon);  // NOTE : ID - hex ��ʱ����ʹ��
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
                //ִ�к����ƫ��
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
