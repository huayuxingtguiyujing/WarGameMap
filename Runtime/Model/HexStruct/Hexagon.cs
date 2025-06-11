
using Sirenix.OdinInspector.Editor.Validation;
using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Assertions;

namespace LZ.WarGameMap.Runtime.HexStruct
{

    using Hex = _Hex<int, int>;
    using HexFraction = _Hex<float, int>;
    using Vector3 = UnityEngine.Vector3;

    public interface _Hex<Number, T> {

        //public readonly Number q, r, s;
        //public _Hex(Number q, Number r, Number s) {
        //    this.q = q;
        //    this.r = r;
        //    this.s = s;
        //}
        public Number q { get; set; }
        public Number r { get; set; }
        public Number s { get; set; }


        #region transfer Hex to point/position
        public virtual Point Hex_To_Pixel(Layout layout) { return Point.OriginPoint; }

        public virtual Hex Pixel_To_Hex(Layout layout, Point point) { return null; }

        public virtual Point Hex_Corner_Offset(Layout layout, int corner) { return Point.OriginPoint; }

        internal virtual List<Point> Polygon_Corners(Layout layout, Hex hex) { return null; }

        internal virtual List<Point> Polygon_Corners_Local(Hex hex) { return null; }
        #endregion
    }

    // 支持 浮点型 的六边形坐标结构
    [Serializable]
    public struct HexagonFraction : HexFraction {

        float _q, _r, _s;

        public float q { get => _q;  set => _q = value; }
        public float r { get => _r;  set => _r = value; }
        public float s { get => _s;  set => _s = value; }

        public HexagonFraction(float q, float r, float s) {
            _q = q; _r = r; _s = s;
        }

    }

    // NOTE: Hexagon 内部使用 轴向 坐标，需要的时候要和 offset 坐标互转
    // 支持 整型 的六边形坐标结构 有q r s三个维度
    public struct Hexagon : Hex {
        int _q, _r, _s;

        public int q { get => _q; set => _q = value; }
        public int r { get => _r; set => _r = value; }
        public int s { get => _s; set => _s = value; }

        public Hexagon(int q, int r, int s) {
            _q = q; _r = r; _s = s;
        }


        #region draw hex edge

        public Hexagon Hex_Round(HexagonFraction h) {
            int q = Convert.ToInt32(h.q);
            int r = Convert.ToInt32(h.r);
            int s = Convert.ToInt32(h.s);

            double q_diff = Mathf.Abs(q - h.q);
            double r_diff = Mathf.Abs(r - h.r);
            double s_diff = Mathf.Abs(s - h.s);

            if (q_diff > r_diff && q_diff > s_diff) {
                q = -r - s;
            } else if (r_diff > s_diff) {
                r = -q - s;
            } else {
                s = -q - r;
            }
            return new Hexagon(q, r, s);
        }

        public HexagonFraction Hex_Lerp(Hexagon a, Hexagon b, float t) {
            return new HexagonFraction(
                Lerp(a.q, b.q, t),
                Lerp(a.r, b.r, t),
                Lerp(a.s, b.s, t)
            );
        }

        private float Lerp(float a, float b, float t) {
            return a * (1 - t) + b * t;
        }

        public List<Hexagon> Hex_LineDraw(Hexagon a, Hexagon b, float t) {
            int distance = Hex_Distance(a, b);
            List<Hexagon> results = new List<Hexagon>();

            //根据跳数 生成六边形
            float step = 1.0f / Mathf.Max(distance, 1);
            for (int i = 0; i <= distance; i++) {
                results.Add(Hex_Round(Hex_Lerp(a, b, step * i)));
            }
            return results;
        }

        #endregion

        #region trans with screen point

        public Point Hex_To_Pixel(Layout layout) {
            Orientation O = layout.orientation;
            double x = (O.f0 * _q + O.f1 * _r) * layout.Size.x;
            double y = (O.f2 * _q + O.f3 * _r) * layout.Size.y;
            return new Point(x + layout.Origin.x, y + layout.Origin.y);
        }

        public Vector2 Get_Hex_Center(Layout layout) {
            Point center = Hex_To_Pixel(layout).ConvertToXZ();
            Vector2 hexCenter = new Vector2((float)center.x, (float)center.z);
            return hexCenter;
        }

        public readonly Hexagon Pixel_To_Hex(Layout layout, Point point) {
            Orientation O = layout.orientation;
            Point offsetPoint = new Point(
                (point.x - layout.Origin.x) / layout.Size.x,
                (point.y - layout.Origin.y) / layout.Size.y
            );

            double q = O.b0 * offsetPoint.x + O.b1 * offsetPoint.y;
            double r = O.b2 * offsetPoint.x + O.b3 * offsetPoint.y;
            return new Hexagon(Convert.ToInt32(q), Convert.ToInt32(r), Convert.ToInt32(-q - r));
        }

