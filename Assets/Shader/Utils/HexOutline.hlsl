#ifndef HexOutline
#define HexOutline

#include "HexLibrary.hlsl"

float _HexGridSize;
float _HexGridScale;
float _HexGridEdgeRatio;    // 六边形边框比例，可以控制六边形边框大小，建议取值为 0.01 ~ 0.15

sampler _HexGridTypeTexture;
float4 _HexGridTypeTexture_ST;
float4 _HexGridTypeTexture_TexelSize;


float3 GetHexOutlineColor(float3 worldPos, float3 _BackgroundColor) : SV_Target{
    
    float3 hex = PixelToHexCubeCoord(worldPos, _HexGridSize);
    hex = CubeCoordToOffset(hex);

    int x_idx = hex.x;
    int y_idx = hex.y;

    float2 hex_uv = float2(x_idx , y_idx) * _HexGridTypeTexture_TexelSize.xy;

    float3 gridColor = tex2D(_HexGridTypeTexture, hex_uv);
    gridColor = float3(0.5, 0.5, 0.5);
    // float3 bgcolor = _BackgroundColor;


    // round version
    // float t = GetDistToHexCenter(worldPos, _HexGridSize) / _HexGridSize;  //
    // better version
    float t = GetRatioToHexEdge(worldPos, _HexGridSize);
    // return float3(t, t, t);

    t = clamp(0, 1 - _HexGridEdgeRatio, t);
    float mask = smoothstep(0.9, 1.0, t);   // 0.7 start lerp
    return lerp(_BackgroundColor, gridColor, mask);
}


        
#endif