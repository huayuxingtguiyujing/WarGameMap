using UnityEngine;
using Sirenix.OdinInspector;
using UnityEditor;
using LZ.WarGameMap.Runtime;

namespace LZ.WarGameMap.MapEditor {

    public abstract class BaseMapEditor: ScriptableObject {
        public abstract string EditorName { get; }

        protected bool notInitScene = true;

        protected EditorSceneManager sceneManager;    // �� static instance

        [FoldoutGroup("����scene", -9)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("����: û�г�ʼ��Scene")]
        public string warningMessage = "������ť��ʼ��!";

        [FoldoutGroup("����scene", -9)]
        [LabelText("����SceneView")]
        [Tooltip("���������󣬲ſ��Խ��л��Ʋ���")]
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


        [FoldoutGroup("����scene", -9)]
        [Button("��ʼ����������", ButtonSizes.Medium)]
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
            //��ǰ��Ļ����,���Ͻ�(0,0)���½�(camera.pixelWidth,camera.pixelHeight)
            Vector2 mousePos = e.mousePosition;
            //retina ��Ļ��Ҫ����ֵ
            float mult = 1;
#if UNITY_5_4_OR_NEWER
            mult = EditorGUIUtility.pixelsPerPoint;
#endif
            //ת����������ɽ��ܵ���Ļ����,���½���(0,0,0);���Ͻ���(camera.pixelWidth,camera.pixelHeight,0)
            mousePos.y = sceneView.camera.pixelHeight - mousePos.y * mult;
            mousePos.x *= mult;
            Vector3 fakePoint = mousePos;
            fakePoint.z = 20;
            Vector3 point = sceneView.camera.ScreenToWorldPoint(fakePoint);
            return point;
        }

    }
}
