Shader "SmoothNormalTool/Outline"
{
    // ═══════════════════════════════════════════════════════════════════
    //  SmoothNormalTool/Outline
    //  支持顶点色、切线空间、UV通道三种平滑法线存储方式的描边 Shader
    //  Unity 2022.3 (Built-in RP)
    // ═══════════════════════════════════════════════════════════════════
    Properties
    {
        [Header(Base)]
        _BaseColor      ("Base Color",      Color)  = (1,1,1,1)
        _MainTex        ("Albedo",          2D)     = "white" {}

        [Header(Outline)]
        _OutlineColor   ("Outline Color",   Color)  = (0,0,0,1)
        [PowerSlider(3.0)]
        _OutlineWidth   ("Outline Width",   Range(0, 0.1)) = 0.015

        [Header(Storage Mode)]
        [KeywordEnum(VertexColor, TangentSpace, UV1, UV2, UV3, UV4)]
        _SmoothNormalSrc ("Smooth Normal Source", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // ── Pass 0: Outline ──────────────────────────────────────────
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex   OutlineVert
            #pragma fragment OutlineFrag
            #pragma shader_feature _SMOOTHNORMALSRC_VERTEXCOLOR _SMOOTHNORMALSRC_TANGENTSPACE _SMOOTHNORMALSRC_UV1 _SMOOTHNORMALSRC_UV2 _SMOOTHNORMALSRC_UV3 _SMOOTHNORMALSRC_UV4
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float  _OutlineWidth;

            struct OutlineAppdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float4 tangent : TANGENT;
                float4 color   : COLOR;
                float4 uv0     : TEXCOORD0;
                float4 uv1     : TEXCOORD1;
                float4 uv2     : TEXCOORD2;
                float4 uv3     : TEXCOORD3;
            };

            struct OutlineV2F
            {
                float4 pos : SV_POSITION;
            };

            // ── 解码函数 ──────────────────────────────────────────────
            float3 SN_FromVertexColor(float4 col)
            {
                float nx = col.b * 2.0 - 1.0;
                float ny = col.a * 2.0 - 1.0;
                float nz = sqrt(max(0, 1.0 - nx * nx - ny * ny));
                return normalize(float3(nx, ny, nz));
            }

            float3 SN_FromTangentSpace(float4 tangentData, float3 objNormal, float4 tan)
            {
                float3 N = normalize(objNormal);
                float3 T = normalize(tan.xyz);
                float3 B = normalize(cross(N, T) * tan.w);
                float3 ts = normalize(tangentData.xyz);
                return normalize(T * ts.x + B * ts.y + N * ts.z);
            }

            float3 SN_FromUV(float2 uv)
            {
                float nx = uv.x;
                float ny = uv.y;
                float nz = sqrt(max(0, 1.0 - nx * nx - ny * ny));
                return normalize(float3(nx, ny, nz));
            }

            OutlineV2F OutlineVert(OutlineAppdata v)
            {
                OutlineV2F o;

                // ── 选择平滑法线 ──────────────────────────────────────
                float3 smoothNormal;

                #if defined(_SMOOTHNORMALSRC_VERTEXCOLOR)
                    smoothNormal = SN_FromVertexColor(v.color);
                #elif defined(_SMOOTHNORMALSRC_TANGENTSPACE)
                    smoothNormal = SN_FromTangentSpace(v.tangent, v.normal, v.tangent);
                #elif defined(_SMOOTHNORMALSRC_UV1)
                    smoothNormal = SN_FromUV(v.uv1.xy);
                #elif defined(_SMOOTHNORMALSRC_UV2)
                    smoothNormal = SN_FromUV(v.uv2.xy);
                #elif defined(_SMOOTHNORMALSRC_UV3)
                    smoothNormal = SN_FromUV(v.uv3.xy);
                #elif defined(_SMOOTHNORMALSRC_UV4)
                    smoothNormal = SN_FromUV(v.uv3.xy); // uv3 maps to TEXCOORD3
                #else
                    smoothNormal = normalize(v.normal);
                #endif

                // ── 屏幕空间等宽偏移 ──────────────────────────────────
                float3 viewSN = mul((float3x3)UNITY_MATRIX_MV, smoothNormal);
                viewSN = normalize(viewSN);

                float4 clipPos = UnityObjectToClipPos(v.vertex);
                clipPos.xy += normalize(viewSN.xy) * _OutlineWidth * clipPos.w;

                o.pos = clipPos;
                return o;
            }

            fixed4 OutlineFrag(OutlineV2F i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // ── Pass 1: Base (Forward Lit) ────────────────────────────────
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex   BaseVert
            #pragma fragment BaseFrag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            float4    _BaseColor;

            struct BaseAppdata
            {
                float4 vertex  : POSITION;
                float3 normal  : NORMAL;
                float2 uv      : TEXCOORD0;
            };

            struct BaseV2F
            {
                float4 pos     : SV_POSITION;
                float2 uv      : TEXCOORD0;
                float3 worldN  : TEXCOORD1;
                LIGHTING_COORDS(2, 3)
            };

            BaseV2F BaseVert(BaseAppdata v)
            {
                BaseV2F o;
                o.pos    = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldN = UnityObjectToWorldNormal(v.normal);
                TRANSFER_VERTEX_TO_FRAGMENT(o);
                return o;
            }

            fixed4 BaseFrag(BaseV2F i) : SV_Target
            {
                fixed4 texCol = tex2D(_MainTex, i.uv) * _BaseColor;

                float3 N = normalize(i.worldN);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float  NdL = max(0, dot(N, L));

                fixed4 col = texCol * (_LightColor0 * NdL + UNITY_LIGHTMODEL_AMBIENT);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"

    CustomEditor "SmoothNormalTool.OutlineShaderGUI"
}
