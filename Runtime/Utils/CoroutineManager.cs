using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace LZ.WarGameMap.Runtime
{
    public class CoroutineManager : MonoBehaviour
    {
        private static CoroutineManager _instance;

        // TODO : ÒªÍêÉÆ CoroutineManager
        public static CoroutineManager GetInstance() {
            if (_instance == null) {
                GameObject go = GameObject.Find("CoroutineManager");
                if(go == null) {
                    go = new GameObject("CoroutineManager");
                    go.hideFlags = HideFlags.HideAndDontSave;
                }

                _instance = go.GetComponent<CoroutineManager>();
                if (_instance == null) {
                    _instance = go.AddComponent<CoroutineManager>();
                }
            }
            return _instance;
        }

        public Coroutine RunCoroutine(IEnumerator routine) {
            return StartCoroutine(routine);
        }

        public void StopRoutine(Coroutine coroutine) {
            if (coroutine != null) {
                StopCoroutine(coroutine);
            }
        }


    }
}
