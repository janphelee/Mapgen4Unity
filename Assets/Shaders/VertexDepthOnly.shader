// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexDepthOnly"{
	Properties{
	}
	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		pass {
			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
			};

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
				float v_z = IN.uv.x;
				return float4(frac(256.0*v_z),floor(256.0*v_z)/256.0,0,1);
			}

			ENDCG
		}
	}
}