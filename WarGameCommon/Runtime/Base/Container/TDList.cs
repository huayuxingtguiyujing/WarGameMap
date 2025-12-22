using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LZ.WarGameCommon {

    // 二维数组! 不是动态数组
    public class TDList<T> : IEnumerable<T> where T : new() {

        int width;  // out list length

        int height; // inner list length

        List<T> list;

        public List<T> Data { get { return list; } }

        public int Count { get { return height * width; } }

        public IEnumerator<T> GetEnumerator() {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }


        public TDList() { list = new List<T>(); }

        public TDList(int width, int height) {
            this.width = width;
            this.height = height;

            // 初始化 list，大小为 width * height
            list = new List<T>(width * height);
            for (int i = 0; i < width * height; i++) {
                list.Add(new T());
            }
        }

        public T this[int x, int y] {
            get {
                if (x < 0 || x >= width || y < 0 || y >= height) {
                    throw new ArgumentOutOfRangeException($"索引 ({x}, {y}) 超出范围");
                }
                return list[x + width * y];
            }
            set {
                if (x < 0 || x >= width || y < 0 || y >= height) {
                    throw new ArgumentOutOfRangeException($"索引 ({x}, {y}) 超出范围");
                }
                list[x + width * y] = value;
            }
        }


        public List<T> GetValues() { return list; }

        public int GetLength(int idx) {
            if (idx == 0) {
                return height;  // 第一个索引
            } else if (idx == 1) {
                return width;
            } else {
                return 0;   // 不支持更多的维度了
            }
        }

        public T GetLastVal() {
            if (list.Count == 0) {
                return default;
            }
            return list[list.Count - 1];
        }

        public bool IsValidIndex(int x, int y) {
            return (x >= 0 && x < width && y >= 0 && y < height);
        }



    }

}
