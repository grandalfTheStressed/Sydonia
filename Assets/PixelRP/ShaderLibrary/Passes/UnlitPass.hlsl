#ifndef  PIXEL_UNLIT_PASS_INCLUDED
#define PIXEL_UNLIT_PASS_INCLUDED

#include "../Common/Utils.hlsl"
#include "../Common/Surface.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes {
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Interpolates {
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Interpolates UnlitPassVertex (Attributes input) {
    UNITY_SETUP_INSTANCE_ID(input);

    Interpolates output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.baseUV = SampleUV(input.baseUV, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST));
    return output;
}

float4 UnlitPassFragment (Interpolates input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
	
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 base = baseMap * baseColor;

    #ifdef _CLIPPING
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif

    return base;
}
#endif