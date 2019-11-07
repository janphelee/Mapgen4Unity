// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexColorsOnly"{
	Properties{
		_ColorMap("Color Map", 2D) = "blue" {}
		_MountainHeight("Mountain Height", Range(50.0,250.0)) = 50.0

		_outline_water("outline_water", Range(0.0,20.0)) = 10.0
		
		_ambient("ambient", Range(0.0,1.0)) = 0.25

		_light_angle_deg("light_angle_deg", Range(0,360)) = 80
		_rotate_deg("rotate_deg", Range(-180,180)) = 0
		_overhead("overhead", Range(0,60)) = 30.0
		_slope("slope", Range(0,1)) = 0.5
		_flat("flat", Range(0,5)) = 2.5

		_vertex_land("vertex_land", 2D) = "blue" {}
	}
	SubShader{
		Tags { "RenderType" = "Opaque" "Queue" = "Geometry"}
		LOD 200

		pass {
			CGPROGRAM
			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _ColorMap;
			float _MountainHeight;
			float _outline_water;
			sampler2D _vertex_land;

			float _ambient;
			float _overhead;
			float _light_angle_deg;
			float _rotate_deg;
			float _slope;
			float _flat;

			/**
			 * v2f 结构说明
			 * float4 pos:POSITION; 其中“POSITION”指定位置变量，而不是由“pos”指定
			 * float2 uv:TEXCOORD0; TEXCOORD0 指定纹理1坐标
			 * float2 em:TEXCOORD1; TEXCOORD1 指定纹理2坐标(不能定义成 em:TEXCOORD0，会跟uv值相同)
			 */
			struct v2f {
				float4 pos:POSITION;
				float2 uv:TEXCOORD0;
				float2 em:TEXCOORD1;
			};

			v2f vert(appdata_full v)
			{
				v2f o;

				float2 uv = v.texcoord.xy;
				float4 pos = v.vertex;

				pos.z = max(uv.x, 0.0) * _MountainHeight;
				o.pos = UnityObjectToClipPos(pos);
				///////////////////////////////////////////////////

				float2 em_xy = pos.xy / 1000.0;
				em_xy.y = 1.0 - em_xy.y;//渲染后的纹理Y轴坐标系对调

				//float2 dx = float2(u_inverse_texture_size, 0),
				//	   dy = float2(0, u_inverse_texture_size);
				//uv.x = v_uv.y;
				o.uv = uv;
				o.em = em_xy;
				return o;
			}

			//为什么用dot函数不行，不知道
			//const float2 _decipher = float2(1.0/256.0, 1.0);
			float decipher(float4 v) {
				// cg shader frag不能用外部变量?
				const float2 _decipher = float2(1.0/256.0, 1.0);
				return dot(_decipher, v.xy);
			}
			float2 angle(float deg){
				float Deg2Rad = 0.0174532924;
				return float2(cos(Deg2Rad*deg),sin(Deg2Rad*deg));
			}
			
			fixed4 frag(v2f IN) :COLOR
			{
				float u_inverse_texture_size = 1.5 / 2048.0;
				//float2 sample_offset = float2(0.5*u_inverse_texture_size, 0.5*u_inverse_texture_size);
				float2 sample_offset = float2(0.5*u_inverse_texture_size, -0.5*u_inverse_texture_size);

				float2 uv = IN.uv;//color map
				float2 em_xy = IN.em + sample_offset;//land texture
				float4 em = tex2D(_vertex_land, em_xy).rgba;

				float2 dx = float2(u_inverse_texture_size, 0),
					   dy = float2(0,-u_inverse_texture_size);

				float zE = decipher(tex2D(_vertex_land, em_xy+dx));
				float zW = decipher(tex2D(_vertex_land, em_xy-dx));
				float zN = decipher(tex2D(_vertex_land, em_xy-dy));
				float zS = decipher(tex2D(_vertex_land, em_xy+dy));
				
				float3 slope_vector = normalize(float3(zS-zN,zE-zW,_overhead*2.0*u_inverse_texture_size));
				float3 light_vector = normalize(float3(angle(_light_angle_deg),lerp(_slope,_flat,slope_vector.z)));
				
				float light = _ambient + max(0.0, dot(light_vector, slope_vector));

				uv.x = em.g;
				float3 biome_color = tex2D(_ColorMap, uv).rgb ;

				return float4(biome_color * light,1.0);
			}

			ENDCG
		}
	}
}