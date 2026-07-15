#ifndef NoiseLibrary
#define NoiseLibrary

// NOTE : 本文件提供地貌 Shader 使用的轻量噪声函数。
// 当前 M0 目标是快速验证视觉效果，因此先使用稳定、便宜的 value noise。

float Hash21(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float ValueNoise(float2 p)
{
    float2 cell = floor(p);
    float2 localPos = frac(p);

    // 使用 smooth 曲线减少格子感，避免噪声边界过硬。
    float2 smoothPos = localPos * localPos * (3.0 - 2.0 * localPos);

    float noise00 = Hash21(cell + float2(0.0, 0.0));
    float noise10 = Hash21(cell + float2(1.0, 0.0));
    float noise01 = Hash21(cell + float2(0.0, 1.0));
    float noise11 = Hash21(cell + float2(1.0, 1.0));

    float noiseX0 = lerp(noise00, noise10, smoothPos.x);
    float noiseX1 = lerp(noise01, noise11, smoothPos.x);
    return lerp(noiseX0, noiseX1, smoothPos.y);
}

float FbmNoise(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    [unroll]
    for (int i = 0; i < 4; i++)
    {
        value += ValueNoise(p * frequency) * amplitude;
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// 保留 SimplexNoise 名称，方便后续替换为真正 simplex 实现时不改调用方。
float SimplexNoise(float2 p)
{
    return FbmNoise(p) * 2.0 - 1.0;
}

#endif
