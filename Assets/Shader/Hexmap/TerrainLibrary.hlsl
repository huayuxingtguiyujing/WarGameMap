#ifndef TerrainLibrary
#define TerrainLibrary

#include "../Utils/HexLibrary.hlsl"

// 用于生成地形效果

//
// 各种地形的值 需要在C# 中通过 TerrainType 结构进行赋值
//      对于各种平原地形，会使用一致的颜色值
//      对于山地类地形，会用 TerrainHeight 来区分不同高度的颜色
float4 _PlainColor;
float4 _HillColor;
float4 _MountainColor;
float4 _PlateauColor;
float4 _SnowColor;

// TODO : TerrainHeight 区分不同高度的颜色，也需要在 C# 中进行设置

sampler2D _GridTerrainTypeTexture;
float4 _GridTerrainTypeTexture_ST;
float4 _GridTerrainTypeTexture_TexelSize;

// TODO : 根据地形类型，返回对应的基础颜色
float4 GetGridBaseColor(float3 worldPos, float _HexGridSize)
{
    float3 offsetHex = WorldToOffset(worldPos, _HexGridSize);
    float4 terrainDataType = tex2D(_GridTerrainTypeTexture, offsetHex.xy);

    // float terrainType = terrainData.r;      // 地形类型
    // float terrainHeight = terrainData.g;    // 地形高度

    // if(terrainType < 0.25){         // plain
    //     return _PlainColor;
    // }else if(terrainType < 0.5){   // hill
    //     return _HillColor;
    // }else if(terrainType < 0.75){  // mountain
    //     return _MountainColor;
    // }else if(terrainType < 0.9){   // plateau
    //     return _PlateauColor;
    // }else{                          // snow
    //     return _SnowColor;
    // }
    return float4(0,0,0,1);
}



#endif