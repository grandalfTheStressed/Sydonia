Shader "Pixel RP/SubPixelMovement"
{
    Properties
    {
        _CameraOffset ("CameraOffset", Vector) = (0,0,0,0)
        _RTSize ("Render Texture Size", Vector) = (256, 256, 0, 0) 
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _Geometry;
            float4 _Geometry_TexelSize;
            float2 _CameraOffset;
            float4 _RTSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                float2 zoomOffset = 1.0 / _RTSize.xy;
                v.uv += zoomOffset; 
                v.uv -= zoomOffset * 2.0; 

                v.uv += _CameraOffset;

                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 boxSize = (abs(ddx(i.uv)) + abs(ddy(i.uv))) * _Geometry_TexelSize.zw;
	            float2 tx = i.uv * _Geometry_TexelSize.zw;
	            float2 txOffset = clamp(frac(tx) / boxSize, 0, 0.5) - clamp((1 - frac(tx)) / boxSize, 0 ,0.5);
	            float2 uv = (float2(tx + 0.5 + txOffset) * _Geometry_TexelSize.xy);
                fixed4 col = tex2D(_Geometry, uv);
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

