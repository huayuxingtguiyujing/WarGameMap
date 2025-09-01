using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using LZ.WarGameMap.Runtime;

namespace LZ.WarGameMap.MapEditor {

    public abstract class BaseMapEditor: ScriptableObject {
        public abstract string EditorName { get; }

        protected bool notInitScene = true;

        protected EditorSceneManager sceneManager;    // 即 static instance

        [FoldoutGroup("配置scene", -9)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: 没有初始化Scene")]
        public string warningMessage = "请点击按钮初始化!";

        [FoldoutGroup("配置scene", -9)]
        [LabelText("锁定SceneView")]
        [Tooltip("仅在锁定后，才可以进行绘制操作")]
        [OnValueChanged("OnLockSceneViewValueChanged")]
        public bool lockSceneView = true;

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
            string mapSetFolerName = AssetsUtility.GetInstance().GetFolderFromPath(MapStoreEnum.WarGameMapSettingPath);
            if (!AssetDatabase.IsValidFolder(MapStoreEnum.WarGameMapSettingPath)) {
                AssetDatabase.CreateFolder(MapStoreEnum.WarGameMapRootPath, mapSetFolerName);
            }
        }

        protected void FindOrCreateSO<T>(ref T so, string folderPath, string assetName) where T : ScriptableObject {
            // assetName = "TerrainSetting_Default.asset"
            // assetName = "HexSetting_Default.asset"
            if (so == null) {
                string terrainSettingPath = folderPath + "/" + assetName;
                so = AssetDatabase.LoadAssetAtPath<T>(terrainSettingPath);
                if (so == null) {           // create it !
                    so = CreateInstance<T>();
                    AssetDatabase.CreateAsset(so, terrainSettingPath);
                    Debug.Log($"successfully create map Setting SO, path : {terrainSettingPath}");
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

            HandleSceneDraw(); 
            
            if (!lockSceneView) {
                return;
            }

            Event e = Event.current;
            var eventType = Event.current.type;
            if (e.button == 0) {
                var controlID = GUIUtility.GetControlID(FocusType.Passive);
                GUIUtility.hotControl = controlID;
                //var eventType = Event.current.GetTypeForControl(controlID);
                switch (eventType) {
                    case EventType.MouseDrag:
                        curDragTriggerTime++;
                        if (curDragTriggerTime < dragInterval) {
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
            } else if (e.button == 1) {
            }

        }

        #endregion

        protected virtual void OnMouseUp(Event e) {
        }

        protected virtual void OnMouseDown(Event e) {
        }

        protected virtual void OnMouseMove(Event e) {
            //SceneView.RepaintAll();
        }

        protected virtual void OnMouseDrag(Event e) {
        }

        protected virtual void HandleSceneDraw() {
        }


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
