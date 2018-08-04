Shader "Flow/Water"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 pixel = tex2D(_MainTex, i.uv);

				fixed4 pixel_n = tex2D(_MainTex, i.uv + fixed2(0,_TexelHeight));
				fixed4 pixel_e = tex2D(_MainTex, i.uv + fixed2(_TexelWidth,0));
				fixed4 pixel_s = tex2D(_MainTex, i.uv - fixed2(0,_TexelHeight));
				fixed4 pixel_w = tex2D(_MainTex, i.uv - fixed2(_TexelWidth,0));

				pixel.r = (pixel_n.r + pixel_e.r +  pixel_s.r + pixel_w.r) / 4.0;

				return pixel;
			}
			ENDCG
		}
	}
}
