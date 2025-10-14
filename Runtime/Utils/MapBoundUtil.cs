using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class MapBoundUtil
    {
        public static List<Vector2> GetBoundPointList(Layout layout, List<Vector2Int> gridOffsetList, List<Vector2Int> boundList)
        {
            List<Vector2> points = new List<Vector2>(gridOffsetList.Count);
            HashSet<Vector2Int> gridOffsetSet = new HashSet<Vector2Int>(gridOffsetList);
            HashSet<Vector2Int> hasAddedPoints = new HashSet<Vector2Int>(gridOffsetList.Count);
            //HashSet<Vector2Int> hasAddedBoundPoint = new HashSet<Vector2Int>();

            foreach (var boundOffset in boundList)
            {
                Vector2Int[] neighbour = HexHelper.GetOffsetHexNeighbour(boundOffset);
                foreach (var neighbor in neighbour)
                {
                    Vector2Int cur = neighbor + boundOffset;
                    if (!gridOffsetSet.Contains(cur))
                    {
                        // TODO : 获取的边缘点不是正确的
                        // Neighbor is boundary grid, add edge point in list
                        Vector2[] edgePoints = HexHelper.GetOffsetHexEdgePoint(layout, boundOffset, cur);
                        Vector2Int edge1 = new Vector2Int((int)edgePoints[0].x, (int)edgePoints[0].y);
                        Vector2Int edge2 = new Vector2Int((int)edgePoints[1].x, (int)edgePoints[1].y);

                        if (!hasAddedPoints.Contains(edge1))
                        {
                            points.Add(edge1);
                            hasAddedPoints.Add(edge1);
                        }
                        if (!hasAddedPoints.Contains(edge2))
                        {
                            points.Add(edge2);
                            hasAddedPoints.Add(edge2);
                        }
                    }
                }
            }

            // TODO : 将边界点连接成边界线
            // Build boundary point map
            List<List<int>> boundaryPointMap = GetBoundaryMap(points);
            //List<List<int>> boundaryPointMap = null;

            List<Vector2> sortedBoundPoints = new List<Vector2>(gridOffsetList.Count);
            HashSet<int> hasAddedPointsIdx = new HashSet<int>(gridOffsetList.Count);
            Stack<int> visitStack = new Stack<int>();
            visitStack.Push(0);

            // Link all vector2, add to sorted bound points
            while (visitStack.Count > 0)
            {
                int idx = visitStack.Pop();
                if (hasAddedPointsIdx.Contains(idx))
                {
                    continue;
                }
                hasAddedPointsIdx.Add(idx);
                sortedBoundPoints.Add(points[idx]);

                // Get neighbor point and check
                List<int> neighbour = boundaryPointMap[idx];
                if (neighbour.Count != 2)
                {
                    Debug.Log($"Wrong index : {neighbour.Count}");
                }

                foreach (var neighborIdx in neighbour)
                {
                    visitStack.Push(neighborIdx);
                }
            }

            return sortedBoundPoints;
        }

        // TODO : implement it
        public static List<List<int>> GetBoundaryMap(List<Vector2> points)
        {
            return null;
        }

    }
}
