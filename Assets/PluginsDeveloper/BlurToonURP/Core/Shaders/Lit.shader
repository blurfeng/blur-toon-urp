Shader "BlurToonURP/Lit"
{
    Properties
    {
        //----------- Basic 基础设置（渲染面 / 裁剪 / 模板测试）-----------
        //渲染面：值即 Cull 模式 0=Off(双面) 1=Front(渲染反面) 2=Back(渲染正面)，由编辑器下拉设置
        _IntRenderFaceType ("RenderFace Type", Float) = 2 //渲染面 0=Both 1=Back 2=Front
        //渲染队列
        _ToggleRenderQueueAuto ("RenderQueue Auto", Float) = 1 //渲染队列自动设置 ●记录 编辑器执行
        //裁剪 Clip（溶解，与下方“透明度裁切”相互独立）
        _IntClipType ("Clip Type", Float) = 0 //裁剪类型 0=Off 1=Dither 2=Alpha ●记录 驱动关键词
        _TexClipMaskMap ("Clip MaskMap", 2D) = "white" {} //裁剪遮罩贴图
        _FloatClipIntensity ("Clip Intensity", Range(0, 1)) = 0 //挖孔裁剪强度
        _FloatClipTransIntensity ("ClipTrans Intensity", Range(-1, 1)) = 0 //透明度裁剪强度
        _ToggleClipTransBaseMapAlpha ("ClipTrans BaseMapAlpha", Float) = 0 //基础贴图A通道生效
        //模板测试 Stencil（同组序号的材质互相影响）
        _IntStencilType ("Stencil Type", Float) = 0 //模板类型 0=Off 1=Discard 2=Reserve ●记录 驱动预设
        _FloatStencilNum ("Stencil No", Float) = 1 //模板组序号（Ref）
        _FloatStencilComp ("Stencil Comparison", Float) = 0 //比较规则（CompareFunction）
        _FloatStencilPass ("Stencil Pass Op", Float) = 0 //测试通过写入规则（StencilOp）
        _FloatStencilFail ("Stencil Fail Op", Float) = 0 //测试失败写入规则（StencilOp）

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

        //漫反射过渡方式（程序化色阶 / Ramp 贴图软过渡）
        _FloatDiffuseType ("Diffuse Type", Float) = 0 //0=色阶(程序化) 1=Ramp贴图 ●记录 设置关键词 _BASEMAP_DIFFUSE_RAMP_ON
        _TexDiffuseRamp ("Diffuse Ramp", 2D) = "white" {} //漫反射渐变贴图（横向 左=暗 右=亮）；默认白=不变暗
        _FloatDiffuseRampV ("Diffuse Ramp Row V", Range(0, 1)) = 0.5 //Ramp 行选择（多行渐变图集）


        //----------- Surface 表面类型 / 透明度裁切 -----------
        [HideInInspector] _Surface ("Surface Type", Float) = 0 //表面类型 0=Opaque 1=Transparent ●记录 由编辑器按类型设置渲染状态
        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1 //源混合因子 ●编辑器设置：Opaque=One  Transparent=SrcAlpha
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0 //目标混合因子 ●编辑器设置：Opaque=Zero  Transparent=OneMinusSrcAlpha
        [HideInInspector] _ZWrite ("ZWrite", Float) = 1 //深度写入 ●编辑器设置：Opaque=1  Transparent=0
        _ToggleAlphaClip ("Alpha Clip Toggle", Float) = 0 //开关 透明度裁切 ●记录 设置关键词 _ALPHATEST_ON
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5 //透明度裁切阈值


        //----------- NormalMap 法线贴图 -----------
        _BumpMap ("Bump Map", 2D) = "bump" {} //法线贴图
        _BumpScale ("Bump Scale", Range(0, 1)) = 1 //强度
        //开关
        _ToggleNormalMapOnBaseMap ("NormalMap On BaseMap", Float) = 0 //开关 基础贴图
        _ToggleNormalMapOnHighLight ("NormalMap On HighLight", Float) = 0 //开关 高光
        _ToggleNormalMapOnMatCap ("NormalMap On MatCap", Float) = 0 //开关 材质捕获
        _ToggleNormalMapOnEmissive ("NormalMap On Emissive", Float) = 0 //开关 自发光（视角变化颜色）
        //（边缘光的法线来源已改为边缘光面板中的专属配置 _FloatRimLightNormalSource，此处不再共用开关）


        //----------- HighLight 高光 -----------
        _ToggleHighLight ("HighLight Toggle", Float) = 0 //高光开关 ●仅用于记录 设置关键词开启
        _TexHighLightMap ("HighLight Map", 2D) = "white" {} //高光贴图（RGB 调制高光色，默认白=纯色高光）
        [HDR]_ColorHighLightColor ("HighLight Color", Color) = (1, 1, 1, 1) //颜色
        _FloatHighLightIntensity ("HighLight Intensity", Range(0, 1)) = 0.5 //强度
        _FloatHighLightSize ("HighLight Size", Range(0, 1)) = 0.5 //大小（范围）
        _FloatHighLightBlur ("HighLight Blur", Range(0, 1)) = 0.1 //边缘羽化
        //阴影遮罩
        _ToggleHighLightShadowMask ("HighLight ShadowMask Toggle", Float) = 0 //开关 阴影遮罩
        _FloatHighLightShadowMaskIntensity ("HighLight ShadowMask Intensity", Range(0, 1)) = 0 //阴影遮罩强度
        _TexHighLightMaskMap ("HighLight MaskMap", 2D) = "white" {} //遮罩贴图
        _FloatHighLightMaskMapIntensity ("HighLight MaskMap Intensity", Range(0, 1)) = 1 //遮罩贴图 强度


        //----------- Outline 外描边 -----------
        _FloatOutlineType("Outline Type", Float) = 0 //外描边类型 0=VertexNormal 1=VertexColor 2=VertexTangent
        _FloatOutlineWidthType("Outline Width Type", Float) = 0 //外描边宽度类型 ●记录值 确认对应的keywords设置
        [HDR]_ColorOutlineColor ("Outline Color", Color) = (0.4,0.4,0.4,1) //颜色
        _FloatOutlineWidth ("Outline Width", Float ) = 1.5 //宽度
        _ToggleOutlineBaseMapBlend ("Outline BaseMapBlend", Float ) = 1 //开关 基础贴图混合
        _FloatOutlineBaseMapBlendIntensity ("Outline BaseMapBlend Intensity", Range(0, 1) ) = 1 //基础贴图混合 强度
        //描边纹理贴图
        _TexOutlineMap ("Outline Map", 2D) = "white" {} //描边纹理贴图（描边专用纹理，调制描边颜色）
        _FloatOutlineMapIntensity ("Outline Map Intensity", Range(0, 1)) = 1 //描边纹理混合 强度

        
        //----------- Rim Light 边缘光 -----------
        _ToggleRimLight ("RimLight Toggle", Float) = 0 //边缘光开关 ●仅用于记录 设置关键词开启
        [HDR] _ColorRimLightColor ("RimLight Color", Color) = (1, 1, 1, 0.3) //颜色
        _FloatRimLightIntensity ("RimLight Intensity", Range(0, 1)) = 0.8 //强度
        _FloatRimLightInsideDistance ("RimLight Inside Distance", Range(0, 1)) = 0.18 //内部距离
        _ToggleRimLightHard ("RimLight Hard", Float) = 0 //开关 硬边缘
        //边缘检测方式：菲涅尔(法线夹角) / 深度差(屏幕空间深度断层)。两种方式共用颜色/强度/内部距离/硬边缘/暗部遮罩/遮罩贴图。
        _FloatRimLightType ("RimLight Type", Float) = 0 //边缘检测方式 0=菲涅尔 1=深度差
        _FloatRimLightDepthWidth ("RimLight Depth Width", Range(0, 16)) = 2 //深度差 采样宽度(像素, 1080p基准)
        _FloatRimLightDepthThreshold ("RimLight Depth Threshold", Range(0, 1)) = 0.1 //深度差 阈值(世界单位, 抑制内部噪声)
        _FloatRimLightDepthThresholdSoft ("RimLight Depth Threshold Soft", Range(0, 1)) = 0.1 //深度差 阈值软过渡
        //法线来源（边缘光专属，仅"菲涅尔"方式使用）
        _FloatRimLightNormalSource ("RimLight Normal Source", Float) = 0 //法线来源 0=几何法线 1=法线贴图 2=混合
        _FloatRimLightNormalMapBlend ("RimLight NormalMap Blend", Range(0, 1)) = 1 //混合模式下 几何↔法线贴图 的混合强度
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


        //----------- MatCap 材质捕获 -----------
        _ToggleMatCap ("MatCap Toggle", Float ) = 0 //材质捕获开关 ●仅用于记录 设置关键词开启
        _TexMatCapMap ("MatCapMap", 2D) = "black" {} //材质捕获贴图
        [HDR]_ColorMatCapMapColor ("MatCapMap Color", Color) = (1,1,1,1) //材质捕获 颜色
        _FloatMatCapColorBlend ("MatCap ColorBlend", Float ) = 1 //颜色混合模式 0=Additive 1=Multiply 2=Lerp ●记录 驱动关键词
        _FloatMatCapColorBlendIntensity ("MatCap ColorBlend Intensity", Range(0, 1)) = 1 //颜色混合强度
        _FloatMatCapRotate ("MatCap Rotate", Range(-1, 1)) = 0 //旋转
        //阴影遮罩
        _ToggleMatCapShadowMask ("MatCap ShadowMask Toggle", Float ) = 0 //开关 阴影遮罩
        _FloatMatCapShadowMaskIntensity ("MatCap ShadowMask Intensity", Range(0, 1)) = 0 //阴影遮罩强度
        //遮罩贴图
        _TexMatCapMaskMap ("MatCap MaskMap", 2D) = "white" {} //遮罩贴图
        _FloatMatCapMaskMapIntensity ("MatCap MaskMap Intensity", Range(-1, 1)) = 0 //遮罩贴图强度


        //----------- Emissive 自发光 -----------
        _ToggleEmissive ("Emissive Toggle", Float ) = 0 //自发光开关 ●仅用于记录 设置关键词开启
        _TexEmissiveMap ("EmissiveMap", 2D) = "white" {} //自发光贴图（RGB=颜色，A=强度）
        [HDR]_ColorEmissiveMapColor ("EmissiveMap Color", Color) = (0,0,0,1) //自发光 颜色（HDR，默认黑=不发光）
        //自发光动画
        _ToggleEmissiveAnim ("EmissiveAnim Toggle", Float) = 0 //动画开关 ●记录 驱动关键词（关=固定 开=动画）
        _FloatEmissiveAnimUVType ("EmissiveAnim UVType", Float) = 0 //UV比例类型 0=FullMap 1=MatCap
        _FloatEmissiveAnimSpeed ("EmissiveAnim Speed", Float ) = 0.5 //移动速度
        _FloatEmissiveAnimDirU ("EmissiveAnim DirU", Range(-1, 1)) = 0.5 //移动方向U
        _FloatEmissiveAnimDirV ("EmissiveAnim DirV", Range(-1, 1)) = 0.5 //移动方向V
        _FloatEmissiveAnimRotate ("EmissiveAnim Rotate", Float ) = 0 //旋转速度
        _ToggleEmissiveAnimPingpong ("EmissiveAnim Pingpong", Float) = 0 //开关 来回移动
        //变化颜色
        _ToggleEmissiveChangeColor ("Emissive ChangeColor Toggle", Float) = 0 //开关 变化颜色
        [HDR]_ColorEmissiveChangeColor ("Emissive ChangeColor", Color) = (0,0,0,1) //变化颜色（HDR）
        _FloatEmissiveChangeSpeed ("Emissive ChangeSpeed", Float ) = 0 //变化速度
        //视角变化颜色
        _ToggleEmissiveViewChangeColor ("Emissive ViewChangeColor Toggle", Float) = 0 //开关 视角变化颜色
        [HDR]_ColorEmissiveViewChangeColor ("Emissive ViewChangeColor", Color) = (0,0,0,1) //视角变化颜色（HDR）

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
        _GlobalLightBaseShade2MixedIntensity ("GlobalLight BaseShade2 Mixed Intensity", Range(0.001, 1)) = 0.5//暗部2和光照颜色的混合强度 0-1
        _ToggleGlobalLightHighLight ("GlobalLight HighLight Toggle", Float) = 1 //高光
        _ToggleGlobalLightRimLight ("GlobalLight RimLight Toggle", Float) = 1 //边缘光
        _GlobalLightRimLightMixedIntensity ("GlobalLight RimLight Mixed Intensity", Range(0.001, 1)) = 0.5//边缘光和光照颜色的混合强度 0-1
        _ToggleGlobalLightRimLightShade ("GlobalLight RimLightShade Toggle", Float) = 1 //边缘光暗部
        _GlobalLightRimLightShadeMixedIntensity ("GlobalLight RimLightShade Mixed Intensity", Range(0.001, 1)) = 0.5//边缘光暗部和光照颜色的混合强度 0-1
        _ToggleGlobalLightOutline ("GlobalLight Outline Toggle", Float) = 1 //描边
        _GlobalLightOutlineMixedIntensity ("GlobalLight Outline Mixed Intensity", Range(0.001, 1)) = 0.5//描边和光照颜色的混合强度 0-1
        _ToggleGlobalLightMatCapMap ("GlobalLight MatCapMap Toggle", Float) = 1 //材质捕获
        
        //阴影设置
        //（阴影投射没有对应属性：由编辑器直接开关 ShadowCaster Pass，状态存于材质的 disabledShaderPasses）
        _ToggleShadowReceive ("ShadowReceive Toggle", Float ) = 1 //开关 阴影接收
        _FloatShadowIntensity ("Shadow Intensity", Range(-1, 1)) = 0 //阴影强度
        //明暗交界处理模式(默认关)：
        // 关(0)=直接使用阴影图(原始行为)：投射阴影覆盖包含交界在内的全部区域，但交界可能出现阴影图分辨率导致的锯齿。
        // 开(1)=交界柔化：把几何(NdotL)平滑自阴影包络 与 阴影图 取“较暗者(min)”融合——两条单调曲线取min仍单调，绝不产生亮缝；
        //        交界由平滑几何主导(消锯齿)、更暗的投射阴影仍能穿透、背光侧照常压暗。柔化程度由“柔化值”控制。
        _ToggleShadowTerminatorSmooth ("Shadow Terminator Smooth Toggle", Float) = 0
        //交界柔化值(仅交界柔化开启时生效)：几何平滑自阴影包络的过渡半宽。越大交界越平滑(几何主导范围越大)，越小交界越锐、投射阴影越贴近交界。任何值都不会出现亮缝。
        _FloatShadowTerminatorSmooth ("Shadow Terminator Smooth", Range(0.02, 0.5)) = 0.1
        //自阴影偏移（在URP全局阴影bias之上，对易出现自阴影粉刺/交界碎裂的模型单独补偿；默认0=不额外偏移，行为不变）
        _FloatSelfShadowDepthBias ("SelfShadow DepthBias", Range(0, 1)) = 0 //沿光方向深度偏移，增大减少自阴影粉刺（过大漏光）
        _FloatSelfShadowNormalBias ("SelfShadow NormalBias", Range(0, 1)) = 0 //沿法线内缩（按1-NoL坡度缩放），增大压制明暗交界碎裂
        //低质量PCF：强制用低质量PCF核重采样主光阴影，减少角色尺度下的阴影透视锯齿（默认0=用URP原采样，不影响原效果）
        _ToggleShadowLowQualityPCF ("Shadow LowQuality PCF Toggle", Float) = 0

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
        _ToggleLightHorLockHighLight ("HorizontalLock HighLight", Float) = 0 //高光
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
            //混合与深度写入由“表面类型”驱动（Opaque=One/Zero/ZWrite On，Transparent=SrcAlpha/OneMinusSrcAlpha/ZWrite Off），编辑器设置对应属性值
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            //渲染面：由“基础设置-渲染面”驱动（0=Off双面 1=Front渲染反面 2=Back渲染正面）
            Cull [_IntRenderFaceType]
            //模板测试：由“基础设置-模板测试”驱动，同组序号材质互相影响（Comp/Pass/Fail 按模板类型预设）
            Stencil
            {
                Ref [_FloatStencilNum]
                Comp [_FloatStencilComp]
                Pass [_FloatStencilPass]
                Fail [_FloatStencilFail]
            }

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            
            // URP 主光阴影接收：补齐后 shadowAttenuation 才会采样真实阴影图（本体接收场景投射阴影）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //URP 14 软阴影：质量由 _SHADOWS_SOFT_LOW/_MEDIUM/_HIGH 关键词区分（管线开启软阴影时会禁用通用 _SHADOWS_SOFT 只启用对应质量）。
            //必须声明全部变体，否则管线启用如 _SHADOWS_SOFT_MEDIUM 时本 Shader 无匹配变体 → 回退到无软阴影 → 即使勾选软阴影也是硬阴影。
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            //逐对象阴影：由 BlurToonPerObjectShadowFeature 逐帧开关。未添加该 Feature 或本帧无投射者时关闭，零开销。
            #pragma multi_compile_fragment _ _BLURTOON_PER_OBJECT_SHADOW
            // Forward+ 渲染路径：附加光照改走聚簇(Cluster)光照循环，需此关键词让 LIGHT_LOOP_BEGIN 切换到聚簇迭代；
            // 缺失时在 Forward+ 渲染器下 GetAdditionalLightsCount 返回 0，附加光照(点光/聚光高光)会静默失效。
            #pragma multi_compile _ _FORWARD_PLUS

            // BlurToonURP Keywords
            #pragma shader_feature_local _ALPHATEST_ON //透明度裁切
            #pragma shader_feature_local _ _CLIP_DITHER _CLIP_ALPHA //裁剪（溶解）：无=关闭 / 挖孔 / 透明度
            #pragma shader_feature_local _BASEMAP_SHADE_THRESHOLDMAP_ON //暗部阈值贴图
            #pragma shader_feature_local _BASEMAP_DIFFUSE_RAMP_ON //漫反射 Ramp贴图 软过渡方式
			#pragma shader_feature_local _ADDLIGHT_ON // 附加光照
            #pragma shader_feature_local _BUILTINLIGHT_ON // 内置光照
            //高光
            #pragma shader_feature_local _HIGHLIGHT_ON
            #pragma shader_feature_local _HIGHLIGHT_MASKMAP_ON
            //边缘光
            #pragma shader_feature_local _RIMLIGHT_ON
            #pragma shader_feature_local _RIMLIGHT_SHADEMASK_ON
            #pragma shader_feature_local _RIMLIGHT_SHADEMASK_COLOR_ON
            #pragma shader_feature_local _RIMLIGHT_MASKMAP_ON
            #pragma shader_feature_local _RIMLIGHT_DEPTH_ON //边缘光 深度差检测方式
            //材质捕获
            #pragma shader_feature_local _MATCAP_ON
            #pragma shader_feature_local _ _MATCAP_COLORBLEND_MULTIPLY _MATCAP_COLORBLEND_LERP //无=Additive / 乘算 / 插值
            //自发光
            #pragma shader_feature_local _EMISSIVE_ON
            #pragma shader_feature_local _ _EMISSIVE_ANIM //无=固定 / 动画
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            //场景深度图（_CameraDepthTexture / SampleSceneDepth）——深度差边缘光使用，需在 URP Asset 开启 Depth Texture
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


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

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //定义的字段属性
            //裁剪遮罩贴图（仅挖孔/透明度模式启用）
            #if defined(_CLIP_DITHER) || defined(_CLIP_ALPHA)
            TEXTURE2D(_TexClipMaskMap); SAMPLER(sampler_TexClipMaskMap); //裁剪遮罩贴图
            #endif

            //暗部阈值贴图
            #if defined(_BASEMAP_SHADE_THRESHOLDMAP_ON)
            TEXTURE2D(_TexShadeThresholdMap); SAMPLER(sampler_TexShadeThresholdMap); //暗部阈值贴图
            #endif

            //漫反射 Ramp 渐变贴图（软过渡方式）。采样复用 URP 全局内联采样器 sampler_LinearClamp
            //（core/ShaderLibrary/GlobalSamplers.hlsl 已全局声明）强制 线性+钳制，与贴图导入的 Wrap/Filter 无关：
            //钳制避免 halfLambert 两端环绕取到反向颜色，线性保证渐变平滑。
            //注意：切勿在此再次 SAMPLER(sampler_LinearClamp)——URP 已声明，重复声明会导致 shader 重定义报错（材质变紫）。
            #if defined(_BASEMAP_DIFFUSE_RAMP_ON)
            TEXTURE2D(_TexDiffuseRamp);
            #endif

            #if defined(_RIMLIGHT_ON) && defined(_RIMLIGHT_MASKMAP_ON)
            TEXTURE2D(_TexRimLightMaskMap); SAMPLER(sampler_TexRimLightMaskMap);
            #endif

            //高光贴图（高光开启时始终采样，默认白=纯色高光）
            #if defined(_HIGHLIGHT_ON)
            TEXTURE2D(_TexHighLightMap); SAMPLER(sampler_TexHighLightMap);
            #endif
            //高光遮罩贴图
            #if defined(_HIGHLIGHT_ON) && defined(_HIGHLIGHT_MASKMAP_ON)
            TEXTURE2D(_TexHighLightMaskMap); SAMPLER(sampler_TexHighLightMaskMap);
            #endif

            //材质捕获贴图 & 遮罩贴图
            #if defined(_MATCAP_ON)
            TEXTURE2D(_TexMatCapMap); SAMPLER(sampler_TexMatCapMap);
            TEXTURE2D(_TexMatCapMaskMap); SAMPLER(sampler_TexMatCapMaskMap);
            #endif

            //自发光贴图
            #if defined(_EMISSIVE_ON)
            TEXTURE2D(_TexEmissiveMap); SAMPLER(sampler_TexEmissiveMap);
            #endif

            //材质属性统一在 LitInput.hlsl 中声明（保证与其它 Pass 的 UnityPerMaterial 完全一致，兼容 SRP Batcher）
            #include "LitInput.hlsl"
            //公共函数库（RotateUV 等）
            #include "BlurFunction.hlsl"
            //阴影函数库（低质量PCF主光阴影采样等）
            #include "ShadowFunction.hlsl"
            //逐对象阴影函数库（高密度瓦片阴影图采样）
            #include "PerObjectShadowFunction.hlsl"

            //==== 深度差边缘光 相关函数 ==================================================
            //跨投影的线性眼空间深度：透视用 LinearEyeDepth；正交按 near..far 线性还原（兼容反向Z）。
            float RimLinearEyeDepth(float rawDepth)
            {
                float persp = LinearEyeDepth(rawDepth, _ZBufferParams);
                float ndc = rawDepth;
                #if UNITY_REVERSED_Z
                ndc = 1.0 - rawDepth;
                #endif
                float ortho = lerp(_ProjectionParams.y, _ProjectionParams.z, ndc); //near..far
                return lerp(persp, ortho, unity_OrthoParams.w); //unity_OrthoParams.w: 1=正交 0=透视
            }

            //深度差边缘信号 ∈[0,1]：沿屏幕“水平方向”朝轮廓外侧偏移若干像素采样场景深度，
            //偏移点比当前像素更远（外侧是更远的背景）时判定为朝向相机的轮廓边缘 → 输出边缘强度。
            //当前像素深度取片元自身 positionCS.z（不依赖深度图是否已包含本物体），偏移点取 _CameraDepthTexture。
            //参考 StarRailNPRShader GetRimLightMask，按本工程简化（无 modelScale/lightMap；采样宽度为屏幕像素、未做透视近大远小校正）。
            //仅水平偏移：主要检测左右(竖直)轮廓，与参考实现一致；且规避 SV_Position 的 Y 轴方向在不同图形 API 下不一致（D3D 向下/GL 向上）导致的上下轮廓偏移方向错误。
            //需在 URP Asset 开启 Depth Texture，否则 _CameraDepthTexture 无效、本方式无效果（可能整体发亮）。
            float DepthDiffRimSignal(float4 positionCS, float3 normalDirWS, float widthPixels, float threshold, float thresholdSoft)
            {
                //当前像素线性眼空间深度
                float depth = RimLinearEyeDepth(positionCS.z);

                //采样偏移方向：屏幕水平方向指向轮廓外侧（取 view 空间法线 x 的符号；+x=右在 view 与像素空间一致，无 Y 翻转问题）。
                //法线朝右(左)的一侧向右(左)偏移，正好跨过竖直轮廓采到外侧更远的背景。
                float3 normalVS = TransformWorldToViewDir(normalDirWS, true);
                float2 offsetDir = float2(sign(normalVS.x), 0.0);

                //偏移像素量：以 1080p 为基准做分辨率无关，并限幅避免极端过宽
                float rimWidth = min(widthPixels * (_ScreenParams.y / 1080.0), 128.0);

                //用整数像素坐标 Load（与 SV_Position 同坐标系，规避不同平台 UV 上下翻转），并夹取到屏幕范围内。
                //-0.5 将 SV_Position 的像素中心对齐到纹素索引。
                int2 loadCoord = int2(positionCS.xy - 0.5 + offsetDir * rimWidth);
                loadCoord = clamp(loadCoord, int2(0, 0), int2(_ScreenParams.xy) - 1);
                float offsetDepth = RimLinearEyeDepth(LoadSceneDepth((uint2)loadCoord));

                //偏移点更远(diff>0)才产生边缘；threshold 抑制内部深度噪声，thresholdSoft 控制柔和度
                float diff = offsetDepth - depth;
                return saturate(smoothstep(threshold, threshold + max(thresholdSoft, 1e-4), diff));
            }
            //============================================================================

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

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                float2 uv = IN.uv;
                //基础贴图采样。sampler_BaseMap是Unity自动生成的对应采样器不需要额外定义。
                float4 colorBaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

                //透明度裁切：Alpha（基础贴图×基础色）低于阈值的像素被丢弃（cutout），需尽早执行以省去后续计算。
                #if defined(_ALPHATEST_ON)
                clip(colorBaseMap.a - _Cutoff);
                #endif

                //-------- Clip 裁剪（溶解）-------- Start
                //与“透明度裁切”相互独立：从裁剪遮罩贴图 R 通道采样强度，做挖孔剔除或透明度淡出。
                #if defined(_CLIP_DITHER)
                //挖孔：低于阈值的像素直接剔除
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(uv, _TexClipMaskMap)).r;
                clip(clipMask - _FloatClipIntensity);
                #elif defined(_CLIP_ALPHA)
                //透明度：按遮罩（可叠加基础贴图A通道）计算裁剪值，剔除并用于最终Alpha（Transparent 表面呈现淡出）
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(uv, _TexClipMaskMap)).r;
                half clipAlpha = lerp(clipMask, clipMask * colorBaseMap.a, _ToggleClipTransBaseMapAlpha) - _FloatClipTransIntensity;
                clip(clipAlpha);
                #endif
                //-------- Clip 裁剪（溶解）-------- End

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS); //观察方向
                float3 normalDirWS = IN.normalWS; //法线方向
                
                //-------- NormalMap 法线贴图 -------- Start
                //法线贴图采样
                float3 normalDirTex = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(uv, _BumpMap)), _BumpScale);
                //将法线贴图中获取的法线转换至世界空间
                //副法线必须由“世界法线 × 世界切线”叉乘得到（tangentWS.w 已含奇偶缩放符号）；
                //切勿用刚采样出的切线空间法线 normalDirTex 参与叉乘，否则 TBN 中间轴与 UV 的 V 方向脱钩，法线细节朝向错乱。
                float3 binormalWS = cross(normalDirWS, IN.tangentWS.xyz) * IN.tangentWS.w; //世界空间的副法线
                normalDirTex = normalize(mul(normalDirTex, half3x3(IN.tangentWS.xyz, binormalWS, normalDirWS)));
                //-------- NormalMap 法线贴图 -------- End

                //-------- Light 光照计算 -------- Start
                //环境光照（Lighting中设置的环境光照等）
                half3 envLightColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
                //自定义的环境光照强度影响
                envLightColor *= _FloatEnvLightIntensity;

                //实时光照
                //主光照
                float4 mainLightShadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light lightMain = GetMainLight(mainLightShadowCoord);
                //可选：用低质量PCF重采样主光阴影(更贴角色尺度、减透视锯齿)；默认关(0)时保持URP原采样
                if (_ToggleShadowLowQualityPCF > 0.5)
                    lightMain.shadowAttenuation = MainLightShadowLowQualityPCF(mainLightShadowCoord, IN.positionWS);
                //逐对象阴影：角色独占一块紧贴自身包围盒的阴影瓦片，纹素密度比级联高数倍，
                //且投影尺寸已量化、中心已吸附到纹素栅格 → 硬边干净、光源与相机移动时不再逐帧重新量化。
                //瓦片只画角色自身，场景投影仍由级联图提供，两者按 Feature 上的 CombineMode 合并
                //（默认 SceneAndSelf：先把角色自身从级联图里剔除再取 min，场景投影与高清自阴影兼得）。
                //未命中瓦片/超出距离时自动回退 URP 结果。
                #if defined(_BLURTOON_PER_OBJECT_SHADOW)
                lightMain.shadowAttenuation = BlurToonApplyPerObjectShadow(
                    lightMain.shadowAttenuation, IN.positionWS, IN.normalWS, lightMain.direction,
                    length(IN.positionWS - _WorldSpaceCameraPos), _ToggleShadowLowQualityPCF);
                #endif
                half3 colorLightMain = lightMain.color * lightMain.distanceAttenuation;
                //阴影衰减
                float shadowAttenuation = saturate(lightMain.shadowAttenuation - _FloatShadowIntensity);
                float3 lightDirWS = lightMain.direction;
                //光照颜色
                half3 realtimeLightColor = colorLightMain;
                
                //附加光照（点光源/聚光灯）
                //方向着色：附加光照“不参与”主光的半兰伯特色阶分段(暗部1/2)，只按“各自光照方向”对表面做半兰伯特，
                //          再累加 光色×距离衰减，形成有方向感的点光/聚光提亮，避免整体均匀发白。
                #if defined(_ADDLIGHT_ON)
                half3 colorLightAdd = half3(0, 0, 0); //必须初始化为0，否则会累加到未定义值上
                uint lightsCount = GetAdditionalLightsCount();
                //Forward+ 下 LIGHT_LOOP_BEGIN 宏会引用 inputData 的屏幕UV/世界坐标来做聚簇光照迭代，需在此提供；
                //经典 Forward 路径不编译此分支（USE_FORWARD_PLUS 未定义时为0），保持原有行为。
                #if USE_FORWARD_PLUS
                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                #endif
                LIGHT_LOOP_BEGIN(lightsCount)
                    Light light = GetAdditionalLight(lightIndex, IN.positionWS);
                    //各附加光照自身方向的半兰伯特，实现方向着色（面向光的一侧更亮，背光侧更暗）
                    half addLightHalfLambert = dot(normalDirWS, light.direction) * 0.5 + 0.5;
                    //光色 × 距离衰减 × 方向着色
                    half3 lightColor = light.color * light.distanceAttenuation * addLightHalfLambert;
                    colorLightAdd += lightColor;
                LIGHT_LOOP_END
                //自定义 附加光照强度
                colorLightAdd *= _FloatAddLightIntensity;
                //混合实时光照颜色（仅作为叠加提亮，不参与主光色阶分段）
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
                float NdotL = dot(normalDirOnBaseMap, lightDirOnBaseMap); //兰伯特余弦 [-1,1]
                float halfLambert = NdotL * 0.5 + 0.5; //半兰伯特 [0,1]
                //根据开关，计算阴影的影响
                //明暗交界有两种处理模式(_ToggleShadowTerminatorSmooth)：
                // 直接模式(0，默认)：直接用阴影图。投射阴影覆盖包含交界在内的全部区域；缺点是交界会暴露阴影图分辨率的锯齿。
                // 柔化模式(1)：几何(NdotL)平滑自阴影包络 与 阴影图 取“较暗者(min)”融合。
                //   两条单调递增曲线取 min 仍单调递增 → 不会出现亮脊/亮缝(自阴影与接收阴影完全融合为一条连续的暗)；
                //   交界由平滑几何包络主导(消锯齿，柔化值越大主导范围越大越平滑)，真正更暗的投射阴影仍能穿透显示，背光侧照常压暗。
                float terminatorSmooth = max(_FloatShadowTerminatorSmooth, 1e-4);
                float selfShadowSmooth = smoothstep(-terminatorSmooth, terminatorSmooth, NdotL); //几何平滑自阴影包络 0=背光(暗) 1=受光(亮)
                float shadowReceiveSmooth = min(selfShadowSmooth, shadowAttenuation);            //取较暗者：结果单调 → 无亮缝，自阴影/接收阴影融合为一
                //按模式在“直接(原始，直接用阴影图) / 柔化(min融合)”之间选择
                float shadowReceive = lerp(shadowAttenuation, shadowReceiveSmooth, _ToggleShadowTerminatorSmooth);
                halfLambert = lerp(halfLambert, halfLambert * shadowReceive, _ToggleShadowReceive);

                //暗部阈值贴图
                #if defined(_BASEMAP_SHADE_THRESHOLDMAP_ON)
                //根据阈值进行强度采样
                float shadeThresholdValue = 1 - SAMPLE_TEXTURE2D(_TexShadeThresholdMap, sampler_TexShadeThresholdMap, TRANSFORM_TEX(uv, _TexShadeThresholdMap)).r;
                shadeThresholdValue *= _FloatShadeThresholdMapIntensity;//自定义强度影响
                halfLambert = saturate(halfLambert - shadeThresholdValue);
                #endif
                
                //漫反射过渡：两种方式二选一，均产出下游共用的 colorFinalBlend 与 lightIntensityShade1/2。
                #if defined(_BASEMAP_DIFFUSE_RAMP_ON)
                //【Ramp 贴图】用 halfLambert 作横向采样坐标(左0=暗 右1=亮)取一维渐变，渐变自带明暗过渡与暗部色，
                //直接乘到基础反照率。美术在贴图内自由控制过渡软硬与分段，替代程序化两段色阶(暗部1/2 色 + 位置/模糊)。
                //halfLambert 已含接收阴影/暗部阈值贴图的影响 → 阴影会把采样推向渐变暗端。
                //saturate 保证坐标∈[0,1]；LOD0 采样避免明暗交界处 halfLambert 屏幕导数过大而选到模糊 mip。
                float rampU = saturate(halfLambert);
                half3 rampColor = SAMPLE_TEXTURE2D_LOD(_TexDiffuseRamp, sampler_LinearClamp, float2(rampU, _FloatDiffuseRampV), 0).rgb;
                float3 colorFinalBlend = colorBaseMapFinal * rampColor;
                //供下游(高光/材质捕获的“阴影遮罩”)复用的暗部因子：渐变越暗→越处于暗部。Rec709 亮度，自包含无需额外依赖。
                float lightIntensityShade1 = saturate(1 - dot(rampColor, half3(0.2126, 0.7152, 0.0722)));
                float lightIntensityShade2 = lightIntensityShade1;
                #else
                //【色阶】程序化两段色阶。
                //色阶过渡的屏幕空间抗锯齿：
                //过渡带宽度(模糊)是以 halfLambert 为单位的固定值。当过渡位置(Step)落在 halfLambert 屏幕梯度陡峭处
                //(如球体轮廓附近/掠射角/低模面片边界)时，过渡带在屏幕上会塌缩到亚像素宽度而形成硬边锯齿。
                //因此用 fwidth(halfLambert)(约等于相邻像素间 halfLambert 的变化量)作为过渡带的最小宽度，
                //保证过渡至少覆盖约1个像素而被抗锯齿；美术设置的模糊更大时按其原值，外观不变。
                float halfLambertFwidth = fwidth(halfLambert);
                //暗部1
                float blurBrightShade1 = max(_FloatBrightShade1Blur * 0.1, halfLambertFwidth); //模糊映射值与屏幕约1像素梯度取较大者
                float lightIntensityShade1 = 1 - saturate(1 + (halfLambert - _FloatBrightShade1Step) / blurBrightShade1);
                //暗部2
                float blurShade1Shade2 = max(_FloatShade1Shade2Blur * 0.05, halfLambertFwidth);
                float lightIntensityShade2 = 1 - saturate(1 + (halfLambert - _FloatShade1Shade2Step) / blurShade1Shade2);

                //混合颜色
                float3 colorFinalBlend = lerp(colorBaseMapFinal, lerp(colorBaseMapShade1, colorBaseMapShade2, lightIntensityShade2), lightIntensityShade1);
                #endif
                //-------- BaseMap 基础贴图 -------- End


                //-------- HighLight 高光 -------- Start
                #if defined(_HIGHLIGHT_ON)
                //高光使用的法线（根据开关，使用顶点法线或法线贴图法线）
                float3 normalDirOnHighLight = lerp(normalDirWS, normalDirTex, _ToggleNormalMapOnHighLight);
                //高光光照方向（可水平锁定：把高度锁到水平，使高光沿水平轴向变化）
                float3 lightDirOnHighLight = lightDirWS;
                lightDirOnHighLight.y = lerp(lightDirOnHighLight.y, 0, _ToggleLightHorLockHighLight);
                //半程向量（沿用受内置光照影响后的 lightDirWS，使高光跟随场景光/内置光方向）
                float3 highLightHalfDir = normalize(lightDirOnHighLight + viewDirWS);
                float highLightNdotH = saturate(dot(normalDirOnHighLight, highLightHalfDir));
                //高光大小：_FloatHighLightSize[0,1] 映射到镜面反射幂[512,4]，值越大幂越小、高光范围越大
                float highLightPower = exp2(lerp(9, 2, _FloatHighLightSize));
                float highLightSpec = pow(highLightNdotH, highLightPower);
                //卡通化：以 0.5 为分界做软阶跃，_FloatHighLightBlur 控制边缘羽化（0≈色阶硬边，大=柔边）
                float highLightBlur = max(_FloatHighLightBlur * 0.5, 0.0001);
                float highLightFactor = smoothstep(0.5 - highLightBlur, 0.5 + highLightBlur, highLightSpec);
                //接收阴影时，阴影处不出现高光
                highLightFactor *= lerp(1, shadowAttenuation, _ToggleShadowReceive);

                //高光遮罩贴图（在遮罩 R 通道绘制高光分布）
                #if defined(_HIGHLIGHT_MASKMAP_ON)
                float highLightMaskValue = SAMPLE_TEXTURE2D(_TexHighLightMaskMap, sampler_TexHighLightMaskMap, TRANSFORM_TEX(uv, _TexHighLightMaskMap)).r;
                highLightFactor = lerp(highLightFactor, highLightFactor * highLightMaskValue, _FloatHighLightMaskMapIntensity);
                #endif

                //高光颜色：高光贴图(RGB) × HDR颜色 × 透明度 × 强度
                half3 colorHighLight = SAMPLE_TEXTURE2D(_TexHighLightMap, sampler_TexHighLightMap, TRANSFORM_TEX(uv, _TexHighLightMap)).rgb;
                colorHighLight *= _ColorHighLightColor.rgb * _ColorHighLightColor.a * _FloatHighLightIntensity;
                //受光照影响 开关（开=乘光照色跟随光照；关=保持自身HDR色）
                colorHighLight = lerp(colorHighLight, colorHighLight * colorLightBlend, _ToggleGlobalLightHighLight);
                //阴影遮罩：暗部（暗部1区域）按强度压暗高光，避免阴影里出现不自然高光
                colorHighLight = lerp(colorHighLight, colorHighLight * (1 - lightIntensityShade1 * _FloatHighLightShadowMaskIntensity), _ToggleHighLightShadowMask);
                //叠加到最终颜色
                colorFinalBlend += colorHighLight * highLightFactor;
                #endif
                //-------- HighLight 高光 -------- End


                //-------- RimLight 边缘光 -------- Start
                #if defined(_RIMLIGHT_ON)
                //计算边缘光颜色，按配置混合光照颜色
                half3 colorRimLight = lerp(_ColorRimLightColor.rgb,
                    lerp(_ColorRimLightColor.rgb, _ColorRimLightColor.rgb * colorLightBlend, _GlobalLightRimLightMixedIntensity),
                    _ToggleGlobalLightRimLight);
                colorRimLight *= _ColorRimLightColor.a; //透明度

                //边缘信号 rimLightNdotV ∈[0,1]（越靠近边缘越大）：两种检测方式二选一，之后的所有处理
                //（强度/内部距离/硬边缘/暗部遮罩/暗部颜色/遮罩贴图）完全共用。
                #if defined(_RIMLIGHT_DEPTH_ON)
                //【深度差】屏幕空间沿“法线朝向”偏移采样场景深度，偏移点更远→朝相机的轮廓边缘→产生边缘光。
                //需在 URP Asset 开启 Depth Texture（否则本方式无效果，甚至整体发亮）。深度方式不使用法线来源配置。
                float rimLightNdotV = DepthDiffRimSignal(IN.positionCS, normalDirWS,
                    _FloatRimLightDepthWidth, _FloatRimLightDepthThreshold, _FloatRimLightDepthThresholdSoft);
                #else
                //【菲涅尔】法线和视线夹角，越靠近边缘值越大。范围为[0,1]。
                //法线方向来源（边缘光专属配置）：0=几何法线 1=法线贴图 2=混合。
                //用 lerp+step 构建无分支选择器（与“描边类型”同款写法），避免关键词变体膨胀。
                float3 normalDirRimBlend = normalize(lerp(normalDirWS, normalDirTex, _FloatRimLightNormalMapBlend));
                float3 normalDirOnRimLight =
                    lerp(normalDirWS,
                        lerp(normalDirTex, normalDirRimBlend, step(1.5, _FloatRimLightNormalSource)),
                        step(0.5, _FloatRimLightNormalSource));
                float rimLightNdotV = saturate(1 - dot(normalDirOnRimLight, viewDirWS));
                #endif
                //强度控制。强度[0,1]映射到[3,0]，exp2为2的x次幂，范围[8,2]。pow为x的y次幂。x=rimLightNdotV小于1，所以y越小rimLightFactor越大。
                //_FloatRimLightIntensity越大，rimLightFactor越大。rimLightFactor在(0,1]范围。
                float rimLightFactor = pow(rimLightNdotV, exp2(lerp(3, 0, _FloatRimLightIntensity)));
                //根据设定的内部延伸距离计算最终系数 或使用硬边缘
                //距离计算 & 硬边缘开关。
                //_FloatRimLightInsideDistance范围为[0,1]，值越大边缘光范围越窄，为1时没有边缘光。rimLightFactor在(0,1]范围。
                rimLightFactor = saturate(
                    lerp((rimLightFactor - _FloatRimLightInsideDistance) / max(1 - _FloatRimLightInsideDistance, 1e-4),
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
                    _ToggleGlobalLightRimLightShade);
                    colorRimLightShade *= _ColorRimLightShadeColor.a; //透明度
                    //暗部边缘光系数
                    float rimLightShadeFactor = pow(rimLightNdotV, exp2(lerp(3, 0, _FloatRimLightShadeColorIntensity)));
                    //距离计算 & 硬边缘开关。
                    rimLightShadeFactor = saturate(
                        lerp((rimLightShadeFactor - _FloatRimLightInsideDistance) / max(1 - _FloatRimLightInsideDistance, 1e-4),
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


                //-------- MatCap 材质捕获 -------- Start
                #if defined(_MATCAP_ON)
                //根据开关使用顶点法线或法线贴图法线
                float3 normalDirOnMatCap = lerp(normalDirWS, normalDirTex, _ToggleNormalMapOnMatCap);
                //法线转换到观察空间得到 MatCap 采样 UV（-1~1 映射到 0~1）
                //法线是“方向向量”，必须用 w=0 变换：w=1 会把视图矩阵的平移列（世界原点在视空间的位置）也加进来，
                //当模型偏离世界原点或相机不对准原点时会把 UV 推出 [0,1]，导致 MatCap 塌成一块边缘纯色。
                float2 uvMatCap = mul(UNITY_MATRIX_V, float4(normalDirOnMatCap, 0)).xy;
                uvMatCap = uvMatCap * 0.5 + 0.5;
                //UV 旋转
                uvMatCap = RotateUV(uvMatCap, _FloatMatCapRotate * 3.141592654, float2(0.5, 0.5));
                //MatCap 贴图采样 × 自定义色（HDR）
                half3 colorMatCapMap = SAMPLE_TEXTURE2D(_TexMatCapMap, sampler_TexMatCapMap, TRANSFORM_TEX(uvMatCap, _TexMatCapMap)).rgb;
                colorMatCapMap *= _ColorMatCapMapColor.rgb * _ColorMatCapMapColor.a;

                //受光照影响 开关
                colorMatCapMap = lerp(colorMatCapMap, colorMatCapMap * colorLightBlend, _ToggleGlobalLightMatCapMap);

                //阴影遮罩：暗部（lightIntensityShade1）处按强度压暗，使 MatCap 在阴影里变暗
                float matcapShadowMaskIntensity = (1 - lightIntensityShade1) + (lightIntensityShade1 * (1 - _FloatMatCapShadowMaskIntensity));
                colorMatCapMap = lerp(colorMatCapMap, colorMatCapMap * matcapShadowMaskIntensity, _ToggleMatCapShadowMask);

                //颜色混合模式：Additive（默认无关键词）/ Multiply / Lerp
                half3 colorMatCapFinal;
                #if defined(_MATCAP_COLORBLEND_MULTIPLY)
                colorMatCapFinal = lerp(colorFinalBlend, colorFinalBlend * colorMatCapMap, _FloatMatCapColorBlendIntensity);
                #elif defined(_MATCAP_COLORBLEND_LERP)
                colorMatCapFinal = lerp(colorFinalBlend, colorMatCapMap, _FloatMatCapColorBlendIntensity);
                #else
                colorMatCapFinal = colorFinalBlend + colorMatCapMap * _FloatMatCapColorBlendIntensity;
                #endif

                //遮罩贴图：按遮罩强度（可偏移）把 MatCap 结果混合回最终颜色
                float matCapMaskIntensity = SAMPLE_TEXTURE2D(_TexMatCapMaskMap, sampler_TexMatCapMaskMap, TRANSFORM_TEX(uv, _TexMatCapMaskMap)).r;
                matCapMaskIntensity = saturate(matCapMaskIntensity + _FloatMatCapMaskMapIntensity);
                colorFinalBlend = lerp(colorFinalBlend, colorMatCapFinal, matCapMaskIntensity);
                #endif
                //-------- MatCap 材质捕获 -------- End


                //-------- Emissive 自发光 -------- Start
                #if defined(_EMISSIVE_ON)
                #if defined(_EMISSIVE_ANIM)
                    //◆ 动画模式
                    //UV比例模式：FullMap（uv）↔ MatCap（观察空间法线，球面映射）
                    //方向向量用 w=0，避免叠加视图矩阵平移列而使 UV 偏移（同基础 MatCap）
                    float2 uvEmissiveMatCap = mul(UNITY_MATRIX_V, float4(normalDirWS, 0)).xy;
                    uvEmissiveMatCap = uvEmissiveMatCap * 0.5 + 0.5;
                    float2 uvEmissive = lerp(uv, uvEmissiveMatCap, _FloatEmissiveAnimUVType);

                    //移动速度（可来回）
                    float timeValue = _Time.y;
                    float emissiveMoveSpeed = timeValue * _FloatEmissiveAnimSpeed;
                    emissiveMoveSpeed = lerp(emissiveMoveSpeed, sin(emissiveMoveSpeed), _ToggleEmissiveAnimPingpong);
                    //旋转
                    float uvEmissiveRotate = _FloatEmissiveAnimRotate * 3.141592654 * emissiveMoveSpeed;
                    uvEmissive = RotateUV(uvEmissive, uvEmissiveRotate, float2(0.5, 0.5));
                    //移动方向
                    float2 emissiveMoveDir = float2(_FloatEmissiveAnimDirU, _FloatEmissiveAnimDirV);
                    uvEmissive = uvEmissive - emissiveMoveDir * emissiveMoveSpeed;

                    //自发光颜色
                    half3 colorEmissiveBlend = _ColorEmissiveMapColor.rgb;
                    //变化颜色（cos 时间曲线映射到 0-1 来回过渡）
                    float colorChangeFactor = cos(_FloatEmissiveChangeSpeed * timeValue) * 0.5 + 0.5;
                    half3 colorChange = lerp(colorEmissiveBlend, _ColorEmissiveChangeColor.rgb, colorChangeFactor);
                    colorEmissiveBlend = lerp(colorEmissiveBlend, colorChange, _ToggleEmissiveChangeColor);
                    //视角变化颜色（菲涅尔：观察方向与法线夹角）
                    float3 normalDirOnEmissive = lerp(normalDirWS, normalDirTex, _ToggleNormalMapOnEmissive);
                    float colorViewChangeNdotV = 1 - saturate(dot(normalDirOnEmissive, viewDirWS));
                    half3 colorViewChange = lerp(colorEmissiveBlend, _ColorEmissiveViewChangeColor.rgb, colorViewChangeNdotV);
                    colorEmissiveBlend = lerp(colorEmissiveBlend, colorViewChange, _ToggleEmissiveViewChangeColor);

                    //颜色采样动画UV，强度(A通道)采样静态UV
                    half3 colorEmissive = SAMPLE_TEXTURE2D(_TexEmissiveMap, sampler_TexEmissiveMap, TRANSFORM_TEX(uvEmissive, _TexEmissiveMap)).rgb;
                    float emissiveIntensity = SAMPLE_TEXTURE2D(_TexEmissiveMap, sampler_TexEmissiveMap, TRANSFORM_TEX(uv, _TexEmissiveMap)).a;
                    half3 colorEmissiveFinal = colorEmissive * colorEmissiveBlend * emissiveIntensity;
                    //最小强度限制（低于阈值不发光，避免暗噪）
                    colorEmissiveFinal *= step(0.005, emissiveIntensity);

                    colorFinalBlend += colorEmissiveFinal;
                #else
                    //◆ 固定模式：强度=贴图A通道
                    float4 colorEmissive = SAMPLE_TEXTURE2D(_TexEmissiveMap, sampler_TexEmissiveMap, TRANSFORM_TEX(uv, _TexEmissiveMap));
                    float emissiveIntensity = colorEmissive.a;
                    half3 colorEmissiveFinal = colorEmissive.rgb * _ColorEmissiveMapColor.rgb * emissiveIntensity;
                    colorFinalBlend += colorEmissiveFinal;
                #endif
                #endif
                //-------- Emissive 自发光 -------- End


                //最终Alpha：默认取基础贴图Alpha；裁剪-透明度模式下改用裁剪值以呈现溶解淡出（仅 Transparent 表面可见）
                half alphaFinal = colorBaseMap.a;
                #if defined(_CLIP_ALPHA)
                alphaFinal = saturate(clipAlpha);
                #endif
                half4 colorFinal = half4(colorFinalBlend, alphaFinal);

                //逐对象阴影调试可视化（由 Feature 的 Debug Mode 驱动，默认关闭时被完全优化掉）
                #if defined(_BLURTOON_PER_OBJECT_SHADOW)
                half3 perObjDebugColor;
                if (BlurToonPerObjectShadowDebug(IN.positionWS, IN.normalWS, perObjDebugColor))
                    return half4(perObjDebugColor, alphaFinal);
                #endif

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
            //渲染面与本体一致，保证投影轮廓正确
            Cull [_IntRenderFaceType]

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            // 点光源/聚光灯阴影投射时，光照方向按逐顶点位置计算
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            // 透明度裁切（镂空处不投射阴影）
            #pragma shader_feature_local _ALPHATEST_ON
            // 裁剪（溶解）：镂空处不投射阴影，使投影轮廓与可见网格一致
            #pragma shader_feature_local _ _CLIP_DITHER _CLIP_ALPHA
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //Shadows.hlsl中使用了LerpWhiteTo，其定义在core包的CommonMaterial.hlsl中，需在其之前引入。
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            //透明度裁切需采样基础贴图 Alpha：SurfaceInput 提供 _BaseMap，LitInput 提供 _BaseColor/_Cutoff 等材质属性
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "LitInput.hlsl"

            //裁剪遮罩贴图（仅挖孔/透明度模式启用，使投影轮廓与本体溶解同步）
            #if defined(_CLIP_DITHER) || defined(_CLIP_ALPHA)
            TEXTURE2D(_TexClipMaskMap); SAMPLER(sampler_TexClipMaskMap);
            #endif

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
                float2 texcoord   : TEXCOORD0; //纹理坐标（透明度裁切用）

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置
                float2 uv : TEXCOORD0; //纹理坐标（透明度裁切用）

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //逐材质自阴影偏移：在 URP 全局阴影 bias 之上，对易出现自阴影粉刺(acne)/交界碎裂的模型做额外补偿。
            //depthBias 沿光方向推动 caster(减 acne)；normalBias 沿法线内缩并按 (1-NoL) 坡度缩放(掠射角最强，压交界碎裂)。
            //两者默认 0=不额外偏移，与原行为完全一致；0.02 为世界单位缩放，使 [0,1] 滑条落在角色尺度可用区间。
            float3 ApplySelfShadowBias(float3 positionWS, float3 normalWS, float3 lightDirection)
            {
                const float biasScale = 0.02;
                float invNdotL = 1.0 - saturate(dot(lightDirection, normalWS));
                float normalScale = invNdotL * _FloatSelfShadowNormalBias * biasScale;
                positionWS += lightDirection * (_FloatSelfShadowDepthBias * biasScale); //沿光方向(深度偏移)
                positionWS -= normalWS * normalScale;                                   //沿法线内缩(坡度缩放)
                return positionWS;
            }

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

                //应用阴影偏移后转换到裁剪空间：先 URP 标准 bias(受光源/阴影设置驱动)，再叠加逐材质自阴影偏移
                float3 positionBiasedWS = ApplyShadowBias(positionWS, normalWS, lightDirectionWS);
                positionBiasedWS = ApplySelfShadowBias(positionBiasedWS, normalWS, lightDirectionWS);
                float4 positionCS = TransformWorldToHClip(positionBiasedWS);

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
                OUT.uv = IN.texcoord;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //透明度裁切：镂空处（Alpha<阈值）不写入深度，使投射阴影的轮廓与本体一致
                #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif

                //裁剪（溶解）：与本体一致，溶解处不投射阴影（挖孔硬剔除；透明度模式按裁剪值硬剔除，使投影轮廓与可见网格一致）
                #if defined(_CLIP_DITHER)
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                clip(clipMask - _FloatClipIntensity);
                #elif defined(_CLIP_ALPHA)
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                half clipAlpha = lerp(clipMask, clipMask * (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a), _ToggleClipTransBaseMapAlpha) - _FloatClipTransIntensity;
                clip(clipAlpha);
                #endif

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
            //渲染面与本体一致
            Cull [_IntRenderFaceType]

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            // 透明度裁切（镂空处不写入深度）
            #pragma shader_feature_local _ALPHATEST_ON
            // 裁剪（溶解）：镂空处不写入深度，使深度轮廓与可见网格一致
            #pragma shader_feature_local _ _CLIP_DITHER _CLIP_ALPHA
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //透明度裁切需采样基础贴图 Alpha：SurfaceInput 提供 _BaseMap，LitInput 提供 _BaseColor/_Cutoff 等材质属性
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "LitInput.hlsl"

            //裁剪遮罩贴图（仅挖孔/透明度模式启用，使深度轮廓与本体溶解同步）
            #if defined(_CLIP_DITHER) || defined(_CLIP_ALPHA)
            TEXTURE2D(_TexClipMaskMap); SAMPLER(sampler_TexClipMaskMap);
            #endif

            //顶点着色器 输入数据结构
            struct Attributes
            {
                float4 positionOS : POSITION; //对象空间顶点位置
                float2 texcoord   : TEXCOORD0; //纹理坐标（透明度裁切用）

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置
                float2 uv : TEXCOORD0; //纹理坐标（透明度裁切用）

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
                OUT.uv = IN.texcoord;
                return OUT;
            }

            half frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                //透明度裁切：镂空处（Alpha<阈值）不写入深度
                #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif

                //裁剪（溶解）：与本体一致，溶解处不写入深度，使深度轮廓与可见网格一致
                #if defined(_CLIP_DITHER)
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                clip(clipMask - _FloatClipIntensity);
                #elif defined(_CLIP_ALPHA)
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                half clipAlpha = lerp(clipMask, clipMask * (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a), _ToggleClipTransBaseMapAlpha) - _FloatClipTransIntensity;
                clip(clipAlpha);
                #endif

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
            //渲染面与本体一致
            Cull [_IntRenderFaceType]

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            // 透明度裁切（镂空处不写入深度/法线）
            #pragma shader_feature_local _ALPHATEST_ON
            // 裁剪（溶解）：镂空处不写入深度/法线，使深度法线轮廓与可见网格一致
            #pragma shader_feature_local _ _CLIP_DITHER _CLIP_ALPHA
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //透明度裁切需采样基础贴图 Alpha：SurfaceInput 提供 _BaseMap，LitInput 提供 _BaseColor/_Cutoff 等材质属性
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "LitInput.hlsl"

            //裁剪遮罩贴图（仅挖孔/透明度模式启用，使深度法线轮廓与本体溶解同步）
            #if defined(_CLIP_DITHER) || defined(_CLIP_ALPHA)
            TEXTURE2D(_TexClipMaskMap); SAMPLER(sampler_TexClipMaskMap);
            #endif

            //顶点着色器 输入数据结构
            struct Attributes
            {
                float4 positionOS : POSITION; //对象空间顶点位置
                float3 normalOS   : NORMAL; //法线
                float4 tangentOS  : TANGENT; //切线
                float2 texcoord   : TEXCOORD0; //纹理坐标（透明度裁切用）

                //GPUInstance功能相关宏 用于传递ID数据
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            //片元着色器 输入数据结构
            struct Varyings
            {
                float4 positionCS : SV_POSITION; //裁剪空间位置
                float3 normalWS : TEXCOORD1; //世界空间法线
                float2 uv : TEXCOORD2; //纹理坐标（透明度裁切用）

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
                OUT.uv = IN.texcoord;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //GPUInstance功能相关宏。
                UNITY_SETUP_INSTANCE_ID(IN);

                //透明度裁切：镂空处（Alpha<阈值）不写入深度/法线
                #if defined(_ALPHATEST_ON)
                half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(alpha - _Cutoff);
                #endif

                //裁剪（溶解）：与本体一致，溶解处不写入深度/法线，使深度法线轮廓与可见网格一致
                #if defined(_CLIP_DITHER)
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                clip(clipMask - _FloatClipIntensity);
                #elif defined(_CLIP_ALPHA)
                half clipMask = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                half clipAlpha = lerp(clipMask, clipMask * (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a), _ToggleClipTransBaseMapAlpha) - _FloatClipTransIntensity;
                clip(clipAlpha);
                #endif

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
            //模板测试：与本体一致，使描边也参与同组遮罩
            Stencil
            {
                Ref [_FloatStencilNum]
                Comp [_FloatStencilComp]
                Pass [_FloatStencilPass]
                Fail [_FloatStencilFail]
            }

            HLSLPROGRAM

            // Keywords ------------------------------------- Start
            // GPU Instancing
            #pragma multi_compile_instancing
            
            // URP 主光阴影接收（描边受阴影影响，在片元采样真实阴影图）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //URP 14 软阴影：质量由 _SHADOWS_SOFT_LOW/_MEDIUM/_HIGH 关键词区分（管线开启软阴影时会禁用通用 _SHADOWS_SOFT 只启用对应质量）。
            //必须声明全部变体，否则管线启用如 _SHADOWS_SOFT_MEDIUM 时本 Shader 无匹配变体 → 回退到无软阴影 → 即使勾选软阴影也是硬阴影。
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            //逐对象阴影：由 BlurToonPerObjectShadowFeature 逐帧开关。未添加该 Feature 或本帧无投射者时关闭，零开销。
            #pragma multi_compile_fragment _ _BLURTOON_PER_OBJECT_SHADOW

            // BlurToonURP Keywords
            //外描边
            #pragma shader_feature_local _OUTLINE_ON // 外描边开关
            #pragma shader_feature_local _OUTLINE_WIDTH_SAME _OUTLINE_WIDTH_SCALING // 外描边类型
            #pragma shader_feature_local _OUTLINE_MAP_ON // 描边纹理贴图
            #pragma shader_feature_local _ALPHATEST_ON // 透明度裁切
            #pragma shader_feature_local _ _CLIP_DITHER _CLIP_ALPHA // 裁剪（溶解）：无=关闭 / 挖孔 / 透明度
            // Keywords ------------------------------------- End

            #pragma vertex vert //顶点着色器
            #pragma fragment frag //片元着色器

            //URP常用的核心方法库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            
            //材质属性统一在 LitInput.hlsl 中声明（保证与其它 Pass 的 UnityPerMaterial 完全一致，兼容 SRP Batcher）
            #include "LitInput.hlsl"
            //阴影函数库（低质量PCF主光阴影采样等）
            #include "ShadowFunction.hlsl"
            //逐对象阴影函数库（高密度瓦片阴影图采样）
            #include "PerObjectShadowFunction.hlsl"

            //描边纹理贴图（描边专用纹理，用于给描边着色/图案，仅在指定贴图后启用关键词）
            #if defined(_OUTLINE_MAP_ON)
            TEXTURE2D(_TexOutlineMap); SAMPLER(sampler_TexOutlineMap);
            #endif

            //裁剪遮罩贴图（仅挖孔/透明度模式启用，使描边与本体同步溶解）
            #if defined(_CLIP_DITHER) || defined(_CLIP_ALPHA)
            TEXTURE2D(_TexClipMaskMap); SAMPLER(sampler_TexClipMaskMap);
            #endif

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

                //顶点色平滑法线：SmoothNormalGenerator 将对象空间平滑法线的 XY 编码进 RG，Z 未存储
                float2 snXY = IN.color.rg * 2.0 - 1.0;
                float3 snOS = normalize(float3(snXY, sqrt(saturate(1.0 - dot(snXY, snXY)))));
                //重建出的 Z 恒为正，与原始顶点法线反向时翻转，补回烘焙时丢失的符号
                if (dot(snOS, normalize(IN.normalOS)) < 0.0) snOS.z = -snOS.z;
                float3 colorDir = TransformObjectToWorldNormal(normalize(snOS));

                //描边类型，通过lerp和step构建的if选择器
                float3 moveDir =
                    lerp(normalInput.normalWS.rgb,
                    lerp(colorDir, normalInput.tangentWS.xyz, step(1.01, _FloatOutlineType)),
                    step(0.01, _FloatOutlineType)
                    );
                moveDir = normalize(moveDir);
                #if defined(_OUTLINE_WIDTH_SAME) //等宽
                    //沿法线方向外扩
                    OUT.positionCS = TransformWorldToHClip(OUT.positionWS.xyz + moveDir * outlineWidth);
                #elif defined(_OUTLINE_WIDTH_SCALING) //变化
                    //顶点相对模型中心的方向需转到世界空间，才能与世界空间的外扩方向 moveDir 一致比较（否则物体旋转会改变描边粗细分布）
                    half3 vertDir = TransformObjectToWorldDir(IN.positionOS.xyz);
                    half signVertNormal = dot(vertDir, moveDir) + 0.3;
                    OUT.positionCS = TransformWorldToHClip(OUT.positionWS.xyz + moveDir * outlineWidth * signVertNormal);
                #endif

                //描边的光照与阴影影响改到片元着色器中计算（见 frag），此处仅传递中性色
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

                //透明度裁切：与本体一致，按 基础贴图Alpha×基础色Alpha 裁掉镂空处的描边，避免描边出现在被裁像素上
                #if defined(_ALPHATEST_ON)
                half alphaOutline = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)).a * _BaseColor.a;
                clip(alphaOutline - _Cutoff);
                #endif

                //裁剪（溶解）：与本体一致，溶解处不绘制描边（描边为硬剔除，不做透明淡出）
                #if defined(_CLIP_DITHER)
                half clipMaskOutline = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                clip(clipMaskOutline - _FloatClipIntensity);
                #elif defined(_CLIP_ALPHA)
                half clipMaskOutline = SAMPLE_TEXTURE2D(_TexClipMaskMap, sampler_TexClipMaskMap, TRANSFORM_TEX(IN.uv, _TexClipMaskMap)).r;
                //与本体一致：可叠加基础贴图A通道后再减透明度裁剪强度（_ToggleClipTransBaseMapAlpha 关时退化为原行为）
                half clipAlphaOutline = lerp(clipMaskOutline, clipMaskOutline * (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)).a * _BaseColor.a), _ToggleClipTransBaseMapAlpha) - _FloatClipTransIntensity;
                clip(clipAlphaOutline);
                #endif

                //外描边颜色和光照色混合
                half4 colorOutlineLightBlend = _ColorOutlineColor * IN.color;

                //基础贴图颜色混合
                half4 colorBaseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(IN.uv, _BaseMap)) * _BaseColor;
                half4 colorBaseMapBlend = lerp(colorOutlineLightBlend, colorOutlineLightBlend * colorBaseMap, _FloatOutlineBaseMapBlendIntensity);
                colorFinal = lerp(colorOutlineLightBlend, colorBaseMapBlend, _ToggleOutlineBaseMapBlend);

                //-------- 描边受光照与阴影影响 -------- Start
                //主光照（带真实投射阴影，依赖本Pass补齐的 _MAIN_LIGHT_SHADOWS 关键词）
                Light mainLight = GetMainLight(IN.shadowCoord);
                //可选：低质量PCF重采样主光阴影，与基础Pass一致
                if (_ToggleShadowLowQualityPCF > 0.5)
                    mainLight.shadowAttenuation = MainLightShadowLowQualityPCF(IN.shadowCoord, IN.positionWS);
                //逐对象阴影：与基础Pass一致，保证描边与本体的受影表现同步
                //描边 Pass 的 Varyings 不带世界法线，传 0 表示不做接收端法线偏移；
                //描边是外扩的背面壳体，本身就离表面有距离，缺少这个偏移不会产生可见问题。
                #if defined(_BLURTOON_PER_OBJECT_SHADOW)
                mainLight.shadowAttenuation = BlurToonApplyPerObjectShadow(
                    mainLight.shadowAttenuation, IN.positionWS, float3(0, 0, 0), mainLight.direction,
                    length(IN.positionWS - _WorldSpaceCameraPos), _ToggleShadowLowQualityPCF);
                #endif
                half3 colorLightMain = mainLight.color * mainLight.distanceAttenuation;
                //阴影衰减：与基础Pass一致，减去阴影强度偏移，并受“阴影接收”开关控制
                half shadowAttenuation = lerp(1, saturate(mainLight.shadowAttenuation - _FloatShadowIntensity), _ToggleShadowReceive);
                //环境光照
                half3 envLightColor = half3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) * _FloatEnvLightIntensity;
                //实时光照（阴影遮挡直接光）叠加环境光，再乘全局曝光，得到描边的光照色
                half3 realtimeLightColor = colorLightMain * _FloatRealtimeLightIntensity * 0.1 * shadowAttenuation;
                half3 colorLightBlend = (envLightColor + realtimeLightColor) * _FloatGlobalExposureIntensity;
                //按开关与混合强度作用到描边颜色（关掉开关时描边保持原色，向后兼容）
                colorFinal.rgb = lerp(colorFinal.rgb,
                    lerp(colorFinal.rgb, colorFinal.rgb * colorLightBlend, _GlobalLightOutlineMixedIntensity),
                    _ToggleGlobalLightOutline);
                //-------- 描边受光照与阴影影响 -------- End

                //-------- 描边纹理贴图颜色混合 --------
                //用一张描边专用纹理调制描边颜色（可做彩色描边、图案、噪声等），UV 与基础贴图相同；
                //仅在材质指定了描边纹理时生效（关键词 _OUTLINE_MAP_ON），未指定时保持原描边颜色，向后兼容。
                #if defined(_OUTLINE_MAP_ON)
                half4 colorOutlineMap = SAMPLE_TEXTURE2D(_TexOutlineMap, sampler_TexOutlineMap, TRANSFORM_TEX(IN.uv, _TexOutlineMap));
                colorFinal.rgb = lerp(colorFinal.rgb, colorFinal.rgb * colorOutlineMap.rgb, _FloatOutlineMapIntensity);
                #endif

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