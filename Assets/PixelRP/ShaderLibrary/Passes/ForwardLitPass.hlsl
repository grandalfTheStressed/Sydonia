#ifndef PIXEL_LIT_PASS_INCLUDED
#define PIXEL_LIT_PASS_INCLUDED

#include "../Common/Surface.hlsl"
#include "../Common/Utils.hlsl"
#include "../Common/Lighting.hlsl"

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

UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)

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

Interpolates LitPassVertex(Attributes input) {
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
float4 LitPassFragment(Interpolates input) : SV_TARGET {

	UNITY_SETUP_INSTANCE_ID(input);
	
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 base = baseMap * baseColor;
	float4 bump = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, input.baseUV) * .5 + .5;
	float3 normalWS = normalize(input.normalWS);

	Surface surface;
	surface.baseUV = input.baseUV;
	surface.position = input.positionWS;
	surface.normal = normalize(normalWS * bump);
	
	if(unity_OrthoParams.w)
	{
		surface.viewDirection = -_camera_forward;
	}
	else
	{
		surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	}

	surface.depth = -TransformWorldToView(input.positionWS).z;
	
	surface.albedo = base.rgb;
	surface.alpha = base.a;
	
	surface.rimEdge = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimEdge);
	surface.rimOffset = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _RimOffset);
	
	surface.diffuseEdge = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DiffuseEdge);
	surface.diffuseOffset = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DiffuseOffset);

	surface.specularEdge = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularEdge) / 2;
	surface.specularOffset = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _SpecularOffset) * 2;
	
	surface.shininess = exp2(10 * UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness) + 1);
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
	#ifdef _CLIPPING
	clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#endif
	float3 color = GetLighting(surface);
	return float4(color, surface.alpha);
}
#endif