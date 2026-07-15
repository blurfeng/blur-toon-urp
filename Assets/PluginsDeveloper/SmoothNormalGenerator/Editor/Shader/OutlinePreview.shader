Shader "SmoothNormalTool/OutlinePreview"
{
    Properties
    {
        _OutlineColor   ("Outline Color",   Color)   = (0, 0, 0, 1)
        _OutlineWidth   ("Outline Width",   Float)   = 0.02
        // 0 = VertexColor, 1 = TangentSpace, 2 = UV
        _StorageMode    ("Storage Mode",    Float)   = 0
        // UV channel index (0-3)
        _UVChannel      ("UV Channel",      Float)   = 1
        // Vertex color channel pair: 0=RG, 1=GB, 2=BA
        _VCChannel      ("VC Channel",      Float)   = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        // ── Outline Pass ─────────────────────────────────────────────
        // Cull Front：只渲染背面（正确）
        // 翻转面外扩技术要求剔除正面，让背面围绕模型边缘可见，形成描边。
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _StorageMode;
            float  _UVChannel;
            float  _VCChannel;

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float4 tangent  : TANGENT;
                float4 color    : COLOR;
                float4 uv0      : TEXCOORD0;
                float4 uv1      : TEXCOORD1;
                float4 uv2      : TEXCOORD2;
                float4 uv3      : TEXCOORD3;
            };

            struct v2f
            {
                float4 pos  : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            // ─────────────────────────────────────────────────────────
            // 修正 Z 符号：存储时只保存了 XY，Z 重建为正值。
            // 用原始顶点法线（对象空间）做点积参考，若重建方向与原始法线
            // 反向则翻转 Z，使平滑法线尽量与外表面方向一致。
            float3 FixNormalZ(float3 smoothN, float3 vertexNormal)
            {
                float3 vn = normalize(vertexNormal);
                if (dot(smoothN, vn) < 0.0)
                    smoothN.z = -smoothN.z;
                return normalize(smoothN);
            }

            // 顶点色解码：XY → [-1,1]，Z 重建后修正符号
            float3 DecodeVertexColor(float4 col, float3 vertexNormal)
            {
                float nx, ny;
                int vch = (int)round(_VCChannel);
                if      (vch == 0) { nx = col.r * 2.0 - 1.0; ny = col.g * 2.0 - 1.0; }
                else if (vch == 1) { nx = col.g * 2.0 - 1.0; ny = col.b * 2.0 - 1.0; }
                else               { nx = col.b * 2.0 - 1.0; ny = col.a * 2.0 - 1.0; }
                float nz = sqrt(max(0.0, 1.0 - nx * nx - ny * ny));
                return FixNormalZ(normalize(float3(nx, ny, nz)), vertexNormal);
            }

            // 切线空间解码：
            // tangent.xyz 已被覆盖为切线空间平滑法线，原始切线数据已丢失。
            // 用 Gram-Schmidt 从顶点法线重建正交切线帧，与 C# 侧
            // SmoothNormalCalculator.ConvertToTangentSpace 使用相同逻辑。
            float3 DecodeTangentSpace(float3 tsNormal, float3 vertexNormal, float tangentW)
            {
                float3 N = normalize(vertexNormal);

                // Gram-Schmidt：构造与 N 正交的 T
                float3 up = abs(N.y) < 0.999 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 T  = normalize(cross(up, N));
                float3 B  = normalize(cross(N, T)) * tangentW;

                return normalize(T * tsNormal.x + B * tsNormal.y + N * tsNormal.z);
            }

            // UV 通道解码：XY 直接为 [-1,1]，Z 重建后修正符号
            float3 DecodeUV(float2 uv, float3 vertexNormal)
            {
                float nz = sqrt(max(0.0, 1.0 - uv.x * uv.x - uv.y * uv.y));
                return FixNormalZ(normalize(float3(uv.x, uv.y, nz)), vertexNormal);
            }

            // ─────────────────────────────────────────────────────────
            v2f vert(appdata v)
            {
                v2f o;

                // ── 解码平滑法线（对象空间）──────────────────────────
                float3 smoothNormal;
                int mode = (int)round(_StorageMode);

                if (mode == 0)
                {
                    smoothNormal = DecodeVertexColor(v.color, v.normal);
                }
                else if (mode == 1)
                {
                    // tangent.xyz 存的是切线空间平滑法线，.w 保持原始翻转符号
                    smoothNormal = DecodeTangentSpace(v.tangent.xyz, v.normal, v.tangent.w);
                }
                else
                {
                    int ch = (int)round(_UVChannel);
                    float2 uvXY;
                    if      (ch == 0) uvXY = v.uv0.xy;
                    else if (ch == 1) uvXY = v.uv1.xy;
                    else if (ch == 2) uvXY = v.uv2.xy;
                    else              uvXY = v.uv3.xy;
                    smoothNormal = DecodeUV(uvXY, v.normal);
                }

                // ── 对象空间 → 世界空间（使用逆转置，正确处理非均匀缩放）
                float3 worldSN = UnityObjectToWorldNormal(smoothNormal);

                // ── 世界空间 → 裁剪空间法线方向（用于 NDC 偏移）──────
                // 将法线投影到裁剪空间 XY 平面，保持屏幕空间等宽描边
                float4 clipPos    = UnityObjectToClipPos(v.vertex);
                float4 clipNormal = mul(UNITY_MATRIX_VP, float4(worldSN, 0.0));

                // 防止 XY 接近零时 normalize 产生 NaN
                float2 offsetDir = clipNormal.xy;
                float  dirLen    = length(offsetDir);
                if (dirLen > 1e-5)
                    offsetDir /= dirLen;
                else
                    offsetDir = float2(0, 0);

                // clipPos.w 保持屏幕空间宽度不随深度变化
                clipPos.xy += offsetDir * _OutlineWidth * clipPos.w;

                o.pos = clipPos;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = _OutlineColor;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
