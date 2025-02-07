

using LZ.WarGameMap.MapEditor;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace LZ.WarGameMap.Runtime.QuadTree {

    // �ο���https://www.cnblogs.com/JimmyZou/p/18385213 ʵ��

    /// <summary>
    /// ��ʾ �Ĳ��� ��ͼ�ϵĵ�
    /// </summary>
    public struct MapPoint {
        public float x; public float y;

        public MapPoint(float x, float y) {
            this.x = x;
            this.y = y;
        }

        public static bool operator ==(MapPoint p1, MapPoint p2) {
            return p1.x == p2.x && p1.y == p2.y;
        }

        public static bool operator !=(MapPoint p1, MapPoint p2) {
            return !(p1 == p2);
        }

        public static MapPoint operator +(MapPoint p1, MapPoint p2) {
            return new MapPoint(p1.x + p2.x, p1.y + p2.y);
        }

        public static MapPoint operator -(MapPoint p1, MapPoint p2) {
            return new MapPoint(p1.x - p2.x, p1.y - p2.y);
        }

        public static MapPoint operator *(MapPoint p1, float modify) {
            return new MapPoint(p1.x * modify, p1.y * modify);
        }

        public static MapPoint operator /(MapPoint p1, float modify) {
            return new MapPoint(p1.x / modify, p1.y / modify);
        }

        public static implicit operator MapPoint(Vector2 v) {
            return new MapPoint(v.x, v.y);
        }

        public static implicit operator MapPoint(Vector3 v) {
            return new MapPoint(v.x, v.z);
        }

        public static implicit operator Vector2(MapPoint v) {
            return new Vector2(v.x, v.y);
        }

        public static implicit operator Vector3(MapPoint v) {
            return new Vector3(v.x, 0, v.y);
        }

    }

    /// <summary>
    /// ��ʾ �Ĳ��� ��ͼ�е�һ����Χ
    /// </summary>
    internal struct Bound {
        public MapPoint leftDown;
        public MapPoint rightUp;
        public MapPoint center;

        public static Bound NullBound = new Bound(0, 0, 0, 0);

        public Bound(MapPoint x, MapPoint y) {
            leftDown = x; rightUp = y;
            center = (leftDown + rightUp) / 2;
        }

        public Bound(float x1, float y1, float x2, float y2) {
            this.leftDown = new MapPoint(x1, y1);
            this.rightUp = new MapPoint(x2, y2);
            center = (leftDown + rightUp) / 2;
        }

        // ����һ���㣬���ظõ���ڵ��������У�0��1��2��3 �ֱ���1 2 3 4���ޣ�
        public int GetAreaInside(MapPoint mp) {
            if(mp.x >= center.x && mp.y >= center.y) {
                return 0;
            }else if (mp.x <= center.x && mp.y >= center.y) {
                return 1;
            } else if (mp.x <= center.x && mp.y <= center.y) {
                return 2;
            } else if (mp.x >= center.x && mp.y <= center.y) {
                return 3;
            } else {
                // ��ʾ�޷����ֵ�������
                return 4;
            }
        }

        public Bound GetOverlapArea(Bound b1) {

            float leftBottomX = Math.Max(leftDown.x, b1.leftDown.x);
            float leftBottomY = Math.Max(leftDown.y, b1.leftDown.y);

            float rightTopX = Math.Min(rightUp.x, b1.rightUp.x);
            float rightTopY = Math.Min(rightUp.y, b1.rightUp.y);

            if (leftBottomX < rightTopX && leftBottomY < rightTopY) {
                return new Bound(new MapPoint(leftBottomX, leftBottomY), new MapPoint(rightTopX, rightTopY));
            } else {
                return NullBound;
            }
        }

        public bool IsPointInArea(MapPoint pos) {
            return pos.x > leftDown.x && pos.y > leftDown.y && pos.x < rightUp.x && pos.y < rightUp.y;
        }

        public static bool operator ==(Bound b1, Bound b2) {
            return b1.leftDown == b2.leftDown && b1.rightUp == b2.rightUp;
        }

        public static bool operator !=(Bound b1, Bound b2) {
            return !(b1 == b2);
        }

        public override bool Equals(object obj) {
            return obj is Bound bound &&
                   EqualityComparer<MapPoint>.Default.Equals(leftDown, bound.leftDown) &&
                   EqualityComparer<MapPoint>.Default.Equals(rightUp, bound.rightUp);
        }

    }

    public class QuadTreeNode<T> where T : class {

        internal Bound bound;

        public int curDepth;

        internal bool isLeaf;

        internal QuadTreeNode<T>[] children;


        internal List<T> managedObject;

        internal List<Vector3> objectPos;

        internal Dictionary<T, Vector3> objectPosDict;


        internal QuadTreeNode(MapPoint leftDown, MapPoint rightUp, int depth, bool _isLeaf) {
            bound = new Bound(leftDown, rightUp);
            curDepth = depth;
            isLeaf = _isLeaf;
            managedObject = new List<T>();
            objectPos = new List<Vector3>();
            if (isLeaf) {
                children = new QuadTreeNode<T>[0];
            } else {
                children = new QuadTreeNode<T>[4];
            }
        }

        // �Ĳ��������в�����
        // ����(BuildQuadTree)����Ҫ�涨������������maxDepth
        // ����(Insert):
        // �Ƴ�(Remove):
        // ����(Search): ��������
        // ���£����ڶ���̬�ƶ��������ÿ���ƶ���Ҫ��������ʵʱ���Ƴ��Ͳ��룬Ҳ���Բ��ýڵ����object

        internal static QuadTreeNode<T> BuildQuadTree(MapPoint leftDown, MapPoint rightUp, int depth, int curDepth) {
            if(depth <= 0) {
                var node = new QuadTreeNode<T>(leftDown, rightUp, curDepth, true);
                return node;
            } else {
                var node = new QuadTreeNode<T>(leftDown, rightUp, curDepth, false);
                var center = (leftDown + rightUp) / 2;
                var centerLeft = new MapPoint(leftDown.x, center.y);
                var centerRight = new MapPoint(rightUp.x, center.y);
                var centerUp = new MapPoint(center.x, rightUp.y);
                var centerDown = new MapPoint(center.x, leftDown.y);

                curDepth++;
                depth--;
                node.children[0] = BuildQuadTree(center, rightUp, depth, curDepth);
                node.children[1] = BuildQuadTree(centerLeft, centerUp, depth, curDepth);
                node.children[2] = BuildQuadTree(leftDown, center, depth, curDepth);
                node.children[3] = BuildQuadTree(centerDown, centerRight, depth, curDepth);
                return node;
            }
        }

        internal void Insert(MapPoint point, T obj) {
            if (isLeaf) {
                managedObject.Add(obj);
                objectPos.Add(point);
            } else {
                int childIdx = bound.GetAreaInside(point);
                if (childIdx < 4) {
                    children[childIdx].Insert(point, obj);
                } else {
                    // ���ڻ����߱�Ե���������뵽�����У���̫�������Ϊ��
                    //managedObject.Add(obj);
                    //objectPos.Add(point);
                }
            }
        }

        internal void Remove(MapPoint point, T obj) {
            if (isLeaf) {
                RemoveManagedObj(obj);
            } else {
                int childIdx = bound.GetAreaInside(point);
                if (childIdx < 4) {
                    children[childIdx].Remove(point, obj);
                } else {
                    // ���ڱ�Ե���������뵽������
                    RemoveManagedObj(obj);
                }
            }
        }

        private void RemoveManagedObj(T obj) {
            for (int i = 0; i < managedObject.Count; i++) {
                if (managedObject[i].Equals(obj)) {
                    managedObject.Remove(obj);
                    break;
                }
            }
        }

        // TODO: ���������Χ��Ѱ�ķ���
        internal void SearchArea(MapPoint p1, MapPoint p2, ref List<T> findObjs) {
             
            Bound area = new Bound(p1, p2);
            // no area overlap, so stop search
            if (bound.GetOverlapArea(area) == Bound.NullBound) {
                return;
            }

            for (int i = 0; i < managedObject.Count; i++) {
                Vector2 objPos = objectPos[i];
                // TODO: Ҫ��ƫ��ֵ��......
                if(area.IsPointInArea(objPos)) {
                    findObjs.Add(managedObject[i]);
                }
            }

            if (!isLeaf) {
                foreach (var child in children)
                {
                    child.SearchArea(p1, p2, ref findObjs);
                }
            }
        }

        internal T SearchObj(MapPoint pos, float fixNum) {

            if (!bound.IsPointInArea(pos)) {
                return null;
            }

            if (isLeaf) {
                T obj = null;
                float rec = float.MaxValue;
                for (int i = 0; i < managedObject.Count; i++) {
                    Vector2 objPos = objectPos[i];
                    float distance = Vector2.Distance(objPos, pos);
                    if (rec > distance) {
                        rec = distance;
                        obj = managedObject[i];
                    }
                }
                return obj;
            }

            if (!isLeaf) {
                foreach (var child in children) {
                    T obj = child.SearchObj(pos, fixNum);
                    if (obj != null) return obj;
                }
            }

            return null;
        }

        // TODO: ��û����ɸ���һ����Ϸ�����λ��, ���ȱʧ�������, ��ôQuadTreeֻ�����ڹ���̬������ 
        internal void UpdateObj(MapPoint originPos, MapPoint newPos, T obj) {

        }


        public void DrawScopeInGizmos() {
            GizmosUtils.DrawRect(bound.leftDown, bound.rightUp, GizmosUtils.GetRandomColor(curDepth));
            foreach (var child in children)
            {
                child.DrawScopeInGizmos();
            }

            if (isLeaf) {
                for(int i = 0; i < objectPos.Count; i++) {
                    GizmosUtils.DrawCube(objectPos[i], Color.black);
                }
            }
        }

    }


    // TODO: Ӧ�����¼����� ��������
    public class QuadTree<T> where T : class{

        public QuadTreeNode<T> root { get; private set; }

        public void BuildTree(Vector3 leftDown, Vector3 rightUp, int depth, List<T> managedObjs, List<Vector3> poss) {
            if(managedObjs.Count < poss.Count) {
                Debug.Log("wrong obj input count!");
                return;
            }

            //Debug.Log(string.Format("start build quad tree, depth: {0}, objs: {1}, pos num: {2}", depth, managedObjs.Count, poss.Count));
            //Debug.Log(string.Format("quad tree scope, left down: {0}, right up: {1} ", leftDown, rightUp));
            root = QuadTreeNode<T>.BuildQuadTree((MapPoint)leftDown, (MapPoint)rightUp, depth, 0);
            for (int i = 0; i < managedObjs.Count; i++) {
                root.Insert((MapPoint)poss[i], managedObjs[i]);
            }

            Debug.Log("now the quad tree is builded!!!");
        }

        public void SearchArea(Vector2 leftDown, Vector2 rightUp, ref List<T> findObjs) {
            if(root == null) {
                return;
            }
            root.SearchArea(leftDown, rightUp, ref findObjs);
        }

        public void SearchObjByPos(Vector2 center, ref T obj) {
            if (root == null) {
                return;
            }
            obj = root.SearchObj(center, 0);
        }


        public void DrawScopeInGizmos() {
            root.DrawScopeInGizmos();
        }
    
    }



}

