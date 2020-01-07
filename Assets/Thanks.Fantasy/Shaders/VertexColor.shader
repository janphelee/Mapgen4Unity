// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Thanks.Fantasy/VertexColor"{
	Properties{
		u_color("u_color", Color) = (1.0, 1.0, 1.0, 0.682)
	}
		SubShader{
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Pass{
		// 开启透明混合
		Blend SrcAlpha OneMinusSrcAlpha

		GLSLPROGRAM

		#ifdef VERTEX
		varying vec4 v_color;

		void main() {
			v_color = gl_Color;
			gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
		}
		#endif

		#ifdef FRAGMENT
		varying vec4 v_color;
		uniform vec4 u_color;

		void main() {
			gl_FragColor = u_color;
		}
		#endif
		ENDGLSL
	}
	}
}