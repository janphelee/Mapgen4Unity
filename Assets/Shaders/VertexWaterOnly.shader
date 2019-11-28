// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexWaterOnly"{
	Properties{
		_rivertexturemap("rivertexturemap", 2D) = "blue" {}
	}
	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		pass {
			Blend One OneMinusSrcAlpha
			BlendOp Add, Add

			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _rivertexturemap;

			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
			};

			v2f vert(appdata_full v)
			{
				v2f o;

				float2 uv = v.texcoord.xy;
				float4 pos = v.vertex;

				o.pos = UnityObjectToClipPos(pos);
				uv.y = 1.0-uv.y;
				o.uv = uv;

				return o;
			}
			
			fixed4 frag(v2f IN) :COLOR
			{
				float3 blue = float3(0.2, 0.5, 0.7);
				float4 c = tex2D(_rivertexturemap, IN.uv);
				return float4(blue*c.a, c.a);
			}

			ENDCG
		}
	}
}