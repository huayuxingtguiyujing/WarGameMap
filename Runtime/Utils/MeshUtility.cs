using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class MeshUtility
    {
        public static string MeshToString(Mesh mesh, string name) {
            StringBuilder sb = new StringBuilder();
            sb.Append("g ").Append(name).Append("\n");

            foreach (Vector3 lv in mesh.vertices) {
                Vector3 wv = lv;
                sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");
            foreach (Vector3 lv in mesh.normals) {
                Vector3 wv = lv;
                sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            }
            sb.Append("\n");
            foreach (Vector3 v in mesh.uv) {
                sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
            }
            
            return sb.ToString();
        }

    }
}
