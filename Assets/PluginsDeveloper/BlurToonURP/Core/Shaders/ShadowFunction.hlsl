#ifndef BLURTOONURP_SHADOW_FUNCTION_INCLUDED
#define BLURTOONURP_SHADOW_FUNCTION_INCLUDED

// BlurToonURP 阴影相关函数库
// ------------------------------------------------------------------------------------
// 依赖 URP 的 Lighting.hlsl / Shadows.hlsl（本文件已自行 include，带包含保护，重复引入安全）。
// 需在“接收阴影”的 Pass（ForwardLit / Outline）中引入。
// ------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// 用“强制低质量 PCF”重采样主光阴影。
// Medium/High 的宽核 PCF 会把更多采样铺展到较大的阴影图区域，在角色尺度的透视下反而放大
// 阴影图纹素的阶梯（perspective aliasing）；低质量核更贴合角色、阴影边缘更干净
// （参考 StarRailNPRShader 的 GetCharacterMainLight）。
// 仅对“阴影图”路径生效；屏幕空间阴影(_MAIN_LIGHT_SHADOWS_SCREEN)/无阴影时回退 URP 默认采样。
half MainLightShadowLowQualityPCF(float4 shadowCoord, float3 positionWS)
{
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && !defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    ShadowSamplingData samplingData = GetMainLightShadowSamplingData();
    half4 shadowParams = GetMainLightShadowParams();
    // 直接调用低质量(4-tap)滤波核：SampleShadowmap 内部按 _SHADOWS_SOFT* 关键词“静态”分发质量，
    // 运行时改 samplingData.softShadowQuality 在静态分支中无效；此处绕过它，无论管线软阴影质量为
    // Low/Medium/High 都强制用低质量核（更贴角色、减透视锯齿）。
    // 注意：仍需在 URP Asset 勾选 Soft Shadows —— 4-tap 的纹素偏移(_MainLightShadowOffset*)只有开启软阴影时
    // 才由管线上传；否则 4 个采样点重合，等同硬阴影（本开关将无可见效果）。
    half atten = SampleShadowmapFilteredLowQuality(TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare),
                                                   shadowCoord, samplingData);
    atten = LerpWhiteTo(atten, shadowParams.x);                    // 应用阴影强度
    atten = BEYOND_SHADOW_FAR(shadowCoord) ? half(1.0) : atten;    // 阴影视锥外不投影
    return lerp(atten, half(1.0), GetMainLightShadowFade(positionWS)); // 随距离淡出，避免阴影距离外硬截断
#else
    return MainLightRealtimeShadow(shadowCoord);
#endif
}

#endif // BLURTOONURP_SHADOW_FUNCTION_INCLUDED
