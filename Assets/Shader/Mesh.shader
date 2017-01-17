Shader "QQ/Mesh"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{ 
		Tags{ "RenderType" = "Opaque" }
		CGINCLUDE
		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include"./ComputeBuffer.cginc"
		uniform StructuredBuffer<Particle> _Particles;
		uniform float _IdOffset;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		int GetID(float2 uv)
		{
			return uv.x + 0.5 + _IdOffset;
		}
		ENDCG

		Pass
		{
			Tags{ "LightMode" = "ForwardBase" }
			ZTest LEqual
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase_fullshadows
			#pragma multi_compile_fog
			uniform float4 _LightColor0;
			struct a2v
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float2 id : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
				float4 color : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				LIGHTING_COORDS(3, 4)
			};
			
			v2f vert (a2v v)
			{
				Particle p = _Particles[GetID(v.id)];
				v.vertex.xyz *= p.scale;
				v.vertex.xyz += p.position;

				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = p.uv;
				o.color = p.color;
				o.normal = UnityObjectToWorldNormal(v.normal);
				UNITY_TRANSFER_FOG(o,o.pos);
				TRANSFER_VERTEX_TO_FRAGMENT(o)
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv)*i.color;

				float3 lightColor = LIGHT_ATTENUATION(i)*_LightColor0.rgb;
				float NdotL = max(0.0, dot(i.normal, normalize(_WorldSpaceLightPos0.xyz)));
				float3 directColor = max(0.0, NdotL) * lightColor;

				col.rgb += directColor.rgb;
				UNITY_APPLY_FOG(i.fogCoord, col);
				clip(col.a-0.2f);
				return col;
			}
			ENDCG
		}
			Pass
			{
				Tags{ "LightMode" = "ShadowCaster" }
				ZWrite On
				ZTest LEqual
				Offset 1, 1

				CGPROGRAM
				#pragma target 3.0
				#pragma vertex vert
				#pragma fragment frag
				#define UNITY_PASS_SHADOWCASTER
				#pragma multi_compile_shadowcaster

				struct a2v {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 id : TEXCOORD1;
				};
				struct v2f {
					V2F_SHADOW_CASTER;
					float2 uv : TEXCOORD1;
				};
				v2f vert(a2v v) {
					Particle p = _Particles[GetID(v.id)];
					v.vertex.xyz *= p.scale;
					v.vertex.xyz += p.position;

					v2f o;
					o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
					o.uv = p.uv;
					TRANSFER_SHADOW_CASTER(o)
					return o;
				}
				float4 frag(v2f i) : COLOR{
					float4 col = tex2D(_MainTex,i.uv);
					clip(col.a - 0.2);
					SHADOW_CASTER_FRAGMENT(i)
				}
				ENDCG
			}
		}
		FallBack "Diffuse"
}
