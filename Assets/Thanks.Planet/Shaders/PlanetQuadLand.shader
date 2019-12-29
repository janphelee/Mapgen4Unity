Shader "Thanks.Planet/PlanetQuadLand"{
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
				uniform float u_light_angle_deg, u_slope, u_flat, u_c, u_d, u_outline_strength;
				uniform vec2 u_light_angle;

				varying vec2 v_tm;

				const float u_inverse_texture_size = 1.0 / 2048;
				const float Deg2Rad = 0.0174532924;

				vec2 degVector(float deg) {
					return vec2(cos(Deg2Rad*deg), sin(Deg2Rad*deg));
				}

				void main() {
					vec2 u_light_angle = degVector(u_light_angle_deg);

					float e = v_tm.x > 0.0 ? 0.5 * (v_tm.x * v_tm.x + 1.0) : 0.5 * (v_tm.x + 1.0);
					float dedx = dFdx(v_tm.x);
					float dedy = dFdy(v_tm.x);
					vec3 slope_vector = normalize(vec3(dedy, dedx, u_d * 2.0 * u_inverse_texture_size));
					vec3 light_vector = normalize(vec3(u_light_angle, mix(u_slope, u_flat, slope_vector.z)));
					float light = u_c + max(0.0, dot(light_vector, slope_vector));
					float outline = 1.0 + u_outline_strength * max(dedx, dedy);
					gl_FragColor = vec4(texture2D(u_colormap, vec2(e, v_tm.y)).rgb * light / outline, 1);
				}
				#endif
				ENDGLSL
			}
		}
}