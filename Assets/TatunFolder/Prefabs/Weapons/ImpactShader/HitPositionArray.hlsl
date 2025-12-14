//UNITY_SHADER_NO_UPGRADE
#ifndef HITPOSITIONARRAY_INCLUDED
#define HITPOSITIONARRAY_INCLUDED

float4 _HitPositions[20]; // position float3 + current radius

void MinDistance_float(float3 PixelWorldPos, out float Out)
{
    //float3 pos = _HitPositions[0].xyz;
    //float dist = distance(pos, PixelWorldPos);

    float minDist = 1000000;
    for (int i=0;i<20;i++){
        float3 pos = _HitPositions[i].xyz;
        float dist = distance(pos, PixelWorldPos) - _HitPositions[i].w;
        if (dist < minDist) {
            minDist = dist;
        }
    }
    Out = minDist;
}
#endif //HITPOSITIONARRAY_INCLUDED