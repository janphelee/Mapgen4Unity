// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "MG/Cartoon/Rim" 
{

	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		
		_RimColor ("Rim Color", Color) = (0.26,0.19,0.16,0.0)
		_RimPower ("Rim Power", Range(0.5,8.0)) = 3.0
	    
	    _Shininess ("Shininess", Range (0.03, 64.0)) = 48.0
	}

	SubShader 
	{
		Tags { "LightMode"="ForwardBase" }
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
		Tags { "Reflection" = "RenderReflectionCartoonRim" }

		pass
		{
			cull off
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _RimColor;
			float _RimPower;			
			float _Shininess;

			float4 _MGCartoonColor;
			float4 _MGAmbientColor;

			struct v2f 
			{
				float4 pos:SV_POSITION;
				float2 uv:TEXCOORD0;
				float3 lightDir:TEXCOORD1;
				float3 viewDir:TEXCOORD2;
				
				float3 normal:TEXCOORD3;
			};

			v2f vert (appdata_full v) {
				v2f o;
				o.pos=UnityObjectToClipPos(v.vertex);
				o.uv=v.texcoord.xy;

				//TANGENT_SPACE_ROTATION;
				o.lightDir=ObjSpaceLightDir(v.vertex);
				o.viewDir=ObjSpaceViewDir(v.vertex);
				o.normal=v.normal;
				return o;
			}
			
			float4 frag(v2f i):COLOR
			{
				float4 c=tex2D(_MainTex, i.uv);
				float3 N=normalize(i.normal);
				c.rgb = c.rgb;
				
				float diff=max(_MGAmbientColor,dot(N,i.lightDir));

				c=c*(diff+_MGAmbientColor)*_MGCartoonColor;
				
				half rim = 1.0 - saturate(dot (normalize(i.viewDir), N));
    			c.rgb += _RimColor.rgb * pow (rim, _RimPower);
    			
    			//c = c+UNITY_LIGHTMODEL_AMBIENT+unity_AmbientGround+unity_AmbientSky;
    			//c = c+UNITY_LIGHTMODEL_AMBIENT;
				
				return c;
			}
			ENDCG
		} 
	}

FallBack "Legacy Shaders/Diffuse"
}
