using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace LZ.WarGameMap.Runtime
{
    public static class DebugUtil
    {

        private static Transform debugParent = null;

        private static string debugParentName = "debugParent";

        private static Transform GetDebugParent()
        {
            if(debugParent != null)
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

            if(parent == null)
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


    }
}
