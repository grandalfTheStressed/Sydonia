#ifndef GEOMETRY_PASS_INCLUDED
#define GEOMETRY_PASS_INCLUDED

#include "../Common/Surface.hlsl"
#include "../Common/Utils.hlsl"
#include "../Common/Lighting.hlsl"
#include "../Common/BakedGI.hlsl"
#include "../Common/LitInput.hlsl"

float4 _Albedo_TexelSize;

float3 SnapToGrid(float3 position, float gridSize)
{
	return floor(position / gridSize) * gridSize;
}

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

	float2 uv = input.baseUV;
	
	float4 base = GetBase(uv);
	float3 normalWS = normalize(input.normalWS);
	
	Fragments frag;
	frag.albedo = float4(base.rgb, 1);
	float depth = GetDepth(input.positionCS_SS);
	frag.position = float4(input.positionWS, depth);
	frag.normal = float4(normalWS, GetEdgeId());
	frag.offsets = float4(
		GetRimOffset(),
		0,
		GetSpecularOffset(),
		exp2(10 * GetSmoothness() + 1));
	frag.edges = float4(
		GetRimEdge(),
		0,
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