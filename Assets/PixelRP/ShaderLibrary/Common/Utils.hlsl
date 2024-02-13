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

bool IsOrthographicCamera () {
    return unity_OrthoParams.w;
}

float OrthographicDepthBufferToLinear (float rawDepth) {
    #if UNITY_REVERSED_Z
    rawDepth = 1.0 - rawDepth;
    #endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}

float2 SampleUV(float2 uv, float4 sampleTexture)
{
    return uv * sampleTexture.xy + sampleTexture.zw;
}

float Quantize(float value, float steps)
{
    return round(value / steps) * steps;
}

float Random( float2 baseUVs )
{
    float noiseRes = 512;
    baseUVs*=noiseRes;
    baseUVs.x=Quantize(baseUVs.x, 1 / noiseRes);
    baseUVs.y=Quantize(baseUVs.y, 1 / noiseRes);
    float2 K1 = float2(
        23.14069263277926, // e^pi (Gelfond's constant)
         2.665144142690225 // 2^sqrt(2) (Gelfondâ€“Schneider constant)
    );
    return Quantize(frac( cos( dot(baseUVs,K1) ) * 12345.6789 ), .2);
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