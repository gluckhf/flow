Shader "Flow/Temperature"
{
	Properties
	{
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_LavaTex ("LavaTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
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

			float getHeatFlow(float temperature_flow, float myCapacity, float theirCapacity)
			{
				float small = 0.00001;

				// Figure out how much will actually flow
				float heat_flow_in = max(temperature_flow * theirCapacity, 0);
				float heat_flow_out = max(-temperature_flow * myCapacity, 0);

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

				float4 height = tex2D(_HeightTex, i.uv);
				float4 height_n = tex2D(_HeightTex, i.uv + fixed2(0,_TexelHeight));
				float4 height_e = tex2D(_HeightTex, i.uv + fixed2(_TexelWidth,0));
				float4 height_s = tex2D(_HeightTex, i.uv - fixed2(0,_TexelHeight));
				float4 height_w = tex2D(_HeightTex, i.uv - fixed2(_TexelWidth,0));

				float small = 0.0000001;
				
				// Calculate the conductivities surrounding this pixel
				float con_water = 0.6;
				float con_steam = 1.0;
				float con_lava = 0.4;
				float con_dirt = 0.1;
					
				float avg_conductivity_this = 
					(water.r * con_water 
				+ steam.r * con_steam 
				+ lava.r * con_lava 
				+ dirt.r * con_dirt)
							/ max(height.r, small);

				float avg_conductivity_n = 
					(water_n.r * con_water 
				+ steam_n.r * con_steam 
				+ lava_n.r * con_lava 
				+ dirt_n.r * con_dirt)
							/ max(height_n.r, small);

				float avg_conductivity_e = 
					(water_e.r * con_water 
				+ steam_e.r * con_steam 
				+ lava_e.r * con_lava 
				+ dirt_e.r * con_dirt)
							/ max(height_e.r, small);

				float avg_conductivity_s = 
					(water_s.r * con_water 
				+ steam_s.r * con_steam 
				+ lava_s.r * con_lava 
				+ dirt_s.r * con_dirt)
							/ max(height_s.r, small);

				float avg_conductivity_w = 
					(water_w.r * con_water 
				+ steam_w.r * con_steam 
				+ lava_w.r * con_lava 
				+ dirt_w.r * con_dirt)
							/ max(height_w.r, small);


				// Calculate the capacities surrounding this pixel
				float cap_water = 1.0;
				float cap_steam = 1.0;
				float cap_lava = 1.0;
				float cap_dirt = 1.0;
					
				float avg_capacity_this = 
					(water.r * cap_water 
				+ steam.r * cap_steam 
				+ lava.r * cap_lava 
				+ dirt.r * cap_dirt)
							/ max(height.r, small);

				float avg_capacity_n = 
					(water_n.r * cap_water 
				+ steam_n.r * cap_steam 
				+ lava_n.r * cap_lava 
				+ dirt_n.r * cap_dirt)
							/ max(height_n.r, small);

				float avg_capacity_e = 
					(water_e.r * cap_water 
				+ steam_e.r * cap_steam 
				+ lava_e.r * cap_lava 
				+ dirt_e.r * cap_dirt)
							/ max(height_e.r, small);

				float avg_capacity_s = 
					(water_s.r * cap_water 
				+ steam_s.r * cap_steam 
				+ lava_s.r * cap_lava 
				+ dirt_s.r * cap_dirt)
							/ max(height_s.r, small);

				float avg_capacity_w = 
					(water_w.r * cap_water 
				+ steam_w.r * cap_steam 
				+ lava_w.r * cap_lava 
				+ dirt_w.r * cap_dirt)
							/ max(height_w.r, small);

				// Calculate the temperatures surrounding this pixel
				// Temperature = heat / average capacity
				float temperature_this = heat_pixel.r / avg_capacity_this;
				float temperature_n = heat_pixel_n.r / avg_capacity_n;
				float temperature_e = heat_pixel_e.r / avg_capacity_e;
				float temperature_s = heat_pixel_s.r / avg_capacity_s;
				float temperature_w = heat_pixel_w.r / avg_capacity_w;

				// Calculate the flow in each direction dur to temperature differences and average conductivities
				float temperature_flow_in_n = avg_conductivity_this * avg_conductivity_n * (temperature_n - temperature_this) / _FlowDivisor;
				float temperature_flow_in_e = avg_conductivity_this * avg_conductivity_e * (temperature_e - temperature_this) / _FlowDivisor;
				float temperature_flow_in_s = avg_conductivity_this * avg_conductivity_s * (temperature_s - temperature_this) / _FlowDivisor;
				float temperature_flow_in_w = avg_conductivity_this * avg_conductivity_w * (temperature_w - temperature_this) / _FlowDivisor;

				float heat_flow_in_n = getHeatFlow(temperature_flow_in_n, avg_capacity_this, avg_capacity_n);
				float heat_flow_in_e = getHeatFlow(temperature_flow_in_e, avg_capacity_this, avg_capacity_e);
				float heat_flow_in_s = getHeatFlow(temperature_flow_in_s, avg_capacity_this, avg_capacity_s);
				float heat_flow_in_w = getHeatFlow(temperature_flow_in_w, avg_capacity_this, avg_capacity_w);

				heat_pixel.r = heat_pixel.r
				+ heat_flow_in_n
				+ heat_flow_in_e
				+ heat_flow_in_s
				+ heat_flow_in_w;
				
				return heat_pixel;
			}
			ENDCG
		}
	}
}
