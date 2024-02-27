#ifndef FORWARD_LIGHTING_INCLUDED
#define FORWARD_LIGHTING_INCLUDED

#include "./ToonShading.hlsl"
#include "./Light.hlsl"

Reflectance GetLighting(Surface surface, Light light) {
	return TBR(surface, light);
}

float3 GetLighting(Surface s) {
	ShadowData sd = GetShadowData(s);

	Reflectance reflectance;
	reflectance.diffuse = 0;
	reflectance.specular = 0;

	Reflectance temp;
	
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		temp = GetLighting(s, GetDirectionalLight(i, s, sd));
		reflectance.diffuse += temp.diffuse;
		reflectance.specular += temp.specular;
	}
	for(int i = 0; i < GetPunctualLightCount(); i++)
	{
		temp = GetLighting(s, GetPunctualLight(i, s, sd));
		reflectance.diffuse += temp.diffuse;
		reflectance.specular += temp.specular;
	}

	reflectance.diffuse = max(reflectance.diffuse, s.albedo * .05) * s.albedo;
	
	return (reflectance.diffuse + reflectance.specular + s.emission).rgb;
}
#endif