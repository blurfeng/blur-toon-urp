Shader "BlurToonURP/Lit"
{
    Properties
    {
        //----------- BaseMap 基础纹理 -----------
        _BaseMap("Base Map", 2D) = "white" {} //基础贴图
        [HDR]_BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        //基础贴图混合颜色
        [HDR]_BaseMapBlendColor ("MainTex ColorBlend", Color) = (1, 1, 1, 1) //基础贴图 混合颜色
        _BaseMapBlendColorIntensity ("MainTex BlendIntensity", Range(0,1)) = 0 //基础贴图 混合颜色强度
        
        [HDR]_Shade1Color ("Shade1 Color", Color) = (0.7, 0.7, 0.7, 1) //暗部1颜色
        [HDR]_Shade2Color ("Shade2 Color", Color) = (0.4, 0.4, 0.4, 1) //暗部2颜色

        //色阶分布与模糊
        _FloatBrightShade1Step ("BaseShade1 Step", Range(0, 1)) = 0.5 //基础→暗部1 位置
        _FloatBrightShade1Blur ("BaseShade1 Blur", Range(0.0001, 3)) = 0.1 //基础→暗部1 羽化
        _FloatShade1Shade2Step ("Shade1Shade2 Step", Range(0, 1)) = 0.4 //暗部1→暗部2 位置
        _FloatShade1Shade2Blur ("Shade1Shade2 Blur", Range(0.0001, 3)) = 0.1 //暗部1→暗部2 羽化

        //暗部阈值贴图
        _ToggleShadeThresholdMap ("Shade ThresholdMap Toggle", Float) = 0 //开关 暗部阈值贴图
        _TexShadeThresholdMap ("Shade ThresholdMap ", 2D) = "white" {} //暗部阈值贴图
        _FloatShadeThresholdMapIntensity ("Shade ThresholdMap Intensity", Range(0, 1)) = 0.5 //暗部阈值贴图 强度
        
        
        //----------- NormalMap 法线贴图 -----------
        _BumpMap ("Bump Map", 2D) = "bump" {} //法线贴图
        _BumpScale ("Bump Scale", Range(0, 1)) = 1 //强度
        //开关
        _ToggleNormalMapOnBaseMap ("NormalMap On BaseMap", Float) = 0 //开关 基础贴图
        _ToggleNormalMapOnHighLight ("NormalMap On HighLight", Float) = 0 //开关 高光
        _ToggleNormalMapOnRimLight ("NormalMap On RimLight", Float) = 0 //开关 边缘光
        
        
        //----------- Outline 外描边 -----------
        _FloatOutlineType("Outline Type", Float) = 0 //外描边类型 0=VertexNormal 1=VertexColor 2=VertexTangent
        _FloatOutlineWidthType("Outline Width Type", Float) = 0 //外描边宽度类型 ●记录值 确认对应的keywords设置
        [HDR]_ColorOutlineColor ("Outline Color", Color) = (0.4,0.4,0.4,1) //颜色
        _FloatOutlineWidth ("Outline Width", Float ) = 1.5 //宽度
        _ToggleOutlineBaseMapBlend ("Outline BaseMapBlend", Float ) = 1 //开关 基础贴图混合
        _FloatOutlineBaseMapBlendIntensity ("Outline BaseMapBlend Intensity", Range(0, 1) ) = 1 //基础贴图混合 强度
        
        
        //----------- Rim Light 边缘光 -----------
        _ToggleRimLight ("RimLight Toggle", Float) = 0 //边缘光开关 ●仅用于记录 设置关键词开启
        [HDR] _ColorRimLightColor ("RimLight Color", Color) = (1, 1, 1, 0.3) //颜色
        _FloatRimLightIntensity ("RimLight Intensity", Range(0, 1)) = 0.8 //强度
        _FloatRimLightInsideDistance ("RimLight Inside Distance", Range(0, 1)) = 0.18 //内部距离
        _ToggleRimLightHard ("RimLight Hard", Float) = 0 //开关 硬边缘
        //暗部遮罩
        _ToggleRimLightShadeMask ("RimLight ShadeMask Toggle", Float ) = 0 //开关 暗部遮罩 ●仅用于记录 设置关键词开启
        _FloatRimLightShadeMaskIntensity ("RimLight ShadeMask Intensity", Range(0, 1)) = 1 //暗部遮罩强度
        _FloatRimLightShadeMaskOffset ("RimLight ShadeMask Offset", Range(-1, 1)) = 0.4 //暗部遮罩偏移
        _ToggleRimLightShadeColor ("RimLight ShadeColor", Float ) = 0 //开关 暗部颜色 ●仅用于记录 设置关键词开启
        [HDR]_ColorRimLightShadeColor ("RimLight ShadeColor", Color) = (1,1,1,0.3) //暗部颜色
        _FloatRimLightShadeColorIntensity ("RimLight ShadeColor Intensity", Range(0, 1)) = 0.8 //暗部颜色 强度
        _ToggleRimLightShadeColorHard ("RimLight ShadeColor Hard", Float ) = 0 //暗部颜色 硬边缘
        //遮罩贴图
        _TexRimLightMaskMap ("RimLight MaskMap", 2D) = "white" {} //遮罩贴图
        _FloatRimLightMaskMapIntensity ("RimLight MaskMap Intensity", Range(-1, 1)) = 0 //遮罩贴图 强度
        
        //----------- Light 光照设置 -----------
        _FloatRealtimeLightIntensity ("Realtime Light Intensity", Range(0, 10)) = 5 //实时光照强度
        _FloatEnvLightIntensity ("Environment Light Intensity", Range(0, 10)) = 2 //环境光照强度
        
        //曝光设置
		_FloatGlobalExposureIntensity ("Global Exposure Intensity", Range(0.001, 10)) = 1 //全局曝光强度
		_FloatBaseMapExposureIntensity ("BaseMap Intensity", Range(0.001, 10)) = 1 //基础贴图亮部曝光强度
		_FloatBaseMapShade1ExposureIntensity ("BaseMapShade1 Intensity", Range(0.001, 10)) = 1 //基础贴图暗部1曝光强度
		_FloatBaseMapShade2ExposureIntensity ("BaseMapShade2 Intensity", Range(0.001, 10)) = 1 //基础贴图暗部2曝光强度
        
        //附加光照设置
		_ToggleAddLight ("PointLight HighLight", Float ) = 1 //开关 附加光照 ●仅用于记录 设置关键词开启
        _FloatAddLightIntensity ("PointLight Intensity", Range(0, 2)) = 1 //附加光照强度
        
        //光照开关
        _ToggleGlobalLightBaseMap ("GlobalLight BaseMap Toggle", Float) = 1 //基础贴图
        _GlobalLightBaseMapMixedIntensity ("GlobalLight BaseMap Mixed Intensity", Range(0.001, 1)) = 0.5//基础贴图和光照颜色的混合强度 0-1
        _ToggleGlobalLightBaseShade1 ("GlobalLight BaseShade1 Toggle", Float) = 1 //暗部1
        _GlobalLightBaseShade1MixedIntensity ("GlobalLight BaseShade1 Mixed Intensity", Range(0.001, 1)) = 0.5//暗部1和光照颜色的混合强度 0-1
        _ToggleGlobalLightBaseShade2 ("GlobalLight BaseShade2 Toggle", Float) = 1 //暗部2
        _GlobalLightBaseShade2MixedIntensity ("GlobalLight BaseShade1 Mixed Intensity", Range(0.001, 1)) = 0.5//暗部2和光照颜色的混合强度 0-1
        _ToggleGlobalLightRimLight ("GlobalLight RimLight Toggle", Float) = 1 //边缘光
        _GlobalLightRimLightMixedIntensity ("GlobalLight RimLight Mixed Intensity", Range(0.001, 1)) = 0.5//边缘光和光照颜色的混合强度 0-1
        _ToggleGlobalLightRimLightShade ("GlobalLight RimLightShade Toggle", Float) = 1 //边缘光暗部
        _GlobalLightRimLightShadeMixedIntensity ("GlobalLight RimLightShade Mixed Intensity", Range(0.001, 1)) = 0.5//边缘光暗部和光照颜色的混合强度 0-1
        
        //阴影设置
        _ToggleShadowCaster ("ShadowCaster Toggle", Float ) = 1 //开关 阴影投射 ●仅用于记录 设置Pass开启
        _ToggleShadowReceive ("ShadowReceive Toggle", Float ) = 1 //开关 阴影接收
        _FloatShadowIntensity ("Shadow Intensity", Range(-1, 1)) = 0 //阴影强度
        
        //内置光照
        _ToggleBuiltInLight ("BuiltInLight Toggle", Float ) = 0 //开关 内置光照
        _FloatBuiltInLightAxisX ("BuiltInLight XAxis", Range(-1, 1)) = 1
        _FloatBuiltInLightAxisY ("BuiltInLight YAxis", Range(-1, 1)) = 1
        _FloatBuiltInLightAxisZ ("BuiltInLight ZAxis", Range(-1, 1)) = -1
        _FloatBuiltInLightDirBlend ("BuiltInLight Dir Blend", Range(0, 1)) = 0.5
        
        //内置光照颜色
        _ToggleBuiltInLightColor ("BuiltInLight Color Toggle", Float) = 0 //开关 内置光照
        [HDR]_ColorBuiltInLightColor ("BuiltInLight Color", Color) = (1,1,1,1) //内置光照颜色
        _FloatBuiltInLightColorBlend ("BuiltInLight Color Blend", Range(0, 1)) = 1 //内置光照颜色 混合强度
        
        //光照水平方向锁定
        _ToggleLightHorLockBaseMap ("HorizontalLock BaseMap", Float ) = 0 //基础贴图
        _ToggleLightHorLockRimLight ("HorizontalLock Rim Light", Float) = 0 //边缘光
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        //基础渲染
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            
            // BlurToonURP Keywords
            #pragma shader_feature_local _BASEMAP_SHADE_THRESHOLDMAP_ON //暗部阈值贴图
			#pragma shader_feature_local _ADDLIGHT_ON // 附加光照
            #pragma shader_feature_local _BUILTINLIGHT_ON // 内置光照
            //边缘光
            #pragma shader_feature_local _RIMLIGHT_ON
            #pragma shader_feature_local _RIMLIGHT_SHADEMASK_ON
            #pragma shader_feature_local _RIMLIGHT_SHADEMASK_COLOR_ON
            #pragma shader_feature_local _RIMLIGHT_MASKMAP_ON
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            

            //顶点着色器 输入数据结构
            struct Attributes
            {
                float4 positionOS : POSITION; //对象空间顶点位置
                float3 normalOS   : NORMAL; //法线
                float4 tangentOS  : TANGENT; //切线
                float2 texcoord   : TEXCOORD0; //纹理坐标

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置
                float2 uv : TEXCOORD0; //传递的纹理坐标

                //启用宏时，Unity会自动在顶点和片元着色器之间插值传递世界空间中的顶点位置，使其可以在片元着色器中直接使用。
                #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
                float3 positionWS : TEXCOORD1;
                #endif

                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //定义的字段属性
            //暗部阈值贴图
            #if defined(_BASEMAP_SHADE_THRESHOLDMAP_ON)
            TEXTURE2D(_TexShadeThresholdMap); SAMPLER(sampler_TexShadeThresholdMap); //暗部阈值贴图
            #endif

            #if defined(_RIMLIGHT_ON) && defined(_RIMLIGHT_MASKMAP_ON)
            TEXTURE2D(_TexRimLightMaskMap); SAMPLER(sampler_TexRimLightMaskMap);
            #endif
            
            CBUFFER_START(UnityPerMaterial)
            //-------- BaseMap 基础纹理 --------
            //已在"Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"中定义的内容。
            //TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); //基础贴图
            //TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap); //法线贴图
            float4 _BaseMap_ST; //基础贴图
            half4 _BaseColor;
            //基础贴图混合颜色
            half4 _BaseMapBlendColor; //基础贴图 混合颜色
            half _BaseMapBlendColorIntensity; //基础贴图 混合颜色强度
            //暗部 Shade
            half4 _Shade1Color; //暗部1颜色
            half4 _Shade2Color; //暗部2颜色
            //色阶分布与模糊
            half _FloatBrightShade1Step; //基础→暗部1 位置
            half _FloatBrightShade1Blur; //基础→暗部1 模糊
            half _FloatShade1Shade2Step; //暗部1→暗部2 位置
            half _FloatShade1Shade2Blur; //暗部1→暗部2 模糊
            //暗部阈值贴图
            #if defined(_BASEMAP_SHADE_THRESHOLDMAP_ON)
            float4 _TexShadeThresholdMap_ST; //暗部阈值 贴图
            half _FloatShadeThresholdMapIntensity; //暗部阈值贴图 强度
            #endif

            
            //-------- NormalMap 法线贴图 --------
            float4 _BumpMap_ST;
            half _BumpScale; //法线贴图强度
            //开关
            half _ToggleNormalMapOnBaseMap; //开关 基础贴图
            half _ToggleNormalMapOnHighLight; //开关 高光
            half _ToggleNormalMapOnRimLight; //开关 边缘光
            
            
            //-------- RimLight 边缘光 --------
            half _ToggleRimLight; //开关 边缘光
            half4 _ColorRimLightColor; //颜色
            half _FloatRimLightIntensity; //强度
            half _FloatRimLightInsideDistance; //内部距离
            half _ToggleRimLightHard; //开关 硬边缘
            //暗部遮罩
            half _FloatRimLightShadeMaskIntensity; //暗部遮罩强度
            half _FloatRimLightShadeMaskOffset; //暗部遮罩偏移
            //暗部颜色
            half4 _ColorRimLightShadeColor; //暗部颜色
            half _FloatRimLightShadeColorIntensity; //暗部颜色 强度
            half _ToggleRimLightShadeColorHard; //暗部颜色 硬边缘
            //遮罩贴图
            float4 _TexRimLightMaskMap_ST;
            half _FloatRimLightMaskMapIntensity; //遮罩贴图强度


            //-------- Light 光照设置 --------
            half _FloatRealtimeLightIntensity; //实时光照强度
            half _FloatEnvLightIntensity; //环境光照强度

            //曝光设置
            half _FloatGlobalExposureIntensity; //全局曝光强度
            half _FloatBaseMapExposureIntensity; //曝光强度
            half _FloatBaseMapShade1ExposureIntensity; //曝光强度
            half _FloatBaseMapShade2ExposureIntensity; //曝光强度

            //附加光照设置
            half _FloatAddLightIntensity; //附加光照强度

            //光照开关
            half _ToggleGlobalLightBaseMap; //基础贴图
            half _GlobalLightBaseMapMixedIntensity; //基础贴图和光照颜色的混合强度 0-1
            half _ToggleGlobalLightBaseShade1; //暗部1
            half _GlobalLightBaseShade1MixedIntensity; //暗部1和光照颜色的混合强度 0-1
            half _ToggleGlobalLightBaseShade2; //暗部2
            half _GlobalLightBaseShade2MixedIntensity; //暗部2和光照颜色的混合强度 0-1
            half _ToggleGlobalLightRimLight; //边缘光
            half _GlobalLightRimLightMixedIntensity; //边缘光和光照颜色的混合强度 0-1
            half _ToggleGlobalLightRimLightShade; //边缘光暗部
            half _GlobalLightRimLightShadeMixedIntensity; //边缘光暗部和光照颜色的混合强度 0-1
            
            //阴影设置
            half _ToggleShadowReceive; //开关 阴影接收
            half _FloatShadowIntensity; //阴影接收强度

            //内置光照
            float _FloatBuiltInLightAxisX; //光照方向X轴
            float _FloatBuiltInLightAxisY; //光照方向Y轴
            float _FloatBuiltInLightAxisZ; //光照方向Z轴
            half _FloatBuiltInLightDirBlend; //光照方向混合0-1，0=场景光方向 1=内置光方向
            half _ToggleBuiltInLightColor; //开关 内置光照颜色
            half4 _ColorBuiltInLightColor; //内置光照颜色
            half _FloatBuiltInLightColorBlend; //内置光照颜色 混合强度
            
            //光照水平方向锁定
            half _ToggleLightHorLockBaseMap; //光照水平锁定 基础贴图
            half _ToggleLightHorLockRimLight; //光照水平锁定 边缘光
            
            CBUFFER_END
            
            //顶点着色器
            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                //不使用TRANSFORM_TEX()进行缩放和偏移，NPR一般不需要在此处进行缩放，节省这一步的计算。
                //但我们任然可以根据不同贴图各自的设定进行转换。
                OUT.uv = IN.texcoord;
                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = normalInput.normalWS;
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = half4(normalInput.tangentWS.xyz, sign);
                //启用宏时，Unity会自动在顶点和片元着色器之间插值传递世界空间中的顶点位置，使其可以在片元着色器中直接使用。
                //否则自行计算
                #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
                half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, OUT.normalWS, viewDirWS);
                OUT.viewDirTS = viewDirTS;
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv;
                //基础贴图采样。sampler_BaseMap是Unity自动生成的对应采样器不需要额外定义。
                float4 colorBaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS); //观察方向
                float3 normalDirWS = IN.normalWS; //法线方向
                
                //-------- NormalMap 法线贴图 -------- Start
                //法线贴图采样
                float3 normalDirTex = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(uv, _BumpMap)), _BumpScale);
                //将法线贴图中获取的法线转换至世界空间
                float3 binormalWS = cross(normalDirWS, normalDirTex.xyz) * IN.tangentWS.w;//世界空间的副法线
                normalDirTex = normalize(mul(normalDirTex, half3x3(IN.tangentWS.xyz, binormalWS, normalDirWS)));
                //-------- NormalMap 法线贴图 -------- End

                //-------- Light 光照计算 -------- Start
                //环境光照（Lighting中设置的环境光照等）
                half3 envLightColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                //自定义的环境光照强度影响
                envLightColor *= _FloatEnvLightIntensity;

                //实时光照
                //主光照
                Light lightMain = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 colorLightMain = lightMain.color * lightMain.distanceAttenuation;
                //阴影衰减
                float shadowAttenuation = saturate(lightMain.shadowAttenuation - _FloatShadowIntensity);
                float3 lightDirWS = lightMain.direction;
                //光照颜色
                half3 realtimeLightColor = colorLightMain;
                
                //附加光照
                #if defined(_ADDLIGHT_ON)
                half3 colorLightAdd;
                uint lightsCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS);
                    half3 lightColor = light.color * light.distanceAttenuation;
                    colorLightAdd += lightColor;
                LIGHT_LOOP_END
                //自定义 附加光照强度
                colorLightAdd *= _FloatAddLightIntensity;
                //混合实时光照颜色
                realtimeLightColor += colorLightAdd;
                #endif

                //自定义的光照强度
                realtimeLightColor *= _FloatRealtimeLightIntensity * 0.1;
                //将所有光照混合
                half3 colorLightBlend = envLightColor + realtimeLightColor;
                //自定义的曝光强度
                colorLightBlend *= _FloatGlobalExposureIntensity;
                
                //-------- Light 光照计算 -------- End

                
                //-------- BuiltInLight 内置光照 -------- Start
                #if defined(_BUILTINLIGHT_ON)
                //内置光照方向
                float3 builtInLightDir = normalize(float3(_FloatBuiltInLightAxisX, _FloatBuiltInLightAxisY, _FloatBuiltInLightAxisZ));
                //以内置光照方向为准
                lightDirWS = lerp(lightDirWS, builtInLightDir, _FloatBuiltInLightDirBlend);
                //内置光照颜色混合
                half3 colorBuiltInLight = lerp(colorLightBlend, _ColorBuiltInLightColor.rgb, _FloatBuiltInLightColorBlend);
                //根据开关来使用场景光照颜色或内置光照颜色
                colorLightBlend = lerp(colorLightBlend, colorBuiltInLight, _ToggleBuiltInLightColor);
                #endif
                //-------- BuiltInLight 内置光照 -------- End
                
                
                //-------- BaseMap 基础贴图 -------- Start
                //基础贴图 颜色混合强度
                half3 colorBaseMapFinal = lerp(colorBaseMap, _BaseMapBlendColor, _BaseMapBlendColorIntensity).rgb;
                //光照影响
                colorBaseMapFinal = lerp(colorBaseMapFinal, lerp(colorBaseMapFinal, colorBaseMapFinal * colorLightBlend, _GlobalLightBaseMapMixedIntensity) * _FloatBaseMapExposureIntensity, _ToggleGlobalLightBaseMap);
                
                //暗部1颜色
                half3 colorBaseMapShade1 = colorBaseMapFinal * _Shade1Color.rgb;
                //使用原色或混合光照
                colorBaseMapShade1 = lerp(colorBaseMapShade1, lerp(colorBaseMapShade1, colorBaseMapShade1 * colorLightBlend, _GlobalLightBaseShade1MixedIntensity) * _FloatBaseMapShade1ExposureIntensity, _ToggleGlobalLightBaseShade1);
                //暗部2颜色
                half3 colorBaseMapShade2 = colorBaseMapFinal * _Shade2Color.rgb;
                //使用原色或混合光照
                colorBaseMapShade2 = lerp(colorBaseMapShade2, lerp(colorBaseMapShade2, colorBaseMapShade2 * colorLightBlend, _GlobalLightBaseShade2MixedIntensity) * _FloatBaseMapShade2ExposureIntensity, _ToggleGlobalLightBaseShade2);
                
                //光照计算
                float3 lightDirOnBaseMap = lightDirWS;
                lightDirOnBaseMap.y = lerp(lightDirOnBaseMap.y, 0, _ToggleLightHorLockBaseMap); //光照水平方向锁定
                //根据开关，使用顶点法线或法线贴图法线
                float3 normalDirOnBaseMap = lerp(normalDirWS, normalDirTex, _ToggleNormalMapOnBaseMap);
                float halfLambert = dot(normalDirOnBaseMap, lightDirOnBaseMap) * 0.5 + 0.5; //半兰伯特
                //根据开关，计算阴影的影响
                halfLambert = lerp(halfLambert, halfLambert * shadowAttenuation, _ToggleShadowReceive);

                //暗部阈值贴图
                #if defined(_BASEMAP_SHADE_THRESHOLDMAP_ON)
                //根据阈值进行强度采样
                float shadeThresholdValue = 1 - SAMPLE_TEXTURE2D(_TexShadeThresholdMap, sampler_TexShadeThresholdMap, TRANSFORM_TEX(uv, _TexShadeThresholdMap)).r;
                shadeThresholdValue *= _FloatShadeThresholdMapIntensity;//自定义强度影响
                halfLambert = saturate(halfLambert - shadeThresholdValue);
                #endif
                
                //暗部1
                _FloatBrightShade1Blur *= 0.1; //将0-10的设置值映射到0-1
                float lightIntensityShade1 = 1 - saturate(1 + (halfLambert - _FloatBrightShade1Step) / _FloatBrightShade1Blur);
                //暗部2
                _FloatShade1Shade2Blur *= 0.05; //将0-10的设置值映射到0-0.5
                float lightIntensityShade2 = 1 - saturate(1 + (halfLambert - _FloatShade1Shade2Step) / _FloatShade1Shade2Blur);
                
                //混合颜色
                float3 colorFinalBlend = lerp(colorBaseMapFinal, lerp(colorBaseMapShade1, colorBaseMapShade2, lightIntensityShade2), lightIntensityShade1);
                //-------- BaseMap 基础贴图 -------- End
                
                
                //-------- RimLight 边缘光 -------- Start
                #if defined(_RIMLIGHT_ON)
                //计算边缘光颜色，按配置混合光照颜色
                half3 colorRimLight = lerp(_ColorRimLightColor.rgb,
                    lerp(_ColorRimLightColor.rgb, _ColorRimLightColor.rgb * colorLightBlend, _GlobalLightRimLightMixedIntensity),
                    _ToggleGlobalLightRimLight);
                colorRimLight *= _ColorRimLightColor.a; //透明度
                //法线方向 TODO使用法线方向来源配置
                float3 normalDirOnRimLight = lerp(normalDirWS, normalDirTex, _ToggleNormalMapOnRimLight);

                //计算边缘光系数，并按系数调整边缘光颜色
                //法线和视线夹角，越靠近边缘值越大。范围为[0,1]。
                float rimLightNdotV = saturate(1 - dot(normalDirOnRimLight, viewDirWS));
                //强度控制。强度[0,1]映射到[3,0]，exp2为2的x次幂，范围[8,2]。pow为x的y次幂。x=rimLightNdotV小于1，所以y越小rimLightFactor越大。
                //_FloatRimLightIntensity越大，rimLightFactor越大。rimLightFactor在(0,1]范围。
                float rimLightFactor = pow(rimLightNdotV, exp2(lerp(3, 0, _FloatRimLightIntensity)));
                //根据设定的内部延伸距离计算最终系数 或使用硬边缘
                //距离计算 & 硬边缘开关。
                //_FloatRimLightInsideDistance范围为[0,1]，值越大边缘光范围越窄，为1时没有边缘光。rimLightFactor在(0,1]范围。
                rimLightFactor = saturate(
                    lerp((rimLightFactor - _FloatRimLightInsideDistance) / (1 - _FloatRimLightInsideDistance),
                        step(_FloatRimLightInsideDistance, rimLightFactor), _ToggleRimLightHard));
                
                //---- 暗部遮罩 ---- 使用，这能防止阴影部分不自然的发亮
                #if defined(_RIMLIGHT_SHADEMASK_ON)
                //通过光源方向计算需要去除的阴影部的边缘光
                float3 lightDirOnRimLight = lightDirWS;
                lightDirOnRimLight.y = lerp(lightDirOnRimLight.y, 0, _ToggleLightHorLockRimLight); //光照方向水平锁定
                //暗部强度。dot范围[1,-1]，实际上在x<0时暗部遮罩才生效，及光照不到的暗部。
                //通过_FloatRimLightShadeMaskOffset偏移rimLightShadeIntensity的范围，以调整暗部遮罩的作用范围。
                float rimLightShadeIntensity = dot(normalDirWS, lightDirOnRimLight) - _FloatRimLightShadeMaskOffset;
                
                //根据阴影强度进行遮罩，_FloatRimLightInsideDistance越大distanceOffset越小，distanceOffset范围(16,1)
                float distanceOffset = exp2(4 * (1 - _FloatRimLightInsideDistance)); //根据内部距离 变化遮罩强度
                //暗部遮罩强度。shadeMaskIntensity为负值，最终通过抵消shadeMaskFactor正值来实现暗部遮罩。
                float shadeMaskIntensity = min(rimLightShadeIntensity * _FloatRimLightShadeMaskIntensity * distanceOffset, 0);
                //shadeMaskFactor接近0来消除最终的边缘光
                float shadeMaskFactor = saturate(rimLightFactor + shadeMaskIntensity); //暗部遮罩强度 计算
                
                colorRimLight *= shadeMaskFactor;
                
                    //---- 暗部颜色 ---- 在去除阴影部分的边缘光后，我们可以按美术需求，再叠加自定义颜色的边缘光
                    #if defined(_RIMLIGHT_SHADEMASK_COLOR_ON)
                    //暗部颜色，按配置混合光照颜色
                    half3 colorRimLightShade = lerp(_ColorRimLightShadeColor.rgb,
                    lerp(_ColorRimLightShadeColor.rgb, _ColorRimLightShadeColor.rgb * colorLightBlend, _GlobalLightRimLightShadeMixedIntensity),
                    _ToggleGlobalLightRimLight);
                    colorRimLightShade *= _ColorRimLightShadeColor.a; //透明度
                    //暗部边缘光系数
                    float rimLightShadeFactor = pow(rimLightNdotV, exp2(lerp(3, 0, _FloatRimLightShadeColorIntensity)));
                    //距离计算 & 硬边缘开关。
                    rimLightShadeFactor = saturate(
                        lerp((rimLightShadeFactor - _FloatRimLightInsideDistance) / (1 - _FloatRimLightInsideDistance),
                            step(_FloatRimLightInsideDistance, rimLightShadeFactor), _ToggleRimLightShadeColorHard));
                    //暗部遮罩强度 计算
                    //遮罩强度越大shadeMaskFactor越接近0，暗部颜色越明显。使用rimLightShadeIntensity值判断使暗部颜色只对暗部生效。
                    rimLightShadeFactor = lerp(saturate(rimLightShadeFactor) * (1 - shadeMaskFactor), 0, step(0, rimLightShadeIntensity));
                    //暗部颜色开关
                    colorRimLight = colorRimLight + colorRimLightShade * rimLightShadeFactor;
                    #endif

                #else
                //使用系数调整边缘光颜色
                colorRimLight *= rimLightFactor;
                #endif

                //通过遮罩贴图绘制边缘光
                #if defined(_RIMLIGHT_MASKMAP_ON)
                //边缘光 遮罩贴图
                float rimlightMaskValue = SAMPLE_TEXTURE2D(_TexRimLightMaskMap, sampler_TexRimLightMaskMap, TRANSFORM_TEX(uv, _TexRimLightMaskMap)).r;
                //遮罩贴图强度
                colorRimLight = lerp(colorRimLight, colorRimLight * rimlightMaskValue, _FloatRimLightMaskMapIntensity);
                #endif
                
                //混合边缘光颜色到最终颜色
                colorFinalBlend = lerp(colorFinalBlend, colorFinalBlend + colorRimLight, _ToggleRimLight);
                
                #endif
                //-------- RimLight 边缘光 -------- End

                
                half4 colorFinal = half4(colorFinalBlend, colorBaseMap.a);
                
                return colorFinal;
            }

            ENDHLSL
        }
        
        //阴影投射
        //向场景投射阴影。由编辑器"阴影设置"中的"阴影投射"开关(SetShaderPassEnabled)控制此Pass的启用。
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode" = "ShadowCaster"}

            //只写入深度，不输出颜色
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            // 点光源/聚光灯阴影投射时，光照方向按逐顶点位置计算
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //Shadows.hlsl中使用了LerpWhiteTo，其定义在core包的CommonMaterial.hlsl中，需在其之前引入。
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            //由URP阴影渲染流程设置的全局变量
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
            float3 _LightPosition; //当前投射阴影的点光源/聚光灯世界位置
            #else
            float3 _LightDirection; //当前投射阴影的方向光世界方向
            #endif

            //顶点着色器 输入数据结构
            struct Attributes
            {
                float4 positionOS : POSITION; //对象空间顶点位置
                float3 normalOS   : NORMAL; //法线

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //计算应用了阴影偏移(法线偏移/深度偏移)后的裁剪空间位置，避免阴影粉刺(Shadow Acne)与漏光(Peter Panning)
            float4 GetShadowPositionHClip(Attributes IN)
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                //根据阴影来源计算世界空间光照方向
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                float3 lightDirectionWS = _LightDirection;
                #endif

                //应用阴影偏移后转换到裁剪空间
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                //将深度限制在近裁剪面，防止阴影被近裁剪面裁掉
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = GetShadowPositionHClip(IN);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //阴影Pass只需要深度，不输出颜色
                return 0;
            }

            ENDHLSL
        }

        //深度
        //将模型写入相机深度图(_CameraDepthTexture)。当渲染管线需要深度预渲染时(如开启深度贴图/MSAA/软粒子/深度雾/屏幕空间扭曲等)使用此Pass。
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode" = "DepthOnly"}

            //只写入深度，颜色仅写入R通道
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //顶点着色器 输入数据结构
            struct Attributes
            {
                float4 positionOS : POSITION; //对象空间顶点位置

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                //深度Pass只需要写入深度
                return IN.positionCS.z;
            }

            ENDHLSL
        }

        //深度法线
        //将模型的世界空间法线写入相机法线图(_CameraNormalsTexture)。屏幕空间环境光遮蔽(SSAO)等依赖法线的后处理需要此Pass。
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode" = "DepthNormals"}

            ZWrite On
            Cull Back

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            //顶点着色器 输入数据结构
            struct Attributes
            {
                float4 positionOS : POSITION; //对象空间顶点位置
                float3 normalOS   : NORMAL; //法线
                float4 tangentOS  : TANGENT; //切线

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置
                float3 normalWS : TEXCOORD1; //世界空间法线

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;

                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vertexInput.positionCS;
                OUT.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                //输出世界空间法线到相机法线图
                float3 normalWS = NormalizeNormalPerPixel(IN.normalWS);
                return half4(normalWS, 0.0);
            }

            ENDHLSL
        }

        //外描边
        Pass
        {
            Name "Outline"
            
            Tags {"LightMode" = "SRPDefaultUnlit"} //不受光照影响
            
            ZWrite On
            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            
            // BlurToonURP Keywords
            //外描边
            #pragma shader_feature_local _OUTLINE_ON // 外描边开关
            #pragma shader_feature_local _OUTLINE_WIDTH_SAME _OUTLINE_WIDTH_SCALING // 外描边类型
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            
            CBUFFER_START(UnityPerMaterial)
            //基础纹理 BaseMap
            //已在"Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"中定义的内容。
            //TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); //基础贴图
            //TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap); //法线贴图
            float4 _BaseMap_ST; //基础贴图
            half4 _BaseColor;

            //外描边
            half _FloatOutlineType;
            half4 _ColorOutlineColor;
            half _FloatOutlineWidth;
            half _ToggleOutlineBaseMapBlend;
            half _FloatOutlineBaseMapBlendIntensity;
            CBUFFER_END

            //顶点着色器 输入数据结构
            struct VertexInput
            {
                float4 positionOS : POSITION; //对象空间顶点位置
                float2 texcoord : TEXCOORD0; //纹理坐标
                float3 normalOS : NORMAL; //对象空间法线
                float4 tangentOS : TANGENT; //对象空间切线
                float4 color : COLOR; //颜色

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置
                float2 uv : TEXCOORD0; //传递的纹理坐标
                float3 positionWS : TEXCOORD1; //世界空间位置
                float4 shadowCoord : TEXCOORD2; //阴影坐标
                float4 color : COLOR; //颜色
                
                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert (VertexInput IN)
            {
                Varyings OUT = (Varyings)0;

                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                #if defined(_OUTLINE_ON)

                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                //坐标转换
                OUT.uv = IN.texcoord;
                OUT.positionWS = vertexInput.positionWS;
                //阴影坐标
                OUT.shadowCoord = TransformWorldToShadowCoord(OUT.positionWS);

                //描边宽度
                half outlineWidth = _FloatOutlineWidth * 0.001;

                //切线
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);

                //顶点色法线
                float3 colorDir = IN.color.rgb;
                float3 binormalWS = cross(normalInput.normalWS, colorDir) * tangentWS.w;//副法线
                colorDir = normalize(mul(colorDir, half3x3(tangentWS.xyz, binormalWS, normalInput.normalWS)));

                //描边类型，通过lerp和step构建的if选择器
                float3 moveDir =
                    lerp(normalInput.normalWS.rgb,
                    lerp(colorDir, tangentWS.xyz, step(1.01, _FloatOutlineType)),
                    step(0.01, _FloatOutlineType)
                    );
                moveDir = normalize(moveDir);
                #if defined(_OUTLINE_WIDTH_SAME) //等宽
                    //沿法线方向外扩
                    OUT.positionCS = TransformWorldToHClip(OUT.positionWS.xyz + moveDir * outlineWidth);
                #elif defined(_OUTLINE_WIDTH_SCALING) //变化
                    half3 vertDir = normalize(IN.positionOS).xyz;
                    half signVertNormal = dot(vertDir, moveDir) + 0.3;
                    OUT.positionCS = TransformWorldToHClip(OUT.positionWS.xyz + moveDir * outlineWidth * signVertNormal);
                #endif

                //TODO 实时光照相关，描边受光照和阴影的影响
                OUT.color.rgb = half3(1, 1, 1);
                
                #endif

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 colorFinal = half4(1, 1, 1, 1);
                
                #if defined(_OUTLINE_ON)

                //外描边颜色和光照色混合
                half4 colorOutlineLightBlend = _ColorOutlineColor * IN.color;

                //基础贴图颜色混合
                half4 colorBaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)) * _BaseColor;
                half4 colorBaseMapBlend = lerp(colorOutlineLightBlend, colorOutlineLightBlend * colorBaseMap, _FloatOutlineBaseMapBlendIntensity);
                colorFinal = lerp(colorOutlineLightBlend, colorBaseMapBlend, _ToggleOutlineBaseMapBlend);

                //TODO 纹理贴图颜色混合

                //TODO 表面类型
                colorFinal = half4(colorFinal.rgb, 1);

                #endif

                return colorFinal;
            }

            ENDHLSL
        }
    }

    CustomEditor "BlurToonURP.EditorGUIx.ShaderGUILit"
}