using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LZ.WarGameMap.Runtime
{
    public class CameraController : MonoBehaviour
    {
        #region Camera Parameters

        private const float minHeight = 5f;
        private const float maxHeight = 1000f;
        private const float tiltStartHeight = 200f;
        private const float tiltNearHeight = 80f;       // 决定什么时候开始倾斜相机
        private const float nearTiltAngle = 60f;        // 倾斜最大角度
        private const float moveSpeed = 100f;       // wasm 移动速度
        private const float sprintMultiplier = 3f;
        private const float zoomRatio = 0.1f;

        #endregion

        #region Runtime State

        private Camera controlledCamera;        // 全局用一个 main camera
        private Rect mapBoundsXZ;
        private Quaternion initialRotation;
        private Quaternion nearRotation;
        private float initialHeight;
        private bool isInitialized;

        public Vector3 focusPoint { get; private set; }
        public float height { get; private set; }
        public Vector3 cameraPosition => controlledCamera.transform.position;
        public bool inputEnabled { get; set; } = true;

        public Camera GetCamera()
        {
            return controlledCamera;
        }

        #endregion

        #region Initialization

        public void Init(Camera camera, TerrainSettingSO terrainSetting, Rect mapBounds)
        {
            if (camera == null || terrainSetting == null)
            {
                throw new ArgumentException("Camera and terrain settings are required.");
            }
            if (camera.orthographic)
            {
                throw new InvalidOperationException("Map camera control requires a perspective camera.");
            }
            if (!IsFinite(mapBounds.xMin) || !IsFinite(mapBounds.yMin)
                || !IsFinite(mapBounds.xMax) || !IsFinite(mapBounds.yMax)
                || mapBounds.width <= 0f || mapBounds.height <= 0f)
            {
                throw new ArgumentException("Map camera bounds must be finite and positive.");
            }

            Vector3 initialPosition = terrainSetting.cameraInitialPosition;
            float initialTiltAngle = terrainSetting.cameraInitialTiltAngle;
            bool hasValidPosition = IsFinite(initialPosition.x) && IsFinite(initialPosition.y)
                && IsFinite(initialPosition.z);
            bool hasValidHeight = initialPosition.y >= minHeight && initialPosition.y <= maxHeight;
            bool hasValidTilt = IsFinite(initialTiltAngle)
                && initialTiltAngle >= 0f && initialTiltAngle <= nearTiltAngle;
            if (!hasValidPosition || !hasValidHeight || !hasValidTilt)
            {
                throw new ArgumentException(
                    $"Initial camera position must be finite, height must be {minHeight}-{maxHeight}, "
                    + $"tilt must be 0-{nearTiltAngle} degrees, Now received position {initialPosition}, tilt {initialTiltAngle}.");
            }

            controlledCamera = camera;
            mapBoundsXZ = mapBounds;
            float initialPitch = 90f - initialTiltAngle;
            float nearPitch = 90f - nearTiltAngle;
            initialRotation = Quaternion.Euler(initialPitch, 0f, 0f);
            nearRotation = Quaternion.Euler(nearPitch, 0f, 0f);

            Vector3 forward = initialRotation * Vector3.forward;
            float focusDistance = -initialPosition.y / forward.y;
            focusPoint = initialPosition + forward * focusDistance;
            height = initialPosition.y;
            initialHeight = initialPosition.y;

            // TODO: 后续以地图实际地表为准，替换 Y=0 观察平面并处理防穿地。
            ClampFocusPoint();
            ApplyCameraPose();
            isInitialized = true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        #endregion

        #region Input Update

        public void Tick(float deltaTime)
        {
            if (!isInitialized || !isActiveAndEnabled || controlledCamera == null || !inputEnabled)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            if (IsTextInputFocused(eventSystem))
            {
                return;
            }

            bool isPointerOverUI = false;
            if (eventSystem != null)
            {
                isPointerOverUI = eventSystem.IsPointerOverGameObject();
            }
            if (!isPointerOverUI)
            {
                float scrollInput = Input.mouseScrollDelta.y;
                float heightChange = height * scrollInput * zoomRatio;
                height = Mathf.Clamp(height - heightChange, minHeight, maxHeight);
            }

            Vector3 moveInput = ReadMoveInput();
            float heightRatio = height / initialHeight;
            float moveDistance = moveSpeed * heightRatio * deltaTime;

            // 按住 Shift 键时加速移动
            if (Input.GetKey(KeyCode.LeftShift))
            {
                moveDistance *= sprintMultiplier;
            }
            focusPoint += moveInput * moveDistance;

            ClampFocusPoint();
            ApplyCameraPose();
        }

        private Vector3 ReadMoveInput()
        {
            Vector3 moveInput = Vector3.zero;
            if (Input.GetKey(KeyCode.W))
            {
                moveInput.z += 1f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                moveInput.z -= 1f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                moveInput.x -= 1f;
            }
            if (Input.GetKey(KeyCode.D))
            {
                moveInput.x += 1f;
            }
            return Vector3.ClampMagnitude(moveInput, 1f);
        }

        private bool IsTextInputFocused(EventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return false;
            }
            GameObject selectedObject = eventSystem.currentSelectedGameObject;
            if (selectedObject == null)
            {
                return false;
            }

            InputField inputField = selectedObject.GetComponent<InputField>();
            if (inputField != null && inputField.isFocused)
            {
                return true;
            }
            TMP_InputField tmpInputField = selectedObject.GetComponent<TMP_InputField>();
            if (tmpInputField != null && tmpInputField.isFocused)
            {
                return true;
            }
            return false;
        }

        #endregion

        private void ClampFocusPoint()
        {
            float focusX = Mathf.Clamp(focusPoint.x, mapBoundsXZ.xMin, mapBoundsXZ.xMax);
            float focusZ = Mathf.Clamp(focusPoint.z, mapBoundsXZ.yMin, mapBoundsXZ.yMax);
            focusPoint = new Vector3(focusX, 0f, focusZ);
        }

        private void ApplyCameraPose()
        {
            float tiltRatio = Mathf.InverseLerp(tiltStartHeight, tiltNearHeight, height);
            float smoothTiltRatio = Mathf.SmoothStep(0f, 1f, tiltRatio);
            Quaternion rotation = Quaternion.Slerp(initialRotation, nearRotation, smoothTiltRatio);

            Vector3 forward = rotation * Vector3.forward;
            float verticalDistance = height - focusPoint.y;
            float focusDistance = verticalDistance / -forward.y;
            Vector3 cameraPosition = focusPoint - forward * focusDistance;
            controlledCamera.transform.SetPositionAndRotation(cameraPosition, rotation);
        }

    }
}
