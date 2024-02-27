#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../Common/Surface.hlsl"
#include "../Common/Utils.hlsl"
#include "../Common/Lighting.hlsl"
#include "../Common/BakedGI.hlsl"
#include "../Common/LitInput.hlsl"

Interpolates MetaPassVertex (Attributes input) {
    Interpolates output;
    input.positionOS.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.positionOS.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0;
    output.positionCS = TransformWorldToHClip(input.positionOS);
    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}

float4 MetaPassFragment (Interpolates input) : SV_TARGET {
    float4 base = GetBase(input.baseUV);
    float3 normalWS = normalize(input.normalWS);

    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.position = input.positionWS;
    surface.normal = normalize(normalWS);
	
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
	
    surface.rimEdge = GetRimEdge(input.baseUV);
    surface.rimOffset = GetRimOffset(input.baseUV);
	
    surface.diffuseEdge = GetDiffuseEdge(input.baseUV);
    surface.diffuseOffset = GetDiffuseOffset(input.baseUV);

    surface.specularEdge = GetSpecularEdge(input.baseUV) / 2;
    surface.specularOffset = GetSpecularOffset(input.baseUV) * 2;
	
    surface.shininess = exp2(10 * GetSmoothness(input.baseUV) + 1);
    surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);
    
    //float3 color = GetLighting(surface, gi);

    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x) {
        meta = float4(surface.albedo, 1.0);
        meta.rgb *= 1 - GetSmoothness(input.baseUV);
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
    return meta;
}

#endif