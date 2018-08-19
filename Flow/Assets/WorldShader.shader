Shader "Flow/World"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_LavaTex ("LavaTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
		_HeatTex ("HeatTex", 2D) = "white" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0
		_NumElements ("NumElements", float) = 1
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

			// Must be redeclared from Properties to be able to be used
			sampler2D _WaterTex;
			sampler2D _SteamTex;
			sampler2D _LavaTex;
			sampler2D _DirtTex;
			sampler2D _HeatTex;
			float _TexelWidth;
			float _TexelHeight;
			float _NumElements;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float water = pow(tex2D(_WaterTex, i.uv).r,0.3);
				float steam = pow(tex2D(_SteamTex, i.uv).r, 0.3);
				float lava = pow(tex2D(_LavaTex, i.uv).r, 0.3);
				float dirt = pow(tex2D(_DirtTex, i.uv).r, 0.3);
				float heat = pow(tex2D(_HeatTex, i.uv).r, 0.3);

				float r = dirt/1.5 + steam + lava;
				float g = dirt/3.0 + steam;
				float b = water*3.0 + steam;

				// Color
				if(0)
				{
					return float4(
					r, 
					g, 
					b, 
					1);
				}

				// B&W + Heat
				{
					float avg = r+g+b / 10.0;
				
					return float4(
					avg + heat, 
					avg + heat, 
					avg, 
					1);
				}
			}
			ENDCG
		}
	}
}
