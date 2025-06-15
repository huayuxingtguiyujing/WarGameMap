Shader "WarGameMap/Terrain/TerrainHeightSmooth"
{
    Properties
    {
        _MinHeight ("Min Height", Float) = 0
        _MaxHeight ("Max Height", Float) = 100

        _Color1 ("Low Color", Color) = (0.1, 0.4, 0.1, 1)   // 水/低地
        _Color2 ("Mid Low Color", Color) = (0.3, 0.7, 0.3, 1)   // 平原
        _Color3 ("Mid Color", Color) = (0.8, 0.8, 0.2, 1)   // 丘陵
        _Color4 ("Mid High Color", Color) = (0.6, 0.4, 0.2, 1)  // 山地
        _Color5 ("High Color", Color) = (1.0, 1.0, 1.0, 1)      // 雪山
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Name "HeightSmooth"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float height : TEXCOORD0;
            };

            float _MinHeight;
            float _MaxHeight;

            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float4 _Color5;

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                o.height = saturate((worldPos.y - _MinHeight) / (_MaxHeight - _MinHeight));
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float h = i.height;

                // 分段权重
                float w1 = 1.0 - smoothstep(0.0, 0.2, h);
                float w2 = smoothstep(0.0, 0.2, h) * (1.0 - smoothstep(0.2, 0.4, h));
                float w3 = smoothstep(0.2, 0.4, h) * (1.0 - smoothstep(0.4, 0.6, h));
                float w4 = smoothstep(0.4, 0.6, h) * (1.0 - smoothstep(0.6, 0.8, h));
                float w5 = smoothstep(0.6, 0.8, h);

                // 加权混合
                float4 color =
                      w1 * _Color1 +
                      w2 * _Color2 +
                      w3 * _Color3 +
                      w4 * _Color4 +
                      w5 * _Color5;

                return color;
            }
            ENDHLSL
        }
    }
}