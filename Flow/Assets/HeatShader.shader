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

				float avg_conductivity_e = 
				 (water_e.r * con_water 
				+ steam_e.r * con_steam 
				+ lava_e.r * con_lava 
				+ dirt_e.r * con_dirt)
							/ (water_e.r + steam_e.r + lava_e.r + dirt_e.r + small);

				float avg_conductivity_w = 
				 (water_w.r * con_water 
				+ steam_w.r * con_steam 
				+ lava_w.r * con_lava 
				+ dirt_w.r * con_dirt)
							/ (water_w.r + steam_w.r + lava_w.r + dirt_w.r + small);

				// Calculate the sum of all the elements

				
								
				float flow_e = avg_conductivity_this * avg_conductivity_e * (this_pixel.r - this_pixel_e.r) * 0.2;
				float flow_w = avg_conductivity_this * avg_conductivity_w * (this_pixel.r - this_pixel_w.r) * 0.2;
				
				//this_pixel.r = avg_conductivity_this * avg_conductivity_e + avg_conductivity_this * avg_conductivity_w;
				this_pixel.r = this_pixel.r - flow_e - flow_w;
				return this_pixel;
			}
			ENDCG
		}
	}
}
