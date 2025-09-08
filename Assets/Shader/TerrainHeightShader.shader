Shader "WarGameMap/Terrain/TerrainHeightSmooth"
{
    Properties
    {
        // river set
        _RiverTex("RiverTexture", 2D) = "white" {}
        _RiverColor("River Color", Color) = (0, 0, 1, 1)
        _NoRiverColor("No River Color", Color) = (1, 1, 1, 1)
        _RiverDownOffset("River Down Offset", Float) = 1

        // landform
        _MinHeight ("Min Height", Float) = 0
        _MaxHeight ("Max Height", Float) = 100

        _Color0 ("Water Color", Color) = (1, 0.85, 0.72, 1)   // 水/浅滩    // 255 218 185
        _Color1 ("Low Color", Color) = (0.1, 0.4, 0.1, 1)   // 平原
        _Color2 ("Mid Low Color", Color) = (0.3, 0.7, 0.3, 1)   // 平原
        _Color3 ("Mid Color", Color) = (0.8, 0.8, 0.2, 1)   // 丘陵
        _Color4 ("Mid High Color", Color) = (0.6, 0.4, 0.2, 1)  // 山地
        _Color5 ("High Color", Color) = (1.0, 1.0, 1.0, 1)      // 雪山

        // light
        _LightDir ("Light Dir", Vector) = (0, -0.5, -0.5, 1)

        _LightIntensity ("Main Light Intensity", Float) = 1.0
        _AmbientIntensity ("Ambient Intensity", Float) = 0.3

        // hex setting
        _HexGridScale("Hex Grid Scale", Float) = 2
        _HexGridSize("Hex Grid Size", Range(1, 300)) = 20
        _HexGridEdgeRatio("Hex Grid Edge Ratio", Range(0.001, 1)) = 0.1
        _HexGridTypeTexture("Hex Grid Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100
        ZWrite On
        Blend Off
        
        Pass
        {
            Name "HeightSmooth"
            Tags { "LightMode" = "UniversalForward" }
            Stencil {
                Ref 2
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // HLSLINCLUDE
            #include "Utils/HexLibrary.hlsl"
            #include "Utils/HexOutline.hlsl"
            #include "Utils/MathLibrary.hlsl"
            // ENDHLSL

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings{
                float4 positionHCS : SV_POSITION;
                // float2 uv : TEXCOORD0;
                float height : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _RiverTex;
            float4 _RiverTex_ST;
            float4 _RiverTex_TexelSize; // x=1/width, y=1/height, z=width, w=height
            float4 _RiverColor;
            float4 _NoRiverColor;
            float _RiverDownOffset;

            float _MinHeight;
            float _MaxHeight;

            float4 _Color0;
            float4 _Color1;
            float4 _Color2;
            float4 _Color3;
            float4 _Color4;
            float4 _Color5;

            float3 _LightDir;

            float _LightIntensity;
            float _AmbientIntensity;

            // 为什么 没有用？
            float3 ApplyRiverEffect(float2 uv, float3 worldPos)
            {
                // NOTE : when setting terrain's uv, will consider it's pos in whole terrain not single cluster
                float4 riverTexColor = tex2Dlod(_RiverTex, float4(uv, 0, 0));
                // Shader target ≥ 3.0
                // float4 col = tex2D(_HexmapDataTexture, i.uv);
                float ratio = LerpColor(_NoRiverColor.xyz, _RiverColor.xyz, riverTexColor.xyz);
                return float3(worldPos.x, worldPos.y - _RiverDownOffset, worldPos.z);
            }


            Varyings vert(Attributes v)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                output.height = saturate((worldPos.y - _MinHeight) / (_MaxHeight - _MinHeight));
                output.normalWS = TransformObjectToWorldNormal(v.normalOS);
                output.positionHCS = TransformWorldToHClip(TransformObjectToWorld(v.positionOS.xyz));

                output.worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
                output.worldPos = ApplyRiverEffect(v.uv, worldPos);
                return output;
            }

            float3 ApplySimpleLight(float3 baseColor, float3 normalWS)
            {
                // float3 mainLight = float3(0, -0.5, -0.5); //GetMainLight();
                float3 lightDir = normalize(_LightDir.xyz);
                float NdotL = saturate(dot(normalize(normalWS), -lightDir));

                float3 diffuse = baseColor * NdotL * _LightIntensity;
                float3 ambient = baseColor * _AmbientIntensity;

                return diffuse + ambient;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float h = i.height;

                // 分段权重
                float w0 = 1.0 - smoothstep(0, 0.1, h);
                float w1 = smoothstep(-1, 0, h)    * (1.0 - smoothstep(0.1, 0.3, h));
                float w2 = smoothstep(0.0, 0.3, h) * (1.0 - smoothstep(0.3, 0.4, h));
                float w3 = smoothstep(0.3, 0.4, h) * (1.0 - smoothstep(0.4, 0.6, h));
                float w4 = smoothstep(0.4, 0.6, h) * (1.0 - smoothstep(0.6, 0.8, h));
                float w5 = smoothstep(0.6, 0.8, h);

                // 加权混合
                float4 baseColor = w0 * _Color0 + w1 * _Color1 + w2 * _Color2 +
                      w3 * _Color3 + w4 * _Color4 + w5 * _Color5;

                // 光照：主方向光 + 环境光
                float4 finalColor = float4(ApplySimpleLight(baseColor.rgb, i.normalWS), 1.0);
                return finalColor;

                // build hex outline!
                // float3 outlineColor = GetHexOutlineColor(i.worldPos, finalColor.xyz);
                // return float4(outlineColor, 1); //  finalColor.a;
            }


            ENDHLSL
        }

    }
}