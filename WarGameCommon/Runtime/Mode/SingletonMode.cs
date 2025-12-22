using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameCommon {

    // 单例的简单实现
    public class Singleton<T> where T : class, new() {
        private static T instance;
        public static T GetInstance() {
            if (instance == null) {
                instance = new T();
            }
            return instance;
        }

        public Singleton() { }
    }

}
