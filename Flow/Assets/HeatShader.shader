Shader "Flow/Heat"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_WaterTex ("WaterTex", 2D) = "white" {}
		_SteamTex ("SteamTex", 2D) = "white" {}
		_LavaTex ("LavaTex", 2D) = "white" {}
		_DirtTex ("DirtTex", 2D) = "white" {}
		_HeightTex ("HeightTex", 2D) = "white" {}
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
			sampler2D _MainTex;
			sampler2D _WaterTex;
			sampler2D _SteamTex;
			sampler2D _LavaTex;
			sampler2D _DirtTex;
			sampler2D _HeightTex;
			float4 _MainTex_ST;
			float _TexelWidth;
			float _TexelHeight;

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

				// Heat movement
				float cap_water = 1.0;
				float cap_steam = 1.0;
				float cap_lava = 1.0;
				float cap_dirt = 1.0;

				float small = 0.001;

				/*
				// Calculate the heat flows inward in each direction
				float flow_in_east = 
				((-1.0 + 2.0 * water.g)* cap_water
				+(-1.0 + 2.0 * steam.g)* cap_steam
				+(-1.0 + 2.0 * lava.g )* cap_lava 
				+(-1.0 + 2.0 * dirt.g )* cap_dirt);
				
				float heat_flow_in_east = max(flow_in_east * this_pixel_e.r, 0)		// Assume flow outgoing, take this cells temperature
				                         + min(flow_in_east * this_pixel.r, 0);	// Assume flow incoming, take other cells temperature
				
				float flow_in_west =   // Western cells in from east is this cells out from west. Negative that is this cells in from west.
				((-1.0 + 2.0 * water.b) * cap_water
				+(-1.0 + 2.0 * steam.b)* cap_steam
				+(-1.0 + 2.0 * lava.b )* cap_lava 
				+(-1.0 + 2.0 * dirt.b )* cap_dirt);
				
				float heat_flow_in_west = max(flow_in_west * this_pixel_w.r, 0)		// Assume flow incoming, take this cells temperature
				                         + min(flow_in_west * this_pixel.r, 0);	// Assume flow outgoing, take other cells temperature
				
				// Calculate the old heat of the cells
				float heat = this_pixel.r * 
				(water.a * cap_water 
				+ steam.a * cap_steam 
				+ lava.a * cap_lava 
				+ dirt.a * cap_dirt);

				// Temperature is heat divided by capacities
				this_pixel.r =
				(heat + heat_flow_in_east + heat_flow_in_west) /
				max(
				water.r * cap_water 
				+ steam.r * cap_steam 
				+ lava.r * cap_lava 
				+ dirt.r * cap_dirt
				, small);
				*/







				/*
				//

				float heat_e = this_pixel_e.r * 
				(water_e.a * cap_water 
				+ steam_e.a * cap_steam 
				+ lava_e.a * cap_lava 
				+ dirt_e.a * cap_dirt);

				float heat_w = this_pixel_w.r * 
				(water_w.a * cap_water 
				+ steam_w.a * cap_steam 
				+ lava_w.a * cap_lava 
				+ dirt_w.a * cap_dirt);


				// Calculating the conductivities surrounding this pixel
				float con_water = 0.6;
				float con_steam = 1.0;
				float con_lava = 0.4;
				float con_dirt = 0.1;

				float small = 0.0000001;

				float avg_conductivity_this = 
				 (water.r * con_water 
				+ steam.r * con_steam 
				+ lava.r * con_lava 
				+ dirt.r * con_dirt)
							/ (water.r + steam.r + lava.r + dirt.r + small);

				float avg_conductivity_n = 
				 (water_n.r * con_water 
				+ steam_n.r * con_steam 
				+ lava_n.r * con_lava 
				+ dirt_n.r * con_dirt)
							/ (water_n.r + steam_n.r + lava_n.r + dirt_n.r + small);

				float avg_conductivity_e = 
				 (water_e.r * con_water 
				+ steam_e.r * con_steam 
				+ lava_e.r * con_lava 
				+ dirt_e.r * con_dirt)
							/ (water_e.r + steam_e.r + lava_e.r + dirt_e.r + small);

				float avg_conductivity_s = 
				 (water_s.r * con_water 
				+ steam_s.r * con_steam 
				+ lava_s.r * con_lava 
				+ dirt_s.r * con_dirt)
							/ (water_s.r + steam_s.r + lava_s.r + dirt_s.r + small);

				float avg_conductivity_w = 
				 (water_w.r * con_water 
				+ steam_w.r * con_steam 
				+ lava_w.r * con_lava 
				+ dirt_w.r * con_dirt)
							/ (water_w.r + steam_w.r + lava_w.r + dirt_w.r + small);

				// Calculate the sum of all the elements
				float flow_n = avg_conductivity_this * avg_conductivity_n * (this_pixel.r - this_pixel_n.r) * 0.2;
				float flow_e = avg_conductivity_this * avg_conductivity_e * (this_pixel.r - this_pixel_e.r) * 0.2;
				float flow_s = avg_conductivity_this * avg_conductivity_s * (this_pixel.r - this_pixel_s.r) * 0.2;
				float flow_w = avg_conductivity_this * avg_conductivity_w * (this_pixel.r - this_pixel_w.r) * 0.2;

				this_pixel.r = this_pixel.r - flow_n - flow_e - flow_s - flow_w;
				*/


				return this_pixel;
			}
			ENDCG
		}
	}
}
