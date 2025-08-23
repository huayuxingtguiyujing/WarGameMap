using System;
using System.Collections.Generic;
using UnityEngine;

namespace LZ.WarGameMap.Runtime
{
    public sealed class RenderTexturePool : IDisposable {

        private int textureCountLimit;

        private int curTexCount;

        private readonly Dictionary<RenderTextureDescriptor, Stack<RenderTexture>> _pool;

        public RenderTexturePool(int textureCountLimit = 100) {
            curTexCount = 0;
            this.textureCountLimit = textureCountLimit;
            _pool = new Dictionary<RenderTextureDescriptor, Stack<RenderTexture>>(new DescriptorComparer());
        }

        public RenderTexture Get(RenderTextureDescriptor desc) {
            if (_pool.TryGetValue(desc, out var stack) && stack.Count > 0) {
                var rt = stack.Pop();
                // 避免外部错误释放后对象失效
                if (!rt || !rt.IsCreated()) {
                    rt.Release();
                    rt.descriptor = desc;
                    rt.Create();
                }
                return rt;
            }

            if(curTexCount >= textureCountLimit) {
                Debug.LogError("warning! cur texture count reach its limit");
                return null;
            }

            var newRT = new RenderTexture(desc) {
                name = $"RT_{desc.width}x{desc.height}_{desc.colorFormat}"
            };
            newRT.Create();
            curTexCount++;
            return newRT;
        }

        public void Release(RenderTexture rt) {
            if (rt == null) {
                return;
            }

            var desc = rt.descriptor;
            if (!_pool.TryGetValue(desc, out var stack)) {
                stack = new Stack<RenderTexture>();
                _pool[desc] = stack;
            }

            // 可选：清屏，防止上次内容残留
            //Graphics.SetRenderTarget(rt);
            //GL.Clear(true, true, Color.clear);

            stack.Push(rt);
            curTexCount--;

            // 如果在 editor 模式中，那么需要调用一次 repaint 以清屏
        }

        public void Dispose() {
            // clear all texture
            foreach (var stack in _pool.Values) {
                foreach (var rt in stack)
                    rt.Release();
            }
            _pool.Clear();
        }


        // renderTexture --- Descriptor 比较器 ---

        sealed class DescriptorComparer : IEqualityComparer<RenderTextureDescriptor> {
            public bool Equals(RenderTextureDescriptor a, RenderTextureDescriptor b) =>
                a.width == b.width &&
                a.height == b.height &&
                a.depthBufferBits == b.depthBufferBits &&
                a.colorFormat == b.colorFormat &&
                a.msaaSamples == b.msaaSamples &&
                a.sRGB == b.sRGB &&
                a.dimension == b.dimension &&
                a.enableRandomWrite == b.enableRandomWrite;

            public int GetHashCode(RenderTextureDescriptor d) {
                unchecked {
                    int hash = 17;
                    hash = hash * 23 + d.width;
                    hash = hash * 23 + d.height;
                    hash = hash * 23 + d.depthBufferBits;
                    hash = hash * 23 + (int)d.colorFormat;
                    hash = hash * 23 + d.msaaSamples;
                    hash = hash * 23 + d.sRGB.GetHashCode();
                    hash = hash * 23 + (int)d.dimension;
                    hash = hash * 23 + d.enableRandomWrite.GetHashCode();
                    return hash;
                }
            }
        }
         
    }
}
