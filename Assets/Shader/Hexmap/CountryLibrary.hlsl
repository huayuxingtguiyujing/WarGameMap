#ifndef CountryLibrary
#define CountryLibrary

#include "../Utils/HexLibrary.hlsl"

// 用于生成区域划分效果
int _HexmapWidth;
int _HexmapHeight;

sampler2D _CountryGridRelationTexture;
// Texture2D<uint4> _CountryGridRelationTexture;
float4 _CountryGridRelationTexture_ST;
float4 _CountryGridRelationTexture_TexelSize;

SamplerState sampler_CountryGridRelationTexture;

sampler _RegionTexture;
float4 _RegionTexture_ST;
float4 _RegionTexture_TexelSize;

sampler _ProvinceTexture;
float4 _ProvinceTexture_ST;
float4 _ProvinceTexture_TexelSize;

sampler _PrefectureTexture;
float4 _PrefectureTexture_ST;
float4 _PrefectureTexture_TexelSize;

sampler _SubPrefectureTexture;
float4 _SubPrefectureTexture_ST;
float4 _SubPrefectureTexture_TexelSize;

float _EdgeRatio;
float4 _BorderLerpColor1;
float4 _BorderLerpColor2;


int IsHexEdgeGrid(float3 worldPos, int flag, float4 gridColor, float _HexGridSize, out float4 testColor)
{
    // 根据预计算的边缘关系纹理，确定每个格子的边缘信息
    // R : region, G : province, B : prefecture, A : subprefecture
    // xx111111 : 前两位保留，后六位分别表示 六个方向的邻居是否属于同一区域
    // 后六位 代表 W  NW  NE  E  SE  SW 方向是否存在其他区域的 grid
    float3 offsetHex = WorldToOffset(worldPos, _HexGridSize);
    int offset_x = round(offsetHex.x);
    int offset_y = round(offsetHex.y);
    float2 hex_uv = (float2(offset_x, offset_y) + 0.5) * _CountryGridRelationTexture_TexelSize.xy;
    float4 relationColor = tex2D(_CountryGridRelationTexture, hex_uv);  // _CountryGridRelationTexture   // _RegionTexture
    testColor = relationColor;

    // 根据 areaIdx 获取对应的邻居位置
    int areaIdx = GetOffsetHexArea(worldPos, offsetHex.xy, _HexGridSize);
    float2 neighbor = GetOffsetHexNeighbor(offsetHex.xy, areaIdx, _HexGridSize);
    int neighbor_x = round(neighbor.x);
    int neighbor_y = round(neighbor.y);
    float2 neighbor_uv = (float2(neighbor_x, neighbor_y) + 0.5) * _CountryGridRelationTexture_TexelSize.xy;

    float4 neighborColor = float4(0,0,0,0);
    switch(flag){
        case 0:
            if(relationColor.r > 0.8f){
                return 1;
                // 是边缘
                neighborColor = tex2D(_RegionTexture, neighbor_uv);
                testColor = neighborColor;

                return dot(normalize(neighborColor), normalize(gridColor)) > 0.98; //neighborColor != gridColor;
                return neighborColor != gridColor;
            }else{
                return 0;
            }
            break;
        case 1:
            if(relationColor.g > 0.9){
                // 是边缘
                neighborColor = tex2D(_ProvinceTexture, neighbor_uv);
                return neighborColor != gridColor;
            }else{
                return 0;
            }
            break;
        case 2:
            if(relationColor.b > 0.9){
                // 是边缘
                neighborColor = tex2D(_PrefectureTexture, neighbor_uv);
                return neighborColor != gridColor;
            }else{
                return 0;
            }
            break;
        case 3:
            if(relationColor.a > 0.9){
                // 是边缘
                neighborColor = tex2D(_SubPrefectureTexture, neighbor_uv);
                return neighborColor != gridColor;
            }else{
                return 0;
            }
            break;
    }
    return 0;
}

float4 LerpHexEdgeBorderColor(float3 worldPos, float3 terrainColor, float4 gridColor, float _HexGridSize)
{
    float edgeFactor = GetRatioToHexEdge(worldPos, _HexGridSize);
    // 边缘描边效果 // _BorderLerpColor1 _BorderLerpColor2
    float edgeMask = smoothstep(0.7, 0.95, edgeFactor);
    float edgeLerp = saturate(edgeMask - _EdgeRatio);
    float3 borderLerpColor = lerp(_BorderLerpColor1.rgb, gridColor.rgb, edgeMask);

    // 将描边与原色混合
    float3 blendColor = lerp(terrainColor, borderLerpColor, edgeLerp);
    return float4(blendColor, 1.0);
}

