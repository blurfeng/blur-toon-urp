using System;
using UnityEngine;

namespace BlurToonURP
{
    /// <summary>
    /// 逐对象阴影的阴影方向来源。
    /// </summary>
    public enum BlurToonShadowDirectionMode
    {
        /// <summary>
        /// 严格跟随主光方向。物理一致，但光源旋转时瓦片的纹素栅格整体旋转，边缘仍会重新量化。
        /// 适合光源基本静止、只想解决分辨率问题的场景。
        /// </summary>
        MainLight = 0,

        /// <summary>
        /// 以“角色→相机”的视线方向为主、按比例混入主光方向。
        /// 光源转动时投影基几乎不变，自阴影边缘极其稳定——直接针对“光源移动时阴影边缘变化剧烈”。
        /// 代价是阴影方向不再严格物理正确（NPR 的常规取舍）。
        /// 思路参考 StarRailNPRShader 的 ShadowUsage.Self（lerp(viewForward, lightForward, 0.2)）。
        /// </summary>
        ViewBlend = 1,
    }

    /// <summary>
    /// 逐对象阴影图集分辨率。
    /// </summary>
    public enum BlurToonShadowAtlasSize
    {
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
    }

    /// <summary>
    /// 逐对象阴影与 URP 主光阴影的合并方式。
    /// 瓦片里只包含投射者自身（不含场景遮挡物），因此这个选择决定了角色能否收到场景投影。
    /// </summary>
    public enum BlurToonShadowCombineMode
    {
        /// <summary>
        /// 取较暗者。角色同时收到 URP 的场景投影与瓦片的高密度自阴影。
        /// 代价：URP 级联里那份低分辨率的角色自阴影仍然存在，阶梯不会完全消失。
        /// </summary>
        Min = 0,

        /// <summary>
        /// 命中瓦片时完全用瓦片结果替换。自阴影边缘最干净。
        /// 代价：角色收不到场景投射到它身上的阴影。仅适合无场景遮挡的演出镜头（立绘、换装界面）。
        /// </summary>
        Replace = 1,

        /// <summary>
        /// 场景投影 + 高清自阴影，两者兼得，推荐默认。
        ///
        /// Min 与 Replace 的两难来自同一件事：角色同时存在于两张阴影图里。
        /// 本模式在采样级联图前，把采样点沿主光方向推离本体一段距离（自剔除偏移），
        /// 使角色自身写入级联图的那部分深度不再能遮挡自己——级联图于是只剩下场景遮挡物的贡献，
        /// 再与瓦片的自阴影取 min，两份阴影各司其职，互不污染。
        ///
        /// 沿光方向平移不改变光空间的 xy，采样的仍是同一个纹素，因此阴影图案不会有任何横向位移，
        /// 只是深度比较的基准前移了。
        /// </summary>
        SceneAndSelf = 2,
    }

    /// <summary>
    /// 逐对象阴影的调试可视化模式。排查“阴影从哪来”时直接看瓦片本身，比在最终画面里猜可靠得多。
    /// </summary>
    public enum BlurToonPerObjectShadowDebugMode
    {
        /// <summary>关闭。</summary>
        Off = 0,

        /// <summary>阴影值灰度：白=无遮挡 黑=完全遮挡。出现细碎噪点/条纹即为自阴影粉刺（bias 不足）。</summary>
        ShadowValue = 1,

        /// <summary>瓦片覆盖：绿=像素落在瓦片内 红=落在瓦片外（回退 URP 阴影）。角色身上出现红色说明瓦片没罩住。</summary>
        TileCoverage = 2,

        /// <summary>瓦片 UV：红绿渐变应平滑铺满角色。某个光源角度下突然整体偏移/翻转即为矩阵构造有跳变。</summary>
        TileUV = 3,

        /// <summary>
        /// 图集原始深度：直接看瓦片里画进了什么。反向 Z 下 1=近 0=远，清空值为远(0)。
        /// 角色所在处一片纯黑 → 几何根本没进图集（剔除问题）；能看到深度渐变 → 瓦片有内容，问题在采样端。
        /// </summary>
        AtlasDepth = 4,

