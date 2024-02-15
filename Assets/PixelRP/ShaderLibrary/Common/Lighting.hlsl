#ifndef FORWARD_LIGHTING_INCLUDED
#define FORWARD_LIGHTING_INCLUDED

#include "./ToonShading.hlsl"
#include "./Light.hlsl"

float3 GetLighting(Surface surface, Light light) {
	return TBR(surface, light);
}

float3 GetLighting(Surface s) {
	ShadowData sd = GetShadowData(s);
	float3 color = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		color += GetLighting(s, GetDirectionalLight(i, s, sd));
	}
	for(int i = 0; i < GetPunctualLightCount(); i++)
	{
		color += GetLighting(s, GetPunctualLight(i, s));
	}
	return color;
}
#endif