using LZ.WarGameCommon;
using System;
using UnityEngine;

namespace LZ.WarGameMap.Runtime {

    public static class Vector3Extension
    {
        public static string ToStringFixed(this Vector3 target) {
            return $"{target.x},{target.y},{target.z}";
        }
        
        public static string ToStringFixed(this Vector3Int target) {
            return $"{target.x},{target.y},{target.z}";
        }
        public static string ToStringFixed(this Vector2Int target) {
            return $"{target.x},{target.y}";
        }

        public static Vector3Int ToVector3Int(this string target) {
            string[] strs = target.Split(',');
            return new Vector3Int(Convert.ToInt32(strs[0]), Convert.ToInt32(strs[1]) , Convert.ToInt32(strs[2]));
        }

        public static Vector2Int ToVector2Int(this string target) {
            string[] strs = target.Split(',');
            return new Vector2Int(Convert.ToInt32(strs[0]), Convert.ToInt32(strs[1]));
        }

        public static string ToStringFixed(this Vector2 target) {
            return $"{target.x},{target.y}";
        }

    }

    public static class TransformExtension {

        public static void ClearObjChildren(this Transform targetContent) {
            //清空界面的子物体
            Transform[] indicatorChildren = targetContent.GetComponentsInChildren<Transform>();
            foreach (Transform tran in indicatorChildren) {
                if (tran == null) continue;
                if (tran.gameObject == targetContent.gameObject) continue;
                // 思考：要不要把对象池的回收功能放到这里呢？
                GameObject.DestroyImmediate(tran.gameObject);
            }
        }

        public static void ClearObjChildren(this GameObject targetContent) {
            targetContent.transform.ClearObjChildren();
        }

        public static void RecycleObjChildren(this Transform targetContent) {
            ObjectPoolMark[] btnGos = targetContent.GetComponentsInChildren<ObjectPoolMark>();
            foreach (var go in btnGos) {
                ObjectPool.GetInstance().RecycleObject(go.gameObject);
            }
        }

        public static void RecycleObjChildren(this GameObject targetContent) {
            targetContent.transform.RecycleObjChildren();
        }

        /// <summary>
        /// 设置rectTransform的偏移
        /// </summary>
        /// <param name="targetContent"></param>
        /// <param name="sizeY">Y方向上的偏移</param>
        public static void SetRectTransformSizeY(this RectTransform targetContent, float sizeY) {
            float originX = targetContent.sizeDelta.x;
            targetContent.sizeDelta = new Vector2(
                originX, sizeY
            );
        }

        public static void SetRectTransformPosY(this RectTransform targetContent, float offsetY) {
            float originX = targetContent.anchoredPosition.x;
            targetContent.anchoredPosition = new Vector2(
                originX, offsetY
            );
            Debug.Log(targetContent.anchoredPosition);
        }

        public static void SetRectTransformSizeX(this RectTransform targetContent, float sizeX) {
            float originY = targetContent.sizeDelta.y;
            targetContent.sizeDelta = new Vector2(
                sizeX, originY
            );
        }

        public static void SetRectTransformPosX(this RectTransform targetContent, float offsetX) {
            float originY = targetContent.anchoredPosition.y;
            targetContent.anchoredPosition = new Vector2(
                offsetX, originY
            );
        }

    }

    public static class VectorExtension {

        public static Vector3 TransToXZ(this Vector2 v2) {
            return new Vector3(v2.x, 0, v2.y);
        }

        public static Vector3 TransToXZ(this Vector2Int v2)
        {
            return new Vector3(v2.x, 0, v2.y);
        }

        public static Vector2 TransFromXZ(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

        public static Vector3Int GetSimilarVInt(this Vector3 v)
        {
            return new Vector3Int((int)v.x, (int)v.y, (int)v.z);
        }

    }

    public static class ColorExtension
    {

        public static string ToStringFixedRGB(this Color target)
        {
            return $"{target.r},{target.g},{target.b}";
        }

        public static string ToStringFixedRGBA(this Color target)
        {
            return $"{target.r},{target.g},{target.b},{target.a}";
        }

        public static Color ToColorRGB(this string target)
        {
            string[] strs = target.Split(',');
            return new Color((float)Convert.ToDouble(strs[0]), (float)Convert.ToDouble(strs[1]), (float)Convert.ToDouble(strs[2]));
        }

        public static Color ToColorRGBA(this string target)
        {
            string[] strs = target.Split(',');
            return new Color((float)Convert.ToDouble(strs[0]), (float)Convert.ToDouble(strs[1]),
                (float)Convert.ToDouble(strs[2]), (float)Convert.ToDouble(strs[3]));
        }
        
        public static float LerpColor(this Color c, Color a, Color b)
        {
            Vector3 ab = new Vector3(b.r - a.r, b.g - a.g, b.b - a.b);
            Vector3 ac = new Vector3(c.r - a.r, c.g - a.g, c.b - a.b);
            float denominator = Vector3.Dot(ab, ab);
            if (denominator < 1e-6f)
            {
                return 0f;
            }
            float t = Vector3.Dot(ac, ab) / denominator;
            return Mathf.Clamp01(t);
        }
    } 

}