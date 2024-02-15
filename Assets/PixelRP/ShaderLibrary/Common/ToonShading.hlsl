#ifndef TOON_SHADING_INCLUDED
#define TOON_SHADING_INCLUDED

#include "../Common/Surface.hlsl"
#include "./Light.hlsl"

float3 GetIncomingLightDirection(Light light)
{
    return normalize(light.direction);
}

float GetLumenocity(Light light, float NdotL)
{
    //hiding gross self shadows
    float lumenocity = NdotL * light.attenuation * light.shadowAttenuation;
    lumenocity += (1 - lumenocity) * .03;
    return lumenocity;
}

float3 GetRadiance(Light light, float lumenocity)
{
    return lumenocity * light.color;
}

float3 RimLight(Surface surface, float NdotV)
{
    float rim = surface.rimOffset * smoothstep(surface.rimEdge - 0.01, surface.rimEdge + 0.01, (1 - NdotV));
    return rim * surface.albedo;
}

float3 Specular(Surface surface, float RdotV, float lumenocity) 
{
    return pow(RdotV, surface.shininess) * surface.specularEdge * lumenocity;
}

float3 TBR(Surface surface, Light light)
{
    const float3 lightDir = GetIncomingLightDirection(light);
    const float NdotL = Qdot(surface.normal, lightDir) > 0 ? 1 : 0;
    const float3 reflection = reflect(-lightDir, surface.normal);
    const float lumenocity = GetLumenocity(light, NdotL);
    const float3 radiance = GetRadiance(light, lumenocity);
    const float NdotV = Qdot(surface.normal, surface.viewDirection);
    const float RdotV = Qdot(reflection, surface.viewDirection);

    float3 diffuse = radiance * surface.albedo;
    float3 specular = surface.specularOffset * 2 * Quantize(Specular(surface, RdotV, lumenocity), .2);
    specular = smoothstep(0.005, 0.01, specular) ;
    
    float3 rimLight = RimLight(surface, NdotV);
    
    float3 reflectance = diffuse + max(specular, rimLight);
    
    #if defined(_PREMULTIPLY_ALPHA)
	reflectance *= surface.alpha;
    #endif
    
    return reflectance;
} 

#endif
