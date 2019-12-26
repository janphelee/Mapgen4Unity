// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Thanks.Planet/PlanetLand"{
	Properties{
		u_colormap("u_colormap", 2D) = "blue" {}
		u_light_angle_deg("u_light_angle_deg", Range(0,360)) = 80
		u_d("u_d", Range(0,100)) = 60
		u_c("u_c", Range(0,1)) = 0.15
		u_slope("u_slope", Range(0,10)) = 6
		u_flat("u_flat", Range(0,5)) = 2.5
		u_outline_strength("u_outline_strength", Range(0.0,30.0)) = 5.0
	}
	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		pass {
			CGPROGRAM

			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D u_colormap;
			float u_light_angle;
			float u_d;
			float u_c;
			float u_slope;
			float u_flat;
			float u_outline_strength;

			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
			};

			v2f vert(appdata_full v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);

				float2 uv = v.texcoord.xy;
				o.uv = uv;
				return o;
			}

			float2 degVector(float deg){
				float Deg2Rad = 0.0174532924;
				return float2(cos(Deg2Rad*deg),sin(Deg2Rad*deg));
			}

			fixed4 frag(v2f IN) :COLOR
			{
				const float u_inverse_texture_size = 1.0/2048;

				float2 v_tm = IN.uv;

				float e = v_tm.x > 0.0? 0.5 * (v_tm.x * v_tm.x + 1.0) : 0.5 * (v_tm.x + 1.0);
				float dedx = ddx(v_tm.x);
				float dedy = ddy(v_tm.x);
				float3 slope_vector = normalize(float3(dedy, dedx, u_d * 2.0 * u_inverse_texture_size));
				float3 light_vector = normalize(float3(degVector(u_light_angle), lerp(u_slope, u_flat, slope_vector.z)));
				float light = u_c + max(0.0, dot(light_vector, slope_vector));
				float outline = 1.0 + u_outline_strength * max(dedx,dedy);

				return float4(tex2D(u_colormap, float2(e, v_tm.y)).rgb * light / outline, 1);
			}

			ENDCG
		}
	}
}