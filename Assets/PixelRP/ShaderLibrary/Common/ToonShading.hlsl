#ifndef TOON_SHADING_INCLUDED
#define TOON_SHADING_INCLUDED

#include "../Common/Surface.hlsl"
#include "./Light.hlsl"

float3 GoochColor(float3 color, float NdotL, float warmIntensity, float coolIntensity)
{
    const float3 warm = float3(.8, .8, 0) * warmIntensity;
    const float3 cool = float3(0, 0, 1) * coolIntensity;
    
    float3 kWarm = (warm + (1 - warmIntensity) * color);
    float3 kCool = (cool + (1 - coolIntensity) * color);
    
    return max(NdotL * kWarm, (1 - NdotL) * kCool);
}

float3 GetIncomingLightDirection(Light light)
{
    return normalize(light.direction);
}

float3 GetLumenocity(Light light, float NdotL)
{
    return NdotL * light.attenuation * light.shadowAttenuation;
}

float3 GetRadiance(Light light, float NdotL)
{
    return GetLumenocity(light, NdotL) * light.color;
}

float3 GetRadiance(Light light, float3 lumenocity)
{
    return lumenocity * light.color;
}

float3 RimLight(Surface surface, float NdotV)
{
    return (1 - surface.rimOffset) * saturate(1 - NdotV * 2 * surface.rimEdge);
}

float3 Specular(Surface surface, float RdotV) 
{
    float specular = RdotV * surface.specularEdge;
    return specular;
}

float3 TBR(Surface surface, Light light)
{
    const float lightDir = GetIncomingLightDirection(light);
    const float NdotL = Qdot(surface.normal, lightDir) * .5 + .5;
    const float3 reflection = reflect(-lightDir, surface.normal);
    const float3 lumenocity = GetLumenocity(light, NdotL);
    const float3 radiance = GetRadiance(light, lumenocity);
    const float NdotV = Qdot(surface.normal, surface.viewDirection);
    const float RdotV = Qdot(reflection, surface.viewDirection);
    float3 diffuse = GoochColor(surface.albedo, NdotL, 0, 0);
    
    //float3 rimLight = RimLight(surface, NdotV);
    float3 specular = surface.specularOffset * 2 * Quantize(Specular(surface, RdotV), .2);
    //specular = Quantize(specular, surface.specularEdge);
    //specular = max(specular, rimLight);
    
    float3 reflectance = (.7 * diffuse + specular * .3) * radiance;
    
    #if defined(_PREMULTIPLY_ALPHA)
	reflectance *= surface.alpha;
    #endif
    
    return reflectance;
} 

#endif
