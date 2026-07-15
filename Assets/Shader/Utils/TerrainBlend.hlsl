#ifndef TerrainBlend
#define TerrainBlend

#include "HexLibrary.hlsl"
#include "TerrainCommon.hlsl"

// 二维叉积（返回 z 分量）
float Cross2D(float2 a, float2 b)
{
    return a.x * b.y - a.y * b.x;
}

// 通过重心插值获取当前 fragment 所属三角形的两个邻居及其混合权重。
// wSelf(自身) + wNbr0(邻居0) + wNbr1(邻居1) = 1.0
// TODO ： 后续应用噪声让地貌过度更加自然
void GetTerrainTriangleInfo(
    float3 worldPos,
    float2 offsetHex,
    float _HexGridSize,
    out float2 nbrOffset0, out uint nbrID0, out float blendWeight0,
    out float2 nbrOffset1, out uint nbrID1, out float blendWeight1, out float4 Debug_Result)
{
    // 1. 直接根据角度确定三角形索引（0°对齐邻居方向，不经过 GetOffsetHexArea）
    float2 Oa = OffsetToWorldXZ(offsetHex, _HexGridSize);
    float2 P = float2(worldPos.x, worldPos.z);
    float angleDeg = GetDegrees(P - Oa);
    int triIdx = ((int)floor(angleDeg / 60.0)) % 6;

    // int triIdx; 
    int dir0;
    int dir1;
    float a = fmod(angleDeg, 360.0); 
    if (a < 0.0) {
        a += 360.0;
    }

    float test = 0;

    // 
    if (a < 60.0) { 
        dir0 = 5;
        dir1 = 0;
        // test = 1;
    }
    else if (a >= 60.0  && a < 120.0) { 
        dir0 = 5;
        dir1 = 4;
    } 
    else if (a >= 120.0 && a < 180.0) { 
        dir0 = 4;
        dir1 = 3;
    }
    else if (a >= 180.0 && a < 240.0) { 
        dir0 = 3;
        dir1 = 2;
    }
    else if (a >= 240.0 && a < 300.0) { 
        dir0 = 2;
        dir1 = 1;
    }
    else { 
        dir0 = 1;
        dir1 = 0;
    }

    // 2. 三角形 T_triIdx 使用邻居 (triIdx, triIdx+1)
    // int dir0 = triIdx;
    // int dir1 = (triIdx + 1) % 6;


    // 3. 获取两个邻居的 offset 坐标与世界中心
    nbrOffset0 = GetOffsetHexNeighbor(offsetHex, dir0, _HexGridSize);
    nbrOffset1 = GetOffsetHexNeighbor(offsetHex, dir1, _HexGridSize);
    float2 Ob = OffsetToWorldXZ(nbrOffset0, _HexGridSize);
    float2 Oc = OffsetToWorldXZ(nbrOffset1, _HexGridSize);

    // 4. 重心坐标（面积比法）
    float2 vOB = Ob - Oa;
    float2 vOC = Oc - Oa;
    float2 vPB = Ob - P;
    float2 vPC = Oc - P;
    float2 vPA = Oa - P;

    // float areaTotal = abs(vOB.x * vOC.y - vOB.y * vOC.x);
    // float wSelf = abs(vPB.x * vPC.y - vPB.y * vPC.x) / areaTotal;
    // float wNbr0 = abs(vPA.x * vPC.y - vPA.y * vPC.x) / areaTotal;
    // float wNbr1 = 1.0 - wSelf - wNbr0;
    // float areaTotal = Cross2D(vOB, vOC);
    // float wSelf = Cross2D(vPB, vPC) / areaTotal;
    // float wNbr0 = Cross2D(vPC, vPA) / areaTotal;
    // float wNbr1 = Cross2D(vPA, vPB) / areaTotal;


    float areaTotal = Cross2D(Ob - Oa, Oc - Oa);
    // 防止退化三角形
    areaTotal = (abs(areaTotal) < 1e-6) ? (areaTotal >= 0 ? 1e-6 : -1e-6) : areaTotal;
    // 三个权重全部独立计算
    float wSelf = Cross2D(Ob - P, Oc - P) / areaTotal;
    float wNbr0 = Cross2D(Oc - P, Oa - P) / areaTotal;
    float wNbr1 = Cross2D(Oa - P, Ob - P) / areaTotal;
    // 数值误差修正
    float weightSum = wSelf + wNbr0 + wNbr1;
    if (abs(weightSum) > 1e-6)
    {
        wSelf /= weightSum;
        wNbr0 /= weightSum;
        wNbr1 /= weightSum;
    }

    // Clamp，防止边界出现极小负数
    // wSelf = saturate(wSelf);
    // wNbr0 = saturate(wNbr0);
    // wNbr1 = saturate(wNbr1);
    // 再归一化一次
    weightSum = wSelf + wNbr0 + wNbr1;
    if (weightSum > 1e-6)
    {
        wSelf /= weightSum;
        wNbr0 /= weightSum;
        wNbr1 /= weightSum;
    }

    // === Debug: 基于三角形三顶点的 offset 坐标生成唯一 HASH ===
// 相同 (offsetHex, nbrOffset0, nbrOffset1) 必定得到相同值
float triangleHash = frac(sin(dot(offsetHex + nbrOffset0 + nbrOffset1, float2(127.1, 311.7))) * 43758.5453);
//  triangleHash = frac(sin(dot(offsetHex, float2(127.1, 311.7))) * 43758.5453);


    // wSelf = 1;
    // wNbr0 = triangleHash;
    // wNbr1 = 0;


    // 5. 读取邻居 terrain ID
    nbrID0 = LoadTerrainID((int2)nbrOffset0);
    nbrID1 = LoadTerrainID((int2)nbrOffset1);

    blendWeight0 = wNbr0;
    blendWeight1 = wNbr1;

    // Debug_Result = (areaTotal > 0) ? float4(1,1,1,1) : float4(0,0,0,1);
    switch(triIdx)
    {
    case 0: 
        Debug_Result = float4(1,0,0,1);
        break;
    case 1: Debug_Result = float4(0,1,0,1);
        break;
    case 2:Debug_Result = float4(0,0,1,1);
        break;
    case 3:Debug_Result = float4(1,1,0,1);
        break;
    case 4:Debug_Result = float4(1,0,1,1);
        break;
    case 5:Debug_Result = float4(0,1,1,1);
        break;
    }
    // Debug_Result =  float4(distance(Oa,Ob)/_HexGridSize, distance(Oa,Oc)/_HexGridSize, 0, 1);

    // 用于debug
    Debug_Result = float4(
        frac(Oa.x * 0.01),   // R通道 = Oa.x 的小数部分
        frac(Ob.x * 0.01),   // G通道 = Ob.x 的小数部分
        frac(Oc.x * 0.01),   // B通道 = Oc.x 的小数部分
        1.0
    );
    Debug_Result = float4(
        frac(triangleHash * 13.7),
        frac(triangleHash * 27.1),
        frac(triangleHash * 41.3),
        1.0
    );
    // Debug_Result = float4(
    //     worldPos.x / 2048, worldPos.y / 2048, worldPos.z / 2048, 1.0
    // );
    // Debug_Result = float4(test, 0, 0, 1.0);      // worldPos
}

