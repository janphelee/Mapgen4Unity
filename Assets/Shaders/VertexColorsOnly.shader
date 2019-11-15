﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexColorsOnly"{
	Properties{
		_ColorMap("Color Map", 2D) = "blue" {}
		_vertex_land("vertex_land", 2D) = "blue" {}
		_vertex_water("vertex_water", 2D) = "blue" {}

		_MountainHeight("Mountain Height", Range(0.0,250.0)) = 50.0

		_outline_water("outline_water", Range(0.0,20.0)) = 10.0
		_outline_depth("outline_depth", Range(0.0,2.0)) = 1.0
		_outline_strength("outline_strength", Range(0.0,30.0)) = 15.0
		_outline_threshold("outline_threshold", Range(0.0,100.0)) = 0.0

		_ambient("ambient", Range(0.0,1.0)) = 0.25

		_light_angle_deg("light_angle_deg", Range(0,360)) = 80
		_rotate_deg("rotate_deg", Range(-180,180)) = 0
		_overhead("overhead", Range(0,60)) = 30.0
		_slope("slope", Range(0,1)) = 0.5
		_flat("flat", Range(0,5)) = 2.5

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
		GrabPass{"_vertex_depth"}

		pass {
			CGPROGRAM
			#pragma vertex vert 
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _ColorMap;
			sampler2D _vertex_land;
			sampler2D _vertex_water;

			sampler2D _vertex_depth;

			float _MountainHeight;
			float _outline_water;

			float _ambient;
			float _overhead;
			float _light_angle_deg;
			float _rotate_deg;
			float _slope;
			float _flat;

			float _outline_depth;
			float _outline_strength;
			float _outline_threshold;

			/**
			 * v2f 结构说明
			 * float4 pos:POSITION; 其中“POSITION”指定位置变量，而不是由“pos”指定
			 * float2 uv:TEXCOORD0; TEXCOORD0 指定纹理1坐标
			 * float2 em:TEXCOORD1; TEXCOORD1 指定纹理2坐标(不能定义成 em:TEXCOORD0，会跟uv值相同)
			 */
			struct v2f {
				float4 pos:SV_POSITION;
				float2 uv:TEXCOORD0;
				float2 em:TEXCOORD1;
				float4 screenPos:TEXCOORD2;
			};

			v2f vert(appdata_full v)
			{
				v2f o;

				float2 uv = v.texcoord.xy;
				float4 pos = v.vertex;

				float2 em_xy = pos.xy / 1000.0;
				em_xy.y = 1.0 - em_xy.y;//渲染后的纹理Y轴坐标系对调

				pos = float4(UnityObjectToViewPos(pos),1);
				// unity观察系的z方向，unity观察系是右手系，
				// 其他都是本地坐标，世界坐标，投影坐标都是左手系，所以观察系轴反向
				pos += float4(0,max(uv.x, 0.0)*_MountainHeight,-pos.y,1);//增加深度-pos.y，越靠近屏幕下方越后渲染
				o.pos = UnityViewToClipPos(pos);
				///////////////////////////////////////////////////

				//float2 dx = float2(u_inverse_texture_size, 0),
				//	   dy = float2(0, u_inverse_texture_size);
				//uv.x = v_uv.y;
				o.uv = uv;
				o.em = em_xy;
				o.screenPos = ComputeScreenPos (o.pos);
				return o;
			}

			float decipher(float4 v) {
				// cg shader frag不能用外部变量?
				//const float2 _decipher = float2(1.0/256.0, 1.0);
				//return dot(_decipher, v.xy);
				return v.y;
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
				float zN = decipher(tex2D(_vertex_land, em_xy-dy));
				float zW = decipher(tex2D(_vertex_land, em_xy-dx));
				float zS = decipher(tex2D(_vertex_land, em_xy+dy));
				
				float3 slope_vector = normalize(float3(zS-zN,zE-zW,_overhead*2.0*u_inverse_texture_size));
				float3 light_vector = normalize(float3(angle(_light_angle_deg),lerp(_slope,_flat,slope_vector.z)));
				// 自定义灯光，使植被突出显示
				float light = _ambient + max(0.0, dot(light_vector, slope_vector));

				// 自定义深度，使山峰边缘突出显示
				float2 d_xy = IN.screenPos.xy/IN.screenPos.w;
				// 我用GrabPass纹理大小为窗口大小，mapgen4贴图大小2048x2048
				//dx = dx*_ScreenParams.x/2048.0;
				//dy = dy*_ScreenParams.y/2048.0;

				_outline_depth = _outline_depth * 5.0*100.0/unity_OrthoParams.y;
				_outline_threshold /= 1000.0;

				float depth0 = decipher(tex2D(_vertex_depth, d_xy)),
					  depth1 = max(max(decipher(tex2D(_vertex_depth, d_xy + _outline_depth*(-dy-dx))),
									   decipher(tex2D(_vertex_depth, d_xy + _outline_depth*(-dy+dx)))),
									   decipher(tex2D(_vertex_depth, d_xy + _outline_depth*(-dy)))),
					  depth2 = max(max(decipher(tex2D(_vertex_depth, d_xy + _outline_depth*(dy-dx))),
									   decipher(tex2D(_vertex_depth, d_xy + _outline_depth*(dy+dx)))),
									   decipher(tex2D(_vertex_depth, d_xy + _outline_depth*(dy))));
				float outline = 1.0 + _outline_strength * (max(_outline_threshold, depth2-depth0) - _outline_threshold);

				// 植被颜色
				uv.x = em.g;
				float3 biome_color = tex2D(_ColorMap, uv).rgb ;

				//return float4(biome_color * light/outline,1.0);
				//return float4(0,tex2D(_vertex_depth, d_xy).g,0,1.0);
				//return float4(0,floor(256.0*IN.uv.x)/256.0,0,1);
				return float4(biome_color/outline,1);
				//return float4(float3(1,1,1)/outline,1);
			}

			ENDCG
		}
	}
}