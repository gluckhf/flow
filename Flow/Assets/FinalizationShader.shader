Shader "Flow/Finalization"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
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
				// Sample this texture pixels
				float4 this_pixel = tex2D(_MainTex, i.uv);
				
				// Add the amount changed by state
				this_pixel.r = this_pixel.r + this_pixel.a;
				this_pixel.a = 0;

				return this_pixel;
			}
			ENDCG
		}
	}
}