        /// <summary>
        /// 接收端深度：该像素在瓦片里的 z 坐标，应平滑铺满角色且落在 (0,1) 开区间内。
        /// 大片贴死在 0 或 1 → 深度范围没罩住角色，越界判定会当作瓦片外而回退 URP 阴影。
        /// </summary>
        ReceiverDepth = 5,

        /// <summary>
        /// 纹素密度：把瓦片的纹素栅格画成棋盘，一格 = 一个阴影纹素。
        /// 判断“边缘锯齿还有没有救”的客观依据：格子小于一个屏幕像素说明台阶是像素级的，
        /// 抗锯齿能解决；格子明显大于一个屏幕像素说明台阶比像素还大，抗锯齿无能为力，
        /// 只能提高 Atlas Size 或减少 Max Caster Count 换更大的瓦片。
        /// </summary>
        TexelDensity = 6,
    }

    /// <summary>
    /// BlurToonPerObjectShadowFeature 的可序列化配置。
    /// </summary>
    [Serializable]
    public class BlurToonPerObjectShadowSettings
    {
        [Header("图集 Atlas")]
        [Tooltip("阴影图集总分辨率。实际每个投射者拿到的瓦片 = 图集边长 / 网格划分数。")]
        public BlurToonShadowAtlasSize atlasSize = BlurToonShadowAtlasSize._2048;

        [Tooltip("同屏最多处理的投射者数量。图集会按 1/4/9/16 的网格划分，数量越多单个瓦片越小。\n" +
                 "1 个 → 瓦片=图集全尺寸；4 个 → 瓦片=图集一半边长；以此类推。")]
        [Range(1, 16)]
        public int maxCasterCount = 4;

        [Header("阴影方向 Direction")]
        [Tooltip("阴影方向来源。ViewBlend 能显著抑制“光源移动时阴影边缘剧烈变化”，推荐角色使用。")]
        public BlurToonShadowDirectionMode directionMode = BlurToonShadowDirectionMode.ViewBlend;

        [Tooltip("ViewBlend 模式下混入主光方向的比例。0=完全跟随视线（最稳定但阴影几乎不随光变化）；" +
                 "1=完全跟随主光（等同 MainLight 模式）。0.2 左右是稳定性与光照表现的平衡点。")]
        [Range(0f, 1f)]
        public float viewBlendToLight = 0.2f;

        [Tooltip("ViewBlend 模式下限制阴影方向的俯仰角，避免接近正上方/正下方俯视时出现不该有的自阴影。" +
                 "取值为阴影方向与世界上方向夹角的允许区间（度）。")]
        public Vector2 viewBlendPitchClampDegrees = new Vector2(90f, 150f);

        [Header("范围 Range")]
        [Tooltip("超过该距离（米）的投射者不再分配瓦片，回退到 URP 级联阴影。")]
        [Min(1f)]
        public float maxDistance = 50f;

        [Tooltip("在 maxDistance 之前多少米开始淡出回 URP 级联阴影，避免硬切换。")]
        [Min(0f)]
        public float fadeRange = 5f;

        [Tooltip("正交视锥朝光源方向额外拉长的距离（米）。\n" +
                 "瓦片只画投射者自身，此值仅用于加大近平面余量。角色带长武器、披风等超出包围球的部件时可调大；" +
                 "一般保持较小值即可，过大只会浪费深度精度。")]
        [Min(0f)]
        public float casterExtrusion = 2f;

        [Tooltip("与 URP 主光阴影的合并方式。\n" +
                 "SceneAndSelf＝场景投影 + 高清自阴影（推荐）：先把角色自身从级联图里剔除再取 min。\n" +
                 "Min＝取较暗者：保留场景投影，但 URP 那份低分辨率自阴影的阶梯仍在。\n" +
                 "Replace＝命中瓦片时完全替换：自阴影边缘最干净，但角色收不到场景投影。")]
        public BlurToonShadowCombineMode combineMode = BlurToonShadowCombineMode.SceneAndSelf;

