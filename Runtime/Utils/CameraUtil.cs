using UnityEngine;
using UnityEngine.EventSystems;

namespace LZ.WarGameMap.Runtime
{
    public static class CameraUtil
    {
        #region Mouse Map Position

        private static readonly Plane mapPlane = new Plane(Vector3.up, Vector3.zero);

        public static bool TryGetMouseMapPosition(Camera camera, out Vector3 mapPosition)
        {
            mapPosition = Vector3.zero;
            if (camera == null || !Application.isFocused)
            {
                return false;
            }

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            {
                return false;
            }

            Vector3 mousePosition = Input.mousePosition;
            Vector2 screenPosition = new Vector2(mousePosition.x, mousePosition.y);
            if (!camera.pixelRect.Contains(screenPosition))
            {
                return false;
            }

            Ray mouseRay = camera.ScreenPointToRay(mousePosition);
            float enterDistance;
            if (!mapPlane.Raycast(mouseRay, out enterDistance))
            {
                return false;
            }

            // TODO: 本版以 Y=0 平面拾取，后续再考虑实际地表高度。
            mapPosition = mouseRay.GetPoint(enterDistance);
            return true;
        }

        #endregion
    }
}
