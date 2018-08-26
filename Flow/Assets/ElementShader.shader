Shader "Flow/Element"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0

		_HeightTex ("HeightTex", 2D) = "black" {}
		_HeatTex ("HeatTex", 2D) = "black" {}
		_FlowDivisor ("FlowDivisor", float) = 5.0
		_FlowGradient ("FlowGradient", float) = 0.0
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

			sampler2D _HeightTex;
			sampler2D _HeatTex;
			float _FlowDivisor;
			float _FlowGradient;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float getFlow(float myHeight, float myAmount, float theirHeight, float theirAmount)
			{
				float small = 0.000001;
				
				// Calculate how much will flow in percentage
				// e.g. 1/3 of my height should flow out
				// One of these will be negative, or both zero if equal heights
				float percent_flow_in = (theirHeight - myHeight) / max(theirHeight, small);
				float percent_flow_out = (myHeight - theirHeight) / max(myHeight, small);

				// Figure out how much will actually flow - max 1.0 in each cell
				float flow_amount_in = min(percent_flow_in * theirAmount, 1.0 - myAmount);
				float flow_amount_out = min(percent_flow_out * myAmount, 1.0 - theirAmount);

				// Use a clever statement to select between the in/out with correct sign
				float flow_in_final = max(flow_amount_in, 0) + min(-flow_amount_out, 0);

				// Scale by the flow divisor
				return flow_in_final / _FlowDivisor;
			}

			float getHeat(float flow, float myHeat, float myHeight, float theirHeat, float theirHeight)
			{
				float small = 0.000001;

				// Calculate how much will flow in percentage of the total height
				// One of these will be negative, or both zero if equal heights
				float percent_flow_in = max(flow / max(theirHeight, small), 0);
				float percent_flow_out = max(-flow / max(myHeight, small), 0);

				// Figure out how much will actually flow
				float heat_amount_in = percent_flow_in * max(theirHeat, 0);
				float heat_amount_out = percent_flow_out * max(myHeat, 0);

				// Use a clever statement to select between the in/out with correct sign
				float heat_in_final = max(heat_amount_in, 0) + min(-heat_amount_out, 0);

				return heat_in_final;
			}

			float4 frag (v2f i) : SV_Target
			{
				// sample this texture pixels
				float4 this_pixel = tex2D(_MainTex, i.uv);

				// Only flow if the element is not solid
				if(_FlowDivisor > 1.0f)
				{
					float small = 0.000001;

					float4 this_pixel_n = tex2D(_MainTex, i.uv + fixed2(0,_TexelHeight));
					float4 this_pixel_e = tex2D(_MainTex, i.uv + fixed2(_TexelWidth,0));
					float4 this_pixel_s = tex2D(_MainTex, i.uv - fixed2(0,_TexelHeight));
					float4 this_pixel_w = tex2D(_MainTex, i.uv - fixed2(_TexelWidth,0));

					// sample the height pixels
					float4 height_pixel = tex2D(_HeightTex, i.uv);
					float4 height_pixel_n = tex2D(_HeightTex, i.uv + fixed2(0,_TexelHeight));
					float4 height_pixel_e = tex2D(_HeightTex, i.uv + fixed2(_TexelWidth,0));
					float4 height_pixel_s = tex2D(_HeightTex, i.uv - fixed2(0,_TexelHeight));
					float4 height_pixel_w = tex2D(_HeightTex, i.uv - fixed2(_TexelWidth,0));
				
					// sample the heat pixels
					float4 heat_pixel = tex2D(_HeatTex, i.uv);
					float4 heat_pixel_n = tex2D(_HeatTex, i.uv + fixed2(0,_TexelHeight));
					float4 heat_pixel_e = tex2D(_HeatTex, i.uv + fixed2(_TexelWidth,0));
					float4 heat_pixel_s = tex2D(_HeatTex, i.uv - fixed2(0,_TexelHeight));
					float4 heat_pixel_w = tex2D(_HeatTex, i.uv - fixed2(_TexelWidth,0));

					// Calculate the inward flows
					// Modify north/south by the gradient in order to give "gravity"
				
					float flow_n = getFlow(max(height_pixel.r + _FlowGradient, 0),
										   this_pixel.r,
										   height_pixel_n.r,
										   this_pixel_n.r);

					float flow_e = getFlow(height_pixel.r,
										   this_pixel.r,
										   height_pixel_e.r,
										   this_pixel_e.r);

					float flow_s = getFlow(height_pixel.r,
										   this_pixel.r,
										   max(height_pixel_s.r + _FlowGradient, 0),
										   this_pixel_s.r);

					float flow_w = getFlow(height_pixel.r,
										   this_pixel.r,
										   height_pixel_w.r,
										   this_pixel_w.r);

				
					// For implementation of heat, keep track of the percentage heat flow
					// GREEN = INWARD NORTH
					this_pixel.g = 0.5 + 0.5 * getHeat(flow_n, heat_pixel.r, height_pixel.r, heat_pixel_n.r, height_pixel_n.r);

					// BLUE = INWARD EAST
					this_pixel.b = 0.5 + 0.5 * getHeat(flow_e, heat_pixel.r, height_pixel.r, heat_pixel_e.r, height_pixel_e.r);
								
					// This pixel red amount is the sum of the flows
					this_pixel.r = this_pixel.r + flow_n + flow_e + flow_s + flow_w;

					// This pixel alpha channel is used by the state change shaders

					if(this_pixel.r < small)
					{
						this_pixel.r = 0;
					}
				}
				return this_pixel;
			}
			ENDCG
		}
	}
}
