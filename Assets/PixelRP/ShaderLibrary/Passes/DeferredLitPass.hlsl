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
SAMPLER(_Highlights);

float4 _Albedo_TexelSize;

struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
};

struct Interpolates {
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
};

Interpolates DeferredPassVertex(Attributes input) {
	Interpolates output;
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	output.baseUV = input.baseUV;
	return output;
}
float4 DeferredPassFragment(Interpolates input) : SV_TARGET {
	GBuffer buffer;
	buffer.kernelUVs = input.baseUV;
	buffer.texelSize = _Albedo_TexelSize;
	buffer.albedo = _Albedo;
	buffer.normal = _Normal;
	buffer.position = _Position;
	buffer.offset = _Offset;
	buffer.edge = _Edge;
	buffer.highlights = _Highlights;
	
	float4 albedoFrag = tex2D(_Albedo, input.baseUV);
	float4 normalFrag = tex2D(_Normal, input.baseUV);
	float4 positionFrag = tex2D(_Position, input.baseUV);
	float4 offsetFrag = tex2D(_Offset, input.baseUV);
	float4 edgeFrag = tex2D(_Edge, input.baseUV);
	float4 highlightsFrag = tex2D(_Highlights, input.baseUV);

	Surface surface;
	surface.position = positionFrag.xyz;
	surface.normal = normalFrag.xyz;

	surface.viewDirection = IsOrthographicCamera() ?
		-_camera_forward : normalize(_WorldSpaceCameraPos - surface.position);

	surface.NdotV = Qdot(surface.normal, surface.viewDirection);
	
	surface.depth = positionFrag.a;
	
	surface.albedo = albedoFrag.rgb;
	surface.alpha = albedoFrag.a;
	
	surface.rimEdge = edgeFrag.r;
	surface.rimOffset = offsetFrag.r;
	
	surface.diffuseEdge = edgeFrag.g;
	surface.diffuseOffset = offsetFrag.g;

	surface.specularEdge = edgeFrag.b;
	surface.specularOffset = offsetFrag.b;

	surface.shininess = offsetFrag.a;
	surface.dither = edgeFrag.a;
	
	surface.emission = highlightsFrag.a;

	float3 color = GetLighting(surface);

	Edges edges = EdgeDetection(buffer, surface.NdotV);

	color = edges.depthEdge == 0 ? color : color * highlightsFrag.g;
	color = edges.normalEdge == 0 ? color : color * highlightsFrag.b;

	//color = surface.NdotV;
	return float4(color, albedoFrag.a);
}
#endif