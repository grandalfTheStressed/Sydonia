#ifndef  PIXEL_UNLIT_PASS_INCLUDED
#define PIXEL_UNLIT_PASS_INCLUDED

#include "../Common/Utils.hlsl"
#include "../Common/Surface.hlsl"
#include "../Common/LitInput.hlsl"

Interpolates UnlitPassVertex (Attributes input) {
    UNITY_SETUP_INSTANCE_ID(input);

    Interpolates output;

    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionWS = TransformObjectToWorld(input.positionOS);
    output.positionCS_SS = TransformWorldToHClip(output.positionWS);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    #ifdef _VERTEX_COLORS
        output.color = input.color;
    #endif
    
    output.baseUV.xy = TransformBaseUV(input.baseUV.xy);
    
    #ifdef _FLIPBOOK_BLENDING
        output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
        output.flipbookUVB.z = input.flipbookBlend;
    #endif
    
    return output;
}

float4 UnlitPassFragment (Interpolates input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
	
    float4 baseMap = GetBaseMap(input.baseUV);
    float4 baseColor = GetBaseColor();

    #ifdef _FLIPBOOK_BLENDING
        baseMap = lerp(baseMap, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.flipbookUVB.xy), input.flipbookUVB.z);
    #endif
    
    float4 base = baseMap * baseColor;
    
    #ifdef _VERTEX_COLORS
        base *= input.color;
    #endif
    
    #ifdef _CLIPPING
        clip(base.a - GetCutoff(input.baseUV));
    #endif

    #ifdef _PREMULTIPLY_ALPHA
        base *= base.a;
    #endif
    
    return base;
}
#endif