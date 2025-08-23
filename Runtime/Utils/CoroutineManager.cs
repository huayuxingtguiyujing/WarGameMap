using System.Collections;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class CoroutineManager : BaseManager
    {
        private static CoroutineManager _instance;

        public static CoroutineManager GetInstance() {
            if(_instance == null)
            {
                _instance = GetInstance<CoroutineManager>();
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