        public readonly Point Hex_Corner_Offset(Layout layout, int corner) {
            // NOTE : 
            //   NW / 1 \ NE
            //    2/     \0 (corner)
            //    |       | 
            //  W |       | E
            //    3\     /5
            //      \   / 
            //   SW   4   SE
            corner = FixCorner(corner);
            Point size = layout.Size;

            double angle = 2.0 * Mathf.PI * (layout.orientation.Start_Angle + corner) / 6;
            return new Point(
                size.x * Mathf.Cos(Convert.ToSingle(angle)),
                size.y * Mathf.Sin(Convert.ToSingle(angle))
            );
        }

        public Vector2 Get_Hex_CornerPos(Layout layout, int corner, float fix = 1.0f) {
            corner = FixCorner(corner);
            Point center = Hex_To_Pixel(layout).ConvertToXZ();
            Point curOffset = Hex_Corner_Offset(layout, corner); // * k
            Point curVertex = center + new Point(curOffset.x, 0, curOffset.y) * fix;   // 


            //Point innerVertex = center + new Point(offset.x * innerRatio, 0, offset.y * innerRatio);

            Vector2 curPoint = new Vector2((float)curVertex.x, (float)curVertex.z);
            return curPoint;
        }

        // 获得 六边形 六个顶点的坐标
        internal List<Point> Polygon_Corners(Layout layout) {
            List<Point> corners = new List<Point>();

            Point center = Hex_To_Pixel(layout);
            for (int i = 0; i < 6; i++) {
                Point offset = Hex_Corner_Offset(layout, i);
                corners.Add(new Point(center.x + offset.x, center.y + offset.y));
            }
            return corners;
        }

        private readonly int FixCorner(int corner) {
            while (corner < 0) {
                corner += 6;
            }
            corner %= 6;
            return corner;
        }

        #endregion

        #region get distance

        public int Hex_Length(Hexagon hex) {
            return (Mathf.Abs(hex.q) + Mathf.Abs(hex.r) + Mathf.Abs(hex.s)) / 2;
        }

        public int Hex_Distance(Hexagon h1, Hexagon h2) {
            return Hex_Length(Hex_Subtract(h1, h2));
        }

        #endregion

        #region get neighbor
        // NOTE : view  HexDirection
        // do not modify it!!!
        static List<Vector3> Hex_Directions = new List<Vector3>{
            new Vector3(0, 1, -1),      // NE   
            new Vector3(-1, 1, 0),      // NW
            new Vector3(-1, 0, 1),      // W
            new Vector3(0, -1, 1),      // SW
            new Vector3(1, -1, 0),      // SE
            new Vector3(1, 0, -1),      // E
        };

        public Hexagon Hex_Direction(HexDirection direction) {
#if UNITY_EDITOR
            Assert.IsTrue(0 <= direction && (int)direction < 6);
#endif
            
            return (Hexagon)Hex_Directions[(int)direction];
        }

        public Hexagon Hex_Neighbor(HexDirection direction) {
            int dir = (int)direction;
            dir = FixCorner(dir);
            direction = (HexDirection)dir;
            return Hexagon_Add(this, Hex_Direction(direction));
        }

        #endregion

        #region other

        private Hexagon Hexagon_Add(Hexagon h1, Hexagon h2) {
            return new Hexagon(h1.q + h2.q, h1.r + h2.r, h1.s + h2.s);
        }

        public static Hexagon operator +(Hexagon h1, Hexagon h2) {
            return new Hexagon(h1.q + h2.q, h1.r + h2.r, h1.s + h2.s);
        }

        public Hexagon Hex_Subtract(Hexagon h1, Hexagon h2) {
            return new Hexagon(h1.q - h2.q, h1.r - h2.r, h1.s - h2.s);
        }

        public static Hexagon operator -(Hexagon h1, Hexagon h2) {
            return new Hexagon(h1.q - h2.q, h1.r - h2.r, h1.s - h2.s);
        }

        public Hex Hex_Multiply(Hexagon h1, Hexagon h2) {
            return new Hexagon(h1.q * h2.q, h1.r * h2.r, h1.s * h2.s);
        }

        public static Hexagon operator *(Hexagon h1, Hexagon h2) {
            return new Hexagon(h1.q * h2.q, h1.r * h2.r, h1.s * h2.s);
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

        public static bool operator ==(Hexagon h1, Hexagon h2) {
            return (h1.q == h2.q) && (h1.r == h2.r) && (h1.s == h2.s);
        }

        public static bool operator !=(Hexagon h1, Hexagon h2) {
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
            return this == (Hexagon)obj;
        }

        #endregion
    
    }

    public enum HexDirection {
        // do not modify it!!!
        //      
        //   NW / 1 \ NE
        //    2/     \0 (corner)
        //    |       | 
        //  W |       | E
        //    3\     /5
        //      \   / 
        //   SW   4   SE

        NE = 0,
        NW = 1,
        W = 2,
        SW = 3,
        SE = 4,
        E = 5,

        None = 6,
    }


}
