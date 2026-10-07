using LZ.WarGameMap.Runtime.HexStruct;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    
    /// <summary>保存有效格子的 offset 坐标和地图尺寸；不保存运行时状态或分块显示资源。</summary>
    [Serializable]
    public class HexMapSO : ScriptableObject {
        [SerializeField] private int mapWidth;
        [SerializeField] private int mapHeight;
        [SerializeField] private bool hasGenerated;
        [SerializeField] private List<Vector2Int> gridCoordinates = new List<Vector2Int>();

        public int MapWidth => mapWidth;
        public int MapHeight => mapHeight;
        public bool HasGenerated => hasGenerated;
        public int GridCount => gridCoordinates.Count;
        public IReadOnlyList<Vector2Int> GridCoordinates => gridCoordinates;

        internal void ReplaceData(int width, int height, List<Vector2Int> coordinates) {
            mapWidth = width;
            mapHeight = height;
            gridCoordinates = coordinates;
            hasGenerated = true;
        }
    }
}
