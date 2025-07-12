#ifndef MathLibrary
#define MathLibrary

// NOTE : 这里放所有与 Math 有关的方法

float DistancePointToSegment(float3 P, float3 A, float3 B)
{
    float3 AB = B - A;
    float3 AP = P - A;
    float t = saturate(dot(AP, AB) / dot(AB, AB)); // Clamp t ∈ [0,1]
    float3 closest = A + t * AB;
    return distance(P, closest);
}

float3 ProjectPointOnSegment(float3 P, float3 A, float3 B)
{
    float3 AB = B - A;
    float3 AP = P - A;
    float t = saturate(dot(AP, AB) / dot(AB, AB)); // Clamp 到线段上
    return A + t * AB;
}

float2 ProjectPointOnSegment(float2 P, float2 A, float2 B)
{
    float2 AB = B - A;
    float2 AP = P - A;
    float t = saturate(dot(AP, AB) / dot(AB, AB)); // Clamp 到线段上
    return A + t * AB;
}

float2 ProjectPointOnLine(float2 P, float2 A, float2 B)
{
    float2 AB = B - A;
    float2 AP = P - A;

    float t = dot(AP, AB) / dot(AB, AB);
    return A + t * AB;
}

float DistancePointToLine(float2 P, float2 A, float2 B)
{
    float2 AB = B - A;
    float2 AP = P - A;

    float2 proj = AB * dot(AP, AB) / dot(AB, AB); // 向量投影
    float2 perp = AP - proj;                      // 垂直向量
    return length(perp);                          // 返回模长，即垂直距离
}


float GetDegrees(float2 angle){
     // 弧度转角度  return [0, 360)
    float angleDeg = degrees(atan2(angle.y, angle.x));
    return fmod(angleDeg + 360.0, 360.0);
}


#endif