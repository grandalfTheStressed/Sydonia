Shader "Pixel RP/DeferredLit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}
    	[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
    	[Toggle(_HIGHLIGHTEDGE)] _HighlightEdge("HighlightEdge", Float) = 0
    	_EdgeId("EdgeId", Range(0, 400000)) = 0
    	_HighlightBrights("HighlightBrights", Range(0, 10)) = 0.0
    	_HighlightDarks("HighlightDarks", Range(0, 1)) = 0.0
        _Emission("Emission", Range(0, 1)) = 0.0
        
        _RimEdge("RimEdge", Range(0, 1)) = 0.5
        _RimOffset("RimOffset", Range(0, 1)) = 0.5
        
        _SpecularEdge("SpecularEdge", Range(0, 1)) = 0.1
        _SpecularOffset("SpecularOffset", Range(0, 1)) = 0.1
        
    	_Smoothness("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Pass
        {
           Tags {
                "LightMode" = "Geometry"
           }

           ZWrite[_ZWrite]

           HLSLPROGRAM
           #pragma multi_compile_instancing
           #pragma vertex GeometryPassVertex
           #pragma fragment GeometryPassFragment
           #include "../ShaderLibrary/Passes/GeometryPass.hlsl" 
           ENDHLSL
        }
        Pass
        {
           ZWrite Off 
           
           Tags {
                "LightMode" = "PixelDeferredLit"
           }

           HLSLPROGRAM
           #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
           #pragma multi_compile _ _PUNCTUAL_PCF3 _PUNCTUAL_PCF5 _PUNCTUAL_PCF7
           #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
           #pragma vertex DeferredPassVertex
           #pragma fragment DeferredPassFragment
           #include "../ShaderLibrary/Passes/DeferredLitPass.hlsl" 
           ENDHLSL
        }
		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma multi_compile _ LOD_FADE_CROSSFADE
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "../ShaderLibrary/Passes/ShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
}
