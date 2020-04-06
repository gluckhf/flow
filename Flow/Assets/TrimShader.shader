Shader "Flow/Trim"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		_TexelWidth("TexelWidth", float) = 0
		_TexelHeight("TexelHeight", float) = 0

		_TrimTex("TrimTex", 2D) = "black" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
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

			sampler2D _TrimTex;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				// sample the trim mask and this texture
				float4 trim_pixel = tex2D(_TrimTex, i.uv);
				float4 this_pixel = tex2D(_MainTex, i.uv);

				// trim the edges by subtracting the trim pixels
				return float4(
					max(this_pixel.r - trim_pixel.r, 0.0f),
					this_pixel.g,
					this_pixel.b,
					this_pixel.a);
			}
			ENDCG
		}
	}
}
