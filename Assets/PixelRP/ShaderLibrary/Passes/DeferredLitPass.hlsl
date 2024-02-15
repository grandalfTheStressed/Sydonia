#ifndef PIXEL_DEFERRED_PASS_INCLUDED
#define PIXEL_DEFERRED_PASS_INCLUDED

#include "../Common/Utils.hlsl"
#include "../Common/Surface.hlsl"
#include "../Common/Lighting.hlsl"

SAMPLER(_Albedo);
SAMPLER(_Normal);
SAMPLER(_Position);
SAMPLER(_Offset);
SAMPLER(_Edge);

struct Attributes {
	float3 positionOS : POSITION;
	float2 albedoUV : TEXCOORD0;
	float2 normalUV : TEXCOORD1;
	float2 positionUV : TEXCOORD2;
	float2 offsetUV : TEXCOORD3;
	float2 edgeUV : TEXCOORD4;
};

struct Interpolates {
	float4 positionCS : SV_POSITION;
	float2 albedoUV : VAR_ALBEDO_UV;
	float2 normalUV : VAR_NORMAL_UV;
	float2 positionUV: VAR_POSITION_UV;
	float2 offsetUV : VAR_OFFSET_UV;
	float2 edgeUV: VAR_EDGE_UV;
};

Interpolates DeferredPassVertex(Attributes input) {
	Interpolates output;
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	output.albedoUV = input.albedoUV;
	output.normalUV = input.normalUV;
	output.positionUV = input.positionUV;
	output.offsetUV = input.offsetUV;
	output.edgeUV = input.edgeUV;
	return output;
}
float4 DeferredPassFragment(Interpolates input) : SV_TARGET {
	float4 albedoFrag = tex2D(_Albedo, input.albedoUV);
	float4 normalFrag = tex2D(_Normal, input.normalUV);
	float4 positionFrag = tex2D(_Position, input.positionUV);
	float4 offsetFrag = tex2D(_Offset, input.offsetUV);
	float4 edgeFrag = tex2D(_Edge, input.edgeUV);

	Surface surface;
	surface.position = positionFrag.xyz;
	surface.normal = normalFrag.xyz;
	
	if(unity_OrthoParams.w)
	{
		surface.viewDirection = -_camera_forward;
	}
	else
	{
		surface.viewDirection = normalize(_WorldSpaceCameraPos - surface.position);
	}

	surface.depth = -TransformWorldToView(surface.position).z;
	
	surface.albedo = albedoFrag.rgb;
	surface.alpha = 1;
	//surface.emission = albedoFrag.a;
	
	surface.rimEdge = edgeFrag.r;
	surface.rimOffset = offsetFrag.r;
	
	surface.diffuseEdge = edgeFrag.g;
	surface.diffuseOffset = offsetFrag.g;

	surface.specularEdge = edgeFrag.b;
	surface.specularOffset = offsetFrag.b;

	surface.shininess = offsetFrag.a;
	surface.dither = edgeFrag.a;
	
	float3 color = GetLighting(surface);
	return float4(color, albedoFrag.a);
}
#endif