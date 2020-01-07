// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Thanks.Fantasy/VertexRimColor"{
	Properties{
		u_color("u_color", Color) = (1.0, 1.0, 1.0, 0.682)
		u_rim("u_rim", Range(0.0,2.0)) = 0.318
	}
		SubShader{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass{
		// 开启透明混合
		Blend SrcAlpha OneMinusSrcAlpha

		GLSLPROGRAM

		#ifdef VERTEX
		uniform float u_rim;
		varying vec4 v_color;
		varying vec3 v_normal;

		void main() {
			v_color = gl_Color;
			v_normal = gl_Normal;

			vec2 pos = gl_Normal.xy*u_rim + gl_Vertex.xy;
			gl_Position = gl_ModelViewProjectionMatrix * vec4(pos,0,1);
		}
		#endif

		#ifdef FRAGMENT
		uniform vec4 u_color;
		varying vec4 v_color;
		varying vec3 v_normal;

		void main() {
			vec4 color = u_color;
			color.a *= v_normal.z;
			gl_FragColor = color;
		}
		#endif
		ENDGLSL
	}
	}
}