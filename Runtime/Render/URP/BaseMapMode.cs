using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace LZ.WarGameMap.Runtime
{
    // TODO : ��ʵ���Ǽ̳� ScriptableRendererFeature
    // Ҫʵ��һ�� MapRender ϵͳ������
    public abstract class BaseMapMode : ScriptableRendererFeature
    {

        // Create��Unity ����������µ��ã�
        //      ��Ⱦ�������״μ���ʱ
        //      ���û������Ⱦ������ʱ
        //      �� Inspector ����и��� Renderer Feature ������ʱ
        // AddRenderPasses��
        //      Unity ÿ֡Ϊÿ��������ø÷����������� Scriptable Renderer ע�� ScriptableRenderPass ʵ��

        public abstract string GetMapModeName();

    }
}
