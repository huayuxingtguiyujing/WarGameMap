
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

        protected EditorSceneManager sceneManager;    // 即 static instance

        [FoldoutGroup("配置scene", -1)]
        [GUIColor(1f, 0f, 0f)]
        [ShowIf("notInitScene")]
        [LabelText("警告: 没有初始化Scene")]
        public string warningMessage = "请点击按钮初始化!";

        [FoldoutGroup("配置scene", -1)]
        [Button("初始化地形配置", ButtonSizes.Medium)]
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
            Event e = Event.current;
            var eventType = Event.current.type;
            if (e.button == 0) {

                //var controlID = GUIUtility.GetControlID(FocusType.Passive);
                //GUIUtility.hotControl = controlID;
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

                //GUIUtility.hotControl = 0;
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


        protected Vector2 GetMousePos(Event e) {

            Vector2 mousePosition = e.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            if (ray.direction.z != 0) {
                float t = -ray.origin.z / ray.direction.z;
                Vector3 worldPosition = ray.origin + t * ray.direction;
                return worldPosition;
            }
            return Vector2.zero;
        }




    }



}
