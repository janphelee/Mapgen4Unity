// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexLandOnly"{
	Properties{
		_outline_water("outline_water", Range(0.0,20.0)) = 10.0
	}
	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		pass {
			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			float _outline_water;

			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
			};

			float2 land(float2 uv, float river) {
				float e = 0.5 * (1.0 + uv.x);
				//float river = 0.0;
				////float river = tex2D(_Water, IN.xy).a;
				if (e >= 0.5) {
					float bump = _outline_water / 256.0;
					float L1 = e + bump;
					float L2 = (e - 0.5) * (bump * 100.0) + 0.5;
					// TODO: simplify equation
					e = min(L1, lerp(L1, L2, river));
				}
				return float2(frac(256.0*e), e);
			}

			v2f vert(appdata_full v)
			{
				float2 uv = v.texcoord.xy;
				float4 pos = v.vertex;

				v2f o;
				o.pos = UnityObjectToClipPos(pos);
				o.uv = uv;

				return o;
			}
			
			fixed4 frag(v2f IN) :COLOR
			{
				float2 e = land(IN.uv, 1.0);
				float4 c = float4(e, 0,1);
				return c;
			}

			ENDCG
		}
	}
}