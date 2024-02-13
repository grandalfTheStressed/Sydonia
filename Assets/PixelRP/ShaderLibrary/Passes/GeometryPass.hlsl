#ifndef GEOMETRY_PASS_INCLUDED
#define GEOMETRY_PASS_INCLUDED

#include "../Common/Utils.hlsl"

TEXTURE2D(_BaseMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_BaseMap);
SAMPLER(sampler_NormalMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _NormalMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float, _Emission)

UNITY_DEFINE_INSTANCED_PROP(float, _RimEdge)
UNITY_DEFINE_INSTANCED_PROP(float, _RimOffset)

UNITY_DEFINE_INSTANCED_PROP(float, _DiffuseEdge)
UNITY_DEFINE_INSTANCED_PROP(float, _DiffuseOffset)

UNITY_DEFINE_INSTANCED_PROP(float, _SpecularEdge)
UNITY_DEFINE_INSTANCED_PROP(float, _SpecularOffset)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 baseUV : TEXCOORD0;
	float2 normalUV : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Interpolates {
	float4 positionCS : SV_POSITION;
	float3 normalWS : NORMAL;
	float3 positionWS : VAR_POSITION;
	float2 baseUV : VAR_BASE_UV;
	float2 normalUV : VAR_NORMAL_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Fragments
{
	float4 albedo : SV_TARGET0;
	float4 normal : SV_TARGET1;
	float4 position : SV_TARGET2;
	float4 offsets : SV_TARGET3;
	float4 edges : SV_TARGET4;
};

Interpolates GeometryPassVertex(Attributes input) {
	UNITY_SETUP_INSTANCE_ID(input);

	Interpolates output;

	UNITY_TRANSFER_INSTANCE_ID(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.baseUV = SampleUV(input.baseUV, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST));
	output.normalUV = SampleUV(input.normalUV, UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalMap_ST));
	return output;
}
Fragments GeometryPassFragment(Interpolates input) {

	UNITY_SETUP_INSTANCE_ID(input);
	
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 base = baseMap * baseColor;
	float3 normalWS = normalize(input.normalWS);
	
	Fragments frag;
	frag.albedo = float4(base.rgb, 1);
	frag.position = float4(input.positionWS, 0);
	frag.normal = float4(normalize(normalWS), 0);
	frag.offsets = float4(
		UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimOffset),
		UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DiffuseOffset),
		UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularOffset),
		0);
	frag.edges = float4(
		UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimEdge),
		UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DiffuseEdge),
		UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularEdge),
		0);
	//UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Emission)
	return frag;
}
#endif