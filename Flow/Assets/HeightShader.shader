Shader "Flow/Height"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0

		_DirtTex ("DirtTex", 2D) = "black" {}
		_CopperTex ("CopperTex", 2D) = "black" {}
		_ObsidianTex ("ObsidianTex", 2D) = "black" {}
		_WaterTex ("WaterTex", 2D) = "black" {}
		_LavaTex ("LavaTex", 2D) = "black" {}
		_SteamTex ("SteamTex", 2D) = "black" {}
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
			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _TexelWidth;
			float _TexelHeight;

			sampler2D _DirtTex;
			sampler2D _CopperTex;
			sampler2D _ObsidianTex;
			sampler2D _WaterTex;
			sampler2D _LavaTex;
			sampler2D _SteamTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// Sum the heights of all elements
				float4 water = tex2D(_WaterTex, i.uv);
				float4 steam = tex2D(_SteamTex, i.uv);
				float4 lava = tex2D(_LavaTex, i.uv);
				float4 dirt = tex2D(_DirtTex, i.uv);
				float4 copper = tex2D(_CopperTex, i.uv);
				float4 obsidian = tex2D(_ObsidianTex, i.uv);

				return float4(dirt.r + steam.r + lava.r + water.r + copper.r + obsidian.r, 0, 0, 0);
			}
			ENDCG
		}
	}
}
