#ifndef TOON_SHADING_INCLUDED
#define TOON_SHADING_INCLUDED

#include "../Common/Surface.hlsl"
#include "./Light.hlsl"

struct Reflectance
{
    float3 diffuse;
    float3 specular;
};

float3 GetIncomingLightDirection(Light light)
{
    return normalize(light.direction);
}

float GetLumenocity(Light light, float NdotL)
{
    float lumenocity = NdotL * light.attenuation;
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

Reflectance TBR(Surface surface, Light light)
{
    const float3 lightDir = GetIncomingLightDirection(light);
    
    const float NdotL = Quantize(Qdot(surface.normal, lightDir), .25);
    
    const float3 reflection = reflect(-lightDir, surface.normal);
    const float RdotV = Qdot(reflection, surface.viewDirection);
    
    const float lumenocity = GetLumenocity(light, NdotL);
    const float3 radiance = GetRadiance(light, lumenocity);
    

    float3 diffuse = radiance;
    float3 specular = Quantize(Specular(surface, RdotV, lumenocity), .2);
    specular = smoothstep(0.005, 0.01, specular);
    specular *= surface.specularOffset; 
    float3 rimLight = RimLight(surface, surface.NdotV);
    specular = max(specular, rimLight) * (radiance * 4);
    specular = min(specular, lumenocity);
    
    Reflectance reflectance;
    reflectance.diffuse = diffuse;
    reflectance.specular = specular;
    
    return reflectance;
} 

#endif
