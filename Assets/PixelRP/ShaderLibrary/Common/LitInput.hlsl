#ifndef LIT_INPUT_INCLUDED
#define LIT_INPUT_INCLUDED

#include "../Common/Utils.hlsl"
#include "../Common/Lighting.hlsl"
#include "../Common/BakedGI.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _Emission)

UNITY_DEFINE_INSTANCED_PROP(float, _RimEdge)
UNITY_DEFINE_INSTANCED_PROP(float, _RimOffset)

UNITY_DEFINE_INSTANCED_PROP(float, _SpecularEdge)
UNITY_DEFINE_INSTANCED_PROP(float, _SpecularOffset)

UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)

UNITY_DEFINE_INSTANCED_PROP(float, _EdgeId)
UNITY_DEFINE_INSTANCED_PROP(float, _HighlightEdge)
UNITY_DEFINE_INSTANCED_PROP(float, _HighlightDarks)
UNITY_DEFINE_INSTANCED_PROP(float, _HighlightBrights)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes {
	float3 positionOS : POSITION;
	#ifdef _VERTEX_COLORS
		float4 color : COLOR;
	#endif
	float3 normalOS : NORMAL;
	#ifdef _FLIPBOOK_BLENDING
		float4 baseUV : TEXCOORD0;
		float flipbookBlend : TEXCOORD1;
	#else
		float2 baseUV : TEXCOORD0;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Interpolates {
	float4 positionCS_SS : SV_POSITION;
	#ifdef _VERTEX_COLORS
		float4 color : VAR_COLOR;
	#endif
	float3 normalWS : NORMAL;
	float3 positionWS : VAR_POSITION;
	float2 baseUV : VAR_BASE_UV;
	#if defined(_FLIPBOOK_BLENDING)
		float3 flipbookUVB : VAR_FLIPBOOK;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

float2 TransformBaseUV (float2 baseUV) {
	float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	return baseUV * baseST.xy + baseST.zw;
}

float4 GetBaseMap (float2 baseUV) {
	return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
}

float4 GetBaseColor () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
}

float4 GetBase (float2 baseUV) {
	float4 map = GetBaseMap(baseUV);
	float4 color = GetBaseColor();
	return map * color;
}

float GetCutoff () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetEmission () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Emission);
}

float GetRimEdge () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimEdge);
}

float GetRimOffset () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimOffset);
}

float GetSpecularEdge () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularEdge);
}

float GetSpecularOffset () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularOffset);
}

float GetSmoothness () {
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
}

float GetEdgeId()
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _EdgeId);
}

float GetHighlight()
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HighlightEdge);
}

float GetDarks()
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HighlightDarks);
}

float GetBrights()
{
	return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HighlightBrights);
}

#endif