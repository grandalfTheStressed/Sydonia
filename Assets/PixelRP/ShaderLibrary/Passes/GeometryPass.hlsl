#ifndef GEOMETRY_PASS_INCLUDED
#define GEOMETRY_PASS_INCLUDED

#include "../Common/Surface.hlsl"
#include "../Common/Utils.hlsl"
#include "../Common/Lighting.hlsl"
#include "../Common/BakedGI.hlsl"
#include "../Common/LitInput.hlsl"

float4x4 _CameraProjection;

struct Fragments
{
	float4 albedo : SV_TARGET0;
	float4 normal : SV_TARGET1;
	float4 position : SV_TARGET2;
	float4 offsets : SV_TARGET3;
	float4 edges : SV_TARGET4;
	float4 highlights : SV_TARGET5;
};

Interpolates GeometryPassVertex(Attributes input) {
	Interpolates output;
	
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionCS_SS = TransformWorldToHClip(output.positionWS);
	output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}

Fragments GeometryPassFragment(Interpolates input) {

	UNITY_SETUP_INSTANCE_ID(input);
	
	float4 base = GetBase(input.baseUV);
	float3 normalWS = normalize(input.normalWS);
	
	Fragments frag;
	frag.albedo = float4(base.rgb, 1);

	frag.position = float4(input.positionWS, GetDepth(input.positionCS_SS));
	frag.normal = float4(normalWS, 0);
	frag.offsets = float4(
		GetRimOffset(),
		GetDiffuseOffset(),
		GetSpecularOffset(),
		exp2(10 * GetSmoothness() + 1));
	frag.edges = float4(
		GetRimEdge(),
		GetDiffuseEdge(),
		GetSpecularEdge(),
		InterleavedGradientNoise(input.positionCS_SS.xy, 0));
	
	frag.highlights = float4(
		GetHighlight(),
		1 - GetDarks(),
		1 + GetBrights(),
		GetEmission());
	
	return frag;
}
#endif