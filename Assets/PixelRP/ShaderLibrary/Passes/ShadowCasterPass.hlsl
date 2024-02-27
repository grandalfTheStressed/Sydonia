#ifndef SHADOW_CASTER_PASS_INCLUDED
#define SHADOW_CASTER_PASS_INCLUDED

#include "../ShaderLibrary/Common/Utils.hlsl"
#include "../Common/LitInput.hlsl"

bool _ShadowPancaking;

Interpolates ShadowCasterPassVertex (Attributes input) {
	Interpolates output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS_SS = TransformWorldToHClip(positionWS);
	output.baseUV = TransformBaseUV(input.baseUV);

	if (_ShadowPancaking) {
		#if UNITY_REVERSED_Z
		output.positionCS_SS.z = min(output.positionCS_SS.z, output.positionCS_SS.w * UNITY_NEAR_CLIP_VALUE);
		#else
		output.positionCS.z = max(output.positionCS.z, output.positionCS.w * UNITY_NEAR_CLIP_VALUE);
		#endif
	}
	return output;
}

void ShadowCasterPassFragment (Interpolates input) {
	UNITY_SETUP_INSTANCE_ID(input);
	float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
	float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
	float4 base = baseMap * baseColor;
	#if defined(_SHADOWS_CLIP)
		clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
	#elif defined(_SHADOWS_DITHER)
		float dither = InterleavedGradientNoise(input.positionCS_SS.xy, 0);
		clip(base.a - dither);
	#elif defined(_SHADOWS_OFF)
		clip(-1);
	#endif
}

#endif