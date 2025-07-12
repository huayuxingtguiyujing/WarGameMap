
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using static TreeEditor.TextureAtlas;
using System.IO;
using LZ.WarGameMap.Runtime;
using System;
using UnityEngine.SceneManagement;

namespace LZ.WarGameMap.MapEditor {

    

    public abstract class BaseMapEditor: ScriptableObject {
        public abstract string EditorName { get; }

        bool notInitScene = true;

        protected EditorSceneManager sceneManager;    // �� static instance

        [FoldoutGroup("����scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("����: û�г�ʼ��Scene")]
        public string warningMessage = "������ť��ʼ��!";

        [FoldoutGroup("����scene", -1)]
        [LabelText("����SceneView")]
        [Tooltip("���������󣬲ſ��Խ��л��Ʋ���")]
        public bool lockSceneView = true;

        [FoldoutGroup("����scene", -1)]
        [Button("��ʼ����������", ButtonSizes.Medium)]
        protected virtual void InitEditor() {
            //sceneManager = EditorSceneManager.GetInstance();
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
        }

        public virtual void Disable() {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        public virtual void Destory() {

        }

        protected virtual void OnSceneGUI(SceneView sceneView) {
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
            SceneView.RepaintAll();
        }

        protected virtual void OnMouseDrag(Event e) {
        }


        protected Vector3 GetMousePos(Event e) {
            //Vector2 mousePosition = e.mousePosition;
            //Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            //if (ray.direction.z != 0) {
            //    float t = -ray.origin.z / ray.direction.z;
            //    Vector3 worldPosition = ray.origin + t * ray.direction;
            //    return worldPosition;
            //}
            return Vector2.zero;

            //SceneView sceneView = SceneView.lastActiveSceneView;
            //if (sceneView == null || sceneView.camera == null) {
            //    Debug.LogError("SceneView camera not available.");
            //    return Vector3.zero;
            //}

            //Vector2 guiMousePos = Event.current.mousePosition;
            //guiMousePos.y = sceneView.position.height - guiMousePos.y;

            //Vector3 screenPos = new Vector3(guiMousePos.x, guiMousePos.y, depthFromCamera);
            //return sceneView.camera.ScreenToWorldPoint(screenPos);
        }

        public bool GetMousePosOnGround(Event e, out Vector3 worldPoint) {
            worldPoint = Vector3.zero;

            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null) {
                Debug.LogError("SceneView camera not available.");
                return false;
            }

            Camera cam = sceneView.camera;
            Vector2 guiPos = e.mousePosition;
            guiPos.y = sceneView.position.height - guiPos.y;

            // create a ray
            Ray ray = cam.ScreenPointToRay(guiPos);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (ground.Raycast(ray, out float enter)) {
                worldPoint = ray.GetPoint(enter);
                return true;
            }
            return false;
        }

        public static Vector3 GetMousePosToScene(Event e) {
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
            //��ƽ������һЩ,���ܿ�����������λ��
            Vector3 fakePoint = mousePos;
            fakePoint.z = 20;
            Vector3 point = sceneView.camera.ScreenToWorldPoint(fakePoint);
            return point;
        }

    }
}
