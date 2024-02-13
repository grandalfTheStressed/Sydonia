#ifndef UTILS_INCLUDED
#define UTILS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "UnityInput.hlsl"
#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_PREV_MATRIX_M unity_prev_MatrixM
#define UNITY_PREV_MATRIX_I_M unity_prev_MatrixIM
#define UNITY_MATRIX_P glstate_matrix_projection
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float4 _camera_forward;

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

float Square(float v) {
    return v * v;
}

float Qdot(float3 v1, float3 v2) {
    return saturate(dot(v1, v2));
}

float Distance2(float3 pA, float3 pB) {
    return dot(pA - pB, pA - pB);
}

float Distance(float3 pA, float3 pB) {
    return sqrt(Distance2(pA, pB));
}

static const float _BayerMatrix[16] = {
    0.0,  8.0,  2.0, 10.0,
    12.0, 4.0, 14.0, 6.0,
    3.0, 11.0, 1.0,  9.0,
    15.0, 7.0, 13.0, 5.0
};

#endif