// flag : 0 - region, 1 - province, 2 - prefecture, 3 - subprefecture
float4 GetHexEdgeBorderColor(float3 worldPos, float3 terrainColor, int flag, float _HexGridSize)
{
    float4 testColor;
    int isEdgeGrid = 0;
    float3 offsetHex = WorldToOffset(worldPos, _HexGridSize);
    int offset_x = round(offsetHex.x);
    int offset_y = round(offsetHex.y);
    float2 hex_uv = (float2(offset_x, offset_y) + 0.5) * _CountryGridRelationTexture_TexelSize.xy;
    switch(flag){
        case 0:
            float4 gridRegionColor = tex2D(_RegionTexture, hex_uv);
            isEdgeGrid = IsHexEdgeGrid(worldPos, flag, gridRegionColor, _HexGridSize, testColor);
            return testColor;
            return float4(isEdgeGrid, 0, 0, 1);
            if(isEdgeGrid == 1){
                // return float4(isEdgeGrid, 0, 0, 1);
                return LerpHexEdgeBorderColor(worldPos, terrainColor, gridRegionColor, _HexGridSize);
            }else{
                return float4(terrainColor, 1.0);
            }
            break;
        case 1:
            float4 gridProvinceColor = tex2D(_ProvinceTexture, hex_uv);
            isEdgeGrid = IsHexEdgeGrid(worldPos, flag, gridProvinceColor, _HexGridSize, testColor);
            if(isEdgeGrid == 1){
                return LerpHexEdgeBorderColor(worldPos, terrainColor, gridProvinceColor, _HexGridSize);
            }else{
                return float4(terrainColor, 1.0);
            }
            break;
        case 2:
            float4 gridPrefectureColor = tex2D(_PrefectureTexture, hex_uv);
            isEdgeGrid = IsHexEdgeGrid(worldPos, flag, gridPrefectureColor, _HexGridSize, testColor);
            if(isEdgeGrid == 1){
                return LerpHexEdgeBorderColor(worldPos, terrainColor, gridPrefectureColor, _HexGridSize);
            }else{
                return float4(terrainColor, 1.0);
            }
            break;
        case 3:
            float4 gridSubPrefectureColor = tex2D(_SubPrefectureTexture, hex_uv);
            isEdgeGrid = IsHexEdgeGrid(worldPos, flag, gridSubPrefectureColor, _HexGridSize, testColor);
            if(isEdgeGrid == 1){
                return LerpHexEdgeBorderColor(worldPos, terrainColor, gridSubPrefectureColor, _HexGridSize);
            }else{
                return float4(terrainColor, 1.0);
            }
            break;
    }
    return float4(0,0,0,1);
}

// TODO : 上面的是旧的，是个失败的描边效果，下面目前没有描边效果
// TODO : 需要实现
float4 LerpCountryAndTerrainColor(float3 terrainColor, float4 gridColor)
{
    float3 blendColor = lerp(terrainColor, gridColor, _EdgeRatio);
    return float4(blendColor, 1.0);
}

float4 GetCountryColor(float3 worldPos, float3 terrainColor, int flag, float _HexGridSize)
{
    // Get uv by world pos
    float3 offsetHex = WorldToOffset(worldPos, _HexGridSize);
    int offset_x = round(offsetHex.x);
    int offset_y = round(offsetHex.y);
    float2 hex_uv = (offsetHex + 0.5) * _RegionTexture_TexelSize.xy;     // float2(offset_x, offset_y)
    // hex_uv = float2(hex_uv.x / _HexmapWidth, hex_uv.y / _HexmapHeight);

    switch(flag){
        case 0:
            float4 gridRegionColor = tex2D(_RegionTexture, hex_uv);
        // return gridRegionColor;
            return LerpCountryAndTerrainColor(terrainColor, gridRegionColor);
        case 1:
            float4 gridProvinceColor = tex2D(_ProvinceTexture, hex_uv);
            return LerpCountryAndTerrainColor(terrainColor, gridProvinceColor);
        case 2:
            float4 gridPrefectureColor = tex2D(_PrefectureTexture, hex_uv);
            return LerpCountryAndTerrainColor(terrainColor, gridPrefectureColor);
        case 3:
            float4 gridSubPrefectureColor = tex2D(_SubPrefectureTexture, hex_uv);
            return LerpCountryAndTerrainColor(terrainColor, gridSubPrefectureColor);
    }
    return float4(0,0,0,1);
}

#endif