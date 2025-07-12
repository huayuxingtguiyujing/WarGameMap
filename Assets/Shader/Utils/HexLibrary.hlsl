#ifndef HexLibrary
#define HexLibrary

// NOTE : 这里放所有与 Hex 有关的方法
#include "MathLibrary.hlsl"

// x y 坐标转为 cube hex 坐标
float3 PixelToHexCubeCoord(float3 worldPos, float _HexGridSize){
    float q = (sqrt(3)/3 * worldPos.x  -  1./3 * worldPos.z) / _HexGridSize;
    float r = 2./3 * worldPos.z / _HexGridSize;
    float s = - q - r;

    int fix_q = round(q);
    int fix_r = round(r);
    int fix_s = round(s);

    float q_diff = abs(fix_q - q);
    float r_diff = abs(fix_r - r);
    float s_diff = abs(fix_s - s);

    int final_q = fix_q, final_r = fix_r, final_s = fix_s;
    if(q_diff > r_diff && q_diff > s_diff){
        final_q = - fix_r - fix_s;
    }else if(r_diff > s_diff){
        final_r = - fix_q - fix_s;
    }else{
        final_s = - fix_q - fix_r;
    }
    return float3(final_q, final_r, final_s);
}

// cube hex 坐标转为 x y
float2 CubeToPixelXZ(float3 cubePos, float _HexGridSize){
    float q = cubePos.x;
    float r = cubePos.y;
    float x = _HexGridSize * (sqrt(3.0) * q + sqrt(3.0)/2.0 * r);
    float z = _HexGridSize * (3.0/2.0 * r);
    return float2(x, z);
}

// offset hex 坐标转为 x y
float2 OffsetToPixelXZ(float2 offsetPos, float _HexGridSize){
    float col = offsetPos.x;
    float row = offsetPos.y;
    float x = _HexGridSize * (sqrt(3.0) * (col + 0.5 * (int(row) & 1)));
    float z = _HexGridSize * (3.0/2.0 * row);
    return float2(x, z);
}

float3 CubeCoordToOffset(float3 cubePos){
    float col = cubePos.x + (cubePos.y - ((int)cubePos.y&1)) / 2;
    float row = cubePos.y;
    return float3(col, row, -col-row);
}

float3 OffsetCoordToCube(float3 offsetPos){
    float q = offsetPos.x - (offsetPos.y - ((int)offsetPos.y&1)) / 2;
    float r = offsetPos.y;
    return float3(q, r, -q-r);
}



//       ___
//    2 /   \ 1
//     /     \ 
//    |       | 
//  3 |       | 0(Edge)
//     \     /
//    4 \___/ 5
//
static const float2 HexEdgeData[6] = {
    {sqrt(3), 0}, {sqrt(3) / 2, 0.75}, {-sqrt(3) / 2, 0.75},
    {-sqrt(3), 0}, {-sqrt(3) / 2, -0.75}, {sqrt(3) / 2, -0.75}
};

static const float2 HexCornerData[6] = { 
    {sqrt(3) / 2, 0.5}, {0, 1}, {-sqrt(3) / 2, 0.5}, 
    {-sqrt(3) / 2, -0.5}, {0, -1}, {sqrt(3) / 2, -0.5}
};

float2 GetHexCorner(float3 cubePos, float _HexGridSize, int idx){
    idx = fmod(idx + 6, 6);
    float2 center = CubeToPixelXZ(cubePos, _HexGridSize);
    return center + _HexGridSize * HexCornerData[idx];
}

void GetHexCorners(float3 cubePos, float _HexGridSize, out float2 corners[6]){
    // get hex corner , the index and relation like below
    //   NW / 1 \ NE
    //    2/     \0 (corner)
    //    |       | 
    //  W |       | E
    //    3\     /5
    //      \   / 
    //   SW   4   SE
    
    float2 center = CubeToPixelXZ(cubePos, _HexGridSize);
    const float startAngle = 30.0 * (3.14159265 / 180.0);
    for(int i = 0; i < 6; i++){
        float angle = startAngle + (float)i * (2.0 * 3.14159265 / 6.0);
        float dx = _HexGridSize * cos(angle);
        float dz = _HexGridSize * sin(angle);
        corners[i] = center + float2(dx, dz);
    }
}

float2 GetHexEdgeCenter(float3 cubePos, float _HexGridSize, float idx){
    int curIdx = fmod(idx + 5, 6);
    int nextIdx = fmod(idx + 6, 6);
    // float2 edgeCenter = (GetHexCorner(cubePos, _HexGridSize, curIdx) + GetHexCorner(cubePos, _HexGridSize, nextIdx)) / 2;
    float2 center = CubeToPixelXZ(cubePos, _HexGridSize);
    float2 edgeCenter = center + _HexGridSize * (HexCornerData[curIdx] + HexCornerData[nextIdx]) / 2;
    return edgeCenter;
}

// get the ratio of point to center, return a ratio (0 ~ 1.0)
float GetRatioToHexEdge(float3 worldPos, float _HexGridSize){
    // firstly get distance to hex center : 
    float3 cubePos = PixelToHexCubeCoord(float3(worldPos.x, 0, worldPos.z), _HexGridSize);
    float2 worldXZ = float2(worldPos.x, worldPos.z);
    float2 hexCenter = CubeToPixelXZ(cubePos, _HexGridSize);

    // get the area idx where the worldXZ in
    float angleDeg = GetDegrees(worldXZ - hexCenter);
    float mappedIdx = floor((angleDeg + 30.0) / 60.0);

    // get the two hex corner, and start caculate distance finally we get ratio
    int curIdx = fmod(mappedIdx - 1 + 6, 6);    
    int nextIdx = fmod(mappedIdx + 6, 6);
    float2 curCorner = GetHexCorner(cubePos, _HexGridSize, curIdx);
    float2 nextCorner = GetHexCorner(cubePos, _HexGridSize, nextIdx);

    float distance_pos_edge = DistancePointToLine(worldXZ, curCorner, nextCorner);
    float distance_center_edge = DistancePointToLine(hexCenter, curCorner, nextCorner);

    return (distance_center_edge - distance_pos_edge) / distance_center_edge;

    // it is same as upside
    // float2 edgeCenter = GetHexEdgeCenter(cubePos, _HexGridSize, mappedIdx);
    // float2 mappedPos = ProjectPointOnSegment(worldXZ, hexCenter, edgeCenter);
    // return distance(mappedPos, hexCenter) / distance(edgeCenter, hexCenter);
}

float GetDistToHexCenter(float3 worldPos, float _HexGridSize){
    float3 cube = PixelToHexCubeCoord(float3(worldPos.x, 0, worldPos.z), _HexGridSize);
    float2 worldXZ = float2(worldPos.x, worldPos.z);
    float2 hexCenter = CubeToPixelXZ(cube, _HexGridSize);

    float dist = distance(worldXZ, hexCenter);
    return dist;
}


#endif