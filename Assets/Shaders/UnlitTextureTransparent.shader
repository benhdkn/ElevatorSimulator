Shader "Ben/Unlit/ColorTextureTransparent" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Tint ("Tint", Color) = (1, 1, 1, 1)
		_Alpha("Alpha", Range(0.0,1.0)) = 0.5
	}

	SubShader {
		Tags {
			"Queue"="Transparent" 
			"RenderType"="Transparent" 
		}

		LOD 100
		ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass {
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata {
				float4 position : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Tint;
			float _Alpha;
			
			v2f vert(appdata v) {
				v2f o;
				o.position = UnityObjectToClipPos(v.position);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv) + _Tint;
				col.a = _Alpha;
				return col;
			}
			
			ENDCG
		}
	}
}