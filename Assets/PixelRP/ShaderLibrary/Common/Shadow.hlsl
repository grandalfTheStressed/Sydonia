#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"
#include "./Utils.hlsl"

#if defined(_DIRECTIONAL_PCF3)
    #define DIRECTIONAL_FILTER_SAMPLES 4
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_DIRECTIONAL_PCF5)
    #define DIRECTIONAL_FILTER_SAMPLES 9
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_DIRECTIONAL_PCF7)
    #define DIRECTIONAL_FILTER_SAMPLES 16
    #define DIRECTIONAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#if defined(_PUNCTUAL_PCF3)
    #define PUNCTUAL_FILTER_SAMPLES 4
    #define PUNCTUAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_3x3
#elif defined(_PUNCTUAL_PCF5)
    #define PUNCTUAL_FILTER_SAMPLES 9
    #define PUNCTUAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_5x5
#elif defined(_PUNCTUAL_PCF7)
    #define PUNCTUAL_FILTER_SAMPLES 16
    #define PUNCTUAL_FILTER_SETUP SampleShadow_ComputeSamples_Tent_7x7
#endif

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_SHADOWED_PUNCTUAL_LIGHT_COUNT 512
#define MAX_CASCADE_COUNT 4

TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
TEXTURE2D_SHADOW(_PunctualShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_Shadows)
    float _Jitter;
    float _NoiseOffset;
    int _CascadeCount;
    float4 _ShadowAtlasSize;
    float4 _ShadowDistanceFade;
    float4 _CascadeCullingSpheres[MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4x4 _PunctualShadowMatrices[MAX_SHADOWED_PUNCTUAL_LIGHT_COUNT];
    float4 _PunctualShadowTiles[MAX_SHADOWED_PUNCTUAL_LIGHT_COUNT];
CBUFFER_END

struct ShadowData {
    int cascadeIndex;
    float cascadeBlend;
    float strength;
};

struct DirectionalShadowData {
    float strength;
    int tileIndex;
    float normalBias;
    bool castShadows;
};

struct PunctualShadowData {
    float strength;
    int tileIndex;
    bool isPoint;
    bool castShadows;
    float3 lightDirection;
    float3 lightPosition;
    float3 spotDirection;
};

float FadedShadowStrength (float distance, float scale, float fade) {
    return saturate((1.0 - distance * scale) * fade);
}

ShadowData GetShadowData (Surface surface) {
    ShadowData data;
    data.cascadeBlend = 1.0;
    data.strength = FadedShadowStrength(surface.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.y);
    int i;
    for (i = 0; i < _CascadeCount; i++) {
        float4 sphere = _CascadeCullingSpheres[i];
        float distanceSqr = DistanceSquared(surface.position, sphere.xyz);
        if (distanceSqr < sphere.w) {
            float fade = FadedShadowStrength(
                distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z
            );
            if (i == _CascadeCount - 1) {
                data.strength *= FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);
            } else {
                data.cascadeBlend = fade;
            }
            break;
        }
    }

    if (i == _CascadeCount && _CascadeCount > 0) {
        data.strength = 0.0;
    }
    #if defined(_CASCADE_BLEND_DITHER)
        else if (data.cascadeBlend < surface.dither) {
            i += 1;
        }
    #endif
    #if !defined(_CASCADE_BLEND_SOFT)
        data.cascadeBlend = 1.0;
    #endif

    data.cascadeIndex = i;
    
    return data;
}

float SampleDirectionalShadowAtlas (float3 positionSTS) {
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float SamplePunctualShadowAtlas (float3 positionSTS, float3 bounds) {
    positionSTS.xy = clamp(positionSTS.xy, bounds.xy, bounds.xy + bounds.z);
    return SAMPLE_TEXTURE2D_SHADOW(_PunctualShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float FilterDirectionalShadow (float3 positionSTS) {
    #if defined(DIRECTIONAL_FILTER_SETUP)
        float weights[DIRECTIONAL_FILTER_SAMPLES];
        float2 positions[DIRECTIONAL_FILTER_SAMPLES];
        float4 size = _ShadowAtlasSize.yyxx;
    
        DIRECTIONAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
        float shadow = 0;
        for (int i = 0; i < DIRECTIONAL_FILTER_SAMPLES; i++) {
            shadow += weights[i] * SampleDirectionalShadowAtlas(float3(positions[i].xy, positionSTS.z));
        }
        return shadow;
    #else
        return SampleDirectionalShadowAtlas(positionSTS);
    #endif
}

float FilterPunctualShadow (float3 positionSTS,float3 bounds) {
    #if defined(PUNCTUAL_FILTER_SETUP)
    real weights[PUNCTUAL_FILTER_SAMPLES];
    real2 positions[PUNCTUAL_FILTER_SAMPLES];
    float4 size = _ShadowAtlasSize.wwzz;
    
    PUNCTUAL_FILTER_SETUP(size, positionSTS.xy, weights, positions);
    float shadow = 0;
    for (int i = 0; i < PUNCTUAL_FILTER_SAMPLES; i++) {
        shadow += weights[i] * SamplePunctualShadowAtlas(float3(positions[i].xy, positionSTS.z), bounds);
    }
    return shadow;
    #else
    return SamplePunctualShadowAtlas(positionSTS, bounds);
    #endif
}

float GetDirectionalShadow(DirectionalShadowData directional, ShadowData global, Surface surface)
{
    float3 normalBias = surface.normal * directional.normalBias * _CascadeData[global.cascadeIndex].y;
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex],float4(surface.position + normalBias, 1.0)).xyz;
    float shadow = FilterDirectionalShadow(positionSTS);
    if (global.cascadeBlend < 1.0) {
        normalBias = surface.normal *(directional.normalBias * _CascadeData[global.cascadeIndex + 1].y);
        positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex + 1],float4(surface.position + normalBias, 1.0)).xyz;
        shadow = lerp(FilterDirectionalShadow(positionSTS), shadow, global.cascadeBlend);
    }
    return lerp(1.0, shadow, directional.strength);
}

float GetDirectionalShadowAttenuation (DirectionalShadowData directional, ShadowData global, Surface surface) {
    // #if !defined(_RECEIVE_SHADOWS)
    // return 1.0;
    // #endif
    float shadowAttenuation = directional.castShadows ? GetDirectionalShadow(directional, global, surface) : 1.0f;

    return shadowAttenuation;
}

float GetPunctualShadow (PunctualShadowData punctual, ShadowData global, Surface surface) {

    float tileIndex = punctual.tileIndex;
    float3 lightPlane = punctual.spotDirection;
    tileIndex += punctual.isPoint ? CubeMapFaceID(-punctual.lightDirection) : 0;
    float4 tileData = _PunctualShadowTiles[tileIndex];
    float3 surfaceToLight = punctual.lightPosition - surface.position;
    float distanceToLightPlane = dot(surfaceToLight, lightPlane);
    float3 normalBias =surface.normal * (distanceToLightPlane * tileData.w);
    float4 positionSTS = mul(_PunctualShadowMatrices[tileIndex],float4(surface.position + normalBias, 1.0));
    return FilterPunctualShadow(positionSTS.xyz / positionSTS.w, tileData.xyz);
}

float GetPunctualShadowAttenuation (PunctualShadowData punctual, ShadowData global, Surface surface) {
    // #if !defined(_RECEIVE_SHADOWS)
    // return 1.0;
    // #endif
    
    float shadowAttenuation = punctual.castShadows ? GetPunctualShadow(punctual, global, surface) : 1;

    return shadowAttenuation;
}

#endif