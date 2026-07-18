# BlurToonURP

## 概要
这是使用 `Unity 2022.3 LTS(URP 14.x)` 的 `URP` 渲染管线开发的 `NPR` 二次元卡通渲染 Shader 工程。  
实现了通用的二次元卡通渲染效果。  
使用 MIT 许可证，你可以随意地使用这个项目。  

![](Documents~/EditorDemo.gif)

## 运行环境
| 项目 | 版本 |
| :-- | :-- |
| Unity | `2022.3.62f3 LTS` |
| URP (Universal RP) | `14.0.12`（`manifest.json` 声明 12.1.12，Unity 2022.3 实际解析为 14.0.12） |
| 渲染管线 | Universal Render Pipeline |
| 许可证 | MIT |

## 快速开始
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
- 编辑器界面：`Assets/PluginsDeveloper/BlurToonURP/Core/Editor/ShaderGUILit.cs`
- 工具脚本：`Assets/PluginsDeveloper/BlurToonURP/Core/Tools/LightController.cs`

![](Documents~/Feature_Inspector.png)

## 功能
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

以基础贴图为主色，结合 `半兰伯特(Half-Lambert)` 光照计算出双层暗部（暗部1、暗部2），并通过色阶阈值与羽化模糊构建出卡通风格的明暗分层。  

![](Documents~/Feature_BaseMap.png)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 基础贴图 | `_BaseMap` | 主纹理（sRGB） |
| 基础色 | `_BaseColor` | 与贴图相乘的自定义颜色（HDR） |
| 混合颜色 / 混合强度 | `_BaseMapBlendColor` / `_BaseMapBlendColorIntensity` | 在基础色上叠加混合一个自定义颜色 |
| 法线贴图 | `_ToggleNormalMapOnBaseMap` | 基础贴图的明暗计算使用法线贴图（需先在【法线贴图】面板指定贴图） |
| 暗部1颜色 | `_Shade1Color` | 第一层暗部颜色 |
| 暗部2颜色 | `_Shade2Color` | 第二层暗部颜色 |

#### Bright Shade Step 阴影色阶
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
通过一张阈值贴图（采样 R 通道）控制 `暗部1` 的分布与强度，可用于在固定区域（如脸部、褶皱）手绘阴影的形状。  
![](Documents~/Feature_ShadeThresholdMap.png)

![](Documents~/Feature_ShadeThresholdMap.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleShadeThresholdMap` → `_BASEMAP_SHADE_THRESHOLDMAP_ON` | 开启阈值贴图 |
| 暗部阈值贴图 | `_TexShadeThresholdMap` | 阈值贴图（linear） |
| 强度 | `_FloatShadeThresholdMapIntensity` | 阈值影响强度 |

---

### Normal Map 法线贴图
`【法线贴图 NormalMap】强度、效果开关`

采样切线空间法线贴图并转换到世界空间，供各效果按需使用。  

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 法线贴图 | `_BumpMap` | 切线空间法线贴图 |
| 强度 | `_BumpScale` | 法线强度 |

> **法线的启用开关采用面向对象设计，分散在各效果面板内**，本面板只负责指定法线贴图本身：  
> - 【基础贴图】→ `法线贴图`（`_ToggleNormalMapOnBaseMap`）  
> - 【高光】→ `法线贴图`（`_ToggleNormalMapOnHighLight`）  
> - 【材质捕获】→ `法线贴图`（`_ToggleNormalMapOnMatCap`）  
> - 【自发光】→ `自发光动画 → 视角变化颜色 → 法线贴图`（`_ToggleNormalMapOnEmissive`）  
> - 【边缘光】→ `法线来源`（`_FloatRimLightNormalSource`，几何法线 / 法线贴图 / 混合）  
>
> 未指定法线贴图却开启了上述任一开关时，编辑器会在该开关下方**红字提示不生效**；本面板未指定贴图时也会给出总提示。  

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

基于 `法线与视线夹角(NdotV)` 计算边缘光，支持强度、内部延伸距离与硬边缘控制。  

![](Documents~/Feature_RimLight.gif)

| 参数 | 属性 | 说明 |
| :-- | :-- | :-- |
| 主开关 | `_ToggleRimLight` → `_RIMLIGHT_ON` | 开启边缘光 |
| 颜色 | `_ColorRimLightColor` | 边缘光颜色（HDR，Alpha 控制不透明度） |
| 强度 | `_FloatRimLightIntensity` | 边缘光范围/强度 |
| 内部距离 | `_FloatRimLightInsideDistance` | 边缘光向内延伸距离，越大范围越窄 |
| 硬边缘 | `_ToggleRimLightHard` | 边缘硬化（去除渐变） |
| 法线来源 / 混合强度 | `_FloatRimLightNormalSource` / `_FloatRimLightNormalMapBlend` | 边缘光法线来源：`几何法线` / `法线贴图` / `混合`（仅混合档用到混合强度） |

> `法线来源` 选择 `法线贴图` 或 `混合` 但未指定法线贴图时，编辑器会红字提示不生效。  
> 该选择使用 `lerp + step` 构建无分支选择器（与「描边类型」同款写法），不额外产生关键词变体。  

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

`交界柔化` 用于解决明暗交界处暴露阴影图分辨率锯齿的问题：  

| 模式 | 行为 |
| :-- | :-- |
| 关（默认） | 直接使用阴影图（原始行为）。投射阴影覆盖包含交界在内的全部区域，但交界可能出现阴影图分辨率导致的锯齿 |
| 开 | 把 `几何(NdotL)平滑自阴影包络` 与 `阴影图` 取**较暗者(min)** 融合。两条单调曲线取 min 仍单调，因此**绝不产生亮缝**；交界由平滑几何主导（消锯齿），更暗的投射阴影仍能穿透，背光侧照常压暗 |

> `柔化值` 为几何平滑自阴影包络的过渡半宽：越大交界越平滑（几何主导范围越大），越小交界越锐、投射阴影越贴近交界。  
> 依赖主光阴影关键词 `_MAIN_LIGHT_SHADOWS*`（已在 `ForwardLit` 与 `Outline` Pass 中补齐），需在 URP Asset 中启用主光阴影，本体与描边方能接收真实投射阴影。

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