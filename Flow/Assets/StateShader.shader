Shader "Flow/State"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0

		_InputTex ("InputTex", 2D) = "black" {}
		_HeatTex ("HeatTex", 2D) = "black" {}
		_TransitionHotTemperature ("TransitionHotTemperature", float) = 0
		_TransitionColdTemperature ("TransitionColdTemperature", float) = 0
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

			sampler2D _InputTex;
			sampler2D _HeatTex;
			float _TransitionHotTemperature;
			float _TransitionColdTemperature;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				float small = 0.000001;

				// sample this texture pixels
				float4 this_pixel = tex2D(_MainTex, i.uv);
				
				// sample the input pixels
				float4 input_pixel = tex2D(_InputTex, i.uv);
				
				// sample the heat pixels
				float4 heat_pixel = tex2D(_HeatTex, i.uv);

				if(heat_pixel.a > _TransitionHotTemperature)
				{
					float amount_to_shift = input_pixel.r * 0.01;
					this_pixel.r = this_pixel.r + amount_to_shift;
					this_pixel.a = 0.5 + 0.5 * -amount_to_shift;
				}
				else if(heat_pixel.a < _TransitionColdTemperature)
				{
					float amount_to_shift = -this_pixel.r * 0.01;
					this_pixel.r = this_pixel.r + amount_to_shift;
					this_pixel.a = 0.5 + 0.5 * -amount_to_shift;
				}
				else
				{
					this_pixel.a = 0.5;
				}

				return this_pixel;
			}
			ENDCG
		}
	}
}
