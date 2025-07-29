using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    // this mono can help you edit bezier curve in scene (Editor Mode)
    // call InitCurveEditer to init it
    // call SyncToBezierCurve to apply the change to node
    public class BezierCurveEditor : MonoBehaviour
    {

        BezierCurve bezierCurve;
        public BezierCurve Curve {  get => bezierCurve;}

        class NodeGo : IDisposable
        {
            GameObject posObj;
            GameObject inObj;
            GameObject outObj;

            public NodeGo() { }

            public NodeGo(BezierNode node, int i, Transform parent)
            {
                // TODO : ÓÐ bug£¬ÐèÒªÐÞ
                //CreateSignObj(ref posObj, $"node_pos_{i}", parent, node.position);
                //CreateSignObj(ref inObj, $"node_in_{i}", parent, node.handleIn);
                //CreateSignObj(ref outObj, $"node_out_{i}", parent, node.handleOut);
            }

            private void CreateSignObj(ref GameObject go, string goName, Transform parent, Vector3 pos)
            {
                posObj = new GameObject(goName);
                posObj.transform.SetParent(parent);
                posObj.transform.position = pos;
                posObj.AddComponent<MeshRenderer>();
                MeshFilter meshFilter = posObj.AddComponent<MeshFilter>();

                int signSize = 5;
                Mesh mesh = new Mesh();
                Vector3[] verts = new Vector3[3] {
                    pos + new Vector3(-signSize * Mathf.Sqrt(3) / 2, 0, -signSize / 2),
                    pos + new Vector3(signSize * Mathf.Sqrt(3) / 2, 0, -signSize / 2), 
                    pos + new Vector3(0, 0, signSize)};
                int[] triangles = new int[3] {
                    0, 2, 1
                };
                mesh.SetVertices(verts.ToList());
                mesh.SetTriangles(triangles, 0);
                meshFilter.sharedMesh = mesh;
            }

            public Vector3 GetPos() { return posObj.transform.position; }

            public Vector3 GetIn() { return inObj.transform.position; }

            public Vector3 GetOut() { return outObj.transform.position; }

            public void Dispose()
            {
                 DestroyImmediate(posObj);
                 DestroyImmediate(inObj);
                 DestroyImmediate(outObj);
            }
        }

        List<NodeGo> NodeGos;

        public void InitCurveEditer(BezierCurve bezierCurve)
        {
            this.bezierCurve = bezierCurve;
            NodeGos = new List<NodeGo>(bezierCurve.Count);
            for (int i = 0; i < bezierCurve.Count; i++)
            {
                NodeGo nodeGo = new NodeGo(bezierCurve.Nodes[i], i, transform);
                NodeGos.Add(nodeGo);
            }
        }

        public void SyncToBezierCurve()
        {
            for(int i = 0; i < bezierCurve.Count; i++)
            {
                bezierCurve.SetNode(i, new BezierNode(NodeGos[i].GetPos(), NodeGos[i].GetIn(), NodeGos[i].GetOut()));
            }
        }

        public void Dispose()
        {
            if(NodeGos != null)
            {
                for (int i = 0; i < NodeGos.Count; i++)
                {
                    NodeGos[i].Dispose();
                }
                NodeGos.Clear();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

    }
}
