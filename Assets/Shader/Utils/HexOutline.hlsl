#ifndef HexOutline
#define HexOutline

#include "HexLibrary.hlsl"

float _HexGridSize = 20;
float _HexGridEdgeRatio = 0.075;        // 六边形边框显示透明度，可以控制六边形边框显示程度，建议取值为 0.01 ~ 0.15
float _HexGridEdgeStartLerp = 0.75;    // 六边形边框比例起点，可以控制六边形边框大小，建议取值 0.7~0.95
float4 _HexGridEdgeColor = float4(0.3, 0.3, 0.3, 1);

// NOTE: _HexGridSize       → 由 TerrainLandformShader 声明
// NOTE: _HexGridEdgeRatio  → 由 Shader Properties 自动声明
// NOTE: _HexGridEdgeStartLerp → 由 Shader Properties 自动声明
// NOTE: _HexGridEdgeColor  → 由 Shader Properties 自动声明

sampler _HexGridTypeTexture;
float4 _HexGridTypeTexture_ST;
float4 _HexGridTypeTexture_TexelSize;


float3 GetHexOutlineColor(float3 worldPos, float3 _BackgroundColor) : SV_Target{
    
    float3 hex = WorldToCube(worldPos, _HexGridSize);
    hex = CubeCoordToOffset(hex);

    int x_idx = hex.x;
    int y_idx = hex.y;

    float2 hex_uv = float2(x_idx , y_idx) * _HexGridTypeTexture_TexelSize.xy;

    float3 gridColor = tex2D(_HexGridTypeTexture, hex_uv);
    // gridColor = float3(0.5, 0.5, 0.5);
    // float3 bgcolor = _BackgroundColor;


    // round version
    // float t = GetDistToHexCenter(worldPos, _HexGridSize) / _HexGridSize;  //
    // better version
    float t = GetRatioToHexEdge(worldPos, _HexGridSize);
    // return float3(t, t, t);

    t = clamp(0, 1 - _HexGridEdgeRatio, t);
    float mask = smoothstep(_HexGridEdgeStartLerp, 1.0, t);   // 0.7 start lerp
    return lerp(_BackgroundColor, gridColor, mask);
}

// 六边形边框叠加函数
// 在 triangle-blend 混合后的 finalAlbedo 上叠加六边形格子边框
// 浅海(terrainID=0)、深海(terrainID=1)、山脉(terrainID=4) 不显示边框
float3 ApplyHexOutline(float3 worldPos, float3 originalAlbedo)
{
    float t = GetRatioToHexEdge(worldPos, _HexGridSize);
    t = clamp(0, 1 - _HexGridEdgeRatio, t);
    float mask = smoothstep(_HexGridEdgeStartLerp, 1.0, t);
    return lerp(originalAlbedo, _HexGridEdgeColor.rgb, mask);
}


// ===== 六边形格子高亮 =====
float  _HighlightHexCoordX;
float  _HighlightHexCoordY;
float4 _HighlightColor;
float  _HighlightEnabled;
float  _HighlightEdgeRatio;
float  _HighlightEdgeStartLerp;

// 高亮描边函数：加法混合叠加到 originalAlbedo
// 灰色边框（ApplyHexOutline）在之后调用，会自然覆盖在高亮之上
float3 ApplyHexHighlight(float3 worldPos, float3 originalAlbedo)
{
    // 当前片元所属的 offset hex 坐标
    float2 offsetHex = WorldToOffset(worldPos, _HexGridSize).xy;
    int2 targetHex = int2(round(_HighlightHexCoordX), round(_HighlightHexCoordY));

    // 不在目标格子内，直接返回
    if (any((int2)offsetHex != targetHex))
    {
        return originalAlbedo;
    }

    // 计算边缘 mask（使用独立参数，不复用 HexGridEdge 的参数）
    float t = GetRatioToHexEdge(worldPos, _HexGridSize);
    t = clamp(0, 1 - _HighlightEdgeRatio, t);
    float mask = smoothstep(_HighlightEdgeStartLerp, 1.0, t);

    // 加法混合
    return originalAlbedo + _HighlightColor.rgb * mask;
}

#endif