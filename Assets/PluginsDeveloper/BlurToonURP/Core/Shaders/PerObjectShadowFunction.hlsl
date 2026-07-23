#ifndef BLURTOONURP_PER_OBJECT_SHADOW_FUNCTION_INCLUDED
#define BLURTOONURP_PER_OBJECT_SHADOW_FUNCTION_INCLUDED

// BlurToonURP 逐对象阴影 采样函数库
// ------------------------------------------------------------------------------------
// 配合 BlurToonPerObjectShadowFeature 使用。Feature 为每个投射者渲染一块紧贴其包围盒的瓦片，
// 纹素密度比 URP 级联阴影高数倍，且投影尺寸经过量化、中心经过纹素吸附，
// 因此 NPR 的硬边不会暴露纹素阶梯，光源/相机移动时也不会逐帧跳变。
//
// 瓦片里只画投射者自身（不含场景遮挡物），因此“角色收场景投影”仍由 URP 级联阴影图负责。
// 两张图各自完整包含角色，直接 min 会把级联那份低分辨率自阴影的阶梯重新引回来——
// SceneAndSelf 模式通过“自剔除偏移”把角色自身从级联图的贡献里摘掉，使两份阴影各司其职。
//
// 关键词 _BLURTOON_PER_OBJECT_SHADOW 由 Feature 逐帧开关，关闭时本文件的函数不会被调用。
// ------------------------------------------------------------------------------------
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//提供 MainLightShadowLowQualityPCF，并间接引入 Lighting.hlsl / Shadows.hlsl（级联重采样所需）
#include "ShadowFunction.hlsl"

// 整个实现只在关键词开启的变体里编译：关闭时连常量缓冲区都不声明，
// 未添加本 Feature 的工程不会为这 16 组矩阵付出任何常量寄存器开销。
// 调用处同样以该关键词包裹，两边保持一致。
#if defined(_BLURTOON_PER_OBJECT_SHADOW)

// 与 BlurToonPerObjectShadowPass.MaxCasterCount 必须一致
#define BLURTOON_PER_OBJ_SHADOW_MAX_COUNT 16

TEXTURE2D_SHADOW(_BlurToonPerObjShadowAtlas);
// 比较采样统一用 URP 的 sampler_LinearClampCompare（Shadows.hlsl 中声明），不要用内联的
// sampler_<纹理名>：内联采样器靠名字从纹理继承状态，而“比较”这一项的继承在各图形后端并不可靠，
// 一旦退化成点采样，硬件 2x2 比较过滤就完全失效，阴影边缘会变成完全硬的纹素台阶。
// URP 自己所有阴影图都走这个显式命名的采样器，原因相同。
#define sampler_BlurToonPerObjShadowAtlas sampler_LinearClampCompare

// 同一张图集再以普通纹理绑定一次，供调试模式直接读取原始深度。
// 比较采样器无法取回深度值本身，只能得到比较结果，因此必须换个名字另绑一次。
TEXTURE2D(_BlurToonPerObjShadowAtlasRaw);
SAMPLER(sampler_BlurToonPerObjShadowAtlasRaw);

// GLES3 上使用 CBUFFER 会有性能回退，与 URP Shadows.hlsl 的处理保持一致
#ifndef SHADER_API_GLES3
CBUFFER_START(BlurToonPerObjectShadow)
#endif

