using Codice.Client.BaseCommands;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    // only valid in UNITY_EDITOR
    // this class is to help manager the texture that storage the hex map data
    public class HexmapDataTexManager : IDisposable
    {

        struct FillHexDataTextureJob : IJobParallelFor {
            // it seems useless
            [ReadOnly] public Color fillColor;
            [WriteOnly] Color[] datas;

            public void Execute(int index) {
                datas[index] = fillColor;
            }
        }

        struct PaintHexDataTextureJob : IJobParallelFor {

            [ReadOnly] public Color fillColor;
            [WriteOnly] public NativeArray<Color> datas;

            public void Execute(int index) {
                datas[index] = fillColor;
            }
        }

        int mapWdith;
        int mapHeight;
        int scale; 
        Vector3 offset;

        int paintUseJobLimit = 100; // use job system if paint pixel num is more than this

        RenderTexture texDataRenderTexture;

        MeshRenderer meshRenderer;

        MeshFilter meshFilter;

        Mesh hexTexMesh;

        GameObject parentObj;

        public bool IsInit {  get; private set; }


        #region init hexDataTexManager

        public HexmapDataTexManager() {
            IsInit = false;
        }

        public void InitHexmapDataTexture(int mapWdith, int mapHeight, int scale, Vector3 offset, GameObject parentObj, Material material) {
            this.mapWdith = mapWdith;
            this.mapHeight = mapHeight;
            this.scale = scale;
            this.offset = offset;
            CreateHexDataMeshObj(scale, offset, parentObj);
            CreateRenderTexture(mapWdith, mapHeight);
            ApplyHexDataTexture(material);
            IsInit = true;

            Debug.Log("init HexmapDataTextureManager over ");
        }

        // this method can be used to set a Texture
        public void InitHexmapDataTexture(Texture2D newTexture, int scale, Vector3 offset, GameObject parentObj, Material material) {
            this.mapWdith = newTexture.width;
            this.mapHeight = newTexture.height;
            this.scale = scale;
            this.offset = offset;
            CreateHexDataMeshObj(scale, offset, parentObj);
            CreateRenderTexture(mapWdith, mapHeight);
            Graphics.Blit(newTexture, texDataRenderTexture);
            ApplyHexDataTexture(material);
            IsInit = true;

            Debug.Log("init HexmapDataTextureManager over ");
        }

        private void CreateHexDataMeshObj(int scale, Vector3 offset, GameObject parentObj) {
            this.parentObj = parentObj;

            meshRenderer = parentObj.GetComponent<MeshRenderer>();
            if (meshRenderer == null) {
                meshRenderer = parentObj.AddComponent<MeshRenderer>();
            }

            meshFilter = parentObj.GetComponent<MeshFilter>();
            if (meshFilter == null) {
                meshFilter = parentObj.AddComponent<MeshFilter>();
            }

            // create the hex mesh by offset,resolution,scale
            if (hexTexMesh != null) {
                GameObject.DestroyImmediate(hexTexMesh);
            }
            hexTexMesh = new Mesh();
            Vector3[] vertexs = new Vector3[4] {
                new Vector3(0, 0, 0), new Vector3(mapWdith * scale, 0, 0),
                new Vector3(mapWdith * scale, 0, mapHeight * scale), new Vector3(0, 0, mapHeight * scale)
            };
            for (int i = 0; i < vertexs.Length; i++) {
                vertexs[i] += offset;
            }

            int[] indices = new int[6] {
                0, 2, 1, 0, 3, 2
            };
            Vector2[] uvs = new Vector2[4] {
                new Vector2(0, 0), new Vector2(1, 0),new Vector2(1, 1),new Vector2(0, 1)
            };
            hexTexMesh.vertices = vertexs;
            hexTexMesh.triangles = indices;
            hexTexMesh.uv = uvs;
        }

        private void CreateRenderTexture(int mapWdith, int mapHeight) {
            if(texDataRenderTexture != null) {
                texDataRenderTexture.Release();
            }
            texDataRenderTexture = new RenderTexture(mapWdith, mapHeight, 16);
            texDataRenderTexture.enableRandomWrite = true;
            texDataRenderTexture.Create(); 
            RenderTexture.active = texDataRenderTexture;
        }

        private void ApplyHexDataTexture(Material material) {
            if (material == null) {
                Debug.LogError("warn : material is null, you should use HexDataTex or the hexmap will be invisible in scene");
            }

            // 用 set property 系列的字段 设置Texture / renderTexture
            material.SetTexture("_HexmapDataTexture", texDataRenderTexture);

            // set materials and show in scene
            meshFilter.sharedMesh = hexTexMesh;
            meshRenderer.sharedMaterial = material;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(meshRenderer);
#endif
        }

        #endregion


        public RenderTexture GetHexDataTexture() {
            return texDataRenderTexture;
        }

        public void PaintHexDataTexture(Vector3 worldPos, int scope, Color color) {
            Vector2Int pixelPos = TransWorldPosToPixel(worldPos);

            int left = Mathf.Clamp(pixelPos.x - scope, 0, mapWdith - 1);
            int right = Mathf.Clamp(pixelPos.x + scope, 0, mapWdith - 1);
            int down = Mathf.Clamp(pixelPos.y - scope, 0, mapHeight - 1);
            int up = Mathf.Clamp(pixelPos.y + scope, 0, mapHeight - 1);

            int width = right - left;
            int height = up - down;

            Debug.Log($"worldPos{worldPos}, pixelPos: {pixelPos}; w: {width}, h: {height}, l: {left}, r: {right}, d: {down}, u:{up}");

            if (width == 0 || height == 0) {
                Debug.LogError("wrong! width / height is null!");
                return;
            }
            
            Texture2D tmp = new Texture2D(width, height, TextureFormat.RGBA32, false);

            if (scope * scope > paintUseJobLimit) {
                // use job to complete
                NativeArray<Color> colors = new NativeArray<Color>(width * height, Allocator.TempJob);
                PaintHexDataTextureJob paintJob = new PaintHexDataTextureJob() { 
                    fillColor = color,
                    datas = colors,
                };
                JobHandle jobHandle = paintJob.Schedule(width * height, 12);
                jobHandle.Complete();
                tmp.SetPixels(colors.ToArray());
            } else {
                for(int i = left; i <= right; i++) {
                    for(int j = down; j <= up; j++) {
                        tmp.SetPixel(i, j, color);
                    }
                }
            }
            tmp.Apply();

            Vector2Int dstXY = new Vector2Int(left, down);
            Graphics.CopyTexture(tmp, 0, 0, 0, 0, width, height, texDataRenderTexture, 0, 0, dstXY.x, dstXY.y);
            GameObject.DestroyImmediate(tmp);

            Debug.Log($"texture paint over! you paint {width * height} pixel");
        }

        private Vector2Int TransWorldPosToPixel(Vector3 worldPos) {
            // TODO : use offset and balabala to transfer!!!
            int pixelX = (int)(worldPos.x - offset.x);
            int pixelZ = (int)(worldPos.z - offset.z);
            pixelX /= scale;
            pixelZ /= scale;
            return new Vector2Int(pixelX, pixelZ);
        }

        public void ShowHideTexture(bool flag) {
            if (!IsInit) {
                Debug.LogError("hexmapDataTexture not init!");
                return;
            }
            meshRenderer.enabled = flag;
        }

        public void Dispose() {
            // release the data
            if (texDataRenderTexture!= null) {
                texDataRenderTexture.Release();
                texDataRenderTexture = null;
            }
            
            IsInit = false;
        }
    
    }
}
