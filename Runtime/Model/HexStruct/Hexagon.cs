
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace LZ.WarGameMap.Runtime.HexStruct
{

    using Hex = _Hex<int, int>;
    using HexFraction = _Hex<float, int>;
    using Vector3 = UnityEngine.Vector3;


    public class _Hex<Number, T> {

        public readonly Number q, r, s;

        public _Hex(Number q, Number r, Number s) {
            this.q = q;
            this.r = r;
            this.s = s;
        }

        #region 与屏幕上的点 互相转换
        public virtual Point Hex_To_Pixel(Layout layout, _Hex<Number, T> hex) { return null; }

        public virtual Hex Pixel_To_Hex(Layout layout, Point point) { return null; }

        public virtual Point Hex_Corner_Offset(Layout layout, int corner) { return null; }

        internal virtual List<Point> Polygon_Corners(Layout layout, Hex hex) { return null; }

        internal virtual List<Point> Polygon_Corners_Local(Hex hex) { return null; }
        #endregion
    }

    // 支持 浮点型 的六边形坐标结构
    [Serializable]
    public class HexagonFraction : HexFraction {
        public HexagonFraction(float q, float r, float s) : base(q, r, s) {
        }

        #region 与屏幕上的点 互相转换

        public override Point Hex_To_Pixel(Layout layout, HexFraction hex) {
            throw new NotImplementedException();
        }

        public override Hex Pixel_To_Hex(Layout layout, Point point) {
            throw new NotImplementedException();
        }

        #endregion
    }

    // NOTE: Hexagon 内部使用 轴向 坐标，需要的时候要和 offset 坐标互转
    // 支持 整型 的六边形坐标结构 有q r s三个维度
    public class Hexagon : Hex {

        //NOTICE: 这里的坐标貌似跟六边形的 q r s 坐标正负性反了
        public Hexagon(int _q, int _r, int _s) : base(_q, _r, _s) {
        }

        #region draw hex edge

        // 将 HexagonFraction 类 转换 为 Hexagon
        public Hexagon Hex_Round(HexagonFraction h) {
            int q = Convert.ToInt32(h.q);
            int r = Convert.ToInt32(h.r);
            int s = Convert.ToInt32(h.s);

            double q_diff = Mathf.Abs(q - h.q);
            double r_diff = Mathf.Abs(r - h.r);
            double s_diff = Mathf.Abs(s - h.s);

            //修正值，不可少
            if (q_diff > r_diff && q_diff > s_diff) {
                q = -r - s;
            } else if (r_diff > s_diff) {
                r = -q - s;
            } else {
                s = -q - r;
            }
            return new Hexagon(q, r, s);
        }

        public float Lerp(float a, float b, float t) {
            return a * (1 - t) + b * t;
        }

        // 六边形 插值
        public HexagonFraction Hex_Lerp(Hex a, Hex b, float t) {
            return new HexagonFraction(
                Lerp(a.q, b.q, t),
                Lerp(a.r, b.r, t),
                Lerp(a.s, b.s, t)
            );
        }

        // 绘制六边形 边
        public List<Hexagon> Hex_LineDraw(Hex a, Hex b, float t) {
            int distance = Hex_Distance(a, b);
            List<Hexagon> results = new List<Hexagon>();

            //根据跳数 生成六边形
            float step = 1.0f / Mathf.Max(distance, 1);

            for (int i = 0; i <= distance; i++) {
                results.Add(Hex_Round(Hex_Lerp(a, b, step * i)));
            }

            return results;
        }

        public List<Hexagon> Hex_Linedraw(Hex a, Hex b) {
            int N = Hex_Distance(a, b);
            //
            HexagonFraction a_nudge = new HexagonFraction((float)(a.q + 1e-6), (float)(a.r + 1e-6), (float)(a.s - 2e-6));
            HexagonFraction b_nudge = new HexagonFraction((float)(b.q + 1e-6), (float)(b.r + 1e-6), (float)(b.s - 2e-6));

            List<Hexagon> results = new List<Hexagon>();

            //根据跳数 生成六边形
            double step = 1.0 / Mathf.Max(N, 1);
            for (int i = 0; i <= N; i++) {
                //报错
                //results.Add( Hex_Round(Hex_Lerp(a_nudge, b_nudge, (float)(step * i))));
            }
            return results;
        }


        #endregion

        #region trans with screen point

        // 将 六边形数据类 转化为 像素点
        public override Point Hex_To_Pixel(Layout layout, Hex hex) {
            Orientation O = layout.orientation;

            //使用 矩阵 将 Hex坐标 转为 屏幕二维坐标点
            double x = (O.f0 * hex.q + O.f1 * hex.r) * layout.Size.x;
            double y = (O.f2 * hex.q + O.f3 * hex.r) * layout.Size.y;
            return new Point(x + layout.Origin.x, y + layout.Origin.y);
        }

        public override Hex Pixel_To_Hex(Layout layout, Point point) {
            Orientation O = layout.orientation;

            //减去原点 除去偏移值
            Point offsetPoint = new Point(
                (point.x - layout.Origin.x) / layout.Size.x,
                (point.y - layout.Origin.y) / layout.Size.y
            );

            //使用 逆矩阵 将 屏幕二维坐标点 转为 Hex坐标 
            // 4.22日 等等，为什么是用的double？这有问题吧？
            double q = O.b0 * offsetPoint.x + O.b1 * offsetPoint.y;
            double r = O.b2 * offsetPoint.x + O.b3 * offsetPoint.y;

            //类型转换
            return new Hex(Convert.ToInt32(q), Convert.ToInt32(r), Convert.ToInt32(-q - r));
        }

        // 获取 六边形的 指定顶点 - 顶点0在中心的左上角 - 顺时针取值
        public override Point Hex_Corner_Offset(Layout layout, int corner) {
            Point size = layout.Size;

            //获取该顶点 的 角度
            double angle = 2.0 * Mathf.PI * (layout.orientation.Start_Angle + corner) / 6;

            return new Point(
                size.x * Mathf.Cos(Convert.ToSingle(angle)),
                size.y * Mathf.Sin(Convert.ToSingle(angle))
            );
        }

        // 获得 六边形 六个顶点的坐标
        internal override List<Point> Polygon_Corners(Layout layout, Hex hex) {
            List<Point> corners = new List<Point>();

            //获取 六边形 的中心点
            Point center = Hex_To_Pixel(layout, hex);

            //生成 六边形 六个顶点的坐标
            for (int i = 0; i < 6; i++) {
                Point offset = Hex_Corner_Offset(layout, i);
                corners.Add(new Point(center.x + offset.x, center.y + offset.y));
            }
            return corners;
        }

        //public override List<Point> Polygon_Corners_Local(Hex hex) {
        //    List<Point> corners = new List<Point>();
        //    //获取 六边形 的中心点
        //    Point center = Hex_To_Pixel(layout, hex);
        //    //生成 六边形 六个顶点的坐标
        //    for (int i = 0; i < 6; i++) {
        //        Point offset = Hex_Corner_Offset(layout, i);
        //        corners.Add(new Point(offset.x, offset.y));
        //    }
        //    return corners;
        //}
        #endregion


        #region get distance
        public int Hex_Length(Hex hex) {
            return (Mathf.Abs(hex.q) + Mathf.Abs(hex.r) + Mathf.Abs(hex.s)) / 2;
        }

        public int Hex_Distance(Hex h1, Hex h2) {
            return Hex_Length(Hex_Subtract(h1, h2));
        }
        #endregion

        #region get neighbor

        // 顺序 邻居0是正左边的邻居 - 顺时针取值
        readonly List<Vector3> Hex_Directions = new List<Vector3>{
            new Vector3(1, 0, -1),
            new Vector3(0, 1, -1),
            new Vector3(-1, 1, 0),
            new Vector3(-1, 0, 1),
            new Vector3(0, -1, 1),
            new Vector3(1, -1, 0),
        };

        public Hexagon Hex_Direction(HexDirection direction) {
#if UNITY_EDITOR
            Assert.IsTrue(0 <= direction && (int)direction < 6);
#endif
            //方向需要模6
            //direction = direction % 6;
            //Debug.Log(direction);
            return (Hexagon)Hex_Directions[(int)direction];
        }

        /// <summary>
        /// 获取该六边形 其中 一个方向的 邻居 - 邻居0在正左边 - 顺时针取值
        /// </summary>
        public Hexagon Hex_Neighbor(HexDirection direction) {
            return Hexagon_Add(this, Hex_Direction(direction));
        }

        #endregion



        private Hex Hex_Add(Hex h1, Hex h2) {
            return new Hex(h1.q + h2.q, h1.r + h2.r, h1.s + h2.s);
        }

        private Hexagon Hexagon_Add(Hexagon h1, Hex h2) {
            return new Hexagon(h1.q + h2.q, h1.r + h2.r, h1.s + h2.s);
        }

        public static Hex operator +(Hexagon h1, Hex h2) {
            return new Hex(h1.q + h2.q, h1.r + h2.r, h1.s + h2.s);
        }

        public Hex Hex_Subtract(Hex h1, Hex h2) {
            return new Hex(h1.q - h2.q, h1.r - h2.r, h1.s - h2.s);
        }

        public static Hex operator -(Hexagon h1, Hex h2) {
            return new Hex(h1.q - h2.q, h1.r - h2.r, h1.s - h2.s);
        }

        public Hex Hex_Multiply(Hex h1, Hex h2) {
            return new Hex(h1.q * h2.q, h1.r * h2.r, h1.s * h2.s);
        }

        public static Hex operator *(Hexagon h1, Hex h2) {
            return new Hex(h1.q * h2.q, h1.r * h2.r, h1.s * h2.s);
        }

        public static implicit operator Vector3(Hexagon hex) {
            return new Vector3(hex.q, hex.r, hex.s);
        }

        public static explicit operator Hexagon(Vector3 vector) {
            return new Hexagon((int)vector.x, (int)vector.y, (int)vector.z);
        }

        public static implicit operator Vector2(Hexagon hex) {
            return new Vector2(hex.q, hex.r);
        }

        public static explicit operator Hexagon(Vector2 vector) {
            return new Hexagon((int)vector.x, (int)vector.y, -(int)vector.x - (int)vector.y);
        }

        public static implicit operator Vector2Int(Hexagon hex) {
            return new Vector2Int(hex.q, hex.r);
        }

        public static explicit operator Hexagon(Vector2Int vector) {
            return new Hexagon((int)vector.x, (int)vector.y, -(int)vector.x - (int)vector.y);
        }

        public static bool operator ==(Hexagon h1, Hex h2) {
            return (h1.q == h2.q) && (h1.r == h2.r) && (h1.s == h2.s);
        }

        public static bool operator !=(Hexagon h1, Hex h2) {
            return !(h1 == h2);
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (!(obj is Hex)) {
                //检查对象类型
                return false;
            }
            return this == (Hex)obj;
        }

    }

    public enum HexDirection {
        W = 0,
        NW = 1,
        NE = 2,
        E = 3,
        SE = 4,
        SW = 5
    }


    public static class HexHelper {


        //public static Hexagon PixelToAxialHex(Vector2 position, int HexGridSize) {
        //    int q = Convert.ToInt32((Mathf.Sqrt(3) / 3 * position.x - 1.0f / 3 * position.y) / HexGridSize);
        //    int r = Convert.ToInt32((2.0f / 3 * position.y) / HexGridSize);
        //    return new Hexagon(q, r, -q-r);
        //}

        public static Hexagon PixelToAxialHex(Vector2 worldPos, int HexGridSize) {
            float q = (Mathf.Sqrt(3) / 3 * worldPos.x - 1.0f/ 3 * worldPos.y) / HexGridSize;
            float r = 2.0f/ 3 * worldPos.y / HexGridSize;
            float s = -q - r;

            int fix_q = Mathf.RoundToInt(q);
            int fix_r = Mathf.RoundToInt(r);
            int fix_s = Mathf.RoundToInt(s);

            float q_diff = Mathf.Abs(fix_q - q);
            float r_diff = Mathf.Abs(fix_r - r);
            float s_diff = Mathf.Abs(fix_s - s);

            int final_q = fix_q, final_r = fix_r, final_s = fix_s;
            if (q_diff > r_diff && q_diff > s_diff) {
                final_q = -fix_r - fix_s;
            } else if (r_diff > s_diff) {
                final_r = -fix_q - fix_s;
            } else {
                final_s = -fix_q - fix_r;
            }
            return new Hexagon(final_q, final_r, final_s);
        }

        public static Vector2Int AxialToOffset(Hexagon hex) {
            var col = hex.q + (hex.r - (hex.r & 1)) / 2;
            var row = hex.r;
            return new Vector2Int(col, row);
        }


        //public static Hexagon OffsetToAxial() {
        //    var q = hex.col - (hex.row - (hex.row & 1)) / 2
        //    var r = hex.row
        //    return Hex(q, r)
        //}

    }

}
