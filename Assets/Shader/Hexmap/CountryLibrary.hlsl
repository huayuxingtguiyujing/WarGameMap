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
float  _RegionBorderWidth;
float  _RegionBorderSmooth;
float4 _RegionBorderColor;
float  _RegionBlendStrength;

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

// NOTE : TODO : 区域划分还是没有实现，当前效果不尽如人意，难搞！

// ApplyRegionDivide : EU4/CK3 风格区域划分着色
// 输入: worldPos (世界坐标), terrainColor (地形混合后的颜色), offsetHex (当前格子 offset 坐标)
// 输出: 混合区域色后的颜色
//
// NOTE (优化预留): 当前通过实时采样邻居格子的 _RegionTexture 颜色来判断边界方向，
// 每次调用需最多 6 次额外纹理采样。后续可将 6 方向边界信息编码到
// _CountryGridRelationTexture.r 的 6 个 bit 中，以消除邻居采样开销。
float3 ApplyRegionDivide(float3 worldPos, float3 terrainColor, float2 offsetHex, float _HexGridSize)
{
    if (_RegionDivideEnabled < 0.5)
        return terrainColor;

    // 1. 采样当前格子的区域颜色
    float3 selfRegionColor = SampleRegionColorAtOffset(offsetHex);

    // TODO : 下面的 SDF 表现是错误的！下次再搞吧

    // // 2. 无归属格子（山脉/海洋）不绘制区域色，直接返回地形色
    // if (IsNotValidCountryColor(selfRegionColor))
    //     return terrainColor;

    // // 3. 确定片元所在的三角扇区方向
    // int areaDir = GetOffsetHexArea(worldPos, offsetHex, _HexGridSize);

    // // 4. 采样该方向邻居的区域颜色，判断是否为边界方向
    // float2 neighborOffset = GetOffsetHexNeighbor(offsetHex, areaDir, _HexGridSize);
    // float3 neighborRegionColor = SampleRegionColorAtOffset(neighborOffset);

    // // 5. 判断该方向是否有区域边界
    // //    (邻居有归属 && 邻居颜色 != 自己颜色) → 边界方向
    // bool isBoundaryDir = !IsNotValidCountryColor(neighborRegionColor) &&
    //                      any(abs(neighborRegionColor - selfRegionColor) > 0.01);

    // // 6. 计算片元到六边形边缘的距离比 (0=中心, 1=边缘)
    // float edgeRatio = GetRatioToHexEdge(worldPos, offsetHex, _HexGridSize);

    // 7. SDF 边界效果计算
    // float borderMask = 0.0;
    // if (isBoundaryDir)
    // {
    //     // 该方向是边界：用 smoothstep 产生从边缘向内的羽化过渡
    //     // edgeRatio 越大越靠近边缘 → borderMask 在边缘附近为 1，向内衰减为 0
    //     borderMask = 1.0 - smoothstep(1.0 - _RegionBorderWidth - _RegionBorderSmooth,
    //                                    1.0 - _RegionBorderWidth,
    //                                    edgeRatio);
    // }

    // 8. 区域内部颜色混合：区域色 lerp 地形色
    //    靠近边界时偏向边界高亮色，内部偏向区域本色
    float3 regionInteriorColor = lerp(terrainColor, selfRegionColor, _RegionBlendStrength);

    // 9. 边界线高亮：白色/亮色描边
    // float3 borderHighlight = lerp(regionInteriorColor, _RegionBorderColor.rgb, borderMask);

    // 10. 最终结果 = 加法叠加到地形色上（半透明叠加策略 B）
    //    注意：这里的叠加发生在 regionInteriorColor 已经混入区域色的基础上，
    //    所以最终输出是：内部=区域色+地形底纹，边界=亮白描边
    return regionInteriorColor;
}

#endif