// 世界坐标 → 瓦片自身 [0,1]³ 坐标（未含瓦片在图集中的位置，便于先做越界判定）
float4x4 _BlurToonPerObjShadowMatrices[BLURTOON_PER_OBJ_SHADOW_MAX_COUNT];
// 投射者的世界包围球：xyz = 中心，w = 半径。SceneAndSelf 用它算自剔除距离
float4   _BlurToonPerObjShadowSpheres[BLURTOON_PER_OBJ_SHADOW_MAX_COUNT];
// 瓦片数据：xy = 图集 UV 偏移，z = 图集 UV 缩放（网格为正方形，xy 共用），w = 该瓦片一个纹素的世界尺寸
float4   _BlurToonPerObjShadowTiles[BLURTOON_PER_OBJ_SHADOW_MAX_COUNT];
// x = 本帧有效瓦片数，y = 阴影强度，z = 距离淡出 scale，w = 距离淡出 bias
float4   _BlurToonPerObjShadowParams;
// x = 合并方式（0=取较暗者 1=命中即替换 2=场景投影+高清自阴影）
// y = 自剔除距离倍率（仅模式 2 使用）
float4   _BlurToonPerObjShadowCombine;
// x = 1/图集边长，y = 图集边长，z = 1/瓦片边长，w = 接收端法线偏移（单位：瓦片纹素）
float4   _BlurToonPerObjShadowAtlasSize;
// 调试模式 0=关 1=阴影值灰度 2=瓦片覆盖(绿=命中 红=未命中) 3=瓦片UV
float4   _BlurToonPerObjShadowDebug;

#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

// 采样逐对象阴影图集。
// 返回值 atten：1=无遮挡 0=完全遮挡；hit：该像素是否落在任何一块瓦片内（0/1）。
// 一个像素可能同时落在多个投射者的阴影体积内（角色互相遮挡），故对所有命中瓦片取最暗者。
void BlurToonSamplePerObjectShadow(float3 positionWS, float3 normalWS, out half atten, out half hit)
{
    atten = half(1.0);
    hit = half(0.0);

    int count = (int)_BlurToonPerObjShadowParams.x;
    // 比较采样的双线性过滤会取到相邻纹素，瓦片边缘需内缩半个纹素，避免跨采到邻居瓦片
    float uvInset = 0.5 * _BlurToonPerObjShadowAtlasSize.z;
    float normalOffsetTexels = _BlurToonPerObjShadowAtlasSize.w;

    for (int i = 0; i < count; i++)
    {
        // 接收端法线偏移：沿表面法线把采样点推离表面若干个纹素的世界距离。
        // 掠射角（光线与表面接近平行）下，一个阴影图纹素在表面上覆盖极长一段，
        // 投射端的深度/法线 bias 无论怎么加都跟不上这个尺度，整片表面会同时翻成自遮挡。
        // 接收端偏移直接按“该瓦片纹素的世界尺寸”补偿，是这种配置下唯一有效的手段，
        // 且不影响非掠射区域（那里偏移量远小于真实遮挡物间距）。
        float3 samplePositionWS = positionWS + normalWS * (_BlurToonPerObjShadowTiles[i].w * normalOffsetTexels);

        float4 shadowCoord = mul(_BlurToonPerObjShadowMatrices[i], float4(samplePositionWS, 1.0));
        // 正交投影下 w 恒为 1，此处保留除法以便将来替换为透视投影
        shadowCoord.xyz /= shadowCoord.w;

        // 越界判定：三个轴都落在 [0,1] 内才算命中该瓦片
        float3 inRangeMin = step(0.0, shadowCoord.xyz);
        float3 inRangeMax = step(shadowCoord.xyz, 1.0);
        half inside = half(inRangeMin.x * inRangeMin.y * inRangeMin.z *
                           inRangeMax.x * inRangeMax.y * inRangeMax.z);

        // 无分支：越界时把 UV 夹回瓦片内部照常采样，最后用 inside 掩掉结果。
        // 投射者通常只有个位数，省去动态分支比省掉几次采样更划算。
        float2 tileUV = clamp(shadowCoord.xy, uvInset, 1.0 - uvInset);
        tileUV = _BlurToonPerObjShadowTiles[i].xy + tileUV * _BlurToonPerObjShadowTiles[i].z;

        // 单次比较采样。刻意不加宽 PCF 核：加宽会让阴影在世界空间真的变软，那是柔化不是抗锯齿，
        // NPR 的硬边会被破坏。过渡宽度就是硬件 2x2 比较过滤给出的一个纹素。
        half sampleAtten = half(SAMPLE_TEXTURE2D_SHADOW(_BlurToonPerObjShadowAtlas,
                                                        sampler_BlurToonPerObjShadowAtlas,
                                                        float3(tileUV, shadowCoord.z)));

        // 越界的瓦片贡献 1（不遮挡）
        sampleAtten = lerp(half(1.0), sampleAtten, inside);
        atten = min(atten, sampleAtten);
        hit = max(hit, inside);
    }

    // 阴影强度：0 时完全不产生阴影
    atten = lerp(half(1.0), atten, half(_BlurToonPerObjShadowParams.y));
}

