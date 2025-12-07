using System;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // use in map editor, show map statu in scene
    public class GizmosCtrl : MonoBehaviour, IDisposable
    {

        static GizmosCtrl instance;
        public static GizmosCtrl GetInstance() {
            if(instance == null) {
                GameObject mapRootObj = GameObject.Find(MapEnum.MapRootName);
                if (mapRootObj == null) {
                    mapRootObj = new GameObject(MapEnum.MapRootName);
                }
                instance = mapRootObj.GetComponent<GizmosCtrl>();
                if (instance == null) {
                    instance = mapRootObj.AddComponent<GizmosCtrl>();
                }
            }
            return instance;
        }

        public void Dispose() {
            if(instance != null ) {
                DestroyImmediate(instance.gameObject);
            }
        }


        public delegate void GizmoDrawEventHandler();

        private GizmoDrawEventHandler onDrawGizmoEvent;

        public event GizmoDrawEventHandler OnDrawGizmoEvent {
            add { onDrawGizmoEvent += value; }
            remove { onDrawGizmoEvent -= value; }
        }

        //public event GizmoDrawEventHandler OnDrawGizmoEvent;

        public void RegisterGizmoEvent(GizmoDrawEventHandler handler) {
            OnDrawGizmoEvent += handler;
        }

        public void UnregisterGizmoEvent(GizmoDrawEventHandler handler) {
            OnDrawGizmoEvent -= handler;
        }

        public void UnregisterGizmosAll() {
            onDrawGizmoEvent = null;
        }

        private void OnDrawGizmos() {
            onDrawGizmoEvent?.Invoke();
        }

        void OnDrawGizmosSelected() {
            //// 在变换位置绘制一个黄色立方体
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawWireCube(transform.position, new Vector3(10, 10, 10));
        }

    }

    // TODO: 实现一个简单的观察者模式
    public abstract class EventMode : MonoBehaviour 
    {

    }

}
