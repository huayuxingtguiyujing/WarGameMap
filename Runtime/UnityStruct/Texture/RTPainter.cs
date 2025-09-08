using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public class RenderTexturePainter
    {

        readonly string paintShaderName = "PaintRTPixels";

        ComputeShader paintShader;
        RenderTexture targetRT;

        public RenderTexturePainter(ComputeShader paintShader, RenderTexture targetRT)
        {
            this.paintShader = paintShader;
            this.targetRT = targetRT;
        }

        struct Pixel
        {
            public Vector2Int pos;
        }

        public void PaintPixels(List<Vector2Int> points, Color color)
        {
            if (points == null || points.Count == 0)
            {
                return;
            }
            if (targetRT == null)
            {
                Debug.LogError("targetRT is null!");
                return;
            }
            if (!targetRT.enableRandomWrite)
            {
                Debug.LogError("targetRT.enableRandomWrite must be true!");
                return;
            }

            // Create compute buffer
            Pixel[] pixelArray = new Pixel[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                pixelArray[i].pos = points[i];
            }
            ComputeBuffer buffer = new ComputeBuffer(pixelArray.Length, sizeof(int) * 2);
            buffer.SetData(pixelArray);

            // Run paint RT shader
            int kernel = paintShader.FindKernel(paintShaderName);
            paintShader.SetBuffer(kernel, "_Pixels", buffer);
            paintShader.SetInt("_PixelCount", pixelArray.Length);
            paintShader.SetVector("_Color", color);
            paintShader.SetTexture(kernel, "_Result", targetRT);
            int threadGroups = Mathf.CeilToInt(pixelArray.Length / 64.0f);
            paintShader.Dispatch(kernel, threadGroups, 1, 1);

            buffer.Release();
        }
    }
}
