Shader "WarGameMap/Terrain/TerrainLandform"
{
    Properties
    {
        
        [NoScaleOffset] _TerrainAlbedoArray("Albedo Array", 2DArray) = "" {}
        [NoScaleOffset] _TerrainNormalArray("Normal Array", 2DArray) = "" {}

        _HexmapWidth("Hexmap Width", Int) = 256
        _HexmapHeight("Hexmap Height", Int) = 256
        _HexGridSize("Hex Grid Size", Float) = 20
        
        _TerrainTiling("Terrain Tiling", Float) = 4
        _TerrainNoiseScale("Terrain Noise Scale", Float) = 1
        _TerrainTintStrength("Terrain Tint Strength", Range(0, 1)) = 0.2
        _DebugView("Debug View", Float) = 0

        [Header(Hex Grid Edge)]
        _HexGridEdgeRatio("Hex Grid Edge Ratio", Range(0.0, 0.5)) = 0.075
        _HexGridEdgeStartLerp("Hex Grid Edge Start Lerp", Range(0.5, 0.95)) = 0.75
        _HexGridEdgeColor("Hex Grid Edge Color", Color) = (0.3, 0.3, 0.3, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        LOD 100
        ZWrite On
        Blend Off

        Pass
        {
            Name "TerrainLandform"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma require 2darray          // <-- 新增：Texture2DArray 支持
            #pragma require structuredbuffer // <-- 新增：StructuredBuffer 支持

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Utils/TerrainBlend.hlsl"
            #include "Utils/HexOutline.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };

            // float _HexGridSize;
            float _TerrainTiling;
            float _TerrainNoiseScale;
            float _TerrainTintStrength;
            float _DebugView;

            // 将 ID 转成稳定的伪随机颜色，方便调试。
            float3 GetDebugIdColor(uint terrainID)
            {
                float idValue = (float)terrainID;
                float3 colorSeed = float3(
                    frac(idValue * 0.3183099 + 0.11),
                    frac(idValue * 0.3678794 + 0.37),
                    frac(idValue * 0.7071067 + 0.73));
                return lerp(0.2.xxx, colorSeed, 0.9);
            }

            // 用方向编号给出固定颜色，便于核对 areaIdx 和 neighbor 映射。
            float3 GetDebugAreaColor(int idx)
            {
                if (idx == 0) return float3(0.95, 0.20, 0.20); // W
                if (idx == 1) return float3(0.95, 0.55, 0.20); // NW
                if (idx == 2) return float3(0.95, 0.90, 0.20); // NE
                if (idx == 3) return float3(0.20, 0.85, 0.25); // E
                if (idx == 4) return float3(0.20, 0.65, 0.95); // SE
                return float3(0.65, 0.25, 0.95);               // SW
            }

            // 邻居方向使用另一套索引顺序，这里单独给颜色，方便核对映射是否正确。
            float3 GetDebugNeighborColor(int idx)
            {
                if (idx == 0) return float3(0.20, 0.85, 0.25); // E
                if (idx == 1) return float3(0.95, 0.90, 0.20); // NE
                if (idx == 2) return float3(0.95, 0.55, 0.20); // NW
                if (idx == 3) return float3(0.95, 0.20, 0.20); // W
                if (idx == 4) return float3(0.65, 0.25, 0.95); // SW
                return float3(0.20, 0.65, 0.95);               // SE
            }

            // 构造世界法线与切线空间法线之间的转换基。
            float3 TransformNormalTSToWS(float3 normalTS, float3 normalWS, float4 tangentWS)
            {
                float3 bitangentWS = cross(normalWS, tangentWS.xyz) * tangentWS.w;
                float3 worldNormal = normalTS.x * tangentWS.xyz + normalTS.y * bitangentWS + normalTS.z * normalWS;
                return normalize(worldNormal);
            }

            Varyings vert(Attributes v)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);

                output.positionHCS = TransformWorldToHClip(worldPos);
                output.worldPos = worldPos;
                output.normalWS = normalize(normalWS);
                output.tangentWS = float4(normalize(tangentWS), v.tangentOS.w * unity_WorldTransformParams.w);
                output.uv = v.uv;

                return output;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // 当前片元对应的 offset hex 坐标。
                float2 offsetHex = WorldToOffset(i.worldPos, _HexGridSize).xy;

                // 取当前格子的 TerrainID。
                uint selfID = LoadTerrainID((int2)offsetHex);
                MaterialParams selfMat = GetTerrainMaterialParams(selfID);

                // ===== M03: 三角形重心插值 =====
                float2 nbrOffset0, nbrOffset1;
                uint nbrID0, nbrID1;
                float blendW0, blendW1;
                float4 Debug_Result;
                GetTerrainTriangleInfo(i.worldPos, offsetHex, _HexGridSize, nbrOffset0, nbrID0, blendW0, nbrOffset1, nbrID1, blendW1, Debug_Result);
                float blendWSelf = 1.0 - blendW0 - blendW1;

                // 材质采样使用较稳定的平铺 UV。
                float2 terrainUV = i.uv * _TerrainTiling;

                float4 selfAlbedo = SampleTerrainAlbedo(terrainUV, selfID);
                float3 selfNormal = SampleTerrainNormal(terrainUV, selfID);

                float3 finalAlbedo;
                float3 finalNormalTS;
                float roughness;
                float metallic;

                // if (selfID == 0) return half4(0.0, 0.0, 0.8, 1.0); // 浅海 深蓝
                // if (selfID == 1) return half4(0.0, 0.3, 0.6, 1.0); // 深海 中蓝
                // if (selfID == 2) return half4(0.2, 0.8, 0.2, 1.0); // 平原 绿
                // if (selfID == 3) return half4(0.6, 0.6, 0.2, 1.0); // 丘陵 黄绿
                // if (selfID == 4) return half4(0.5, 0.3, 0.2, 1.0); // 山脉 棕
                // if (selfID == 5) return half4(0.8, 0.7, 0.4, 1.0); // 高原 沙黄
                // if (selfID == 6) return half4(0.9, 0.9, 0.9, 1.0); // 雪地 白
                // if (selfID == 7) return half4(0.6, 0.2, 0.6, 1.0); // 紫
                // if (selfID == 8) return half4(0.2, 0.6, 0.8, 1.0); // 青
                // if (selfID == 9)  return half4(0.9, 0.4, 0.2, 1.0); // 橙
                // if (selfID == 10) return half4(0.5, 0.1, 0.5, 1.0); // 深紫
                // if (selfID == 11) return half4(0.1, 0.7, 0.7, 1.0); // 青绿
                // if (selfID == 12) return half4(0.9, 0.2, 0.3, 1.0); // 红
                // if (selfID == 13) return half4(0.3, 0.4, 0.1, 1.0); // 橄榄绿
                // if (selfID == 14) return half4(0.7, 0.1, 0.3, 1.0); // 深红
                // if (selfID == 15) return half4(0.1, 0.5, 0.3, 1.0); // 墨绿
                // if (selfID == 16) return half4(0.8, 0.5, 0.1, 1.0); // 金
                // if (selfID == 17) return half4(0.3, 0.3, 0.7, 1.0); // 蓝紫
                // if (selfID == 18) return half4(0.7, 0.7, 0.1, 1.0); // 黄
                // if (selfID == 19) return half4(0.4, 0.2, 0.1, 1.0); // 深棕


                // ===== 优化：三个 hex ID 相同时跳过邻居采样 =====
                if (nbrID0 == selfID && nbrID1 == selfID)
                {
                    finalAlbedo   = selfAlbedo.rgb;
                    finalNormalTS = selfNormal;
                    roughness     = selfMat.roughness;
                    metallic      = selfMat.metallic;
                }
                else
                {
                    float4 albedo0 = SampleTerrainAlbedo(terrainUV, nbrID0);
                    float3 normal0 = SampleTerrainNormal(terrainUV, nbrID0);
                    MaterialParams mat0 = GetTerrainMaterialParams(nbrID0);

                    float4 albedo1 = SampleTerrainAlbedo(terrainUV, nbrID1);
                    float3 normal1 = SampleTerrainNormal(terrainUV, nbrID1);
                    MaterialParams mat1 = GetTerrainMaterialParams(nbrID1);

                    // blend 计算目前是有问题的
                    // 加权求和（三方同时加权，非顺序 lerp）
                    finalAlbedo = selfAlbedo.rgb * blendWSelf
                                + albedo0.rgb * blendW0
                                + albedo1.rgb * blendW1;
                    // finalAlbedo = selfAlbedo.rgb * blendWSelf;
                    // finalAlbedo = selfAlbedo.rgb;

                    finalNormalTS = normalize(
                        selfNormal * blendWSelf
                      + normal0   * blendW0
                      + normal1   * blendW1);

                    roughness = selfMat.roughness * blendWSelf
                              + mat0.roughness   * blendW0
                              + mat1.roughness   * blendW1;

                    metallic = selfMat.metallic * blendWSelf
                             + mat0.metallic   * blendW0
                             + mat1.metallic   * blendW1;
                }
                finalNormalTS = float3(0, 1, 0);

                // ===== 六边形格子边框叠加（三角形混合之后）=====
                
                // finalAlbedo = ApplyHexOutline(i.worldPos, finalAlbedo, selfID);
                if (!ShouldExcludeHexOutline(selfID)) {
                    finalAlbedo = ApplyHexOutline(i.worldPos, finalAlbedo);
                }

                // 用一个很轻的 ID 色调把材质拉开一点，便于快速辨认不同地貌。
                finalAlbedo *= lerp(1.0.xxx, GetDebugIdColor(selfID), saturate(_TerrainTintStrength * 0.12));

                // 叠加世界空间噪声，让地貌表面不要太平。
                float noiseValue = GetTerrainDetailNoiseMultiplier(i.worldPos.xz * _TerrainNoiseScale, selfMat);
                finalAlbedo *= lerp(1.0, noiseValue, saturate(selfMat.detailStrength));

                // 先把切线空间法线转到世界空间，再做最小光照。
                float3 normalWS = TransformNormalTSToWS(finalNormalTS, normalize(i.normalWS), normalize(i.tangentWS));

                // Debug：后续迭代适配三角形可视化。
                if (_DebugView > 0.5)
                {
                    // TODO: 适配三角形可视化（triIdx / blendW0 / blendW1）
                    if (_DebugView < 1.5) return half4(GetDebugIdColor(selfID), 1.0);
                    if (_DebugView < 2.5) return half4(blendW0.xxx, 1.0);
                    if (_DebugView < 3.5) return half4(blendW1.xxx, 1.0);
                    if (_DebugView < 4.5) return half4(blendWSelf, blendW0, blendW1, 1.0); 
                    if (_DebugView < 5.5) return float4(blendWSelf + blendW0 + blendW1, 0, 0, 1);
                    if (_DebugView < 6.5) return float4(1, 0, blendWSelf, 1);
                    if (_DebugView < 7.5) return Debug_Result;
                    return half4(finalAlbedo, 1.0);
                } 

                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 viewDirWS = normalize(GetCameraPositionWS() - i.worldPos);

                half ndotl = saturate(dot(normalWS, lightDir));
                half3 ambient = SampleSH(normalWS);

                // 最小可见的高光，roughness 越高越钝，metallic 越高越亮。
                half3 halfDir = normalize(lightDir + viewDirWS);
                half specPower = lerp(16.0h, 96.0h, saturate(1.0h - roughness));
                half specMask = pow(saturate(dot(normalWS, halfDir)), specPower);
                half specStrength = lerp(0.02h, 0.18h, saturate(metallic));

                half3 litColor = finalAlbedo * (ambient + mainLight.color * ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                litColor += mainLight.color * specMask * specStrength;

                return half4(litColor, 1.0);
            }

            ENDHLSL
        }
    }
}
