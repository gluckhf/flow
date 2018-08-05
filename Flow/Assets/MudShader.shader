Shader "Flow/Mud"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_HeightTex ("HeightTex", 2D) = "white" {}
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
			sampler2D _MainTex;
			sampler2D _HeightTex;
			float4 _MainTex_ST;
			float _TexelWidth;
			float _TexelHeight;
			float _NumElements;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				// sample the mud texture pixels
				float4 mud_pixel = tex2D(_MainTex, i.uv);
				float4 mud_pixel_n = tex2D(_MainTex, i.uv + fixed2(0,_TexelHeight));
				float4 mud_pixel_e = tex2D(_MainTex, i.uv + fixed2(_TexelWidth,0));
				float4 mud_pixel_s = tex2D(_MainTex, i.uv - fixed2(0,_TexelHeight));
				float4 mud_pixel_w = tex2D(_MainTex, i.uv - fixed2(_TexelWidth,0));

				// sample the height texture pixels
				float4 height_pixel = tex2D(_HeightTex, i.uv)* _NumElements;
				float4 height_pixel_n = tex2D(_HeightTex, i.uv + fixed2(0,_TexelHeight))* _NumElements;
				float4 height_pixel_e = tex2D(_HeightTex, i.uv + fixed2(_TexelWidth,0))* _NumElements;
				float4 height_pixel_s = tex2D(_HeightTex, i.uv - fixed2(0,_TexelHeight))* _NumElements;
				float4 height_pixel_w = tex2D(_HeightTex, i.uv - fixed2(_TexelWidth,0))* _NumElements;

				float divisor = 5 + _NumElements;

				mud_pixel.r = mud_pixel.r
				
				// North
				- max(min(height_pixel.r - height_pixel_n.r, mud_pixel.r) / divisor, 0) // assume that this cell has more "height"
				+ max(min(height_pixel_n.r - height_pixel.r, mud_pixel_n.r) / divisor, 0) // assume that the other cell has more "height"
				
				// East
				- max(min(height_pixel.r - height_pixel_e.r, mud_pixel.r) / divisor, 0) // assume that this cell has more "height"
				+ max(min(height_pixel_e.r - height_pixel.r, mud_pixel_e.r) / divisor, 0) // assume that the other cell has more "height"
				
				// South
				- max(min(height_pixel.r - height_pixel_s.r, mud_pixel.r) / divisor, 0) // assume that this cell has more "height"
				+ max(min(height_pixel_s.r - height_pixel.r, mud_pixel_s.r) / divisor, 0) // assume that the other cell has more "height"
				
				// West
				- max(min(height_pixel.r - height_pixel_w.r, mud_pixel.r) / divisor, 0) // assume that this cell has more "height"
				+ max(min(height_pixel_w.r - height_pixel.r, mud_pixel_w.r) / divisor, 0); // assume that the other cell has more "height"
				
				return mud_pixel;
			}
			ENDCG
		}
	}
}
