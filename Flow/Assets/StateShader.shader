Shader "Flow/State"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "black" {}
		_TexelWidth ("TexelWidth", float) = 0
		_TexelHeight ("TexelHeight", float) = 0

		_InputTex ("InputTex", 2D) = "black" {}
		_HeatTex ("HeatTex", 2D) = "black" {}
		_TransitionTemperature ("TransitionTemperature", float) = 0
		_Hysteresis ("Hysteresis", float) = 0
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
			float _TransitionTemperature;
			float _Hysteresis;

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

				// Sample this texture pixels
				float4 this_pixel = tex2D(_MainTex, i.uv);
				
				// Sample the input pixels
				float4 input_pixel = tex2D(_InputTex, i.uv);
				
				// Sample the heat pixels
				float4 heat_pixel = tex2D(_HeatTex, i.uv);

				// Temperature greater than 0 indicated positive transition
				// Temperature less than 0 indicated positive transition
				if(_TransitionTemperature > 0 && heat_pixel.a >  _TransitionTemperature + _Hysteresis
				|| _TransitionTemperature < 0 && heat_pixel.a < -_TransitionTemperature - _Hysteresis)
				{
					// Hot and heating or cold and cooling - this pixel is taking from the input
					this_pixel.a += input_pixel.r * 0.01;
				}
				else if(_TransitionTemperature > 0 && heat_pixel.a <  _TransitionTemperature - _Hysteresis
				     || _TransitionTemperature < 0 && heat_pixel.a > -_TransitionTemperature + _Hysteresis)
				{
					// Hot and cooling or cold and heat - this pixel is giving somewhere
					 this_pixel.a -= this_pixel.r * 0.01;
				}

				return this_pixel;
			}
			ENDCG
		}
	}
}
