using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

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
            //// �ڱ任λ�û���һ����ɫ������
            //Gizmos.color = Color.yellow;
            //Gizmos.DrawWireCube(transform.position, new Vector3(10, 10, 10));
        }

    }


    // TODO: ʵ��һ���򵥵Ĺ۲���ģʽ
    public abstract class EventMode : MonoBehaviour {
    }

}
