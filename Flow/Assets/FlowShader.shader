﻿Shader "Flow/Flow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_HeightTex ("HeightTex", 2D) = "white" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0
		_NumElements ("NumElements", float) = 1
		_FlowDivisor ("FlowDivisor", float) = 1
		_FlowGradient ("FlowGradient", float) = 1
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
			sampler2D _HeightTex;
			float4 _MainTex_ST;
			float _TexelWidth;
			float _TexelHeight;
			float _NumElements;
			float _FlowDivisor;
			float _FlowGradient;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample this texture pixels
				float4 this_pixel = tex2D(_MainTex, i.uv);
				float4 this_pixel_n = tex2D(_MainTex, i.uv + fixed2(0,_TexelHeight));
				float4 this_pixel_e = tex2D(_MainTex, i.uv + fixed2(_TexelWidth,0));
				float4 this_pixel_s = tex2D(_MainTex, i.uv - fixed2(0,_TexelHeight));
				float4 this_pixel_w = tex2D(_MainTex, i.uv - fixed2(_TexelWidth,0));

				// sample the height pixels
				float4 height_pixel = tex2D(_HeightTex, i.uv) * _NumElements;
				float4 height_pixel_n = tex2D(_HeightTex, i.uv + fixed2(0,_TexelHeight)) * _NumElements;
				float4 height_pixel_e = tex2D(_HeightTex, i.uv + fixed2(_TexelWidth,0)) * _NumElements;
				float4 height_pixel_s = tex2D(_HeightTex, i.uv - fixed2(0,_TexelHeight)) * _NumElements;
				float4 height_pixel_w = tex2D(_HeightTex, i.uv - fixed2(_TexelWidth,0)) * _NumElements;
				
				// Calculate the flows
				float4 flow_n = max(min(height_pixel_n.r - (height_pixel.r + _FlowGradient), this_pixel_n.r) / _FlowDivisor, 0) // assume that the other cell has more "height"
						      - max(min((height_pixel.r + _FlowGradient) - height_pixel_n.r, this_pixel.r) / _FlowDivisor, 0); // assume that this cell has more "height"				

				float4 flow_e = max(min(height_pixel_e.r - height_pixel.r, this_pixel_e.r) / _FlowDivisor, 0) // assume that the other cell has more "height"
							  - max(min(height_pixel.r - height_pixel_e.r, this_pixel.r) / _FlowDivisor, 0); // assume that this cell has more "height"
				
				float4 flow_s = max(min((height_pixel_s.r + _FlowGradient) - height_pixel.r, this_pixel_s.r) / _FlowDivisor, 0) // assume that the other cell has more "height"
					          - max(min(height_pixel.r - (height_pixel_s.r + _FlowGradient), this_pixel.r) / _FlowDivisor, 0); // assume that this cell has more "height"
				
				float4 flow_w = max(min(height_pixel_w.r - height_pixel.r, this_pixel_w.r) / _FlowDivisor, 0) // assume that the other cell has more "height"
							  - max(min(height_pixel.r - height_pixel_w.r, this_pixel.r) / _FlowDivisor, 0); // assume that this cell has more "height"

				// For implementation of heat, keep track of what flowed in the relevant channels
				// Only keep track of northern and eastern flows, 
				// as e.g. southern flow = negative northern flow of the cell below
				this_pixel.g = 0.5 + 0.5 * flow_e;
				this_pixel.b = 0.5 + 0.5 * flow_w;

				// Keep track of the old amount of stuff in this cell
				this_pixel.a = this_pixel.r;

				// This pixel red amount is the sum of the flows
				this_pixel.r = this_pixel.r + flow_n + flow_e + flow_s + flow_w;

				return this_pixel;
			}
			ENDCG
		}
	}
}