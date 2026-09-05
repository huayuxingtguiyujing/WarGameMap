#ifndef CountryLibrary
#define CountryLibrary

#include "../Utils/HexLibrary.hlsl"

// 用于生成区域划分效果
// int _HexmapWidth;
// int _HexmapHeight;

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

// 新版的 SDF - 区域划分
// ===== SDF 距离场（由 SDFComputer 生成，注入到 material）=====
sampler2D _RegionDistanceTexture;
float4    _RegionDistanceTexture_TexelSize;
float4    _RegionSDFWorldRect;      // xy = minXZ, zw = width/height（compute 生成时使用）
float     _RegionSDFTexelWorldSize;

// terrain 真实世界范围与 UV 变换（渲染采样阶段使用）
float4    _RegionTerrainWorldRect;  // xy = terrain minXZ, zw = terrain width/height
float4    _RegionSDFUVTransform;    // xy = scale, zw = offset（预留）
float4    _RegionSDFUVRect;         // xy = minUV, zw = sizeUV（hex 网格在 SDF 纹理 UV 空间的子矩形）


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

// ===== 区域划分 SDF 效果 (EU4/CK3 风格政治地图) =====
float  _RegionDivideEnabled;
float  _RegionBorderWidth;     // 0~1 归一化：边界线宽度
float  _RegionBorderSmooth;    // 0~1 归一化：边界过渡平滑度
float4 _RegionBorderColor;     // 边界高亮色（当前公式未使用，保留兼容）
float  _RegionBlendStrength;   // 区域色叠加总强度
float  _RegionGradientWidth;   // 0~1 归一化：从边界向内的渐变宽度
float  _EdgeColorMul;          // 边界处区域色强度（压暗）
float  _GradientColorMul;      // 渐变处区域色强度
float  _RegionEdgeAlpha;          // 边界线 alpha 强度
float  _RegionGradientAlphaInside;  // 区域内部渐变 alpha
float  _RegionGradientAlphaOutside; // 区域边界渐变 alpha

// 判断颜色是否是无归属颜色（NotValidCountryColor = (0.25, 0.25, 0.25)）
bool IsNotValidCountryColor(float3 c)
{
    return all(abs(c - float3(0.25, 0.25, 0.25)) < 0.01);
}

// 采样某个 offset hex 坐标的 Region 颜色
float3 SampleRegionColorAtOffset(float2 offsetHex)
{
    float2 uv = (offsetHex + 0.5) * _RegionTexture_TexelSize.xy;
    return tex2Dlod(_RegionTexture, float4(uv, 0, 0)).rgb;
}

// -------------------------------------

// 将 terrain UV（i.uv，= worldXZ / clusterSize）变换到 SDF 纹理 UV 空间。
// 通过 _RegionSDFUVRect（hex 网格在 SDF 纹理 UV 空间里的子矩形）做平移缩放。
float2 TransformRegionSDFUV(float2 terrainUV)
{
    return (terrainUV - _RegionSDFUVRect.xy) / _RegionSDFUVRect.zw;
}

// 根据 terrain UV 采样连续距离场。
// 返回 0~1 归一化值：0 = 正在边界上，1 = 离边界足够远。
// 越界返回 1.0（视为远离边界）。
float SampleRegionDistanceByTerrainUV(float2 terrainUV)
{
    float2 sdfUV = TransformRegionSDFUV(terrainUV);
    if (any(sdfUV < 0.0) || any(sdfUV > 1.0))
        return 1.0;
    return tex2Dlod(_RegionDistanceTexture, float4(sdfUV, 0, 0)).r;
}

// ApplyRegionDivide : EU4/CK3 风格区域划分着色
// 输入: worldPos (世界坐标), terrainColor (地形混合后的颜色), offsetHex (当前格子 offset 坐标)
// 输出: 混合区域色后的颜色
//
float3 ApplyRegionDivide(float3 worldPos, float2 uv, float3 terrainColor, float2 offsetHex, float _HexGridSize)
{
    if (_RegionDivideEnabled < 0.5)
        return terrainColor;

    // 1. 采样当前格子的区域颜色
    float3 selfRegionColor = SampleRegionColorAtOffset(offsetHex);

    // 2. 无归属格子（山脉/海洋）不绘制区域色，直接返回地形色
    if (IsNotValidCountryColor(selfRegionColor))
        return terrainColor;

    // 3. 采样连续 SDF 距离（0 = 边界，1 = 远离边界），基于 terrain UV
    float d = SampleRegionDistanceByTerrainUV(uv);

    // 4. 渐变强度：从边界(1)向内部(0)平滑过渡
    float gradientT = 1.0 - saturate((d - _RegionBorderWidth) / _RegionGradientWidth);

    // 5. 边界 mask：贴近边界处为 1，向内平滑衰减为 0
    float edgeMask = smoothstep(
        _RegionBorderWidth + _RegionBorderSmooth,
        _RegionBorderWidth,
        d
    );

    // 6. 渐变 alpha：内部用 Inside，边界用 Outside
    float gradientAlpha = lerp(
        _RegionGradientAlphaInside,
        _RegionGradientAlphaOutside,
        gradientT
    );

    // 7. 区域色覆盖层：渐变处纯区域色，边界处区域色压暗
    float3 gradientRGB = selfRegionColor * _GradientColorMul;
    float3 edgeRGB = selfRegionColor * _EdgeColorMul;

    float3 overlayRGB = lerp(gradientRGB, edgeRGB, edgeMask);
    float overlayAlpha = _RegionBlendStrength * max(gradientAlpha, edgeMask * _RegionEdgeAlpha);

    // 8. 最终：地形色与区域覆盖层混合
    return lerp(terrainColor, overlayRGB, saturate(overlayAlpha));
}

#endif