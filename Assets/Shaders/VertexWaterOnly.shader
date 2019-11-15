// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexWaterOnly"{
	Properties{
		_MountainHeight("Mountain Height", Range(50.0,250.0)) = 50.0
	}
	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		pass {
			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			float _MountainHeight;

			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
			};

			v2f vert(appdata_full v)
			{
				v2f o;

				float2 uv = v.texcoord.xy;
				float4 pos = v.vertex;

				
				pos = float4(UnityObjectToViewPos(pos),1);
				pos += float4(0,max(uv.x, 0.0)*_MountainHeight,-pos.y,1);
				o.pos = UnityViewToClipPos(pos);

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