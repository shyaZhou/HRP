#ifndef LIGHT_H_
#define LIGHT_H_

#define DIRECTIONAL 0
#define POINT       1
#define SPOT        2
#define SPHERE      3
#define TUBE        4
#define QUAD        5
#define DISC        6
#define MESH        7

struct Light
{
    float4 position_range;
    float4 radiance_type;       // Directional, Point, Spot, Sphere, Tube, Quad, Disc, Mesh
    float4 mainDirection_id;
    float4 geometry;            // Spot:    cosineAngle(x)
                                // Sphere:  radius(x)
                                // Tube:    length(x), radius(y)
                                // Quad:    size(xy), second axil sphere angle(zw)
                                // Disc:    radius(x)
};

struct SolvedLocalLight
{
    float3 dir;
    float3 radiance;
    int id;
};

float4 _TileCount;
StructuredBuffer<Light> _LocalLightBuffer;


int _AreaLightCount;
StructuredBuffer<Light> _AreaLightBuffer;


#ifdef ZIBIN_COMPUTE
RWStructuredBuffer<uint> _TileLights;
#else
StructuredBuffer<uint> _TileLights;
#endif



#define BegineLocalLightsLoop(uv, pos, invVP) {\
uint4 tileCount = _TileCount;\
float4 camPos_ = mul(invVP, float4(0, 0, 1, 1));\
float3 nearPlane_ = camPos_.xyz / camPos_.w;\
uint2 tileId = uint2(floor(uv * tileCount.xy)); \
uint maxLightCount = tileCount.w;\
uint start_offset = (tileId.x + tileId.y * tileCount.x) * (tileCount.w + 1);\
float4 farPlane_ = mul(invVP, float4(0, 0, 0, 1));\
farPlane_ /= farPlane_.w;\
float far = distance(farPlane_.xyz, nearPlane_);\
float3 viewDir_ = (farPlane_.xyz - nearPlane_) / far;\
uint bin = 1 << uint(floor(sqrt(dot(pos - nearPlane_, viewDir_) / far) * tileCount.z));\
uint lightCount = _TileLights[start_offset];\
start_offset += 1;\
for (uint i = 0; i < lightCount; i++)\
{\
    uint lIdx = _TileLights[start_offset + i];\
    uint lmask = lIdx & 0xFFFFFF;\
    Light light_ = _LocalLightBuffer[lIdx >> 24];\
    if (bin & lmask) {\
        SolvedLocalLight light;\
        light.dir = light_.position_range.xyz - pos;\
        float Ldis = length(light.dir);\
        light.dir /= Ldis;\
        light.radiance = light_.radiance_type.xyz * max(0, 1 - Ldis / light_.position_range.w);\
        if (light_.radiance_type.w == SPOT){\
            float dotLDir = max(0, dot(-light.dir, light_.mainDirection_id.xyz)); \
            light.radiance *= max(0, 1 - sqrt(1 -dotLDir*dotLDir) / dotLDir / light_.geometry.x);\
        }\

#define EndLocalLightsLoop }}}



#endif