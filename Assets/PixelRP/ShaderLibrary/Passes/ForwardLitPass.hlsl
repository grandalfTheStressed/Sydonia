#ifndef PIXEL_LIT_PASS_INCLUDED
#define PIXEL_LIT_PASS_INCLUDED

#include "../Common/Surface.hlsl"
#include "../Common/Utils.hlsl"
#include "../Common/Lighting.hlsl"
#include "../Common/BakedGI.hlsl"
#include "../Common/LitInput.hlsl"

Interpolates LitPassVertex(Attributes input) {
	Interpolates output;

	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_SETUP_INSTANCE_ID(input);
	
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionCS_SS = TransformWorldToHClip(output.positionWS);
	output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}
float4 LitPassFragment(Interpolates input) : SV_TARGET {

	UNITY_SETUP_INSTANCE_ID(input);
	
	float4 base = GetBase(input.baseUV);
	float3 normalWS = normalize(input.normalWS);

	Surface surface;
	surface.position = input.positionWS;
	surface.normal = normalize(normalWS);
	
	surface.viewDirection = IsOrthographicCamera() ?
		-_camera_forward : normalize(_WorldSpaceCameraPos - surface.position);

	surface.NdotV = Qdot(surface.normal, surface.viewDirection);
	
	surface.depth = GetDepth(input.positionCS_SS);
	
	surface.albedo = base.rgb;
	surface.alpha = base.a;
	
	surface.rimEdge = GetRimEdge();
	surface.rimOffset = GetRimOffset();
	
	surface.diffuseEdge = GetDiffuseEdge();
	surface.diffuseOffset = GetDiffuseOffset();

	surface.specularEdge = GetSpecularEdge() / 2;
	surface.specularOffset = GetSpecularOffset() * 2;
	
	surface.shininess = exp2(10 * GetSmoothness() + 1);
	surface.dither = InterleavedGradientNoise(input.positionCS_SS.xy, 0);

	surface.emission = GetEmission();
	
	float3 color = GetLighting(surface);

	#ifdef _CLIPPING
		clip(base.a - GetCutoff());
	#endif
	
	#if defined(_PREMULTIPLY_ALPHA)
		color *= surface.alpha;
	#endif
	
	return float4(color, surface.alpha);
}
#endif