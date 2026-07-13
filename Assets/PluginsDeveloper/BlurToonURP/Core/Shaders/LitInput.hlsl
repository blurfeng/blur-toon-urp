#ifndef BLURTOONURP_LIT_INPUT_INCLUDED
#define BLURTOONURP_LIT_INPUT_INCLUDED

// BlurToonURP Lit Shader 材质属性常量缓冲区（UnityPerMaterial）
// ------------------------------------------------------------------------------------
// 为保证 SRP Batcher 兼容，同一 SubShader 内所有 Pass 的 UnityPerMaterial 必须逐字节完全一致
// （相同的属性、相同的顺序、相同的大小）。因此将全部材质属性集中在此文件，由各 Pass 统一 include，
// 避免各 Pass 手写 CBUFFER 导致布局不一致而使批处理失效。
//
// 注意：
// 1. 贴图（TEXTURE2D/SAMPLER）不属于 UnityPerMaterial，仍在各 Pass 中按需（可带 #if）单独声明。
// 2. 此处所有属性均“无条件”声明，不要用 #if 包裹，否则会随关键词变体改变 CBUFFER 大小，再次破坏兼容性。
// 3. 使用前需先 include "...ShaderLibrary/Core.hlsl"（提供 CBUFFER_START/CBUFFER_END 宏）。
// ------------------------------------------------------------------------------------

CBUFFER_START(UnityPerMaterial)
//-------- BaseMap 基础纹理 --------
//_BaseMap / _BumpMap 等贴图已在 "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl" 中定义。
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
//暗部阈值贴图（属性无条件声明以保证 CBUFFER 布局稳定，实际是否采样由片元中的关键词决定）
float4 _TexShadeThresholdMap_ST; //暗部阈值 贴图
half _FloatShadeThresholdMapIntensity; //暗部阈值贴图 强度


//-------- Surface 表面类型 / 透明度裁切 --------
//_Surface/_SrcBlend/_DstBlend/_ZWrite 仅供固定管线渲染状态与编辑器使用，HLSL 只读取 _Cutoff；
//此处一并纳入 CBUFFER 以与 URP 约定一致、保证各 Pass 布局完全相同。
half _Surface; //表面类型 0=Opaque 1=Transparent
half _SrcBlend; //源混合因子
half _DstBlend; //目标混合因子
half _ZWrite; //深度写入
half _Cutoff; //透明度裁切阈值


//-------- NormalMap 法线贴图 --------
float4 _BumpMap_ST;
half _BumpScale; //法线贴图强度
//开关
half _ToggleNormalMapOnBaseMap; //开关 基础贴图
half _ToggleNormalMapOnHighLight; //开关 高光
//（边缘光法线来源改为边缘光段的专属配置 _FloatRimLightNormalSource / _FloatRimLightNormalMapBlend）


//-------- HighLight 镜面高光 --------
//（_ToggleHighLight 仅用于记录并驱动关键词 _HIGHLIGHT_ON，HLSL 不读取，故不在此声明）
half4 _ColorHighLightColor; //高光颜色
half _FloatHighLightIntensity; //高光强度
half _FloatHighLightSize; //高光大小（范围）
half _FloatHighLightBlur; //高光边缘羽化
float4 _TexHighLightMaskMap_ST; //高光遮罩贴图 ST
half _FloatHighLightMaskMapIntensity; //高光遮罩贴图 强度


//-------- RimLight 边缘光 --------
half _ToggleRimLight; //开关 边缘光
half4 _ColorRimLightColor; //颜色
half _FloatRimLightIntensity; //强度
half _FloatRimLightInsideDistance; //内部距离
half _ToggleRimLightHard; //开关 硬边缘
//法线来源（边缘光专属）
half _FloatRimLightNormalSource; //法线来源 0=几何法线 1=法线贴图 2=混合
half _FloatRimLightNormalMapBlend; //混合模式下 几何↔法线贴图 的混合强度
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
half _ToggleGlobalLightOutline; //描边
half _GlobalLightOutlineMixedIntensity; //描边和光照颜色的混合强度 0-1

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


//-------- Outline 外描边 --------
half _FloatOutlineType;
half4 _ColorOutlineColor;
half _FloatOutlineWidth;
half _ToggleOutlineBaseMapBlend;
half _FloatOutlineBaseMapBlendIntensity;
//描边纹理贴图
float4 _TexOutlineMap_ST; //描边纹理贴图 ST
half _FloatOutlineMapIntensity; //描边纹理混合 强度
CBUFFER_END

#endif // BLURTOONURP_LIT_INPUT_INCLUDED
