Shader "Flow/Heat2"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_LavaTex ("LavaTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
		_CopperTex ("CopperTex", 2D) = "white" {}
		_HeightTex ("HeightTex", 2D) = "white" {}
		_HeatTex ("HeatTex", 2D) = "white" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0
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
			sampler2D _WaterTex;
			sampler2D _SteamTex;
			sampler2D _LavaTex;
			sampler2D _DirtTex;
			sampler2D _CopperTex;
			sampler2D _HeightTex;
			sampler2D _HeatTex;
			float _TexelWidth;
			float _TexelHeight;
			float _FlowDivisor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float getHeatFlow(float temperature_flow, float myCapacity, float myAmount, float theirCapacity, float theirAmount)
			{
				float small = 0.000001;

				// Figure out how much will actually flow
				float heat_flow_in = max(temperature_flow * theirCapacity * theirAmount, 0);
				float heat_flow_out = max(-temperature_flow * myCapacity * myAmount, 0);

				// Use a clever statement to select between the in/out with correct sign
				float heat_flow_final = max(heat_flow_in, 0) + min(-heat_flow_out, 0);

				return heat_flow_final;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample this texture pixels
				float4 heat_pixel = tex2D(_HeatTex, i.uv);
				float4 heat_pixel_n = tex2D(_HeatTex, i.uv + fixed2(0,_TexelHeight));
				float4 heat_pixel_e = tex2D(_HeatTex, i.uv + fixed2(_TexelWidth,0));
				float4 heat_pixel_s = tex2D(_HeatTex, i.uv - fixed2(0,_TexelHeight));
				float4 heat_pixel_w = tex2D(_HeatTex, i.uv - fixed2(_TexelWidth,0));

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


					// Calculate the capacities surrounding this pixel
					// https://en.wikipedia.org/wiki/Heat_capacity#Table_of_specific_heat_capacities
					float cap_scaling = 4.1813;
					float cap_water =  4.1813/cap_scaling; // Water
					float cap_steam =  2.0800/cap_scaling; // Water (steam)
					float cap_lava =   1.5600/cap_scaling; // Molten salt
					float cap_dirt =   0.8000/cap_scaling; // Soil
					float cap_copper = 0.3850/cap_scaling; // Copper

					float avg_capacity_this = 
						(water.r * cap_water 
					+ steam.r * cap_steam 
					+ lava.r * cap_lava 
					+ dirt.r * cap_dirt
					+ copper.r * cap_copper)
								/ max(height.r, small);

					float avg_capacity_n = 
						(water_n.r * cap_water 
					+ steam_n.r * cap_steam 
					+ lava_n.r * cap_lava 
					+ dirt_n.r * cap_dirt
					+ copper_n.r * cap_copper)
								/ max(height_n.r, small);

					float avg_capacity_e = 
						(water_e.r * cap_water 
					+ steam_e.r * cap_steam 
					+ lava_e.r * cap_lava 
					+ dirt_e.r * cap_dirt
					+ copper_e.r * cap_copper)
								/ max(height_e.r, small);

					float avg_capacity_s = 
						(water_s.r * cap_water 
					+ steam_s.r * cap_steam 
					+ lava_s.r * cap_lava 
					+ dirt_s.r * cap_dirt
					+ copper_s.r * cap_copper)
								/ max(height_s.r, small);

					float avg_capacity_w = 
						(water_w.r * cap_water 
					+ steam_w.r * cap_steam 
					+ lava_w.r * cap_lava 
					+ dirt_w.r * cap_dirt
					+ copper_w.r * cap_copper)
								/ max(height_w.r, small);

					// Calculate the temperatures surrounding this pixel
					// Temperature = heat / (average capacity * amount)
					float temperature_this = heat_pixel.r / (avg_capacity_this * height.r);
					float temperature_n = heat_pixel_n.r / (avg_capacity_n * height_n.r);
					float temperature_e = heat_pixel_e.r / (avg_capacity_e * height_e.r);
					float temperature_s = heat_pixel_s.r / (avg_capacity_s * height_s.r);
					float temperature_w = heat_pixel_w.r / (avg_capacity_w * height_w.r);

					// Calculate the flow in each direction due to temperature differences and average conductivities
					float temperature_flow_in_n = avg_conductivity_this * avg_conductivity_n * (temperature_n - temperature_this) / _FlowDivisor;
					float temperature_flow_in_e = avg_conductivity_this * avg_conductivity_e * (temperature_e - temperature_this) / _FlowDivisor;
					float temperature_flow_in_s = avg_conductivity_this * avg_conductivity_s * (temperature_s - temperature_this) / _FlowDivisor;
					float temperature_flow_in_w = avg_conductivity_this * avg_conductivity_w * (temperature_w - temperature_this) / _FlowDivisor;

					float heat_flow_in_n = getHeatFlow(temperature_flow_in_n, avg_capacity_this, height.r, avg_capacity_n, height_n.r);
					float heat_flow_in_e = getHeatFlow(temperature_flow_in_e, avg_capacity_this, height.r, avg_capacity_e, height_e.r);
					float heat_flow_in_s = getHeatFlow(temperature_flow_in_s, avg_capacity_this, height.r, avg_capacity_s, height_s.r);
					float heat_flow_in_w = getHeatFlow(temperature_flow_in_w, avg_capacity_this, height.r, avg_capacity_w, height_w.r);

					// Heat in red
					heat_pixel.r = heat_pixel.r
					+ heat_flow_in_n
					+ heat_flow_in_e
					+ heat_flow_in_s
					+ heat_flow_in_w;

					// Capacity in green
					heat_pixel.g = avg_capacity_this;

					// Conductivity in blue
					heat_pixel.b = avg_conductivity_this;

					// Temperature in alpha				
					heat_pixel.a = max(heat_pixel.r / max(height.r * avg_capacity_this, small), 0);
				}
				else
				{
					heat_pixel.r = 0;
					heat_pixel.g = 0;
					heat_pixel.b = 0;
					heat_pixel.a = 0;
				}

				return heat_pixel;
			}
			ENDCG
		}
	}
}