// 计算“自剔除距离”：从接收点沿主光方向前进多远，才能离开投射者自身的包围球。
//
// 这段长度正是“接收点与光源之间、可能属于角色自己的那一截”，剔除它就等于把角色从级联图里摘掉：
//   接收点在朝光的一侧 → 前方没有自身几何，距离≈0，贴身的场景遮挡物照常投影到角色上；
//   接收点在背光的一侧 → 整个身体挡在前面，距离≈包围球直径，自身被完整剔除。
// 若图省事统一用“包围球直径”当剔除距离，朝光面会被过度剔除——
// 表现就是远处建筑的投影正常、而两米内的物体投影凭空消失。
//
// 接收点落在球外时返回 0：那个球对它而言是别的物体（另一个角色），别人的投影必须保留。
float BlurToonComputeSelfRejectDistance(float3 positionWS, float3 mainLightDirection)
{
    float rejectDistance = 0.0;
    int count = (int)_BlurToonPerObjShadowParams.x;

    for (int i = 0; i < count; i++)
    {
        float3 toCenter = positionWS - _BlurToonPerObjShadowSpheres[i].xyz;
        float radius = _BlurToonPerObjShadowSpheres[i].w;
        float distanceSq = dot(toCenter, toCenter);
        float radiusSq = radius * radius;

        // 射线 P + t*L 与球面的正向交点：t = -b + sqrt(r² - |d|² + b²)。
        // 球内保证 r² - |d|² ≥ 0，判别式不小于 b²，开方与结果均非负；max 只是防浮点误差。
        float b = dot(toCenter, mainLightDirection);
        float exitDistance = -b + sqrt(max(radiusSq - distanceSq + b * b, 0.0));

        // 无分支：球外的投射者不参与自剔除
        float inSphere = step(distanceSq, radiusSq);
        rejectDistance = max(rejectDistance, exitDistance * inSphere);
    }

    return rejectDistance;
}

// 采样 URP 级联阴影图，但把“投射者自身”从结果里剔除。
//
// 做法：沿主光方向把采样点推离本体 rejectDistance 米之后再做深度比较。
// 级联投影是沿光方向的正交投影，因此沿光方向平移完全不改变光空间的 xy —— 采到的是同一个纹素，
// 阴影图案零横向位移，变的只是深度比较的基准。基准前移之后，所有位于该距离之内的遮挡物
// 都不再产生阴影，而排在最前面的那个遮挡物正是角色自己。
// 于是级联图在角色像素上只剩场景遮挡物的贡献，可以干净地与瓦片自阴影取 min。
//
// 副作用：紧贴角色的场景遮挡物（脚边台阶、贴身的墙）也会在这个距离内失去投影，由 selfRejectScale 权衡。
// 返回 false 表示当前配置无法重采样（屏幕空间阴影 / 未开启主光阴影），调用方应沿用原值。
bool BlurToonSampleSceneShadowExcludingSelf(
    float3 positionWS, float3 mainLightDirection, float rejectDistance,
    half useLowQualityPCF, out half sceneShadow)
{
    sceneShadow = half(1.0);

#if !defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    return false;
#elif defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
    // 屏幕空间阴影图按屏幕像素索引，位移世界坐标会采到别的像素，此手段不适用
    return false;
#else
    // URP 约定 Light.direction 由表面指向光源，故加号即“朝光源推”
    float3 offsetPositionWS = positionWS + mainLightDirection * rejectDistance;

    // 级联索引用“接收点”而非偏移点来选：两者光空间 xy 相同，用接收点可保证与未剔除时落在同一张级联上，
    // 避免在级联边界附近因偏移跨级而出现接缝。
    #ifdef _MAIN_LIGHT_SHADOWS_CASCADE
        half cascadeIndex = ComputeCascadeIndex(positionWS);
    #else
        half cascadeIndex = half(0.0);
    #endif
    float4 shadowCoord = float4(mul(_MainLightWorldToShadow[cascadeIndex], float4(offsetPositionWS, 1.0)).xyz, 0.0);

    // 与主路径用同一套滤波核，否则自剔除前后阴影的软硬会不一致。
    // useLowQualityPCF 来自材质属性（逐 Draw 恒定），此处是无发散的一致分支。
    UNITY_BRANCH
    if (useLowQualityPCF > half(0.5))
        sceneShadow = MainLightShadowLowQualityPCF(shadowCoord, positionWS);
    else
        sceneShadow = MainLightRealtimeShadow(shadowCoord);

    return true;
#endif
}

