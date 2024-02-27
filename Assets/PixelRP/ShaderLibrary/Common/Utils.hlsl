#ifndef UTILS_INCLUDED
#define UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
    #define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

SAMPLER(sampler_linear_clamp);
SAMPLER(sampler_point_clamp);

float4 _camera_forward;

struct GBuffer
{
    float2 kernelUVs;
    float texelSize;
    sampler albedo;
    sampler normal;
    sampler position;
    sampler offset;
    sampler edge;
    sampler highlights;
};

bool IsOrthographicCamera () {
    return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear (float rawDepth) {
    #if UNITY_REVERSED_Z
        rawDepth = 1.0 - rawDepth;
    #endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

float GetDepth(float4 positionCS_SS)
{
   return IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionCS_SS.z) : positionCS_SS.w;
}

float InvLerp(float a, float b, float v)
{
    return (v - a) / (b - a);
}

struct Edges
{
    float depthEdge;
    float normalEdge;
};

Edges EdgeDetection(GBuffer buffer, float NdotV)
{
    const float kernel[3][3] = {
        {-1, -1, -1},
        {-1,  8, -1},
        {-1, -1, -1}
    };

    float2 pixelUVs;
    float3 normalGradient = 0.0;
    float depthGradient = 0.0;

    for(int i = -1; i <= 1; i++) {
        for(int j = -1; j <= 1; j++) {
            pixelUVs = buffer.kernelUVs + buffer.texelSize * float2(i, j);
        
            const float3 sampledNormal = tex2D(buffer.normal, pixelUVs).rgb;
            const float sampledDepth = tex2D(buffer.position, pixelUVs).a;

            normalGradient += sampledNormal * kernel[i + 1][j + 1];
            depthGradient += sampledDepth * kernel[i + 1][j + 1];
        }
    }

    float3 normalGradientMagnitude = normalGradient;
    float depthGradientMagnitude = depthGradient;
    
    float depth = depthGradientMagnitude;
    depth = InvLerp(.001, .050, depth) < 1 ? 0 : 1;

    float normal = max(max(normalGradientMagnitude.r, normalGradientMagnitude.g), normalGradientMagnitude.b);
    normal = normal < 1 ? 0 : 1 * (1 - depth);
    
    Edges edges;
    edges.normalEdge = normal;
    edges.depthEdge = depth;
    
    return edges;
}

float Quantize(float value, float stepSize)
{
    return round(value / stepSize) * stepSize;
}

float Random( float2 coords )
{
    float noiseRes = 30;
    coords*=noiseRes;
    coords.x=Quantize(coords.x, 1 / noiseRes);
    coords.y=Quantize(coords.y, 1 / noiseRes);
    float2 K1 = float2(
        23.14069263277926, // e^pi (Gelfond's constant)
         2.665144142690225 // 2^sqrt(2) (Gelfondâ€“Schneider constant)
    );
    return Quantize(frac( cos( dot(coords,K1) ) * 12345.6789 ), .2);
}

float ApplyNoiseToEdges(float value, float stepSize, float dither) {
    float quantizedValue = Quantize(value, stepSize);
    
    return dither > .6 ? quantizedValue : value;
}

float Qdot(float3 v1, float3 v2) {
    return saturate(dot(v1, v2));
}

float Square (float x) {
    return x * x;
}

float DistanceSquared(float3 pA, float3 pB) {
    return dot(pA - pB, pA - pB);
}

float Distance(float3 pA, float3 pB) {
    return sqrt(DistanceSquared(pA, pB));
}

float3 DecodeNormal (float4 sample, float scale) {
    #if defined(UNITY_NO_DXT5nm)
    return normalize(UnpackNormalRGB(sample, scale));
    #else
    return normalize(UnpackNormalmapRGorAG(sample, scale));
    #endif
}

float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS) {
    float3x3 tangentToWorld =
        CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld);
}

#endif