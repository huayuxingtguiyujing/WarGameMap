
using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using static TreeEditor.TextureAtlas;
using System.IO;

namespace LZ.WarGameMap.MapEditor {

    public abstract class BaseMapEditor: ScriptableObject
    {

        public abstract string EditorName { get; }

        protected bool notInitScene = true;

        protected abstract void InitEditor();


        public virtual void Enable() {
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

        protected virtual void OnMouseUp(Event e) {
        }

        protected virtual void OnMouseDown(Event e) {
        }

        protected virtual void OnMouseMove(Event e) {

            SceneView.RepaintAll();
        }

        protected virtual void OnMouseDrag(Event e) {
        }

        protected virtual void OnDrawGizmos() {

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
