Shader "Flow/World"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
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
			sampler2D _DirtTex;
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
				float water = sqrt(tex2D(_WaterTex, i.uv).r);
				float steam = sqrt(tex2D(_SteamTex, i.uv).r);
				float dirt = sqrt(tex2D(_DirtTex, i.uv).r);

				return float4(
				dirt/1.5 + steam, 
				dirt/3.0 + steam, 
				water*3 + steam, 
				1);
			}
			ENDCG
		}
	}
}
