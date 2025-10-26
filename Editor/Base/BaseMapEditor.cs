using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using LZ.WarGameMap.Runtime;
using LZ.WarGameMap.Runtime.Enums;

namespace LZ.WarGameMap.MapEditor {

    public abstract class BaseMapEditor: ScriptableObject {
        public abstract string EditorName { get; }

        protected bool notInitScene = true;

        protected EditorSceneManager sceneManager;    // 即 static instance

        [FoldoutGroup("配置scene", -9)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: "), ReadOnly]
        public string warningNotInit = "没有初始化Editor, 请点击按钮初始化!";

        [FoldoutGroup("配置scene", -9)]
        [LabelText("锁定SceneView")]
        [Tooltip("仅在锁定后，才可以进行绘制操作")]
        [OnValueChanged("OnLockSceneViewValueChanged")]
        public bool lockSceneView = true;

        [FoldoutGroup("配置scene", -9)]
        [LabelText("接收键盘输入")]
        [Tooltip("仅为true时，接收键盘输入")]
        [OnValueChanged("OnLockSceneViewValueChanged")]
        public bool enableKeyCode = true;

        private void OnLockSceneViewValueChanged() {

            // set orthographic if lock
            if (SceneView.lastActiveSceneView != null) {
                var sv = SceneView.lastActiveSceneView;
                Undo.RecordObject(sv, "Set Scene View Orthographic");
                sv.orthographic = lockSceneView;
                sv.Repaint();
            }
        }


        [FoldoutGroup("配置scene", -9)]
        [Button("初始化地形配置", ButtonSizes.Medium)]
        protected virtual void InitEditor()
        {
            notInitScene = false;
        }

        protected virtual void InitMapSetting() {
            string mapSetFolerName = AssetsUtility.GetFolderFromPath(MapStoreEnum.WarGameMapSettingPath);
            if (!AssetDatabase.IsValidFolder(MapStoreEnum.WarGameMapSettingPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.WarGameMapRootPath, mapSetFolerName);
            }
        }

        // TODO : move to Runtime/Util/AssetsUtility.cs
        public static void FindOrCreateSO<T>(ref T so, string folderPath, string assetName) where T : ScriptableObject {
            // assetName = "TerrainSetting_Default.asset"
            // assetName = "HexSetting_Default.asset"
            if (so == null) {
                string soPath = folderPath + "/" + assetName;
                so = AssetDatabase.LoadAssetAtPath<T>(soPath);
                if (so == null) {           // create it !
                    so = CreateInstance<T>();
                    AssetDatabase.CreateAsset(so, soPath);
                    Debug.Log($"Successfully create map Setting SO, path : {soPath}");
                }
            }
        }

        #region behaviors

        public virtual void Enable() {
            sceneManager = EditorSceneManager.GetInstance();
            SceneView.duringSceneGui += OnSceneGUI;
            // if lock scene view in some editor window, the editor will not work
            lockSceneView = false;
        }

        public virtual void Disable() {
            SceneView.duringSceneGui -= OnSceneGUI;
            lockSceneView = false;
        }

        public virtual void Destory() {

        }

        private int curDragTriggerTime = 0;

        private int dragInterval = 5;

        protected virtual void OnSceneGUI(SceneView sceneView) {
            if (!RootMapEditor.IsCurMapEditor(this))
            {
                return;
            }

            HandleSceneDraw();

            Event e = Event.current;
            if (enableKeyCode)
            {
                OnKeyCodeUpEvent(e);
            }
            if (lockSceneView) {
                OnButtonEvent(e);
            }
        }

        private void OnKeyCodeUpEvent(Event e)
        {
            if (e.type == EventType.KeyUp)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Q:
                        OnKeyCodeQ();
                        break;
                    case KeyCode.W:
                        OnKeyCodeW();
                        break;
                    case KeyCode.E:
                        OnKeyCodeE();
                        break;
                    case KeyCode.A:
                        OnKeyCodeA();
                        break;
                    case KeyCode.S:
                        OnKeyCodeS();
                        break;
                    case KeyCode.D:
                        OnKeyCodeD();
                        break;
                    case KeyCode.R:
                        OnKeyCodeR();
                        break;
                    case KeyCode.F:
                        OnKeyCodeF();
                        break;

                    // 数字键 0~9
                    case KeyCode.Alpha0:
                        OnKeyCodeAlphaNum(0);
                        break;
                    case KeyCode.Alpha1:
                        OnKeyCodeAlphaNum(1);
                        break;
                    case KeyCode.Alpha2:
                        OnKeyCodeAlphaNum(2);
                        break;
                    case KeyCode.Alpha3:
                        OnKeyCodeAlphaNum(3);
                        break;
                    case KeyCode.Alpha4:
                        OnKeyCodeAlphaNum(4);
                        break;
                    case KeyCode.Alpha5:
                        OnKeyCodeAlphaNum(5);
                        break;
                }
                e.Use();
            }
        }

        private void OnButtonEvent(Event e)
        {
            //var eventType = Event.current.GetTypeForControl(controlID);
            if (e.button == 0)
            {
                var eventType = Event.current.type;
                var controlID = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlID;
                switch (eventType)
                {
                    case EventType.MouseDrag:
                        curDragTriggerTime++;
                        if (curDragTriggerTime < dragInterval)
                        {
                            return;
                        }
                        curDragTriggerTime = 0;
                        OnMouseDrag(e);
                        break;
                    case EventType.MouseUp:
                        OnMouseUp(e);
                        break;
                    case EventType.MouseDown:
                        OnMouseDown(e);
                        break;
                    case EventType.MouseMove:
                        OnMouseMove(e);
                        break;
                }
                GUIUtility.hotControl = 0;
            }
            else if (e.button == 1)
            {
            }
        }

        #endregion

        protected virtual void OnKeyCodeQ() { }
        protected virtual void OnKeyCodeW() { }
        protected virtual void OnKeyCodeE() { }

        protected virtual void OnKeyCodeA() { }
        protected virtual void OnKeyCodeS() { }
        protected virtual void OnKeyCodeD(){ }

        protected virtual void OnKeyCodeR(){ }
        protected virtual void OnKeyCodeF(){ }

        protected virtual void OnKeyCodeAlphaNum(int num) { }

        protected virtual void OnMouseUp(Event e) { }
        protected virtual void OnMouseDown(Event e) { }
        protected virtual void OnMouseMove(Event e) { }
        protected virtual void OnMouseDrag(Event e) { }
        protected virtual void HandleSceneDraw() { }

        protected Vector3 GetMousePosToScene(Event e) {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            //当前屏幕坐标,左上角(0,0)右下角(camera.pixelWidth,camera.pixelHeight)
            Vector2 mousePos = e.mousePosition;
            //retina 屏幕需要拉伸值
            float mult = 1;
#if UNITY_5_4_OR_NEWER
            mult = EditorGUIUtility.pixelsPerPoint;
#endif
            //转换成摄像机可接受的屏幕坐标,左下角是(0,0,0);右上角是(camera.pixelWidth,camera.pixelHeight,0)
            mousePos.y = sceneView.camera.pixelHeight - mousePos.y * mult;
            mousePos.x *= mult;
            Vector3 fakePoint = mousePos;
            fakePoint.z = 20;
            Vector3 point = sceneView.camera.ScreenToWorldPoint(fakePoint);
            return point;
        }

    }
}
