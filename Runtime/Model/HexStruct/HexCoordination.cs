using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime.HexStruct {

    /// <summary>
    /// 用于将Cube Coordinate转化为屏幕坐标，存储2x2正向矩阵 2x2逆矩阵，起始角度
    /// </summary>
    public struct Orientation {
        // 2x2正向矩阵（坐标变换矩阵）
        public readonly double f0, f1, f2, f3;
        // 2x2逆矩阵
        public readonly double b0, b1, b2, b3;
        //该方向的起始旋转角度
        public readonly double Start_Angle;

        public Orientation(double f0, double f1, double f2, double f3,
            double b0, double b1, double b2, double b3,
            double start_Angle) {
            this.f0 = f0;
            this.f1 = f1;
            this.f2 = f2;
            this.f3 = f3;
            this.b0 = b0;
            this.b1 = b1;
            this.b2 = b2;
            this.b3 = b3;
            Start_Angle = start_Angle;
        }

        //两个方向的常量
        public static readonly Orientation Layout_Pointy = new Orientation(
            Mathf.Sqrt(3.0f), Mathf.Sqrt(3.0f) / 2.0, 0.0, 3.0 / 2.0,
            Mathf.Sqrt(3.0f) / 3.0, -1.0 / 3.0, 0.0, 2.0 / 3.0,
            0.5);
        public static readonly Orientation Layout_Flat = new Orientation(
            3.0 / 2.0, 0.0, Mathf.Sqrt(3.0f) / 2.0, Mathf.Sqrt(3.0f),
            2.0 / 3.0, 0.0, -1.0 / 3.0, Mathf.Sqrt(3.0f) / 3.0,
            0.0);
    }

    /// <summary>
    /// 点类, 专供 GameMap
    /// </summary>
    public struct Point {
        public readonly double x, y;
        public double z;

        public static Point OriginPoint = new Point(0, 0);

        public Point(double x, double y) {
            this.x = x;
            this.y = y;
            this.z = 0;
        }

        public Point(double x, double y, double z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Point ConvertToXZ() {
            return new Point(Convert.ToSingle(this.x), 0, Convert.ToSingle(this.y));
        }

        public static implicit operator Vector2(Point point) {
            return new Vector2(Convert.ToSingle(point.x), Convert.ToSingle(point.y));
        }

        public static explicit operator Point(Vector2 vector) {
            return new Point(vector.x, vector.y);
        }

        public static implicit operator Vector3(Point point) {
            return new Vector3(Convert.ToSingle(point.x), Convert.ToSingle(point.y), Convert.ToSingle(point.z));
        }

        public static explicit operator Point(Vector3 vector) {
            return new Point(vector.x, vector.y, vector.z);
        }

        // 重载运算符

        public static Point operator +(Point point1, Point point2) {
            return new Point(point1.x + point2.x, point1.y + point2.y, point1.z + point2.z);
        }

        public static Point operator -(Point point1, Point point2) {
            return new Point(point1.x - point2.x, point1.y - point2.y, point1.z - point2.z);
        }

        public static Point operator /(Point point1, double d) {
            return new Point(point1.x / d, point1.y / d, point1.z / d);
        }

        public static Point operator *(Point point1, double d) {
            return new Point(point1.x * d, point1.y * d, point1.z * d);
        }

    }

    /// <summary>
    /// 布局类
    /// </summary>
    public struct Layout {
        //变换矩阵 用于转化 Hex坐标 和 屏幕坐标
        public readonly Orientation orientation;
        //屏幕 中六边形边 的大小
        internal readonly Point Size;
        internal readonly int Width;
        internal readonly int Height;
        //屏幕布局的原点
        internal readonly Point Origin;

        public Layout(Orientation orientation, Point size, Point origin, int h, int w) {
            this.orientation = orientation;
            Size = size;
            Origin = origin;
            Width = w;
            Height = h;
        }

    }

}
