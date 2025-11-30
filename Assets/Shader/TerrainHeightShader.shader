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

        // light TODO: 之后会修改光照相关
        _LightDir ("Light Dir", Vector) = (0, -0.5, -0.5, 1)

        _LightIntensity ("Main Light Intensity", Float) = 1.0
        _AmbientIntensity ("Ambient Intensity", Float) = 0.3

        // Hex setting
        _HexmapWidth("Hexmap Width", Int) = 256
        _HexmapHeight("Hexmap Height", Int) = 256
        _HexGridSize("Hex Grid Size", Range(1, 300)) = 20
        // 描边-边界相关
        _EdgeRatio("Edge Ratio", Float) = 0.8
        
        // 省份边界相关
        _CountryGridRelationTexture("Country Grid Relation Texture", 2D) = "white" {}   // TODO : 过期了这个

        _RegionTexture("Region Texture", 2D) = "white" {}
        _ProvinceTexture("Province Texture", 2D) = "white" {}
        _PrefectureTexture("Prefecture Texture", 2D) = "white" {}
        _SubPrefectureTexture("SubPrefecture Texture", 2D) = "white" {}

        _BorderLerpColor1("Border Lerp1 Color", Color) = (0.05, 0.05, 0.05, 1)
        _BorderLerpColor2("Border Lerp2 Color", Color) = (0.72, 0.65, 0.25, 1)
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

            // HLSLINCLUDE
            #include "Hexmap/CountryLibrary.hlsl"
            #include "Hexmap/TerrainLibrary.hlsl"
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

                // TODO : 传入区域边界插值结果到 uv 中......
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                // float2 uv : TEXCOORD0;
                float height : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float edgeFactor : TEXCOORD3;   // 表示片元到边界的距离
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


            // 用于处理河滩
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

                output.edgeFactor = GetRatioToHexEdge(worldPos, _HexGridSize);

                worldPos = mul(unity_ObjectToWorld, v.positionOS).xyz;
                output.worldPos = ApplyRiverEffect(v.uv, worldPos);
                return output;
            }

            float3 ApplySimpleLight(float3 baseColor, float3 normalWS)
            {
                // 光照：主方向光 + 环境光
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

                // 分段权重 TODO : 后续要改
                float w0 = 1.0 - smoothstep(0, 0.1, h);
                float w1 = smoothstep(-1, 0, h)    * (1.0 - smoothstep(0.1, 0.3, h));
                float w2 = smoothstep(0.0, 0.3, h) * (1.0 - smoothstep(0.3, 0.4, h));
                float w3 = smoothstep(0.3, 0.4, h) * (1.0 - smoothstep(0.4, 0.6, h));
                float w4 = smoothstep(0.4, 0.6, h) * (1.0 - smoothstep(0.6, 0.8, h));
                float w5 = smoothstep(0.6, 0.8, h);

                // 地形颜色，加权混合
                float4 baseColor = w0 * _Color0 + w1 * _Color1 + w2 * _Color2 +
                      w3 * _Color3 + w4 * _Color4 + w5 * _Color5;
                float3 litColor = ApplySimpleLight(baseColor.rgb, i.normalWS);

                // HexOutline.hlsl // 边缘描边效果 // _BorderLerpColor1 _BorderLerpColor2
                // float edgeMask = smoothstep(0.9, 0.95, i.edgeFactor);
                // float edgeLerp = saturate(edgeMask - _EdgeRatio);
                // float3 borderLerpColor = lerp(_BorderLerpColor1.rgb, _BorderLerpColor2.rgb, edgeMask);
                
                // flag : 0 ， 代表仅显示 region 层的
                // float3 blendColor = GetHexEdgeBorderColor(i.worldPos, litColor, 0,  _HexGridSize).rgb;

                // // 使用 URP SurfaceData，支持 Emission 发光 ========
                // SurfaceData surfaceData;
                // ZERO_INITIALIZE(SurfaceData, surfaceData);
                // // 传入混合后的基础颜色（用于光照）
                // surfaceData.albedo = blendColor;
                // // 设置发光，只对边缘生效（可在 inspector 控制 _EmissionStrength）
                // surfaceData.emission = borderLerpColor * edgeLerp * 2;
                // // 此方法会自动套用光照、阴影、发光等效果
                // float4 output = UniversalFragmentPBR(input, surfaceData);

                float4 countryColor = GetCountryColor(i.worldPos, litColor, 0, _HexGridSize);

                float4 finalColor = float4(countryColor.xyz, 1.0);    //   blendColor countryColor.xyz litColor borderLerpColor
                return finalColor;

                // build hex outline!
                // float3 outlineColor = GetHexOutlineColor(i.worldPos, finalColor.xyz);
                // return float4(outlineColor, 1); //  finalColor.a;
            }

            ENDHLSL
        }
    }
}