// 把逐对象阴影并入主光阴影。
// 瓦片里只有投射者自身（不含场景遮挡物），因此合并方式决定了角色能否收到场景投影：
//   Min(0)          取较暗者——保留 URP 的场景投影，但 URP 那份低分辨率自阴影的阶梯也保留了；
//   Replace(1)      命中瓦片即完全替换——自阴影边缘最干净，但角色收不到场景投影；
//   SceneAndSelf(2) 先用自剔除偏移把角色自身从级联图里摘掉，再与瓦片自阴影取 min——两者兼得。
// mainLightDirection 由表面指向光源（URP 约定），SceneAndSelf 用它决定剔除方向。
// useLowQualityPCF 透传材质上的同名开关，保证重采样与主路径滤波一致。
// distanceToCamera 用于在范围边界处平滑过渡回级联阴影，避免硬切换。
half BlurToonApplyPerObjectShadow(half mainLightShadow, float3 positionWS, float3 normalWS,
                                  float3 mainLightDirection, float distanceToCamera, half useLowQualityPCF)
{
    half atten, hit;
    BlurToonSamplePerObjectShadow(positionWS, normalWS, atten, hit);

    // 距离淡出：saturate(distance * scale + bias) 在范围末端由 0 升到 1，用它把权重压回 0
    half fade = half(saturate(distanceToCamera * _BlurToonPerObjShadowParams.z + _BlurToonPerObjShadowParams.w));
    half weight = hit * (half(1.0) - fade);

    int mode = (int)_BlurToonPerObjShadowCombine.x;

    // 场景投影的来源。默认沿用调用方算好的 URP 结果（里面混着角色自己）；
    // SceneAndSelf 重采样一次把角色自身剔掉。mode 是全局常量，分支无发散。
    half sceneShadow = mainLightShadow;
    UNITY_BRANCH
    if (mode == 2)
    {
        // 不在任何包围球内时为 0，偏移退化为不偏移（与原值一致，且此时 weight 也为 0）
        float rejectDistance = BlurToonComputeSelfRejectDistance(positionWS, mainLightDirection)
                             * _BlurToonPerObjShadowCombine.y;
        half rejectedShadow;
        if (BlurToonSampleSceneShadowExcludingSelf(positionWS, mainLightDirection, rejectDistance,
                                                   useLowQualityPCF, rejectedShadow))
            sceneShadow = rejectedShadow;
    }

    // Replace 丢弃场景投影只用瓦片；Min 与 SceneAndSelf 都是取较暗者，区别只在 sceneShadow 是否已剔除自身
    half combined = (mode == 1) ? atten : min(sceneShadow, atten);

    return lerp(mainLightShadow, combined, weight);
}

