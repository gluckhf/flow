Shader "Flow/World"
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
		_HeatTex ("HeatTex", 2D) = "black" {}
		_Highlite ("Highlite", int) = 0
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
			sampler2D _HeatTex;
			int _Highlite;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float dirt = pow(tex2D(_DirtTex, i.uv).r, 0.3);
				float copper = pow(tex2D(_CopperTex, i.uv).r, 0.3);
				float obsidian = pow(tex2D(_ObsidianTex, i.uv).r, 0.3);
				float water = pow(tex2D(_WaterTex, i.uv).r,0.3);
				float lava = pow(tex2D(_LavaTex, i.uv).r, 0.3);
				float steam = pow(tex2D(_SteamTex, i.uv).r, 0.3);
				float heat = tex2D(_HeatTex, i.uv).r;
				float temperature = tex2D(_HeatTex, i.uv).a;

				float r = copper*0.66 + dirt*0.56 + obsidian*0.40 + steam*0.80 + lava*1.00 + water*0.00;
				float g = copper*0.33 + dirt*0.34 + obsidian*0.40 + steam*0.80 + lava*0.00 + water*0.20;
				float b = copper*0.00 + dirt*0.23 + obsidian*0.40 + steam*0.80 + lava*0.00 + water*0.80;

				// Color
				if(_Highlite == 0)
				{
					return float4(
					r, 
					g, 
					b, 
					1);
				}

				float output = 0;

				if(_Highlite == 1) { output = dirt; }
				if(_Highlite == 2) { output = copper; }
				if(_Highlite == 3) { output = obsidian; }
				if(_Highlite == 4) { output = water; }
				if(_Highlite == 5) { output = lava; }
				if(_Highlite == 6) { output = steam; }
				if(_Highlite == 7) { output = temperature; }

				// B&W + output
				{
					float avg = (r+g+b) / 3.0;

					return float4(
					min(1.0, avg + output), 
					min(1.0, avg + output),
					avg, 
					1);
				}
			}
			ENDCG
		}
	}
}
