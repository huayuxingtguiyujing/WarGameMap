using System;
using UnityEngine;


    [Serializable]
    public class GuidScriptableObject : ScriptableObject {
        [HideInInspector, SerializeField]
        private byte[] m_Guid;

        public Guid Guid {
            get {
                return new Guid(m_Guid);
            }
        }

        public void InitThisUnitGuid() {
            //生成一个guid
            if (m_Guid.Length == 0) {
                m_Guid = Guid.NewGuid().ToByteArray();
            }
        }

        void OnValidate() {
            InitThisUnitGuid();
        }
    }
