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

            int paintRTSizeScale;

            public NodeGo() { }

            public NodeGo(BezierNode node, int i, Transform parent, int paintRTSizeScale)
            {
                this.paintRTSizeScale = paintRTSizeScale;
                //CreateSignObj(ref posObj, $"node_pos_{i}", parent, node.position);
                //CreateSignObj(ref inObj, $"node_in_{i}", parent, node.handleIn);
                //CreateSignObj(ref outObj, $"node_out_{i}", parent, node.handleOut);
                posObj = DebugUtil.DebugGameObject($"node_pos_{i}", node.position / paintRTSizeScale, parent);
                inObj = DebugUtil.DebugGameObject($"node_in_{i}", node.handleIn / paintRTSizeScale, parent);
                outObj = DebugUtil.DebugGameObject($"node_out_{i}", node.handleOut / paintRTSizeScale, parent);
            }

/*            private void CreateSignObj(ref GameObject go, string goName, Transform parent, Vector3 pos)
            {
                Vector3 fixedPos = pos / paintRTSizeScale;
                posObj = new GameObject(goName);
                posObj.transform.SetParent(parent);
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
            }*/

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

        // pos = pos / paintRTSizeScale, it will fix the pos of bezier
        int paintRTSizeScale;

        public void InitCurveEditer(BezierCurve bezierCurve, int paintRTSizeScale = 1)
        {
            this.bezierCurve = bezierCurve;
            this.paintRTSizeScale = paintRTSizeScale;       // ???为什么直接除是不对的？
            NodeGos = new List<NodeGo>(bezierCurve.Count);
            for (int i = 0; i < bezierCurve.Count; i++)
            {
                NodeGo nodeGo = new NodeGo(bezierCurve.Nodes[i], i, transform, paintRTSizeScale);
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
