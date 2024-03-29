#ifndef PIXEL_LIGHT_INCLUDED
#define PIXEL_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_PUNCTUAL_LIGHT_COUNT 512

#include "./Utils.hlsl"
#include "./Surface.hlsl"
#include "./Shadow.hlsl"

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
float4 _PunctualLightShadowData[MAX_PUNCTUAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData (int lightIndex, ShadowData shadowData) {
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.castShadows = data.strength > 0.0;
	return data;
}

Light GetDirectionalLight (int index, Surface surface, ShadowData global) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData dirShadowData = GetDirectionalShadowData(index, global);
	light.attenuation = GetDirectionalShadowAttenuation(dirShadowData, global, surface);
	return light;
}

int GetPunctualLightCount () {
	return _PunctualLightCount;
}

PunctualShadowData GetPunctualShadowData (int lightIndex) {
	PunctualShadowData data;
	data.strength = _PunctualLightShadowData[lightIndex].x;
	data.tileIndex = _PunctualLightShadowData[lightIndex].y;
	data.castShadows = _PunctualLightShadowData[lightIndex].x > 0.0;
	data.isPoint = _PunctualLightShadowData[lightIndex].z == 1.0;
	data.lightPosition = 0.0;
	data.lightDirection = 0.0;
	data.spotDirection = 0.0;
	return data;
}

Light GetPunctualLight (int index, Surface surface, ShadowData global) {
	Light light;
	light.color = _PunctualLightColors[index].rgb;
	float3 position = _PunctualLightPositions[index].xyz;
	float3 ray = position - surface.position;
	
	light.direction = normalize(ray);
	float distanceSqr = max(dot(ray, ray), 0.00001);
	float4 spotAngles = _PunctualLightSpotAngles[index];
	float3 spotDirection = _PunctualLightDirections[index].xyz;
	
	PunctualShadowData punctualShadowData = GetPunctualShadowData(index);
	punctualShadowData.lightPosition = position;
	punctualShadowData.lightDirection = light.direction;
	punctualShadowData.spotDirection = spotDirection;
	
	float shadowAttenuation = GetPunctualShadowAttenuation(punctualShadowData, global, surface);
	float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _PunctualLightPositions[index].w)));
	float spotAttenuation = Square(saturate(dot(spotDirection, light.direction) * spotAngles.x + spotAngles.y));

	light.attenuation = shadowAttenuation * spotAttenuation * rangeAttenuation / distanceSqr;
	
	return light;
}
#endif