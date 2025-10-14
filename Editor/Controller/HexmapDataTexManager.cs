using LZ.WarGameMap.Runtime;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace LZ.WarGameMap.MapEditor
{

    public struct PaintHexDataTextureJob : IJobParallelFor {

        [ReadOnly] public Color fillColor;
        [WriteOnly] public NativeArray<Color> datas;

        public void Execute(int index) {
            datas[index] = fillColor;
        }
    }

    // only valid in UNITY_EDITOR
    // this class is to help manager the texture that storage the hex map data
    public class HexmapDataTexManager : IDisposable
    {
        int mapWdith;
        int mapHeight;
        int scale; 
        Vector3 offset;

        int paintUseJobLimit = 100; // use job system if paint pixel num is more than this

        RenderTexture texDataRenderTexture;

        // RT data cache
        class RTDataCache : IDisposable
        {
            int mapWidth;
            int mapHeight;
            int curTexCacheIndex = 0;
            int texDataCacheNum = 4;

            int curPageIndex = -1;

            List<Color> texDataCache = new List<Color>();

            Texture2D cacheDataTex;     // use it to quickly switch texture

            public bool IsValid { get; private set; }

            public RTDataCache() { IsValid = false; }

            public RTDataCache(int texDataCacheNum, int mapWidth, int mapHeight)
            {
                this.mapWidth = mapWidth;
                this.mapHeight = mapHeight;
                this.texDataCacheNum = texDataCacheNum;
                int capa = texDataCacheNum * mapWidth * mapHeight;
                texDataCache = new List<Color>(capa);
                texDataCache.FillInList(capa);
                IsValid = true;
            }

            public void InitTexCache(int index, List<Color> colors)
            {
                if (!IsValid)
                {
                    Debug.Log($"Not a valid RTDataCache!");
                    return;
                }
                if (texDataCacheNum <= index || index < 0)
                {
                    Debug.Log($"invalid index : {index}, the tex data num is : {texDataCacheNum}");
                    return;
                }
                int offset = index * mapHeight * mapWidth;

                //Color[] buffer = new Color[texDataCache.Count];
                //colors.CopyTo(0, buffer, offset, colors.Count);
                for (int i = 0; i < colors.Count; i++)
                {
                    texDataCache[offset + i] = colors[i];
                }
            }

            public void PaintTexCache(List<Vector2Int> poss, Color color)
            {
                int length = mapHeight * mapWidth;
                int offset = curPageIndex * length;
                foreach (var pos in poss)
                {
                    if(CheckIndexValid(pos))
                    {
                        int index = offset + pos.y * mapWidth + pos.x;
                        texDataCache[index] = color;
                    }
                }
            }

            private bool CheckIndexValid(Vector2Int pos)
            {
                int index = pos.y * mapWidth + pos.x;
                return index >= 0 && index < mapHeight * mapWidth;
            }

            public Texture2D SwitchToTexCache(int index)
            {
                if (!IsValid)
                {
                    Debug.Log($"Not a valid RTDataCache!");
                    return null;
                }
                if (texDataCacheNum <= index || index < 0)
                {
                    Debug.Log($"invalid index : {index}, the tex data num is : {texDataCacheNum}");
                    return null;
                }

                this.curPageIndex = index;
                if (cacheDataTex == null)
                {
                    cacheDataTex = new Texture2D(mapWidth, mapHeight);
                }
                int offset = index * mapHeight * mapWidth;
                cacheDataTex.SetPixels(texDataCache.GetRange(offset, mapHeight * mapWidth).ToArray());
                cacheDataTex.Apply();

                Debug.Log($"Now, switch to tex cache index : {index}, length : {mapHeight * mapWidth}");

                return cacheDataTex;
            }

            public void Dispose()
            {
                if(cacheDataTex != null)
                {
                    GameObject.DestroyImmediate(cacheDataTex);
                }
                texDataCache.Clear();
            }
        }

        RTDataCache rtDataCache = new RTDataCache();

        bool useTexDataCache = false;
        

        ComputeShader paintRTShader;

        MeshRenderer meshRenderer;

        MeshFilter meshFilter;

        Mesh hexTexMesh;

        GameObject parentObj;

        public bool IsInit {  get; private set; }

        bool notShowInScene;


        #region Init hexDataTexManager

        public HexmapDataTexManager() {
            IsInit = false;
        }

        public void InitHexmapDataTexture(int mapWdith, int mapHeight, int scale, Vector3 offset, 
            GameObject parentObj, Material material, ComputeShader paintRTShader, 
            bool notShowInScene = false, bool useTexCache = false, int texCacheNum = 1, FilterMode filterMode = FilterMode.Bilinear) {
            this.mapWdith = mapWdith;
            this.mapHeight = mapHeight;
            this.scale = scale;
            this.offset = offset;
            this.notShowInScene = notShowInScene;
            this.useTexDataCache = useTexCache;
            this.paintRTShader = paintRTShader;

            CreateHexDataMeshObj(scale, offset, parentObj);
            CreateRenderTexture(mapWdith, mapHeight, filterMode);
            ApplyHexDataTexture(material);
            IsInit = true;

            // Set notShowInScene true when you need this manager only manage texture and do not show in scene
            if (notShowInScene)
            {
                ShowHideTexture(false);
            }
            if (useTexDataCache)
            {
                CreateTexDataCache(texCacheNum);
            }

            //Debug.Log("Init HexmapDataTextureManager over ");
        }

        // Use it to set a Texture
        public void InitHexmapDataTexture(Texture2D newTexture, int scale, Vector3 offset, 
            GameObject parentObj, Material material, ComputeShader paintRTShader, 
            bool notShowInScene = false, bool useTexCache = false, int texCacheNum = 1, FilterMode filterMode = FilterMode.Bilinear) {
            this.mapWdith = newTexture.width;
            this.mapHeight = newTexture.height;
            this.scale = scale;
            this.offset = offset;
            this.notShowInScene = notShowInScene;
            this.useTexDataCache = useTexCache;
            this.paintRTShader = paintRTShader;

            CreateHexDataMeshObj(scale, offset, parentObj);
            CreateRenderTexture(mapWdith, mapHeight, filterMode);
            Graphics.Blit(newTexture, texDataRenderTexture);
            ApplyHexDataTexture(material);
            IsInit = true;

            if (notShowInScene)
            {
                ShowHideTexture(false);
            }
            if (useTexDataCache)
            {
                CreateTexDataCache(texCacheNum);
            }

            Debug.Log("init HexmapDataTextureManager over ");
        }

        private void CreateHexDataMeshObj(int scale, Vector3 offset, GameObject texObj) {
            this.parentObj = texObj;

            meshRenderer = texObj.GetComponent<MeshRenderer>();
            if (meshRenderer == null) {
                meshRenderer = texObj.AddComponent<MeshRenderer>();
            }

            meshFilter = texObj.GetComponent<MeshFilter>();
            if (meshFilter == null) {
                meshFilter = texObj.AddComponent<MeshFilter>();
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

        private void CreateRenderTexture(int mapWdith, int mapHeight, FilterMode filterMode) {
            if(texDataRenderTexture != null) {
                texDataRenderTexture.Release();
            }
            texDataRenderTexture = new RenderTexture(mapWdith, mapHeight, 16);
            texDataRenderTexture.enableRandomWrite = true;
            texDataRenderTexture.filterMode = filterMode;
            texDataRenderTexture.Create(); 
            RenderTexture.active = texDataRenderTexture;
        }

        private void ApplyHexDataTexture(Material material) {
            if (material == null) {
                Debug.Log("HexmapDataTexManager warn : material is null, you should use HexDataTex or the hexmap will be invisible in scene");
                return;
            }

            // 用 set property 系列的字段 设置Texture / renderTexture
            material.SetTexture("_HexmapDataTexture", texDataRenderTexture);
            // Set brush Texture : _HexGridTexture
            material.SetTexture("_HexGridTexture", texDataRenderTexture);

            // Set materials and show in scene
            meshFilter.sharedMesh = hexTexMesh;
            meshRenderer.sharedMaterial = material;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(meshRenderer);
#endif
        }

        private void CreateTexDataCache(int texDataCacheNum)
        {
            rtDataCache = new RTDataCache(texDataCacheNum, mapWdith, mapHeight);
        }

        // Call it when you refresh scene (AssetDatabase.Refresh())
        public void UpdateHexManager()
        {
            if(meshRenderer is null)
            {
                return;
            }
            ApplyHexDataTexture(meshRenderer.sharedMaterial);
        }

        #endregion


        #region Cache tex data

        // Tex data cache, support scene need many RT data
        public void InitTexCache(int index, List<Color> colors)
        {
            rtDataCache.InitTexCache(index, colors);
            Debug.Log($"now you init index : {index} texture cache data");
        }

        public void SwitchToTexCache(int index)
        {
            Texture2D cacheDataTex = rtDataCache.SwitchToTexCache(index);
            Graphics.Blit(cacheDataTex, texDataRenderTexture);
            RenderTexture.active = texDataRenderTexture;
        }

        #endregion


        public RenderTexture GetHexDataTexture() {
            return texDataRenderTexture;
        }

        public List<Color> GetHexDataTexColors()
        {
            return TextureUtility.GetRTColorList(texDataRenderTexture);
        }

        public void SetRTPixel(List<Color> colors)
        {
            Texture2D temp = new Texture2D(mapWdith, mapHeight, TextureFormat.RGBA32, false);
            temp.SetPixels(colors.ToArray());
            temp.Apply();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = texDataRenderTexture;
            Graphics.Blit(temp, texDataRenderTexture);

            RenderTexture.active = prev;
            GameObject.DestroyImmediate(temp);
        }

        public void ShowHideTexture(bool flag)
        {
            if (!IsInit)
            {
                Debug.LogError("hexmapDataTexture not init!");
                return;
            }
            if (meshRenderer == null)
            {
                Debug.LogError("meshRenderer is null, but manager is inited!");
                return;
            }
            meshRenderer.enabled = flag;
        }

        public void PaintHexDataTexture_RectScope(Vector3 worldPos, int scope, Color color) {
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

        private Vector2Int TransWorldPosToPixel(Vector3 worldPos)
        {
            // TODO : use offset and balabala to transfer!!!
            int pixelX = (int)(worldPos.x - offset.x);
            int pixelZ = (int)(worldPos.z - offset.z);
            pixelX /= scale;
            pixelZ /= scale;
            return new Vector2Int(pixelX, pixelZ);
        }

        // For hexmap paint
        RenderTexturePainter renderTexturePainter;

        public void PaintHexDataTexture_Scope(List<Vector2Int> poss, Color color)
        {
            if(renderTexturePainter == null)
            {
                renderTexturePainter = new RenderTexturePainter(this.paintRTShader, texDataRenderTexture);
            }
            renderTexturePainter.PaintPixels(poss, color);

            // 为什么山脉editor当中，这里不会被触发?
            if (useTexDataCache)
            {
                rtDataCache.PaintTexCache(poss, color);
            }
        }

        public void Dispose() {
            if (texDataRenderTexture!= null) {
                texDataRenderTexture.Release();
                texDataRenderTexture = null;
            }
            rtDataCache.Dispose();

            IsInit = false;
        }
    
    }
}
