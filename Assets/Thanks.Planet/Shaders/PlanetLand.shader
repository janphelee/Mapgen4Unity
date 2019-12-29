// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Thanks.Planet/PlanetLand"{
	Properties{
		u_colormap("u_colormap", 2D) = "blue" {}
	}
		SubShader{
		Pass{
			GLSLPROGRAM

			#ifdef VERTEX
			varying vec2 v_tm;

			void main() {
				v_tm = gl_MultiTexCoord0.xy;
				gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
			}
			#endif

			#ifdef FRAGMENT
			uniform sampler2D u_colormap;

			varying vec2 v_tm;

			void main() {
				float e = v_tm.x > 0.0 ? 0.5 * (v_tm.x * v_tm.x + 1.0) : 0.5 * (v_tm.x + 1.0);
				gl_FragColor = texture2D(u_colormap, vec2(e, v_tm.y));
			}
			#endif
			ENDGLSL
		}
	}
}