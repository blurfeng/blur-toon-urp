#ifndef BLURTOONURP_FUNCTION_INCLUDED
#define BLURTOONURP_FUNCTION_INCLUDED

// BlurToonURP 公共着色器函数库
// ------------------------------------------------------------------------------------
// 存放可被多个 Pass / 效果复用的通用函数（UV 旋转、噪声等）。
// 只依赖 HLSL 内建函数，使用前无需额外 include（各 Pass 已 include Core.hlsl）。
// ------------------------------------------------------------------------------------

//旋转 UV。uv:uv坐标 radian:弧度 pivot:旋转锚点
float2 RotateUV(float2 uv, float radian, float2 pivot)
{
    float rCos = cos(radian);
    float rSin = sin(radian);
    return mul(uv - pivot, float2x2(rCos, -rSin, rSin, rCos)) + pivot;
}

#endif // BLURTOONURP_FUNCTION_INCLUDED
