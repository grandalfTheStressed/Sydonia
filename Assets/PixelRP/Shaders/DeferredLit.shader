Shader "Pixel RP/DeferredLit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _NormalMap("NormalMap", 2D) = "bump" {}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
        _Emission("Emission", Range(0, 1)) = 0.0
        
        _RimEdge("RimEdge", Range(0, 1)) = 0.5
        _RimOffset("RimOffset", Range(0, 1)) = 0.5
        
        _DiffuseEdge("DiffuseEdge", Range(0, 1)) = 0.7
        _DiffuseOffset("DiffuseOffset", Range(0, 1)) = 0.5
        
        _SpecularEdge("SpecularEdge", Range(0, 1)) = 0.1
        _SpecularOffset("SpecularOffset", Range(0, 1)) = 0.1
        
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
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
           #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
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
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "../ShaderLibrary/Passes/ShadowCasterPass.hlsl"
			ENDHLSL
		}
    }
}
