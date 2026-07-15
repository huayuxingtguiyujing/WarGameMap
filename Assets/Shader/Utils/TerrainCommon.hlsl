#ifndef TerrainCommon
#define TerrainCommon

#include "NoiseLibrary.hlsl"

// NOTE : 地貌 Shader 的通用资源声明与采样封装。

struct MaterialParams
{
    float roughness;
    float metallic;
    float detailFrequency;
    float detailStrength;
};

StructuredBuffer<uint> _TerrainIDBuffer;
Texture2DArray _TerrainAlbedoArray;
Texture2DArray _TerrainNormalArray;
SamplerState sampler_LinearRepeat_Terrain;

// 地貌材质参数
StructuredBuffer<MaterialParams> _TerrainMaterialParamsBuffer;
// 不渲染六边形边框的 hex 地形格子
StructuredBuffer<uint> _ExcludeOutlineLUT;

uint _HexmapWidth;
uint _HexmapHeight;

int2 ClampTerrainIDCoord(int2 offsetHex)
{
    int2 minCoord = int2(0, 0);
    int2 maxCoord = int2((int)_HexmapWidth - 1, (int)_HexmapHeight - 1);
    return clamp(offsetHex, minCoord, maxCoord);
}

uint LoadTerrainID(int2 offsetHex)
{
    // TODO：这里是debug逻辑，后续要填入真正的terrainid
    // 用 hex 坐标的 hash 生成伪随机 terrainID，仅用于验证 blend 视觉
    // uint2 pos = (uint2)offsetHex;
    // uint hash = pos.x * 0x9E3779B9u + pos.y * 0x27D1EB47u;
    // hash ^= hash >> 16;
    // hash *= 0x85EBCA6Bu;
    // hash ^= hash >> 13;
    // hash *= 0xC2B2AE35u;
    // hash ^= hash >> 16;
    // return hash % 5u + 11;  // 映射到 五种 terrainID，方便看混合
    
    // TODO ： 之后替换为真实数据：
    // int2 coord = ClampTerrainIDCoord(offsetHex);
    // return _TerrainIDMap.Load(int3(coord, 0)).r;
    int2 coord = ClampTerrainIDCoord(offsetHex);
    uint idx = (uint)(coord.y * (int)_HexmapWidth + coord.x);
    return _TerrainIDBuffer[idx];
}

MaterialParams GetTerrainMaterialParams(uint terrainID)
{
    return _TerrainMaterialParamsBuffer[terrainID];
}

float4 SampleTerrainAlbedo(float2 uv, uint terrainID)
{
    return _TerrainAlbedoArray.Sample(sampler_LinearRepeat_Terrain, float3(uv, terrainID));
}

float3 SampleTerrainNormal(float2 uv, uint terrainID)
{
    float4 normalColor = _TerrainNormalArray.Sample(sampler_LinearRepeat_Terrain, float3(uv, terrainID));

    // Unity 常见 normal map 编码为 0~1，这里先解码到切线空间 -1~1。
    float3 normalTS = normalColor.xyz * 2.0 - 1.0;
    return normalize(normalTS);
}

float GetTerrainDetailNoiseMultiplier(float2 worldXZ, MaterialParams mat)
{
    return 1.0 + SimplexNoise(worldXZ * mat.detailFrequency) * mat.detailStrength;
}

bool ShouldExcludeHexOutline(uint terrainID)
{
    return _ExcludeOutlineLUT[terrainID] == 1;
}

#endif