// 调试可视化。返回 true 表示应当用 debugColor 覆盖最终输出。
// 排查“阴影从哪来”时，直接看瓦片本身比在最终画面里猜要可靠得多：
//   模式1 阴影值：白=无遮挡 黑=完全遮挡。若出现细碎噪点/条纹 → 自阴影粉刺(bias 不足)。
//   模式2 瓦片覆盖：绿=该像素落在瓦片内 红=落在瓦片外(回退 URP 阴影)。
//                   角色身上出现红色 → 包围球太小或瓦片边界压到了模型上。
//   模式3 瓦片UV：红绿渐变应当平滑铺满角色。若在某个光源角度突然整体偏移/翻转 → 矩阵构造有跳变。
bool BlurToonPerObjectShadowDebug(float3 positionWS, float3 normalWS, out half3 debugColor)
{
    debugColor = half3(0, 0, 0);

    int mode = (int)_BlurToonPerObjShadowDebug.x;
    if (mode <= 0)
        return false;

    half atten, hit;
    BlurToonSamplePerObjectShadow(positionWS, normalWS, atten, hit);

    if (mode == 1)
    {
        debugColor = atten.xxx;
    }
    else if (mode == 2)
    {
        debugColor = lerp(half3(1, 0, 0), half3(0, 1, 0), hit);
    }
    else if (mode == 3)
    {
        //取第一块瓦片的局部坐标，直观反映矩阵是否连续
        float4 coord = mul(_BlurToonPerObjShadowMatrices[0], float4(positionWS, 1.0));
        coord.xyz /= coord.w;
        debugColor = half3(saturate(coord.x), saturate(coord.y), 0);
    }
    else if (mode == 4)
    {
        //图集原始深度：直接看瓦片里到底画进了什么。
        //反向Z下 1=近 0=远，清空值为远(0)。若角色所在瓦片一片纯黑 → 几何根本没被画进去(剔除问题)；
        //能看到角色形状的深度渐变 → 瓦片有内容，问题在采样端。
        float4 coord = mul(_BlurToonPerObjShadowMatrices[0], float4(positionWS, 1.0));
        coord.xyz /= coord.w;
        float2 uv = _BlurToonPerObjShadowTiles[0].xy + saturate(coord.xy) * _BlurToonPerObjShadowTiles[0].z;
        float rawDepth = SAMPLE_TEXTURE2D_LOD(_BlurToonPerObjShadowAtlasRaw,
                                              sampler_BlurToonPerObjShadowAtlasRaw, uv, 0).r;
        debugColor = half3(rawDepth, rawDepth, rawDepth);
    }
    else if (mode == 5)
    {
        //接收端深度：该像素在瓦片里的 z 坐标。应当平滑铺满角色且落在 (0,1) 开区间内。
        //若大片贴到 0 或 1 → 深度范围没罩住角色，越界判定会把它当作瓦片外 → 回退 URP 阴影。
        float4 coord = mul(_BlurToonPerObjShadowMatrices[0], float4(positionWS, 1.0));
        coord.xyz /= coord.w;
        debugColor = half3(saturate(coord.z), saturate(coord.z), saturate(coord.z));
    }
    else
    {
        //纹素密度：把瓦片的纹素栅格直接画成棋盘，一格 = 一个阴影纹素。
        //这是判断“锯齿还有没有救”的唯一客观依据：
        //  格子小于一个屏幕像素 → 台阶是像素级的，抗锯齿能解决；
        //  格子明显大于一个屏幕像素 → 台阶比像素还大，任何抗锯齿都无能为力，
        //                             只能提高 Atlas Size 或减少 Max Caster Count 换更大的瓦片。
        float4 coord = mul(_BlurToonPerObjShadowMatrices[0], float4(positionWS, 1.0));
        coord.xyz /= coord.w;
        //瓦片局部 [0,1] 除以“1/瓦片边长”即得到纹素坐标
        float2 texelCoord = coord.xy / max(_BlurToonPerObjShadowAtlasSize.z, 1e-6);
        float checker = fmod(floor(texelCoord.x) + floor(texelCoord.y), 2.0);
        debugColor = lerp(half3(0.15, 0.15, 0.25), half3(0.95, 0.95, 1.0), half(checker));
    }

    return true;
}

#endif // _BLURTOON_PER_OBJECT_SHADOW

#endif // BLURTOONURP_PER_OBJECT_SHADOW_FUNCTION_INCLUDED