        [Tooltip("SceneAndSelf 模式的自剔除距离倍率。\n" +
                 "基准距离由着色器逐像素算出——从该像素沿主光方向走多远才离开角色的包围球，" +
                 "即“前方还有多少可能是角色自己的空间”。朝光面接近 0，背光面接近包围球直径。\n" +
                 "1＝刚好剔到包围球边界（默认）；调小会让 URP 那份低分辨率自阴影重新渗出；\n" +
                 "调大会连贴近角色的场景遮挡物也一起剔掉，导致近处物体投不到角色身上。\n" +
                 "0＝完全关闭自剔除，等同 Min。\n" +
                 "注意：屏幕空间阴影（URP Asset 的 Screen Space Shadows）下无法按世界位置重采样，本项自动失效并回退 Min。")]
        [Range(0f, 2f)]
        public float selfRejectScale = 1f;

        [Header("偏移 Bias")]
        [Tooltip("硬件深度偏移（常量项）。用光栅化阶段的深度偏移而不是顶点位移，" +
                 "不会随光源方向形变 caster 轮廓，因此不会造成阴影边界在表面上滑动。")]
        public float depthBias = 1.0f;

        [Tooltip("硬件深度偏移（坡度项）。掠射角处自动加大偏移，压制自阴影粉刺。")]
        public float slopeBias = 2.5f;

        [Tooltip("法线偏移，单位为“瓦片纹素”。沿法线内缩 caster，压制明暗交界处的自阴影碎裂。" +
                 "瓦片分辨率高时所需的值远小于级联阴影。")]
        [Range(0f, 3f)]
        public float normalBias = 0.5f;

        [Tooltip("接收端法线偏移，单位为“瓦片纹素”。采样前沿表面法线把采样点推离表面。\n" +
                 "掠射角（光线与表面接近平行）下，一个纹素在表面上覆盖极长一段，投射端 bias 无论怎么调都跟不上，" +
                 "整片表面会同时翻成自遮挡——这正是“光源转到与表面平行时突然全黑”的成因。\n" +
                 "接收端偏移按纹素世界尺寸补偿，是这种配置下唯一有效的手段。过大会让贴近表面的细小阴影漏光。")]
        [Range(0f, 8f)]
        public float receiverNormalOffset = 1.5f;

        [Header("表现 Appearance")]
        [Tooltip("阴影强度。1=完全遮挡，0=不产生阴影。")]
        [Range(0f, 1f)]
        public float strength = 1f;

        [Tooltip("是否把投影矩阵吸附到纹素栅格。开启后角色移动时阴影边缘按整纹素跳变而不是连续爬行，" +
                 "配合投射者上的“包围半径量化”一起使用效果最好。")]
        public bool stabilizeTexelSnapping = true;

        [Header("调试 Debug")]
        [Tooltip("把瓦片内容直接画到角色上，用于定位阴影异常的来源。发布前记得关闭。")]
        public BlurToonPerObjectShadowDebugMode debugMode = BlurToonPerObjectShadowDebugMode.Off;

        /// <summary>
        /// 图集按正方形网格划分，返回容纳 casterCount 个投射者所需的每边瓦片数（1/2/4）。
        /// 用 2 的幂次可让瓦片边长整除图集边长，避免出现非整数纹素的瓦片边界。
        /// </summary>
        public static int GetTileGridDimension(int casterCount)
        {
            int needed = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, casterCount)));
            return Mathf.NextPowerOfTwo(Mathf.Max(1, needed));
        }

        /// <summary>按配置上限划分时的每边瓦片数，即最坏情况。</summary>
        public int GetTileGridDimension()
        {
            return GetTileGridDimension(maxCasterCount);
        }

        /// <summary>
        /// 最坏情况下单个瓦片的边长（像素）。
        /// 运行时实际使用的是按本帧入选数量划分出的瓦片，只会比这个大。
        /// </summary>
        public int GetTileResolution()
        {
            return (int)atlasSize / GetTileGridDimension();
        }

        /// <summary>本配置下实际可容纳的投射者数量。</summary>
        public int GetEffectiveCasterCapacity()
        {
            int grid = GetTileGridDimension();
            return Mathf.Min(maxCasterCount, grid * grid);
        }
    }
}
