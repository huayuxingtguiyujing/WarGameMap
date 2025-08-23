//#define ENABLE_DEBUG_LOW
//#define ENABLE_DEBUG_MEDIUM
#define ENABLE_DEBUG_HIGH

#define ENABLE_DEBUG
#define ENABLE_DEBUG_EDITOR
#define ENABLE_DEBUG_RUNTIME

using System.Linq;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public enum DebugPriority
    {
        Low = 0, 
        Medium = 1, 
        High = 2
    }

    public static class DebugUtility
    {
        private static Transform debugParent = null;

        private static string debugParentName = "debugParent";

        private static Transform GetDebugParent()
        {
            if (debugParent != null)
            {
                return debugParent;
            }
            GameObject debugParentObj = GameObject.Find(debugParentName);
            if (debugParentObj == null)
            {
                debugParentObj = new GameObject(debugParentName);
            }
            debugParent = debugParentObj.transform;
            return debugParent;
        }

        public static GameObject DebugGameObject(string goName, Vector3 pos, Transform parent)
        {
            Vector3 fixedPos = pos;
            GameObject posObj = new GameObject(goName);

            if (parent == null)
            {
                posObj.transform.SetParent(GetDebugParent());
            }
            else
            {
                posObj.transform.SetParent(parent);
            }
            posObj.transform.position = fixedPos;
            posObj.AddComponent<MeshRenderer>();
            MeshFilter meshFilter = posObj.AddComponent<MeshFilter>();

            int signSize = 5;
            Mesh mesh = new Mesh();
            Vector3[] verts = new Vector3[3] {
                    fixedPos + new Vector3(-signSize * Mathf.Sqrt(3) / 2, 0, -signSize / 2),
                    fixedPos + new Vector3(signSize * Mathf.Sqrt(3) / 2, 0, -signSize / 2),
                    fixedPos + new Vector3(0, 0, signSize)};
            int[] triangles = new int[3] {
                    0, 2, 1
                };
            mesh.SetVertices(verts.ToList());
            mesh.SetTriangles(triangles, 0);
            meshFilter.sharedMesh = mesh;

            return posObj;
        }


        public static void Log(int mes, DebugPriority priority = DebugPriority.Low, bool forceShow = false)
        {
            Log(mes.ToString(), priority, forceShow);
        }

        public static void Log(float mes, DebugPriority priority = DebugPriority.Low, bool forceShow = false)
        {
            Log(mes.ToString(), priority, forceShow);
        }

        public static void Log(string mes, DebugPriority priority = DebugPriority.Low, bool forceShow = false)
        {
#if ENABLE_DEBUG
#if UNITY_EDITOR && ENABLE_DEBUG_EDITOR
#if ENABLE_DEBUG_LOW
            Debug.Log(mes);
#elif ENABLE_DEBUG_MEDIUM
            if(priority == DebugPriority.Medium || priority == DebugPriority.High) Debug.Log(mes);
#elif ENABLE_DEBUG_HIGH
            if (priority == DebugPriority.High) Debug.Log(mes);
#endif
#elif ENABLE_DEBUG_RUNTIME
#if ENABLE_DEBUG_LOW
            Debug.Log(mes);
#elif ENABLE_DEBUG_MEDIUM
            if(priority == DebugPriority.Medium || priority == DebugPriority.High) Debug.Log(mes);
#elif ENABLE_DEBUG_HIGH
            if(priority == DebugPriority.High) Debug.Log(mes);
#endif
#endif
#endif
        }
        public static void LogError(int mes, DebugPriority priority = DebugPriority.Low, bool forceShow = false)
        {
            LogError(mes.ToString(), priority, forceShow);
        }

        public static void LogError(float mes, DebugPriority priority = DebugPriority.Low, bool forceShow = false)
        {
            LogError(mes.ToString(), priority, forceShow);
        }

        public static void LogError(string mes, DebugPriority priority = DebugPriority.Low, bool forceShow = false)
        {
#if ENABLE_DEBUG
#if UNITY_EDITOR && ENABLE_DEBUG_EDITOR
#if ENABLE_DEBUG_LOW
            Debug.LogError(mes);
#elif ENABLE_DEBUG_MEDIUM
            if(priority == DebugPriority.Medium || priority == DebugPriority.High) Debug.LogError(mes);
#elif ENABLE_DEBUG_HIGH
            if (priority == DebugPriority.High) Debug.LogError(mes);
#endif
#elif ENABLE_DEBUG_RUNTIME
#if ENABLE_DEBUG_LOW
            Debug.LogError(mes);
#elif ENABLE_DEBUG_MEDIUM
            if(priority == DebugPriority.Medium || priority == DebugPriority.High) Debug.LogError(mes);
#elif ENABLE_DEBUG_HIGH
            if(priority == DebugPriority.High) Debug.LogError(mes);
#endif
#endif
#endif
        }

    }
}
