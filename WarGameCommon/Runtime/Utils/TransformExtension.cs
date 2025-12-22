using UnityEngine;

namespace LZ.WarGameCommon {

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

}