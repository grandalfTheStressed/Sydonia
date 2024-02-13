#ifndef PIXEL_SURFACE_INCLUDED
#define PIXEL_SURFACE_INCLUDED

struct Surface {
	float2 baseUV;
	float3 position;
	float3 depth;
	float3 normal;
	float3 viewDirection;
	float3 albedo;
	
	uint renderingLayerMask;

	float dither;
	
	float alpha;
	float emission;
	
	float rimEdge;
	float rimOffset;
	
	float diffuseEdge;
	float diffuseOffset;
	
	float specularEdge;
	float specularOffset;
	
	float distanceAttenuationEdge;

	float ShadingResolution;
	float shadingOffset;
};

#endif