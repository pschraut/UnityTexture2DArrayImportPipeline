// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

// Modified to work with a 2DArray rather than Texture

Shader "Unlit/Texture2D Array" {
Properties {
    _MainTex ("Base (RGB)", 2DArray) = "" {}
	_Slice ("Slice", Int) = 0
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 100

    Pass {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 3.5
            #pragma multi_compile_fog
			#pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

			UNITY_DECLARE_TEX2DARRAY(_MainTex);
            float4 _MainTex_ST;
			UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(int, _Slice)
			UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord.z = UNITY_ACCESS_INSTANCED_PROP(Props, _Slice);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.texcoord);
                UNITY_APPLY_FOG(i.fogCoord, col);
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
        ENDCG
    }
}

}
