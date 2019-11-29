// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexLandOnly"{
	Properties{
		_vertex_water("vertex_water", 2D) = "blue" {}
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
			
			sampler2D _vertex_water;//u_water

			float _outline_water;

			struct v2f {
				float4 pos:SV_POSITION;
				float2 v_em:TEXCOORD1;
				float4 v_xy:TEXCOORD2;
			};

			v2f vert(appdata_full v)
			{
				float4 a_xy = v.vertex;
				float2 a_em = v.texcoord.xy;

				v2f o;

				o.pos = UnityObjectToClipPos(a_xy);
				o.v_em = a_em;
				o.v_xy = ComputeScreenPos(o.pos);

				return o;
			}
			
			fixed4 frag(v2f IN) :COLOR
			{
				float2 v_em = IN.v_em;
				float2 v_xy = IN.v_xy.xy/IN.v_xy.w;

				float e = 0.5 * (1.0 + v_em.x);
				float river = tex2D(_vertex_water, v_xy).a;
				if (e >= 0.5) {
					float bump = _outline_water / 256.0;
					float L1 = e + bump;
					float L2 = (e - 0.5) * (bump * 100.0) + 0.5;
					// TODO: simplify equation
					e = min(L1, lerp(L1, L2, river));
				}

				return float4(frac(256.0*e), e, 0, 1);
				//return tex2D(_vertex_water, v_xy);
			}

			ENDCG
		}
	}
}