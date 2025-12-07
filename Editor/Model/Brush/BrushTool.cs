using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{
    public enum BrushToolType
    {
        Circle,
        CircleLinearLerped,
        CircleSoothStep1,
        CircleSoothStep2,
        CircleNoise
    }

    [Serializable]
    public abstract class BaseBrushTool
    {
        public abstract string GetBrushToolName();

        public abstract string GetBrushToolDesc();

        // Strength will only be applyed in Y axis
        public abstract void Brush(float brushStrength, float brushScope, Vector3 brushCenter, List<Vector3> brushTargets);
    }

    [Serializable]
    public class CircleBrushTool : BaseBrushTool
    {
        public override string GetBrushToolDesc() { return ""; }

        public override string GetBrushToolName() { return "圈型"; }

        public override void Brush(float brushStrength, float brushScope, Vector3 brushCenter, List<Vector3> brushTargets) 
        { 
            // TODO : test it
            for(int i = 0; i <  brushTargets.Count; i++)
            {
                Vector3 last = brushTargets[i];
                last = new Vector3(last.x, last.y + brushStrength, last.z);
                brushTargets[i] = last;
            }
        }
    }

    [Serializable]
    public class CircleLinearLerpBrushTool : BaseBrushTool
    {
        public override string GetBrushToolDesc() { return ""; }

        public override string GetBrushToolName() { return "线性渐变 圈型"; }

        public override void Brush(float brushStrength, float brushScope, Vector3 brushCenter, List<Vector3> brushTargets)
        {
            // TODO : test it
            Vector2 brushCenterXZ = new Vector2(brushCenter.x, brushCenter.z);
            for (int i = 0; i < brushTargets.Count; i++)
            {
                Vector3 last = brushTargets[i];
                Vector2 lastXZ = new Vector2(last.x, last.z);
                float distance = Vector2.Distance(lastXZ, brushCenterXZ);
                float ratio = Mathf.Abs(brushScope - distance) / brushScope;
                last = new Vector3(last.x, last.y + brushStrength * ratio, last.z);
                brushTargets[i] = last;
            }
        }
    }

}

