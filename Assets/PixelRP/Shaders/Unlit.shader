Shader "Pixel RP/Unlit" {
	
	Properties {
		_BaseMap("Texture", 2D) = "white" {}
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
	}
	
	SubShader {
		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			
			HLSLPROGRAM
			#pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
			#pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "../ShaderLibrary/Passes/UnlitPass.hlsl"
			ENDHLSL
		}
	}
}