Shader "Flow/Height"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_LavaTex ("LavaTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0
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
			float _TexelWidth;
			float _TexelHeight;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// Sum the heights of all elements
				float4 water = tex2D(_WaterTex, i.uv);
				float4 steam = tex2D(_SteamTex, i.uv);
				float4 lava = tex2D(_LavaTex, i.uv);
				float4 dirt = tex2D(_DirtTex, i.uv);

				return float4(dirt.r + steam.r + lava.r + water.r, 0, 0, 0);
			}
			ENDCG
		}
	}
}
