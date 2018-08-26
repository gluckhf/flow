Shader "Flow/HeatFlow"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0

		_WaterTex ("WaterTex", 2D) = "black" {}
		_SteamTex ("SteamTex", 2D) = "black" {}
		_LavaTex ("LavaTex", 2D) = "black" {}
		_DirtTex ("DirtTex", 2D) = "black" {}
		_CopperTex ("CopperTex", 2D) = "black" {}
		_HeightTex ("HeightTex", 2D) = "black" {}
		_FlowDivisor ("FlowDivisor", float) = 1
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

			sampler2D _WaterTex;
			sampler2D _SteamTex;
			sampler2D _LavaTex;
			sampler2D _DirtTex;
			sampler2D _CopperTex;
			sampler2D _HeightTex;
			float _FlowDivisor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float getHeatFlow(float temperature_flow, float myHeight, float theirHeight)
			{
				float small = 0.000001;

				// Figure out how much will actually flow
				float heat_flow_in = max(temperature_flow * theirHeight, 0);
				float heat_flow_out = max(-temperature_flow * myHeight, 0);

				// Use a clever statement to select between the in/out with correct sign
				float heat_flow_final = max(heat_flow_in, 0) + min(-heat_flow_out, 0);

				return heat_flow_final;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample this texture pixels
				float4 this_pixel = tex2D(_MainTex, i.uv);
				
				float4 this_pixel_n = tex2D(_MainTex, i.uv + fixed2(0,_TexelHeight));
				float4 this_pixel_e = tex2D(_MainTex, i.uv + fixed2(_TexelWidth,0));
				float4 this_pixel_s = tex2D(_MainTex, i.uv - fixed2(0,_TexelHeight));
				float4 this_pixel_w = tex2D(_MainTex, i.uv - fixed2(_TexelWidth,0));

				// Sample relevant elements
				float4 water = tex2D(_WaterTex, i.uv);
				float4 water_n = tex2D(_WaterTex, i.uv + fixed2(0,_TexelHeight));
				float4 water_e = tex2D(_WaterTex, i.uv + fixed2(_TexelWidth,0));
				float4 water_s = tex2D(_WaterTex, i.uv - fixed2(0,_TexelHeight));
				float4 water_w = tex2D(_WaterTex, i.uv - fixed2(_TexelWidth,0));

				float4 steam = tex2D(_SteamTex, i.uv);
				float4 steam_n = tex2D(_SteamTex, i.uv + fixed2(0,_TexelHeight));
				float4 steam_e = tex2D(_SteamTex, i.uv + fixed2(_TexelWidth,0));
				float4 steam_s = tex2D(_SteamTex, i.uv - fixed2(0,_TexelHeight));
				float4 steam_w = tex2D(_SteamTex, i.uv - fixed2(_TexelWidth,0));

				float4 lava = tex2D(_LavaTex, i.uv);
				float4 lava_n = tex2D(_LavaTex, i.uv + fixed2(0,_TexelHeight));
				float4 lava_e = tex2D(_LavaTex, i.uv + fixed2(_TexelWidth,0));
				float4 lava_s = tex2D(_LavaTex, i.uv - fixed2(0,_TexelHeight));
				float4 lava_w = tex2D(_LavaTex, i.uv - fixed2(_TexelWidth,0));

				float4 dirt = tex2D(_DirtTex, i.uv);
				float4 dirt_n = tex2D(_DirtTex, i.uv + fixed2(0,_TexelHeight));
				float4 dirt_e = tex2D(_DirtTex, i.uv + fixed2(_TexelWidth,0));
				float4 dirt_s = tex2D(_DirtTex, i.uv - fixed2(0,_TexelHeight));
				float4 dirt_w = tex2D(_DirtTex, i.uv - fixed2(_TexelWidth,0));

				float4 copper = tex2D(_CopperTex, i.uv);
				float4 copper_n = tex2D(_CopperTex, i.uv + fixed2(0,_TexelHeight));
				float4 copper_e = tex2D(_CopperTex, i.uv + fixed2(_TexelWidth,0));
				float4 copper_s = tex2D(_CopperTex, i.uv - fixed2(0,_TexelHeight));
				float4 copper_w = tex2D(_CopperTex, i.uv - fixed2(_TexelWidth,0));

				float4 height = tex2D(_HeightTex, i.uv);
				float4 height_n = tex2D(_HeightTex, i.uv + fixed2(0,_TexelHeight));
				float4 height_e = tex2D(_HeightTex, i.uv + fixed2(_TexelWidth,0));
				float4 height_s = tex2D(_HeightTex, i.uv - fixed2(0,_TexelHeight));
				float4 height_w = tex2D(_HeightTex, i.uv - fixed2(_TexelWidth,0));

				float small = 0.000001;
								
				if(height.r > small)
				{
					// Calculate the conductivities surrounding this pixel
					// https://en.wikipedia.org/wiki/List_of_thermal_conductivities
					float con_scaling = 4.0;
					float con_copper = 4.0 / con_scaling;
					float con_lava = 1.0 / con_scaling; //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.430.2215&rep=rep1&type=pdf
					float con_water = 0.591 / con_scaling; // Water
					float con_steam = 0.0471 / con_scaling; // Water vapor, 600°K
					float con_dirt = 0.0335 / con_scaling; // Soil, organic, dry
					
					float avg_conductivity_this = 
						(water.r * con_water 
					+ steam.r * con_steam 
					+ lava.r * con_lava 
					+ dirt.r * con_dirt
					+ copper.r * con_copper)
								/ max(height.r, small);

					float avg_conductivity_n = 
						(water_n.r * con_water 
					+ steam_n.r * con_steam 
					+ lava_n.r * con_lava 
					+ dirt_n.r * con_dirt
					+ copper_n.r * con_copper)
								/ max(height_n.r, small);

					float avg_conductivity_e = 
						(water_e.r * con_water 
					+ steam_e.r * con_steam 
					+ lava_e.r * con_lava 
					+ dirt_e.r * con_dirt
					+ copper_e.r * con_copper)
								/ max(height_e.r, small);

					float avg_conductivity_s = 
						(water_s.r * con_water 
					+ steam_s.r * con_steam 
					+ lava_s.r * con_lava 
					+ dirt_s.r * con_dirt
					+ copper_s.r * con_copper)
								/ max(height_s.r, small);

					float avg_conductivity_w = 
						(water_w.r * con_water 
					+ steam_w.r * con_steam 
					+ lava_w.r * con_lava 
					+ dirt_w.r * con_dirt
					+ copper_w.r * con_copper)
								/ max(height_w.r, small);

					// Calculate the temperatures surrounding this pixel
					// Temperature = heat / average capacity
					float temperature_this = this_pixel.r / height.r;
					float temperature_n = this_pixel_n.r / height_n.r;
					float temperature_e = this_pixel_e.r / height_e.r;
					float temperature_s = this_pixel_s.r / height_s.r;
					float temperature_w = this_pixel_w.r / height_w.r;

					// Calculate the flow in each direction dur to temperature differences and average conductivities
					float temperature_flow_in_n = avg_conductivity_this * avg_conductivity_n * (temperature_n - temperature_this) / _FlowDivisor;
					float temperature_flow_in_e = avg_conductivity_this * avg_conductivity_e * (temperature_e - temperature_this) / _FlowDivisor;
					float temperature_flow_in_s = avg_conductivity_this * avg_conductivity_s * (temperature_s - temperature_this) / _FlowDivisor;
					float temperature_flow_in_w = avg_conductivity_this * avg_conductivity_w * (temperature_w - temperature_this) / _FlowDivisor;

					float heat_flow_in_n = getHeatFlow(temperature_flow_in_n, height.r, height_n.r);
					float heat_flow_in_e = getHeatFlow(temperature_flow_in_e, height.r, height_e.r);
					float heat_flow_in_s = getHeatFlow(temperature_flow_in_s, height.r, height_s.r);
					float heat_flow_in_w = getHeatFlow(temperature_flow_in_w, height.r, height_w.r);

					// Heat in red
					this_pixel.r = this_pixel.r
					+ heat_flow_in_n
					+ heat_flow_in_e
					+ heat_flow_in_s
					+ heat_flow_in_w;

					// Conductivity in blue
					this_pixel.b = avg_conductivity_this;
					
					// Temperature in alpha				
					this_pixel.a = max(this_pixel.r / max(height.r, small), 0);
				}
				else
				{
					this_pixel.r = 0;
					this_pixel.g = 0;
					this_pixel.b = 0;
					this_pixel.a = 0;
				}
				
				return this_pixel;
			}
			ENDCG
		}
	}
}
