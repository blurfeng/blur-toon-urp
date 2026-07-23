![](Documents~/Header.gif)

<p align="center">
  <!-- <img alt="GitHub Release" src="https://img.shields.io/github/v/release/blurfeng/BlurToonURP?color=blue"> -->
  <img alt="Unity" src="https://img.shields.io/badge/Unity-2022.3-black?logo=unity">
  <img alt="URP" src="https://img.shields.io/badge/URP-14.x-black?logo=unity">
  <!-- <img alt="GitHub Downloads (all assets, all releases)" src="https://img.shields.io/github/downloads/blurfeng/BlurToonURP/total?color=green"> -->
  <img alt="GitHub Repo License" src="https://img.shields.io/badge/license-MIT-blueviolet">
  <img alt="GitHub Repo Issues" src="https://img.shields.io/github/issues/blurfeng/BlurToonURP?color=yellow">
</p>

<p align="center">
  🌍
  中文 |
  <a href="./README_EN.md">English</a> |
  <a href="./README_JA.md">日本語</a>
</p>

<p align="center">
  📥
  <a href="#-快速开始">快速开始</a> |
  <a href="#-功能">功能一览</a>
</p>

# BlurToonURP - 二次元卡通渲染 Shader
这是使用 `Unity 2022.3 LTS(URP 14.x)` 的 `URP` 渲染管线开发的 `NPR` 二次元卡通渲染 Shader 工程。  
实现了通用的二次元卡通渲染效果，涵盖基础设置、阴影色阶、高光、外描边、边缘光、自发光、材质捕获与光照设置等完整功能。  
使用 MIT 许可证，你可以随意地使用这个项目。  

