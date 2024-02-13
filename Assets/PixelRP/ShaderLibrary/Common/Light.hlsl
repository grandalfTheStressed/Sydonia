#ifndef PIXEL_LIGHT_INCLUDED
#define PIXEL_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_PUNCTUAL_LIGHT_COUNT 512

#include "./Utils.hlsl"
#include "./Surface.hlsl"

CBUFFER_START(_Light)
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

int _PunctualLightCount;
float4 _PunctualLightColors[MAX_PUNCTUAL_LIGHT_COUNT];
float4 _PunctualLightPositions[MAX_PUNCTUAL_LIGHT_COUNT];
float4 _PunctualLightDirections[MAX_PUNCTUAL_LIGHT_COUNT];
float4 _PunctualLightSpotAngles[MAX_PUNCTUAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
	float shadowAttenuation;
};

int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

Light GetDirectionalLight(int index, Surface s) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	light.attenuation = 1;
	light.shadowAttenuation = 1;
	return light;
}

int GetPunctualLightCount()
{
	return _PunctualLightCount;
}

Light GetPunctualLight(int index, Surface s) {

	Light light;
	light.color = _PunctualLightColors[index].rgb;
	float3 position = _PunctualLightPositions[index].xyz;
	float3 ray = position - s.position;
	light.direction = normalize(ray);
	float distanceSqr = max(dot(ray, ray), 0.00001);
	float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _PunctualLightPositions[index].w)));
	float4 spotAngles = _PunctualLightSpotAngles[index];
	float spotAttenuation = Square(saturate(dot(_PunctualLightDirections[index].xyz, light.direction) * spotAngles.x + spotAngles.y));
	light.attenuation = spotAttenuation * rangeAttenuation / distanceSqr;
	light.shadowAttenuation = 1;
	return light;
}

#endif