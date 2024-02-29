#ifndef PIXEL_SURFACE_INCLUDED
#define PIXEL_SURFACE_INCLUDED

struct Surface {
	float3 position;
	float3 depth;
	float3 normal;
	float3 viewDirection;
	float3 albedo;

	float NdotV;

	float dither;
	
	float alpha;
	float emission;
	
	float rimEdge;
	float rimOffset;
	
	float specularEdge;
	float specularOffset;

	float shininess;
};

#endif