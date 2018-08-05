﻿Shader "Flow/World"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_MudTex ("MudTex", 2D) = "white" {}
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
			sampler2D _MudTex;
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
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 water = tex2D(_WaterTex, i.uv);
				fixed4 mud = tex2D(_MudTex, i.uv);
				fixed4 dirt = tex2D(_DirtTex, i.uv);
				return fixed4(dirt.r, mud.r, water.r, 1);
			}
			ENDCG
		}
	}
}