## 📜 目录
- [💻 运行环境](#-运行环境)
- [🌱 快速开始](#-快速开始)
- [✨ 功能](#-功能)
  - [Basic 基础设置](#basic-基础设置)
    - [RenderQueue 渲染队列](#renderqueue-渲染队列)
    - [Clip 裁剪（溶解）](#clip-裁剪溶解)
    - [Stencil 模板测试](#stencil-模板测试)
  - [Base Map 基础贴图](#base-map-基础贴图)
    - [Diffuse Type 漫反射过渡方式](#diffuse-type-漫反射过渡方式)
    - [Bright Shade Step 阴影色阶](#bright-shade-step-阴影色阶)
    - [Shade Threshold Map 阴影阈值贴图](#shade-threshold-map-阴影阈值贴图)
  - [Normal Map 法线贴图](#normal-map-法线贴图)
  - [HighLight 高光](#highlight-高光)
    - [Mask Map 遮罩贴图](#mask-map-遮罩贴图)
  - [Outline 外描边](#outline-外描边)
  - [Rim Light 边缘光](#rim-light-边缘光)
    - [Rim Light Type 边缘检测方式](#rim-light-type-边缘检测方式)
    - [Shade Mask 暗部遮罩](#shade-mask-暗部遮罩)
    - [Mask Map 遮罩贴图](#mask-map-遮罩贴图-1)
  - [Emissive 自发光](#emissive-自发光)
    - [Emissive Animation 自发光动画](#emissive-animation-自发光动画)
  - [MatCap 材质捕获](#matcap-材质捕获)
    - [Mask Map 遮罩贴图](#mask-map-遮罩贴图-2)
  - [Light Setting 光照设置](#light-setting-光照设置)
  - [Per Object Shadow 逐对象阴影](#per-object-shadow-逐对象阴影)
    - [合并方式](#合并方式)
    - [Debug Mode 调试模式](#debug-mode-调试模式)
  - [渲染 Pass 说明](#渲染-pass-说明)
  - [Debug 调试](#debug-调试)
  - [Tools 工具](#tools-工具)

## 💻 运行环境
| 项目 | 版本 |
| :-- | :-- |
| Unity | `2022.3.62f3 LTS` |
| URP (Universal RP) | `14.0.12`（`manifest.json` 声明 12.1.12，Unity 2022.3 实际解析为 14.0.12） |
| 渲染管线 | Universal Render Pipeline |
| 许可证 | MIT |

## 🌱 快速开始
1. 使用 `Unity 2022.3 LTS` 打开工程，并确保项目已启用 `URP` 渲染管线。  
2. 新建材质球，将 Shader 切换为 `BlurToonURP/Lit`。  
3. 在材质球的 `Inspector` 面板中，通过配套编辑器界面折叠面板调整各项效果。  
4. 参考示例资源：
   - 示例材质：`Assets/PluginsDeveloper/BlurToonURP/Example/Materials/Lit.mat`
   - 示例预制体：`Assets/PluginsDeveloper/BlurToonURP/Example/Prefabs/Sphere.prefab`

Shader 与编辑器源码位置：  
- Shader：`Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/Lit.shader`
- 材质属性缓冲区：`Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/LitInput.hlsl`
- 通用函数库：`Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/BlurFunction.hlsl`
- 阴影函数库：`Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/ShadowFunction.hlsl`
- 编辑器界面：`Assets/PluginsDeveloper/BlurToonURP/Core/Editor/ShaderGUILit.cs`
- 工具脚本：`Assets/PluginsDeveloper/BlurToonURP/Core/Tools/LightController.cs`

![](Documents~/Feature_Inspector.png)

## ✨ 功能
---
功能代码编写尽力保证 `代码整洁` 和 `性能高效`。完整的 `代码注释` 使你直接阅读 Shader 文件也可以快速理解整个代码工作流程。  
同时开发了配套的 `Inspector 编辑器界面`，使创作者能更方便地进行效果的调整。  
下方各功能标题与材质球编辑器界面的折叠面板一一对应。  

> 渲染管线包含 5 个 Pass：`ForwardLit`（基础光照渲染）、`ShadowCaster`（阴影投射）、`DepthOnly`（深度）、`DepthNormals`（深度法线）与 `Outline`（外描边）。

> 所有材质属性集中声明于 `LitInput.hlsl` 的 `UnityPerMaterial` 常量缓冲区，由全部 Pass 统一 `include`，保证各 Pass 布局逐字节一致以兼容 `SRP Batcher`。

---

### Basic 基础设置
`【基础设置 Basic】表面类型、渲染面、透明度裁切、裁剪、模板测试`

统一管理材质的基础渲染状态。切换 `表面类型` 时，编辑器会自动设置对应的混合因子、深度写入、渲染队列与 `RenderType` 标签。  
透明度裁切贯穿 `ForwardLit / Outline / ShadowCaster / DepthOnly / DepthNormals` 全部 Pass，因此镂空轮廓在**投射阴影、深度图、SSAO 法线**中都与本体保持一致。  

![](Documents~/Feature_Basic.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 表面类型 | `_Surface`（→ 自动设置 `_SrcBlend` / `_DstBlend` / `_ZWrite` / 渲染队列 / `RenderType`） | `Opaque` 不透明（`One/Zero`，写深度）/ `Transparent` 透明（标准 Alpha 混合，关深度，Transparent 队列） |
| 渲染面 | `_IntRenderFaceType`（→ `Cull [_IntRenderFaceType]`） | `Both` 双面（Cull Off）/ `Back` 反面（Cull Front）/ `Front` 正面（Cull Back，默认） |
| 透明度裁切 | `_ToggleAlphaClip` → `_ALPHATEST_ON` | 开启后按 `基础贴图Alpha × 基础色Alpha` 裁切像素（Cutout） |
| 裁切阈值 | `_Cutoff` | Alpha 低于此值的像素被丢弃 |

> 说明：透明仅提供**标准 Alpha 混合**（不含 Premultiply/Additive/Multiply 多模式）。  
> `渲染面` 作用于 `ForwardLit / ShadowCaster / DepthOnly / DepthNormals`；`Outline` Pass 固定 `Cull Front`（描边原理即渲染背面），不受此项影响。  
> 描边 Pass 的混合保持独立（`SrcAlpha OneMinusSrcAlpha`），不随表面类型切换，以保证描边稳定可见。  

#### RenderQueue 渲染队列
开启 `渲染队列自动` 时，由编辑器按 `表面类型 / 透明度裁切 / 模板类型` 推导队列；关闭后可手动指定（多选编辑时作用到全部选中材质）。  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 渲染队列自动 | `_ToggleRenderQueueAuto` | 开（默认）= 自动推导；关 = 保留手动设置的队列，编辑器不再覆盖 |
| 渲染队列 | `Material.renderQueue` | 仅在关闭自动时可编辑 |

自动推导规则：

| 条件 | 队列 |
| :-- | :-- |
| 表面类型 = `Transparent` | `Transparent`（3000） |
| 模板类型 = `Reserve` 保留 | `AlphaTest - 1`（2449，先于 `Discard` 渲染以写入遮罩） |
| 模板类型 = `Discard` 丢弃 | `AlphaTest`（2450） |
| 开启透明度裁切 | `AlphaTest`（2450） |
| 其余（不透明） | `Geometry`（2000） |

#### Clip 裁剪（溶解）
从裁剪遮罩贴图的 `R` 通道采样强度，实现挖孔剔除或透明度溶解淡出。与上方 `透明度裁切` **相互独立**，可同时使用。  

![](Documents~/Feature_Clip.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 裁剪类型 | `_IntClipType` → `_CLIP_DITHER` / `_CLIP_ALPHA`（无关键词 = 关闭） | `Off` 关闭 / `Dither` 挖孔 / `Alpha` 透明度 |
| 裁剪贴图 | `_TexClipMaskMap` | 采样 R 通道作为裁剪强度（0-1），UV 独立可缩放偏移 |
| 裁剪强度 | `_FloatClipIntensity` | 仅 `Dither`：低于此值的像素被剔除 |
| 透明强度 | `_FloatClipTransIntensity` | 仅 `Alpha`：范围 `-1~1`，越大溶解越多 |
| 基础贴图A通道生效 | `_ToggleClipTransBaseMapAlpha` | 仅 `Alpha`：把基础贴图 Alpha 叠乘进裁剪计算 |

> `Alpha` 模式的裁剪值会写入最终 Alpha（`alphaFinal = saturate(clipAlpha)`），因此需将 `表面类型` 设为 `Transparent` 才能看到透明淡出，否则仅表现为硬剔除。  
> 裁剪在 `ForwardLit` 与 `Outline` 两个 Pass 生效；`ShadowCaster / DepthOnly / DepthNormals` **不参与裁剪**，即溶解处仍会投射阴影、仍写入深度。  
> `Outline` Pass 的裁剪为**硬剔除**（不做透明淡出），且 `Alpha` 模式下描边仅用 `遮罩 - 透明强度` 判断，不叠加基础贴图 Alpha。  

#### Stencil 模板测试
通过模板缓冲区实现遮罩效果，**相同 `模板组序号` 的材质球才会互相影响**。典型用途：让身体不透过衣服显示、眼睛穿透头发显示等。  

![](Documents~/Feature_Stencil.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 模板类型 | `_IntStencilType`（→ 编辑器预设 Comp/Pass/Fail） | `Off` 关闭 / `Discard` 丢弃（同组遮罩处不绘制）/ `Reserve` 保留（写入遮罩，且先于丢弃渲染） |
| 模板组序号 | `_FloatStencilNum`（`Ref`） | 相同序号才互相影响（0-255） |
| 比较规则 / 通过写入 / 失败写入 | `_FloatStencilComp` / `_FloatStencilPass` / `_FloatStencilFail` | 由 `模板类型` 自动预设，无需手动设置 |

预设映射（值对应 `UnityEngine.Rendering.CompareFunction` / `StencilOp`）：

| 模板类型 | Comp | Pass | Fail |
| :-- | :-- | :-- | :-- |
| `Off` 关闭 | `Disabled`(0) | `Keep`(0) | `Keep`(0) |
| `Discard` 丢弃 | `NotEqual`(6) | `Keep`(0) | `Keep`(0) |
| `Reserve` 保留 | `Always`(8) | `Replace`(2) | `Replace`(2) |

> 模板测试作用于 `ForwardLit` 与 `Outline` 两个 Pass，因此描边也会一并被遮罩/写入。  

---

### Base Map 基础贴图
`【基础贴图 BaseMap】基础贴图及暗部贴图`

以基础贴图为主色，结合 `半兰伯特(Half-Lambert)` 光照计算出暗部，构建卡通风格的明暗分层。  
暗部的**过渡生成方式**有两种，由 `漫反射过渡方式` 二选一：`色阶`（程序化两段暗部）与 `Ramp贴图`（一维渐变软过渡）。二者产出下游共用的最终颜色与暗部因子，是同一个漫反射环节的两种实现，并非并列的独立特性。  

![](Documents~/Feature_BaseMap.png)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 基础贴图 | `_BaseMap` | 主纹理（sRGB） |
| 基础色 | `_BaseColor` | 与贴图相乘的自定义颜色（HDR） |
| 混合颜色 / 混合强度 | `_BaseMapBlendColor` / `_BaseMapBlendColorIntensity` | 在基础色上叠加混合一个自定义颜色 |
| 法线贴图 | `_ToggleNormalMapOnBaseMap` | 基础贴图的明暗计算使用法线贴图（需先在【法线贴图】面板指定贴图） |
| 漫反射过渡方式 | `_FloatDiffuseType` → `_BASEMAP_DIFFUSE_RAMP_ON` | `色阶`（0，默认）/ `Ramp贴图`（1），见下方【漫反射过渡方式】 |
| 暗部1颜色 | `_Shade1Color` | 第一层暗部颜色（仅 `色阶` 方式） |
| 暗部2颜色 | `_Shade2Color` | 第二层暗部颜色（仅 `色阶` 方式） |

#### Diffuse Type 漫反射过渡方式
决定「半兰伯特值 → 明暗颜色」这一步的生成方式。切换后编辑器只显示对应方式的参数，另一套参数仍保留在材质中不会丢失。  

| 方式 | 关键词 | 原理 | 适用 |
| :-- | :-- | :-- | :-- |
| `色阶`（默认） | 无关键词 | 程序化两段色阶：按 `位置 / 模糊` 在亮部、暗部1、暗部2 三层颜色间过渡 | 分层明确的卡通阴影，纯参数调节、无需额外贴图 |
| `Ramp贴图` | `_BASEMAP_DIFFUSE_RAMP_ON` | 用半兰伯特值作横向 UV 采样一维渐变贴图（左=暗 右=亮），结果直接乘到基础色 | 过渡软硬与分段完全由美术在贴图内绘制，更自由 |

`Ramp贴图` 方式的专属参数：  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| Ramp 渐变贴图 | `_TexDiffuseRamp` | 横向渐变（左=暗部 右=亮部）。**默认白 = 不变暗**（未指定贴图时安全降级） |
| 行选择(V) | `_FloatDiffuseRampV` | 多行渐变图集时选取所在行；单行图取 `0.5` |

> Ramp 采样在 Shader 内强制 `Clamp / Linear / LOD0`，无需修改贴图导入的 Wrap/Filter 设置；建议关闭 Mipmap。强制 LOD0 是为了避免明暗交界处半兰伯特的屏幕导数过大而选到模糊 mip。  
> 两种方式都受 `阴影接收` 与 `暗部阈值贴图` 影响——它们作用在半兰伯特值上，因此阴影会把 Ramp 的采样坐标推向渐变的暗端。  
> `Ramp贴图` 方式下不使用 `暗部1/2 颜色` 与 `阴影色阶` 参数；下游（高光 / 材质捕获的「阴影遮罩」）所需的暗部因子改由 Ramp 采样色的 Rec709 亮度反推，与色阶方式共用同一条链路。  

#### Bright Shade Step 阴影色阶
> 仅 `色阶` 方式生效。  

以半兰伯特值为依据，控制 `亮部→暗部1` 与 `暗部1→暗部2` 两级过渡的分界位置与羽化模糊程度，从而实现硬边卡通阴影或柔和渐变阴影。  

![](Documents~/Feature_ShadeStep.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 亮部→暗部1 : 位置 | `_FloatBrightShade1Step` | 第一级阴影分界位置 |
| 亮部→暗部1 : 模糊 | `_FloatBrightShade1Blur` | 第一级阴影边缘羽化 |
| 暗部1→暗部2 : 位置 | `_FloatShade1Shade2Step` | 第二级阴影分界位置 |
| 暗部1→暗部2 : 模糊 | `_FloatShade1Shade2Blur` | 第二级阴影边缘羽化 |

> 色阶过渡带做了**屏幕空间抗锯齿**：用 `fwidth(halfLambert)` 作为过渡带的最小宽度，保证过渡至少覆盖约 1 个像素。这样在球体轮廓、掠射角、低模面片边界等半兰伯特梯度陡峭处不会塌缩成硬边锯齿；美术设置的模糊更大时按其原值，外观不变。  

#### Shade Threshold Map 阴影阈值贴图
> `色阶` 与 `Ramp贴图` 两种方式共用。  

通过一张阈值贴图（采样 R 通道）控制暗部的分布与强度，可用于在固定区域（如脸部、褶皱）手绘阴影的形状。  
![](Documents~/Feature_ShadeThresholdMap.png)

![](Documents~/Feature_ShadeThresholdMap.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleShadeThresholdMap` → `_BASEMAP_SHADE_THRESHOLDMAP_ON` | 开启阈值贴图 |
| 暗部阈值贴图 | `_TexShadeThresholdMap` | 阈值贴图（linear） |
| 强度 | `_FloatShadeThresholdMapIntensity` | 阈值影响强度 |

---

### Normal Map 法线贴图
`【法线贴图 NormalMap】贴图、强度、各效果生效状态`

采样切线空间法线贴图并转换到世界空间，供各效果按需使用。  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 法线贴图 | `_BumpMap` | 切线空间法线贴图（含缩放偏移） |
| 强度 | `_BumpScale` | 法线强度 |

**法线的启用开关采用面向对象设计，分散在各效果面板内**，本面板只负责指定法线贴图本身，并汇总各效果的生效状态：  

| 效果 | 开关位置 | 属性 |
| :-- | :-- | :-- |
| 基础贴图 | 【基础贴图】→ `法线贴图` | `_ToggleNormalMapOnBaseMap` |
| 高光 | 【高光】→ `法线贴图` | `_ToggleNormalMapOnHighLight` |
| 材质捕获 | 【材质捕获】→ `法线贴图` | `_ToggleNormalMapOnMatCap` |
| 自发光 | 【自发光】→ `自发光动画 → 视角变化颜色 → 法线贴图` | `_ToggleNormalMapOnEmissive` |
| 边缘光 | 【边缘光】→ `法线来源`（几何法线 / 法线贴图 / 混合） | `_FloatRimLightNormalSource` |

本面板底部会**只读列出**上述每一项当前的生效状态，便于一眼看出「开了法线开关却没有变化」到底是缺贴图、强度为 0，还是效果自身的前置开关未开。  

> 未指定法线贴图却开启了上述任一开关时，编辑器会在该开关下方**红字提示不生效**。  
> 本面板在未指定贴图、或 `强度` 为 0（采样结果等同平面法线）时也会给出总提示。  

---

### HighLight 高光
`【高光 HighLight】高光颜色、大小、遮罩`

基于 `法线与半程向量夹角(NdotH)` 计算卡通风格高光，支持高光贴图、颜色、强度、大小（范围）与边缘羽化，并可跟随光照颜色、被阴影遮罩。  

![](Documents~/Feature_HighLight.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleHighLight` → `_HIGHLIGHT_ON` | 开启高光 |
| 高光贴图 | `_TexHighLightMap` | 高光基础色 = 贴图RGB(sRGB) × 颜色。默认白 = 纯色高光，可用贴图做彩色/图案高光 |
| 颜色 | `_ColorHighLightColor` | 高光颜色（HDR，Alpha 参与强度） |
| 强度 | `_FloatHighLightIntensity` | 高光强度 |
| 大小 | `_FloatHighLightSize` | 高光范围。`[0,1]` 映射到反射幂 `[512,4]`，值越大高光越大 |
| 边缘羽化 | `_FloatHighLightBlur` | 高光边缘软硬。`0` ≈ 色阶硬边，越大越柔，连续覆盖「色阶↔柔边」 |
| 法线贴图 | `_ToggleNormalMapOnHighLight` | 高光朝向使用法线贴图 |
| 阴影遮罩 / 强度 | `_ToggleHighLightShadowMask` / `_FloatHighLightShadowMaskIntensity` | 按暗部1区域压暗高光，避免阴影里出现不自然高光 |

> 开启 `阴影接收` 时，高光系数会乘以阴影衰减，即**真实投射阴影处不出现高光**。  
> 高光的 `受光照颜色影响` 由 `光照设置 → 光照开关 → 高光`（`_ToggleGlobalLightHighLight`）控制；  
> `光照设置 → 光照方向锁定 → 高光`（`_ToggleLightHorLockHighLight`）可将高光的光照方向锁定至水平。  

#### Mask Map 遮罩贴图
使用一张与基础贴图同 UV 的遮罩贴图（采样 R 通道）逐像素控制高光的分布与强度。  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 遮罩贴图 | `_TexHighLightMaskMap` → `_HIGHLIGHT_MASKMAP_ON` | 高光遮罩贴图（存在贴图即自动开启关键词） |
| 遮罩强度 | `_FloatHighLightMaskMapIntensity` | 遮罩影响强度 |

---

### Outline 外描边
`【外描边 Outline】粗细、颜色`

独立的 `Outline` Pass（`LightMode = SRPDefaultUnlit`），使用 `Cull Front` **剔除正面（即只渲染背面）**并沿法线方向外扩顶点实现描边。支持多种描边方向来源与宽度模式。  

![](Documents~/Feature_Outline.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_OUTLINE_ON`（Pass 开关） | 开启外描边 |
| 描边类型 | `_FloatOutlineType` | 描边方向来源：`VertexNormal` / `VertexColor` / `VertexTangent` |
| 宽度类型 | `_FloatOutlineWidthType` → `_OUTLINE_WIDTH_SAME` / `_OUTLINE_WIDTH_SCALING` | `Same` 等宽（所有顶点外扩相同距离）/ `Scaling` 变化宽度（按 `dot(顶点方向, 外扩方向) + 0.3` 缩放，凸出处更粗、凹陷处更细；为逐顶点固定值，**与视角无关**） |
| 颜色 | `_ColorOutlineColor` | 描边颜色 |
| 宽度 | `_FloatOutlineWidth` | 描边宽度 |
| 基础贴图混合 / 强度 | `_ToggleOutlineBaseMapBlend` / `_FloatOutlineBaseMapBlendIntensity` | 描边色与基础贴图颜色混合，使描边更自然 |
| 描边纹理 / 强度 | `_TexOutlineMap` → `_OUTLINE_MAP_ON` / `_FloatOutlineMapIntensity` | 用描边专用纹理调制描边颜色（彩色描边/图案/噪声），指定贴图即自动生效 |

> 描边的 `受光照与阴影影响` 由 `光照设置 → 光照开关 → 描边`（`_ToggleGlobalLightOutline` / `_GlobalLightOutlineMixedIntensity`）控制，阴影部分复用 `阴影接收` 开关。  
> 描边同时支持 `透明度裁切` 与 `裁剪 Clip`、`模板测试 Stencil`（见【基础设置】面板）；但固定 `Cull Front`，不受 `渲染面` 影响。  

---

### Rim Light 边缘光
`【边缘光 RimLight】颜色、大小、遮罩`

在物体轮廓处叠加发亮的边缘，支持强度、内部延伸距离与硬边缘控制。  
边缘的**检测方式**有两种（`菲涅尔` / `深度差`）二选一，二者只负责产出同一个边缘信号，之后的颜色、强度、内部距离、硬边缘、暗部遮罩、遮罩贴图等处理**完全共用**。  

![](Documents~/Feature_RimLight.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleRimLight` → `_RIMLIGHT_ON` | 开启边缘光 |
| 颜色 | `_ColorRimLightColor` | 边缘光颜色（HDR，Alpha 控制不透明度） |
| 强度 | `_FloatRimLightIntensity` | 边缘光范围/强度 |
| 内部距离 | `_FloatRimLightInsideDistance` | 边缘光向内延伸距离，越大范围越窄 |
| 硬边缘 | `_ToggleRimLightHard` | 边缘硬化（去除渐变） |
| 检测方式 | `_FloatRimLightType` → `_RIMLIGHT_DEPTH_ON` | `菲涅尔`（0，默认）/ `深度差`（1），见下方【边缘检测方式】 |

#### Rim Light Type 边缘检测方式

| 方式 | 关键词 | 原理 | 特点 |
| :-- | :-- | :-- | :-- |
| `菲涅尔`（默认） | 无关键词 | 按 `法线与视线夹角(NdotV)`，越接近掠射角越亮 | 柔和渐变、随表面法线细节起伏；无额外依赖 |
| `深度差` | `_RIMLIGHT_DEPTH_ON` | 屏幕空间沿法线朝向偏移若干像素采样场景深度，偏移点更远即判定为朝向相机的轮廓边缘 | 轮廓等宽干净、不受表面法线细节影响；**需开启 Depth Texture** |

`菲涅尔` 方式的专属参数：  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 法线来源 / 混合强度 | `_FloatRimLightNormalSource` / `_FloatRimLightNormalMapBlend` | 边缘光法线来源：`几何法线` / `法线贴图` / `混合`（仅混合档用到混合强度） |

`深度差` 方式的专属参数：  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 采样宽度(像素) | `_FloatRimLightDepthWidth` | 偏移采样的像素距离，即边缘宽度。以 `1080p` 为基准做分辨率无关缩放，并限幅 128 像素 |
| 深度阈值 | `_FloatRimLightDepthThreshold` | 世界单位。深度差需超过此值才算边缘，用于抑制模型内部的深度噪声 |
| 阈值软过渡 | `_FloatRimLightDepthThresholdSoft` | 阈值附近的柔和过渡宽度，越大边缘越软 |

> ⚠ `深度差` 方式依赖 `_CameraDepthTexture`，**必须在 URP Asset 中开启 `Depth Texture`**，否则无效果甚至整体发亮。编辑器在选择该方式时会红字提示。  
> 当前像素深度取片元自身的 `positionCS.z`（不依赖深度图是否已包含本物体），偏移点用整数像素坐标 `Load` 采样，规避不同图形 API 的 UV 上下翻转问题。  
> 偏移**仅沿屏幕水平方向**（取观察空间法线 x 的符号），因此主要检测左右方向的竖直轮廓——这既与参考实现一致，也规避了 `SV_Position` 的 Y 轴方向在 D3D（向下）与 GL（向上）下不一致导致的上下轮廓偏移方向错误。  
> `深度差` 方式不使用 `法线来源` 配置；`菲涅尔` 方式的 `法线来源` 选择 `法线贴图` 或 `混合` 但未指定法线贴图时，编辑器会红字提示不生效。该选择使用 `lerp + step` 构建无分支选择器（与「描边类型」同款写法），不额外产生关键词变体。  

#### Shade Mask 暗部遮罩
对 `主光源反方向` 的边缘光进行遮罩，防止背光/阴影部分出现不自然的边缘发亮。可在遮罩后为暗部叠加专属颜色的边缘光。  

![](Documents~/Feature_RimLightShadeMask.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 暗部遮罩开关 | `_ToggleRimLightShadeMask` → `_RIMLIGHT_SHADEMASK_ON` | 开启暗部遮罩 |
| 遮罩强度 / 遮罩偏移 | `_FloatRimLightShadeMaskIntensity` / `_FloatRimLightShadeMaskOffset` | 遮罩强度与作用范围偏移 |
| 暗部颜色开关 | `_ToggleRimLightShadeColor` → `_RIMLIGHT_SHADEMASK_COLOR_ON` | 在暗部叠加专属颜色边缘光 |
| 暗部颜色 / 强度 / 硬边缘 | `_ColorRimLightShadeColor` / `_FloatRimLightShadeColorIntensity` / `_ToggleRimLightShadeColorHard` | 暗部边缘光颜色相关设置 |

#### Mask Map 遮罩贴图
使用一张与基础贴图同 UV 的遮罩贴图（采样 R 通道）逐像素控制边缘光的分布与强度。  

![](Documents~/Feature_RimLightMaskMap.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 遮罩贴图 | `_TexRimLightMaskMap` → `_RIMLIGHT_MASKMAP_ON` | 边缘光遮罩贴图（存在贴图即自动开启关键词） |
| 遮罩强度 | `_FloatRimLightMaskMapIntensity` | 遮罩影响强度（`-1~1`） |

---

### Emissive 自发光
`【自发光 Emissive】遮罩、颜色(HDR)、动画`

用自发光贴图叠加不受光照影响的发光色，支持固定发光与动画发光（UV 滚动 / 旋转 / 往复、时间变化色、视角变化色）。  

![](Documents~/Feature_Emissive.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleEmissive` → `_EMISSIVE_ON` | 开启自发光 |
| 自发光贴图 | `_TexEmissiveMap` | `RGB` = 发光颜色（sRGB），`A` = 发光强度（遮罩） |
| 颜色 | `_ColorEmissiveMapColor` | 自发光颜色（HDR），**默认黑色 = 不发光** |

自发光 = `贴图RGB × HDR颜色 × 强度(贴图A通道)`，直接叠加到最终颜色。  

> ⚠ **HDR 发光需要 Bloom 后处理才可见**：Shader 输出的是未截断的 HDR 颜色，提高 HDR 颜色的 `Intensity` 本身只是加亮。要产生泛光（发光溢出）效果，需在场景的 `Global Volume` 中启用 `Bloom`，并确保 URP Asset 勾选了 `HDR`。  

#### Emissive Animation 自发光动画
`自发光动画-开关` 关闭时为**固定模式**（仅 `贴图 × 颜色 × A通道`）；开启后进入**动画模式**，下列参数才生效。  

![](Documents~/Feature_EmissiveAnim.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 动画开关 | `_ToggleEmissiveAnim` → `_EMISSIVE_ANIM`（无关键词 = 固定） | 关 = 固定 / 开 = 动画 |
| UV比例模式 | `_FloatEmissiveAnimUVType` | `FullMap` UV 满铺映射 / `MatCap` 按观察空间法线球面映射，随视角流动 |
| 移动速度 | `_FloatEmissiveAnimSpeed` | UV 滚动速度 |
| 移动方向 U / V | `_FloatEmissiveAnimDirU` / `_FloatEmissiveAnimDirV` | UV 滚动方向 |
| 旋转速度 | `_FloatEmissiveAnimRotate` | 绕 UV 中心 `(0.5, 0.5)` 旋转 |
| 来回移动 | `_ToggleEmissiveAnimPingpong` | 用 `sin` 把时间曲线变为往复 |
| 颜色变化 / 变化颜色 / 变化速度 | `_ToggleEmissiveChangeColor` / `_ColorEmissiveChangeColor` / `_FloatEmissiveChangeSpeed` | 按 `cos` 时间曲线在自发光色与变化色之间来回过渡 |
| 视角变化颜色 | `_ToggleEmissiveViewChangeColor` / `_ColorEmissiveViewChangeColor` | 按观察方向与法线夹角（菲涅尔）在自发光色与该颜色间过渡 |
| ｜法线贴图 | `_ToggleNormalMapOnEmissive` | 视角变化颜色的菲涅尔计算使用法线贴图 |

> 动画模式下，**颜色**采样动画 UV，而**强度（A 通道）**采样静态 UV——因此发光区域固定在贴图绘制的位置，只有其中的颜色/图案在流动。  
> 强度低于 `0.005` 的像素不发光（`step` 阈值），避免贴图暗噪产生微光。  

---

### MatCap 材质捕获
`【材质捕获 MatCap】贴图、混合模式、遮罩`

球面环境贴图（MatCap / Sphere Map）：把世界法线转换到观察空间作为采样 UV，实现与视角相关的材质质感（金属、玉石、皮革等），无需真实反射即可获得丰富的表面表现。  

![](Documents~/Feature_MatCap.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleMatCap` → `_MATCAP_ON` | 开启材质捕获 |
| 材质捕获贴图 | `_TexMatCapMap` | 球面环境贴图（sRGB），**默认黑色 = 无效果** |
| 颜色 | `_ColorMatCapMapColor` | 自定义色（HDR，Alpha 参与强度） |
| 颜色混合模式 | `_FloatMatCapColorBlend` → `_MATCAP_COLORBLEND_MULTIPLY` / `_MATCAP_COLORBLEND_LERP`（无关键词 = Additive） | `Additive` 相加（线性减淡）/ `Multiply` 相乘（正片叠底）/ `Lerp` 插值混合 |
| 混合强度 | `_FloatMatCapColorBlendIntensity` | 混合模式的强度 |
| 旋转 | `_FloatMatCapRotate` | 绕 UV 中心旋转（`-1~1` 对应 `-π~π`） |
| 法线贴图 | `_ToggleNormalMapOnMatCap` | 采样朝向使用法线贴图 |
| 阴影遮罩 / 强度 | `_ToggleMatCapShadowMask` / `_FloatMatCapShadowMaskIntensity` | 按暗部1区域压暗 MatCap，使其在阴影里变暗 |

> MatCap 的 `受光照颜色影响` 由 `光照设置 → 光照开关 → 材质捕获`（`_ToggleGlobalLightMatCapMap`）控制。  

#### Mask Map 遮罩贴图
使用一张与基础贴图同 UV 的遮罩贴图（采样 R 通道）逐像素控制 MatCap 的分布与强度。  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 遮罩贴图 | `_TexMatCapMaskMap` | 遮罩贴图（默认白色 = 全显示，始终采样，无需关键词） |
| 遮罩强度 | `_FloatMatCapMaskMapIntensity` | 遮罩**偏移量**（`-1~1`）：与遮罩值相加后 `saturate`，`0` 时按贴图原值，正值整体增强、负值整体减弱 |

---

### Light Setting 光照设置
`【光照设置 LightSetting】光照开关、光照强度`

整合了实时光照、环境光照、曝光、附加光照、逐项光照开关、阴影、内置光照与光照方向锁定等设置。  

![](Documents~/Feature_LightSetting.gif)

**光照强度**

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 实时光照强度 | `_FloatRealtimeLightIntensity` | 主光源强度 |
| 环境光照强度 | `_FloatEnvLightIntensity` | 环境光/光照探针强度 |

**曝光设置**

![](Documents~/Feature_Exposure.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 全局曝光强度 | `_FloatGlobalExposureIntensity` | 全局曝光 |
| 基础贴图亮部曝光 | `_FloatBaseMapExposureIntensity` | 亮部曝光 |
| 基础贴图暗部1曝光 | `_FloatBaseMapShade1ExposureIntensity` | 暗部1曝光 |
| 基础贴图暗部2曝光 | `_FloatBaseMapShade2ExposureIntensity` | 暗部2曝光 |

**附加光照（点光源等）**

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleAddLight` → `_ADDLIGHT_ON` | 开启附加光照（点光源/聚光灯） |
| 强度 | `_FloatAddLightIntensity` | 附加光照强度。按各光源自身方向做半兰伯特方向着色（面光更亮、背光更暗），再叠加 `光色 × 距离衰减`，避免整体均匀发白 |

**光照开关（逐项受光照影响）**

可分别设置以下项目是否受光照颜色影响：  

| 项目 | 开关属性 | 混合强度属性 |
| :-- | :-- | :-- |
| 基础贴图 | `_ToggleGlobalLightBaseMap` | `_GlobalLightBaseMapMixedIntensity` |
| 暗部贴图1 | `_ToggleGlobalLightBaseShade1` | `_GlobalLightBaseShade1MixedIntensity` |
| 暗部贴图2 | `_ToggleGlobalLightBaseShade2` | `_GlobalLightBaseShade2MixedIntensity` |
| 高光 | `_ToggleGlobalLightHighLight` | —（简单开关，无混合强度） |
| 边缘光 | `_ToggleGlobalLightRimLight` | `_GlobalLightRimLightMixedIntensity` |
| 暗部边缘光 | `_ToggleGlobalLightRimLightShade` | `_GlobalLightRimLightShadeMixedIntensity` |
| 描边 | `_ToggleGlobalLightOutline` | `_GlobalLightOutlineMixedIntensity` |
| 材质捕获 | `_ToggleGlobalLightMatCapMap` | —（简单开关，无混合强度） |

其中 `描边` 使描边跟随主光颜色，并随投射阴影一起变暗。  

**阴影设置**

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 阴影投射 | `ShadowCaster` Pass 开关（`Material.SetShaderPassEnabled("ShadowCaster", ...)`，无对应材质属性） | 向场景投射阴影。开启透明度裁切时，镂空处不投射阴影 |
| 阴影接收 / 强度 | `_ToggleShadowReceive` / `_FloatShadowIntensity` | 接收场景投射的真实阴影；强度为阴影衰减的偏移量（负值提亮/柔化阴影，正值加深） |
| 交界柔化 / 柔化值 | `_ToggleShadowTerminatorSmooth` / `_FloatShadowTerminatorSmooth` | 处理明暗交界的锯齿，见下方说明 |
| 自阴影偏移 : 深度偏移 | `_FloatSelfShadowDepthBias` | 投射时沿光方向推动 caster，增大可减少自阴影粉刺（Shadow Acne） |
| 自阴影偏移 : 法线偏移 | `_FloatSelfShadowNormalBias` | 投射时沿法线内缩并按 `1-NdotL` 坡度缩放（掠射角最强），增大可压制明暗交界碎裂 |
| 低质量PCF | `_ToggleShadowLowQualityPCF` | 强制用低质量（4-tap）PCF 核重采样主光阴影，减少角色尺度下的阴影透视锯齿 |

`交界柔化` 用于解决明暗交界处暴露阴影图分辨率锯齿的问题：  

| 模式 | 行为 |
| :-- | :-- |
| 关（默认） | 直接使用阴影图（原始行为）。投射阴影覆盖包含交界在内的全部区域，但交界可能出现阴影图分辨率导致的锯齿 |
| 开 | 把 `几何(NdotL)平滑自阴影包络` 与 `阴影图` 取**较暗者(min)** 融合。两条单调曲线取 min 仍单调，因此**绝不产生亮缝**；交界由平滑几何主导（消锯齿），更暗的投射阴影仍能穿透，背光侧照常压暗 |

> `柔化值` 为几何平滑自阴影包络的过渡半宽：越大交界越平滑（几何主导范围越大），越小交界越锐、投射阴影越贴近交界。  
> 依赖主光阴影关键词 `_MAIN_LIGHT_SHADOWS*`（已在 `ForwardLit` 与 `Outline` Pass 中补齐），需在 URP Asset 中启用主光阴影，本体与描边方能接收真实投射阴影。

`自阴影偏移` 与 `低质量PCF` 是叠加在 URP 全局设置之上的**逐材质补偿**，默认值均为「不改变原行为」，只对个别容易出问题的模型单独调整：  

| 项目 | 默认 | 说明 |
| :-- | :-- | :-- |
| 自阴影偏移（深度 / 法线） | `0 / 0` = 不额外偏移 | 在 URP 全局阴影 Bias 之上，于 `ShadowCaster` Pass 内推动顶点。深度偏移过大会漏光（Peter-Panning）；法线偏移按坡度缩放，主要压制交界碎裂。滑条 `[0,1]` 对应约 `0~0.02` 世界单位，落在角色尺度的可用区间 |
| 低质量PCF | `关` = 用 URP 原采样 | Medium/High 的宽核 PCF 会把采样铺展到更大的阴影图区域，在角色尺度透视下反而放大纹素阶梯；低质量核更贴合角色、边缘更干净。仅对**阴影图**路径生效，屏幕空间阴影（`_MAIN_LIGHT_SHADOWS_SCREEN`）与无阴影时回退默认采样 |

> ⚠ `低质量PCF` 仍需在 URP Asset 勾选 `Soft Shadows`——4-tap 的纹素偏移只有开启软阴影时才由管线上传，否则 4 个采样点重合、等同硬阴影，此开关将无可见效果。  
> 实现见 `Core/Shaders/ShadowFunction.hlsl` 的 `MainLightShadowLowQualityPCF`。  

> 以上均是在 URP 级联阴影图这一张图上做补偿，受限于角色在级联图里能分到的纹素数量。若阶梯与边缘跳变仍不可接受，见 [Per Object Shadow 逐对象阴影](#per-object-shadow-逐对象阴影)——它为角色单独分配高分辨率阴影瓦片，从源头提升纹素密度。  

**内置光照（材质专属）**

![](Documents~/Feature_BuiltInLight.gif)

开启后引入材质球自带的光照方向与颜色，使角色在不同场景中保持一致的固定打光。  

注意二者都是**混合**而非直接替换场景光照：光照方向按 `方向混合强度` 在「场景光方向 ↔ 内置光方向」之间插值（默认 0.5 = 各占一半）；光照颜色只有在额外开启 `内置光照颜色开关` 后才参与混合，否则仍完全使用场景光照颜色。要完全脱离场景光照方向，需把 `方向混合强度` 设为 1。  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 内置光照开关 | `_ToggleBuiltInLight` → `_BUILTINLIGHT_ON` | 开启内置光照 |
| 方向 X / Y / Z | `_FloatBuiltInLightAxisX/Y/Z` | 内置光照方向 |
| 方向混合强度 | `_FloatBuiltInLightDirBlend` | 0=场景光方向，1=内置光方向 |
| 内置光照颜色开关 / 颜色 / 混合强度 | `_ToggleBuiltInLightColor` / `_ColorBuiltInLightColor` / `_FloatBuiltInLightColorBlend` | 内置光照专属颜色 |

**光照方向锁定**

将光照高度锁定至水平，使明暗沿水平轴向变化。可分别对以下项目生效：  

| 项目 | 属性 |
| :-- | :-- |
| 基础贴图 | `_ToggleLightHorLockBaseMap` |
| 高光 | `_ToggleLightHorLockHighLight` |
| 边缘光暗部遮罩 | `_ToggleLightHorLockRimLight` |

---

### Per Object Shadow 逐对象阴影
`Renderer Feature + 场景组件，不在材质编辑器面板中`

为角色单独分配一块紧贴其包围盒的高分辨率阴影瓦片，解决 URP 级联阴影图在角色尺度下纹素密度不足的问题。**不对阴影边缘做任何柔化**，NPR 的硬边完整保留。

**要解决什么**

以本工程的配置为例（阴影距离 150、4 级联、图集 4096 → 每级 2048），级联 0 的包围球直径约 13~14 m，摊到 2048 只有约 `6.4 mm/纹素`，一个 1.8 m 高的角色仅占约 280 纹素。由此产生两个问题：

| 现象 | 成因 |
| :-- | :-- |
| NPR 硬边暴露出纹素阶梯 | 角色在 1080p 上占 900 px 高时，一个阴影纹素≈3 个屏幕像素 |
| 光源/角色移动时边缘逐帧跳变 | 级联的纹素栅格随光源方向与相机位置重新量化 |

本功能让角色独占一块瓦片：图集 2048 时，同屏 1 个角色 → 瓦片 2048（贴合 2.2 m 的角色约 `1.1 mm/纹素`），4 个 → 瓦片 1024（约 `2.1 mm/纹素`），密度提升 3~6 倍。配合包围半径量化与纹素吸附，角色移动时边缘也不再爬行。

**启用步骤**

1. 在 URP Renderer 资产（本工程为 `Assets/Settings/URP-HighFidelity-Renderer.asset`）上 `Add Renderer Feature` → `BlurToon Per Object Shadow`；
2. 给角色根节点挂 `BlurToonURP/Per Object Shadow Caster (逐对象阴影投射者)` 组件；
3. 确认主光的 `Shadow Type` 不是 `No Shadows`，且角色材质的 `阴影投射` 保持开启（关闭后该角色不会有自阴影）。

> 组件的 Inspector 会直接显示**登记状态**与当前包围球尺寸。投射者通过静态表向 Feature 登记，一旦登记失败（组件被禁用、正在编辑预制体资产而非场景实例等）整套功能会静默失效且没有任何报错，因此把状态摆在面板上。  

**工作方式**

- 渲染时机为 `AfterRenderingShadows`，位于 URP 主光阴影之后、不透明物体之前，两张阴影图互不干扰。仅对 `Game` 与 `SceneView` 相机生效。
- 瓦片使用**材质自带的 `ShadowCaster` Pass** 渲染，因此 `Lit.shader` 无需新增任何 Pass；材质上关闭 `阴影投射` 的部件同样不会进入瓦片，语义与场景阴影一致。
- 逐个 `cmd.DrawRenderer` 显式提交投射者自己的渲染器，不走 `context.DrawShadows`——后者依赖 Unity 的阴影投射者剔除，实测在某些光源角度下会整块丢弃几何、导致自投影突然消失。
- 代价是**瓦片里只有投射者自身，不含场景遮挡物**。角色收到的场景投影仍来自 URP 级联图，两者由 `Combine Mode` 决定如何合并（见下方[合并方式](#合并方式)）。
- 图集按**本帧实际入选的投射者数量**动态划分为 `1×1 / 2×2 / 4×4`：画面里只有一个角色时它独占整张图集，不必加大图集内存即可翻倍纹素密度。代价是入选数量跨过 1/4/16 分界时瓦片尺寸会变，那一帧阴影质量有一次突变。
- 关键词 `_BLURTOON_PER_OBJECT_SHADOW` 逐帧开关，本帧无投射者时完全关闭，着色器回退到 URP 阴影，零开销；未添加本 Feature 的工程连常量缓冲区都不会声明。

**Feature 参数**（URP Renderer 资产上）

| 分组 | 参数 | 默认 | 说明 |
| :-- | :-- | :-- | :-- |
| 图集 Atlas | `Atlas Size` | `2048` | 阴影图集总分辨率（512 / 1024 / 2048 / 4096） |
| 图集 Atlas | `Max Caster Count` | `4` | 同屏最多处理的投射者数量（1~16）。超出者按优先级丢弃、回退 URP 级联阴影 |
| 阴影方向 Direction | `Direction Mode` | `ViewBlend` | `MainLight`＝严格跟随主光，物理一致但光源旋转时纹素栅格整体旋转；`ViewBlend`＝以「角色→相机」视线为主并按比例混入主光，光源转动时投影基几乎不变，边缘极其稳定 |
| 阴影方向 Direction | `View Blend To Light` | `0.2` | `ViewBlend` 下混入主光方向的比例。0＝完全跟随视线（最稳定但阴影几乎不随光变化），1＝等同 `MainLight` |
| 阴影方向 Direction | `View Blend Pitch Clamp Degrees` | `(90, 150)` | `ViewBlend` 下限制阴影方向与世界上方向的夹角区间（度），避免俯视/仰视时出现不该有的自阴影 |
| 范围 Range | `Max Distance` | `50` | 超过该距离（米）的投射者不再分配瓦片 |
| 范围 Range | `Fade Range` | `5` | 在 `Max Distance` 之前多少米开始淡出回 URP 级联阴影，避免硬切换 |
| 范围 Range | `Caster Extrusion` | `2` | 正交视锥朝光源方向额外拉长的距离（米），用于加大近平面余量。角色带长武器、披风等超出包围球的部件时可调大 |
| 范围 Range | `Combine Mode` | `SceneAndSelf` | 与 URP 主光阴影的合并方式，见下方[合并方式](#合并方式) |
| 范围 Range | `Self Reject Scale` | `1` | 仅 `SceneAndSelf`：自剔除距离倍率，见下方[合并方式](#合并方式) |
| 偏移 Bias | `Depth Bias` / `Slope Bias` | `1.0` / `2.5` | 硬件光栅化深度偏移（常量项 / 坡度项）。用光栅化偏移而非顶点位移，因此不会随光源方向形变 caster 轮廓，阴影边界不会在表面上滑动 |
| 偏移 Bias | `Normal Bias` | `0.5` | 投射端法线偏移，单位为**瓦片纹素**。沿法线内缩 caster，压制明暗交界处的自阴影碎裂。瓦片分辨率高，所需值远小于级联阴影 |
| 偏移 Bias | `Receiver Normal Offset` | `1.5` | 接收端法线偏移，单位为**瓦片纹素**。掠射角下一个纹素在表面上覆盖极长一段，投射端 bias 无论怎么调都跟不上，整片表面会同时翻成自遮挡（即「光源转到与表面平行时突然全黑」）；接收端按纹素世界尺寸补偿是这种配置下唯一有效的手段。过大会让贴近表面的细小阴影漏光 |
| 表现 Appearance | `Strength` | `1` | 阴影强度。1＝完全遮挡，0＝不产生阴影 |
| 表现 Appearance | `Stabilize Texel Snapping` | `开` | 把投影矩阵吸附到纹素栅格，角色移动时边缘按整纹素跳变而不是连续爬行。需配合投射者上的 `Radius Quantize` 一起使用 |
| 调试 Debug | `Debug Mode` | `Off` | 把瓦片内容直接画到角色上，见下方 [Debug Mode 调试模式](#debug-mode-调试模式)。**发布前记得关闭** |

**投射者组件参数**（角色根节点上）

| 参数 | 默认 | 说明 |
| :-- | :-- | :-- |
| `Renderers` | 空 | 参与包围盒计算的渲染器。留空则在启用时自动收集子层级下的全部渲染器。运行时换装等增删部件后需手动调用 `RefreshRenderers()` |
| `Bounds Padding` | `0.1` | 包围盒外扩（米）。避免动作幅度大时肢体擦出瓦片边界导致阴影被裁掉 |
| `Radius Quantize` | `0.25` | 包围半径的量化步长（米）。骨骼动画会让包围盒逐帧变化，若正交投影尺寸跟着变，纹素大小就逐帧改变、纹素吸附完全失效。向上取整到该步长可锁定投影尺寸，是消除运动时边缘爬行的关键。设为 0 表示不量化（不推荐） |
| `Priority Bias` | `0` | 同屏投射者超过上限时，按「距离相机近 + 处于画面中心」排序取前 N 个，此值直接加到排序权重上（越小越优先）。主角可设为负值以确保永远拿得到瓦片 |

> 包围球半径取 AABB 对角线的一半，保证任意光照方向下正交投影都能完整罩住对象，不必随光源方向重新拟合（重新拟合会让投影尺寸随光源变化，正是要避免的抖动来源）。选中对象时 Scene 视图会绘制该包围球线框。  

#### 合并方式
瓦片只包含投射者自身，因此 `Combine Mode` 直接决定角色能否收到场景投影：

| 模式 | 行为 | 代价 |
| :-- | :-- | :-- |
| `Min` 取较暗者 | 角色同时收到 URP 场景投影与瓦片的高密度自阴影 | URP 级联里那份低分辨率的角色自阴影仍然存在，阶梯不会完全消失 |
| `Replace` 命中即替换 | 命中瓦片时完全用瓦片结果替换，自阴影边缘最干净 | **角色收不到场景投射到它身上的阴影**，仅适合无场景遮挡的演出镜头（立绘、换装界面） |
| `SceneAndSelf`（默认，推荐） | 场景投影 + 高清自阴影，两者兼得 | 需要阴影图路径，见下方说明 |

`Min` 与 `Replace` 的两难来自同一件事：**角色同时存在于两张阴影图里**。`SceneAndSelf` 在采样级联图前，把采样点沿主光方向推离本体一段距离（自剔除偏移），使角色自身写入级联图的那部分深度不再能遮挡自己——级联图于是只剩下场景遮挡物的贡献，再与瓦片的自阴影取 min，两份阴影各司其职、互不污染。

沿光方向平移不改变光空间的 xy，采样的仍是同一个纹素，因此阴影图案**不会有任何横向位移**，只是深度比较的基准前移了。

自剔除的基准距离由着色器**逐像素**算出：从该像素沿主光方向走多远才能离开角色的包围球（射线—球求交），朝光面接近 0、背光面接近包围球直径。`Self Reject Scale` 是它的倍率：

| 取值 | 效果 |
| :-- | :-- |
| `1`（默认） | 刚好剔到包围球边界 |
| 调小 | URP 那份低分辨率自阴影重新渗出 |
| 调大 | 连贴近角色的场景遮挡物也一起剔掉，导致近处物体投不到角色身上 |
| `0` | 完全关闭自剔除，等同 `Min` |

> ⚠ `SceneAndSelf` 需要按世界位置重采样级联图，**屏幕空间阴影**（URP Asset 的 `Screen Space Shadows`，关键词 `_MAIN_LIGHT_SHADOWS_SCREEN`）下无法做到，此模式自动失效并回退 `Min`。  
> 重采样时会尊重材质上的 `低质量PCF` 开关，与 `ForwardLit` 的主光阴影采样保持一致。  

#### Debug Mode 调试模式
排查「阴影从哪来」时直接看瓦片本身，比在最终画面里猜可靠得多。

| 模式 | 显示内容 | 怎么读 |
| :-- | :-- | :-- |
| `Off` | 关闭 | — |
| `ShadowValue` | 阴影值灰度 | 白＝无遮挡，黑＝完全遮挡。出现细碎噪点/条纹即为自阴影粉刺（bias 不足） |
| `TileCoverage` | 瓦片覆盖 | 绿＝像素落在瓦片内，红＝落在瓦片外（回退 URP 阴影）。角色身上出现红色说明瓦片没罩住 |
| `TileUV` | 瓦片 UV | 红绿渐变应平滑铺满角色。某个光源角度下突然整体偏移/翻转即为矩阵构造有跳变 |
| `AtlasDepth` | 图集原始深度 | 反向 Z 下 1＝近 0＝远，清空值为远(0)。角色所在处一片纯黑 → 几何根本没进图集（剔除问题）；能看到深度渐变 → 瓦片有内容，问题在采样端 |
| `ReceiverDepth` | 接收端深度 | 该像素在瓦片里的 z，应平滑铺满角色且落在 (0,1) 开区间内。大片贴死在 0 或 1 → 深度范围没罩住角色 |
| `TexelDensity` | 纹素密度棋盘 | 一格＝一个阴影纹素。**判断「边缘锯齿还有没有救」的客观依据**：格子小于一个屏幕像素说明台阶是像素级的，后处理抗锯齿能解决；格子明显大于一个屏幕像素说明台阶比像素还大，只能提高 `Atlas Size`、减少同屏投射者数量或收小包围球 |

**源码位置**

| 文件 | 作用 |
| :-- | :-- |
| `Core/Runtime/PerObjectShadow/BlurToonPerObjectShadowFeature.cs` | Renderer Feature 入口 |
| `Core/Runtime/PerObjectShadow/BlurToonPerObjectShadowPass.cs` | 渲染 Pass：筛选、分块、绘制、上传常量 |
| `Core/Runtime/PerObjectShadow/BlurToonPerObjectShadowSettings.cs` | 可序列化配置与枚举 |
| `Core/Runtime/PerObjectShadow/BlurToonPerObjectShadowUtils.cs` | 矩阵构造、纹素吸附、优先级排序 |
| `Core/Runtime/PerObjectShadow/BlurToonPerObjectShadowCaster.cs` | 投射者组件 |
| `Core/Editor/BlurToonPerObjectShadowCasterEditor.cs` | 投射者检视面板（登记状态显示） |
| `Core/Shaders/PerObjectShadowFunction.hlsl` | 着色器端采样函数库 |

---

### 渲染 Pass 说明
除 `ForwardLit`（基础光照）与 `Outline`（外描边）外，还包含以下 Pass，保证物体在完整 URP 渲染流程中正常工作：

| Pass | LightMode | 作用 |
| :-- | :-- | :-- |
| `ShadowCaster` | `ShadowCaster` | 向场景投射阴影（由「阴影设置 → 阴影投射」开关控制）。支持点光/聚光的逐顶点光照方向与透明度裁切 |
| `DepthOnly` | `DepthOnly` | 写入相机深度图 `_CameraDepthTexture`，供深度雾、软粒子、屏幕空间描边等使用 |
| `DepthNormals` | `DepthNormals` | 写入相机法线图 `_CameraNormalsTexture`，供 SSAO 等依赖法线的后处理使用 |

各 Pass 的渲染状态一览：

| Pass | Cull | 透明度裁切 | 裁剪 Clip | 模板测试 |
| :-- | :-- | :--: | :--: | :--: |
| `ForwardLit` | `[_IntRenderFaceType]` | ✅ | ✅ | ✅ |
| `Outline` | `Front`（固定） | ✅ | ✅（硬剔除） | ✅ |
| `ShadowCaster` | `[_IntRenderFaceType]` | ✅ | ❌ | ❌ |
| `DepthOnly` | `[_IntRenderFaceType]` | ✅ | ❌ | ❌ |
| `DepthNormals` | `[_IntRenderFaceType]` | ✅ | ❌ | ❌ |

> 三个附加 Pass 均支持透明度裁切（`_ALPHATEST_ON`），使镂空物体的阴影与深度/法线轮廓与本体一致。  
> `ShadowCaster` Pass 同时被 [Per Object Shadow 逐对象阴影](#per-object-shadow-逐对象阴影) 用于渲染逐对象阴影图集，因此该功能不需要为 `Lit.shader` 新增任何 Pass。  

---

### Debug 调试
`【调试 Debug】仅编辑器用，不影响正式流程`

位于 Inspector **最顶部**的调试功能区。开启 `高亮定位（Scene）` 后，Scene 视图中所有使用当前材质的物体会被叠加为**品红色实心**（可穿透遮挡）并绘制包围盒线框与名称标签，便于在复杂场景中快速定位该材质被谁使用。  

- 精确到**子网格**：同一物体上不同材质槽只高亮真正使用当前材质的部分（如面部皮肤 vs 眼睛）。
- 支持 `SkinnedMeshRenderer`（按当前姿势烘焙网格）。
- 纯编辑器功能：不修改材质/Shader/渲染管线，仅在 Scene 视图叠加绘制，不影响 Game 视图与正式运行流程。
- 开关为编辑器会话级状态，不写入材质；脚本重编译/域重载后自动关闭。

源码：`Assets/PluginsDeveloper/BlurToonURP/Core/Editor/MaterialDebugHighlight.cs`

---

### Tools 工具
| 脚本 | 说明 |
| :-- | :-- |
| `LightController.cs` | 挂载于光源上，可让光源按设定的欧拉角速度自动旋转（含延迟启动、持续时间），常用于效果演示与录制。 |

---