// Deprecated: replaced by GetTerrainTriangleInfo. Kept for reference.
int AreaToNeighborDirection(int areaIdx)
{
    int safeAreaIdx = clamp(areaIdx, 0, 5);

    if (safeAreaIdx == 0) return 3;
    if (safeAreaIdx == 1) return 4;
    if (safeAreaIdx == 2) return 5;
    if (safeAreaIdx == 3) return 0;
    if (safeAreaIdx == 4) return 1;
    return 2;
}

float2 GetBlendNeighborOffset(float3 worldPos, float2 offsetHex, float _HexGridSize)
{
    int areaIdx = GetOffsetHexArea(worldPos, offsetHex, _HexGridSize);
    int neighborDir = AreaToNeighborDirection(areaIdx);
    return GetOffsetHexNeighbor(offsetHex, neighborDir, _HexGridSize);
}

float GetTerrainBlendWeight(float3 worldPos, float2 offsetHex, float _HexGridSize)
{
    float edgeFactor = GetRatioToHexEdge(worldPos, offsetHex, _HexGridSize);
    return smoothstep(0.6, 1.0, edgeFactor) * 0.5f;
}

void GetTerrainNeighborInfo(
    float3 worldPos,
    float2 offsetHex,
    float _HexGridSize,
    out float2 nbrOffset,
    out uint nbrID,
    out float blendWeight)
{
    nbrOffset = GetBlendNeighborOffset(worldPos, offsetHex, _HexGridSize);
    nbrID = LoadTerrainID((int2)nbrOffset);
    blendWeight = GetTerrainBlendWeight(worldPos, offsetHex, _HexGridSize);
}

float3 BlendTerrainAlbedo(float3 selfAlbedo, float3 nbrAlbedo, uint selfID, uint nbrID, float blendWeight)
{
    if (selfID == nbrID)
    {
        return selfAlbedo;
    }

    return lerp(selfAlbedo, nbrAlbedo, blendWeight);
}

float3 BlendTerrainNormal(float3 selfNormalTS, float3 nbrNormalTS, uint selfID, uint nbrID, float blendWeight)
{
    if (selfID == nbrID)
    {
        return selfNormalTS;
    }

    return normalize(lerp(selfNormalTS, nbrNormalTS, blendWeight));
}

float BlendTerrainScalar(float selfValue, float nbrValue, uint selfID, uint nbrID, float blendWeight)
{
    if (selfID == nbrID)
    {
        return selfValue;
    }

    return lerp(selfValue, nbrValue, blendWeight);
}

#endif
