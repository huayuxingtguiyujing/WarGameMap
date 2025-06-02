#ifndef HexLibrary
#define HexLibrary

// TODO : 这里放所有与 Hex 有关的方法

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


#endif