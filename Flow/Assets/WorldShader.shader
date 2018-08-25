Shader "Flow/World"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_LavaTex ("LavaTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
		_CopperTex ("CopperTex", 2D) = "white" {}
		_HeatTex ("HeatTex", 2D) = "white" {}
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
			sampler2D _CopperTex;
			sampler2D _HeatTex;
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
				// sample the texture
				float water = pow(tex2D(_WaterTex, i.uv).r,0.3);
				float steam = pow(tex2D(_SteamTex, i.uv).r, 0.3);
				float lava = pow(tex2D(_LavaTex, i.uv).r, 0.3);
				float dirt = pow(tex2D(_DirtTex, i.uv).r, 0.3);
				float copper = pow(tex2D(_CopperTex, i.uv).r, 0.3);
				float heat = tex2D(_HeatTex, i.uv).r;
				float temperature = tex2D(_HeatTex, i.uv).a;

				float r = copper*0.66 + dirt*0.56 + steam + lava;
				float g = copper*0.33 + dirt*0.34 + steam;
				float b =               dirt*0.23 + steam +       water*3.0;

				// Color
				if(0)
				{
					return float4(
					r, 
					g, 
					b, 
					1);
				}

				float output = temperature;

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
