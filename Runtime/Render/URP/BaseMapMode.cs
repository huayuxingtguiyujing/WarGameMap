using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LZ.WarGameMap.Runtime
{
    // TODO : 其实不是继承 ScriptableRendererFeature
    // 要实现一套 MapRender 系统！！！
    public abstract class BaseMapMode : ScriptableRendererFeature
    {

        // Create：Unity 在以下情况下调用：
        //      渲染器特性首次加载时
        //      启用或禁用渲染器特性时
        //      在 Inspector 面板中更改 Renderer Feature 的属性时
        // AddRenderPasses：
        //      Unity 每帧为每个相机调用该方法，用于向 Scriptable Renderer 注入 ScriptableRenderPass 实例

        public abstract string GetMapModeName();

    }
}
