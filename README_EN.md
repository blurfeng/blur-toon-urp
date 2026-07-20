![](Documents~/EditorDemo.gif)

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
  <a href="./README.md">中文</a> |
  English |
  <a href="./README_JA.md">日本語</a>
</p>

<p align="center">
  📥
  <a href="#-quick-start">Quick Start</a> |
  <a href="#-features">Features</a>
</p>

# BlurToonURP - Anime-Style Toon Rendering Shader
This is an `NPR` anime-style toon rendering Shader project developed with the `URP` render pipeline on `Unity 2022.3 LTS (URP 14.x)`.  
It implements a general-purpose anime toon rendering look, covering a complete feature set: basic settings, shade steps, highlights, outlines, rim light, emissive, MatCap and light settings.  
It uses the MIT license, so you are free to use this project however you like.  

## 📜 Table of Contents
- [💻 Requirements](#-requirements)
- [🌱 Quick Start](#-quick-start)
- [✨ Features](#-features)
  - [Basic](#basic)
    - [RenderQueue](#renderqueue)
    - [Clip (Dissolve)](#clip-dissolve)
    - [Stencil](#stencil)
  - [Base Map](#base-map)
    - [Diffuse Type](#diffuse-type)
    - [Bright Shade Step](#bright-shade-step)
    - [Shade Threshold Map](#shade-threshold-map)
  - [Normal Map](#normal-map)
  - [HighLight](#highlight)
    - [Mask Map](#mask-map)
  - [Outline](#outline)
  - [Rim Light](#rim-light)
    - [Rim Light Type](#rim-light-type)
    - [Shade Mask](#shade-mask)
    - [Mask Map](#mask-map-1)
  - [Emissive](#emissive)
    - [Emissive Animation](#emissive-animation)
  - [MatCap](#matcap)
    - [Mask Map](#mask-map-2)
  - [Light Setting](#light-setting)
  - [Render Pass Overview](#render-pass-overview)
  - [Debug](#debug)
  - [Tools](#tools)

## 💻 Requirements
| Item | Version |
| :-- | :-- |
| Unity | `2022.3.62f3 LTS` |
| URP (Universal RP) | `14.0.12` (`manifest.json` declares 12.1.12; Unity 2022.3 actually resolves it to 14.0.12) |
| Render Pipeline | Universal Render Pipeline |
| License | MIT |

## 🌱 Quick Start
1. Open the project with `Unity 2022.3 LTS` and make sure the `URP` render pipeline is enabled.  
2. Create a new material and switch its Shader to `BlurToonURP/Lit`.  
3. In the material's `Inspector` panel, tune each effect through the accompanying editor UI foldouts.  
4. Reference example assets:
   - Example material: `Assets/PluginsDeveloper/BlurToonURP/Example/Materials/Lit.mat`
   - Example prefab: `Assets/PluginsDeveloper/BlurToonURP/Example/Prefabs/Sphere.prefab`

Shader and editor source locations:  
- Shader: `Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/Lit.shader`
- Material property buffer: `Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/LitInput.hlsl`
- General function library: `Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/BlurFunction.hlsl`
- Shadow function library: `Assets/PluginsDeveloper/BlurToonURP/Core/Shaders/ShadowFunction.hlsl`
- Editor UI: `Assets/PluginsDeveloper/BlurToonURP/Core/Editor/ShaderGUILit.cs`
- Tool scripts: `Assets/PluginsDeveloper/BlurToonURP/Core/Tools/LightController.cs`

![](Documents~/Feature_Inspector.png)

## ✨ Features
---
The feature code is written with `clean code` and `high performance` in mind. Thorough `code comments` let you read the Shader files directly and quickly understand the whole code workflow.  
A matching `Inspector editor UI` was also developed so creators can adjust effects more conveniently.  
Each feature heading below maps one-to-one to a foldout panel in the material editor UI.  

> ⚠ The in-editor inspector UI is currently Chinese-only. Labels in the tables below are translations; the **Property** column identifies the exact field.

> The render pipeline contains 5 Passes: `ForwardLit` (base lighting render), `ShadowCaster` (shadow casting), `DepthOnly` (depth), `DepthNormals` (depth normals) and `Outline` (outline).

> All material properties are declared centrally in the `UnityPerMaterial` constant buffer in `LitInput.hlsl`, which every Pass `include`s uniformly, guaranteeing byte-for-byte identical layout across Passes for `SRP Batcher` compatibility.

---

### Basic
`【基础设置 Basic】表面类型、渲染面、透明度裁切、裁剪、模板测试`

Manages the material's basic render states in one place. When you switch `Surface Type`, the editor automatically sets the corresponding blend factors, depth write, render queue and `RenderType` tag.  
Alpha clipping runs through all Passes — `ForwardLit / Outline / ShadowCaster / DepthOnly / DepthNormals` — so cut-out silhouettes stay consistent with the body in **cast shadows, the depth texture and SSAO normals**.  

![](Documents~/Feature_Basic.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Surface Type | `_Surface` (→ automatically sets `_SrcBlend` / `_DstBlend` / `_ZWrite` / render queue / `RenderType`) | `Opaque` (`One/Zero`, depth write) / `Transparent` (standard alpha blending, depth write off, Transparent queue) |
| Render Face | `_IntRenderFaceType` (→ `Cull [_IntRenderFaceType]`) | `Both` double-sided (Cull Off) / `Back` back faces (Cull Front) / `Front` front faces (Cull Back, default) |
| Alpha Clipping | `_ToggleAlphaClip` → `_ALPHATEST_ON` | When enabled, pixels are clipped by `base map alpha × base color alpha` (Cutout) |
| Clip Threshold | `_Cutoff` | Pixels with alpha below this value are discarded |

> Note: transparency only provides **standard alpha blending** (no Premultiply/Additive/Multiply modes).  
> `Render Face` applies to `ForwardLit / ShadowCaster / DepthOnly / DepthNormals`; the `Outline` Pass is fixed to `Cull Front` (rendering back faces is the outline principle itself) and is unaffected by this option.  
> The outline Pass keeps its own independent blending (`SrcAlpha OneMinusSrcAlpha`) and does not follow the surface type switch, so the outline stays reliably visible.  

#### RenderQueue
When `Auto Render Queue` is enabled, the editor derives the queue from `Surface Type / Alpha Clipping / Stencil Type`; when disabled you can set it manually (in multi-selection editing it applies to all selected materials).  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Auto Render Queue | `_ToggleRenderQueueAuto` | On (default) = auto derivation; Off = keeps the manually set queue, the editor no longer overrides it |
| Render Queue | `Material.renderQueue` | Editable only when auto is disabled |

Auto derivation rules:

| Condition | Queue |
| :-- | :-- |
| Surface Type = `Transparent` | `Transparent` (3000) |
| Stencil Type = `Reserve` | `AlphaTest - 1` (2449, rendered before `Discard` so it can write the mask) |
| Stencil Type = `Discard` | `AlphaTest` (2450) |
| Alpha Clipping enabled | `AlphaTest` (2450) |
| Everything else (opaque) | `Geometry` (2000) |

#### Clip (Dissolve)
Samples an intensity from the `R` channel of a clip mask texture to achieve hole-punch culling or alpha dissolve fade-out. It is **fully independent** of `Alpha Clipping` above and both can be used at the same time.  

![](Documents~/Feature_Clip.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Clip Type | `_IntClipType` → `_CLIP_DITHER` / `_CLIP_ALPHA` (no keyword = off) | `Off` / `Dither` hole punch / `Alpha` transparency |
| Clip Map | `_TexClipMaskMap` | Samples the R channel as clip intensity (0-1), with its own independent UV scale and offset |
| Clip Intensity | `_FloatClipIntensity` | `Dither` only: pixels below this value are culled |
| Transparency Intensity | `_FloatClipTransIntensity` | `Alpha` only: range `-1~1`, larger means more dissolve |
| Base Map Alpha Channel Applies | `_ToggleClipTransBaseMapAlpha` | `Alpha` only: multiplies the base map alpha into the clip calculation |

> The clip value in `Alpha` mode is written to the final alpha (`alphaFinal = saturate(clipAlpha)`), so you must set `Surface Type` to `Transparent` to see the transparent fade-out; otherwise it only behaves as a hard cull.  
> Clipping is active in the `ForwardLit` and `Outline` Passes; `ShadowCaster / DepthOnly / DepthNormals` **do not participate in clipping**, meaning dissolved areas still cast shadows and still write depth.  
> Clipping in the `Outline` Pass is a **hard cull** (no transparent fade), and in `Alpha` mode the outline only uses `mask - transparency intensity` for the test, without multiplying in the base map alpha.  

#### Stencil
Achieves masking through the stencil buffer; **only materials with the same `Stencil Group Number` affect each other**. Typical uses: keeping the body from showing through clothes, letting eyes show through hair, and so on.  

![](Documents~/Feature_Stencil.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Stencil Type | `_IntStencilType` (→ editor presets Comp/Pass/Fail) | `Off` / `Discard` (not drawn where the same group's mask is) / `Reserve` (writes the mask, and renders before Discard) |
| Stencil Group Number | `_FloatStencilNum` (`Ref`) | Only identical numbers affect each other (0-255) |
| Comparison / Pass Write / Fail Write | `_FloatStencilComp` / `_FloatStencilPass` / `_FloatStencilFail` | Automatically preset by `Stencil Type`, no manual setup needed |

Preset mapping (values correspond to `UnityEngine.Rendering.CompareFunction` / `StencilOp`):

| Stencil Type | Comp | Pass | Fail |
| :-- | :-- | :-- | :-- |
| `Off` | `Disabled`(0) | `Keep`(0) | `Keep`(0) |
| `Discard` | `NotEqual`(6) | `Keep`(0) | `Keep`(0) |
| `Reserve` | `Always`(8) | `Replace`(2) | `Replace`(2) |

> Stencil testing applies to the `ForwardLit` and `Outline` Passes, so the outline is masked/written along with the body.  

---

### Base Map
`【基础贴图 BaseMap】基础贴图及暗部贴图`

Uses the base map as the main color and combines it with `Half-Lambert` lighting to compute the shade, building the toon-style light/dark layering.  
There are two **transition generation methods** for the shade, chosen between via `Diffuse Type`: `Steps` (procedural two-level shade) and `Ramp Texture` (soft transition via a 1D gradient). Both produce the same final color and shade factor shared downstream — they are two implementations of the same diffuse stage, not two parallel independent features.  

![](Documents~/Feature_BaseMap.png)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Base Map | `_BaseMap` | Main texture (sRGB) |
| Base Color | `_BaseColor` | Custom color multiplied with the texture (HDR) |
| Blend Color / Blend Intensity | `_BaseMapBlendColor` / `_BaseMapBlendColorIntensity` | Overlays and blends a custom color onto the base color |
| Normal Map | `_ToggleNormalMapOnBaseMap` | The base map's light/dark calculation uses the normal map (you must first assign a texture in the [Normal Map](#normal-map) panel) |
| Diffuse Type | `_FloatDiffuseType` → `_BASEMAP_DIFFUSE_RAMP_ON` | `Steps` (0, default) / `Ramp Texture` (1), see [Diffuse Type](#diffuse-type) below |
| Shade 1 Color | `_Shade1Color` | First shade layer color (`Steps` method only) |
| Shade 2 Color | `_Shade2Color` | Second shade layer color (`Steps` method only) |

#### Diffuse Type
Determines how the "Half-Lambert value → light/dark color" step is generated. After switching, the editor only shows the parameters of the selected method; the other set is still kept in the material and is not lost.  

| Method | Keyword | Principle | Suited for |
| :-- | :-- | :-- | :-- |
| `Steps` (default) | no keyword | Procedural two-level steps: transitions between the three colors (bright, shade 1, shade 2) according to `Step / Blur` | Toon shadows with clearly separated layers; purely parameter-driven, no extra texture needed |
| `Ramp Texture` | `_BASEMAP_DIFFUSE_RAMP_ON` | Uses the Half-Lambert value as the horizontal UV to sample a 1D gradient texture (left = dark, right = bright); the result is multiplied directly onto the base color | Transition hardness and banding are entirely painted into the texture by the artist, offering more freedom |

Parameters exclusive to the `Ramp Texture` method:  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Ramp Gradient Map | `_TexDiffuseRamp` | Horizontal gradient (left = shade, right = bright). **Defaults to white = no darkening** (safe fallback when no texture is assigned) |
| Row Selection (V) | `_FloatDiffuseRampV` | Selects the row when using a multi-row gradient atlas; use `0.5` for a single-row texture |

> Ramp sampling is forced to `Clamp / Linear / LOD0` inside the Shader, so there is no need to change the texture import Wrap/Filter settings; disabling Mipmaps is recommended. LOD0 is forced to avoid picking a blurry mip when the screen-space derivative of the Half-Lambert value is too large at the light/dark boundary.  
> Both methods are affected by `Shadow Receive` and the `Shade Threshold Map` — they act on the Half-Lambert value, so shadows push the Ramp's sampling coordinate toward the dark end of the gradient.  
> The `Ramp Texture` method does not use the `Shade 1/2 Color` or `Bright Shade Step` parameters; the shade factor required downstream (for the "shadow mask" of HighLight / MatCap) is instead derived back from the Rec709 luminance of the sampled Ramp color, sharing the same chain as the Steps method.  

#### Bright Shade Step
> Only effective with the `Steps` method.  

Based on the Half-Lambert value, controls the boundary position and feather blur of the two transitions `bright → shade 1` and `shade 1 → shade 2`, allowing hard-edged toon shadows or soft gradient shadows.  

![](Documents~/Feature_ShadeStep.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Bright→Shade1 : Step | `_FloatBrightShade1Step` | First-level shadow boundary position |
| Bright→Shade1 : Blur | `_FloatBrightShade1Blur` | First-level shadow edge feathering |
| Shade1→Shade2 : Step | `_FloatShade1Shade2Step` | Second-level shadow boundary position |
| Shade1→Shade2 : Blur | `_FloatShade1Shade2Blur` | Second-level shadow edge feathering |

> The step transition band has **screen-space anti-aliasing**: `fwidth(halfLambert)` is used as the minimum width of the transition band, guaranteeing the transition covers at least about 1 pixel. This prevents collapse into hard aliased edges where the Half-Lambert gradient is steep — sphere silhouettes, grazing angles, low-poly face boundaries. When the artist sets a larger blur, that value is used as-is and the look does not change.  

#### Shade Threshold Map
> Shared by both the `Steps` and `Ramp Texture` methods.  

Uses a threshold texture (sampling the R channel) to control the distribution and intensity of the shade, useful for hand-painting shadow shapes in fixed areas such as the face or cloth folds.  
![](Documents~/Feature_ShadeThresholdMap.png)

![](Documents~/Feature_ShadeThresholdMap.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_ToggleShadeThresholdMap` → `_BASEMAP_SHADE_THRESHOLDMAP_ON` | Enables the threshold map |
| Shade Threshold Map | `_TexShadeThresholdMap` | Threshold texture (linear) |
| Intensity | `_FloatShadeThresholdMapIntensity` | Threshold influence intensity |

---

### Normal Map
`【法线贴图 NormalMap】贴图、强度、各效果生效状态`

Samples a tangent-space normal map and converts it to world space for each effect to use as needed.  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Normal Map | `_BumpMap` | Tangent-space normal map (with scale and offset) |
| Intensity | `_BumpScale` | Normal intensity |

**The normal map enable toggles follow an object-oriented design and are distributed across the individual effect panels.** This panel only assigns the normal map itself and summarizes the effective state of each effect:  

| Effect | Toggle Location | Property |
| :-- | :-- | :-- |
| Base Map | Base Map → `Normal Map` | `_ToggleNormalMapOnBaseMap` |
| HighLight | HighLight → `Normal Map` | `_ToggleNormalMapOnHighLight` |
| MatCap | MatCap → `Normal Map` | `_ToggleNormalMapOnMatCap` |
| Emissive | Emissive → `Emissive Animation → View Change Color → Normal Map` | `_ToggleNormalMapOnEmissive` |
| Rim Light | Rim Light → `Normal Source` (Geometry Normal / Normal Map / Blend) | `_FloatRimLightNormalSource` |

At the bottom of this panel each of the above items is listed **read-only** with its current effective state, so you can tell at a glance whether "I turned the normal toggle on but nothing changed" is caused by a missing texture, an intensity of 0, or the effect's own prerequisite toggle being off.  

> When any of the above toggles is enabled without a normal map assigned, the editor shows a **red warning beneath that toggle saying it has no effect**.  
> This panel also shows an overall warning when no texture is assigned, or when `Intensity` is 0 (the sampling result equals the flat normal).  

---

### HighLight
`【高光 HighLight】高光颜色、大小、遮罩`

Computes toon-style specular highlights based on the `angle between the normal and the half vector (NdotH)`, supporting a highlight texture, color, intensity, size (range) and edge feathering, and it can follow the light color and be masked by shadows.  

![](Documents~/Feature_HighLight.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_ToggleHighLight` → `_HIGHLIGHT_ON` | Enables the highlight |
| HighLight Map | `_TexHighLightMap` | Highlight base color = texture RGB (sRGB) × color. Defaults to white = plain-color highlight; a texture can be used for colored/patterned highlights |
| Color | `_ColorHighLightColor` | Highlight color (HDR, alpha contributes to intensity) |
| Intensity | `_FloatHighLightIntensity` | Highlight intensity |
| Size | `_FloatHighLightSize` | Highlight range. `[0,1]` maps to a reflection power of `[512,4]`; larger values give a bigger highlight |
| Edge Blur | `_FloatHighLightBlur` | Highlight edge hardness. `0` ≈ hard stepped edge; larger is softer, continuously covering "stepped ↔ soft edge" |
| Normal Map | `_ToggleNormalMapOnHighLight` | The highlight orientation uses the normal map |
| Shadow Mask / Intensity | `_ToggleHighLightShadowMask` / `_FloatHighLightShadowMaskIntensity` | Darkens the highlight in the shade 1 region, avoiding unnatural highlights inside shadows |

> When `Shadow Receive` is enabled, the highlight factor is multiplied by the shadow attenuation, meaning **no highlight appears in real cast shadows**.  
> Whether the highlight is `affected by light color` is controlled by `Light Setting → Light Toggles → HighLight` (`_ToggleGlobalLightHighLight`);  
> `Light Setting → Light Direction Lock → HighLight` (`_ToggleLightHorLockHighLight`) can lock the highlight's light direction to horizontal.  

#### Mask Map
Uses a mask texture sharing the base map's UV (sampling the R channel) to control the highlight's distribution and intensity per pixel.  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Mask Map | `_TexHighLightMaskMap` → `_HIGHLIGHT_MASKMAP_ON` | Highlight mask texture (the keyword is enabled automatically when a texture is present) |
| Mask Intensity | `_FloatHighLightMaskMapIntensity` | Mask influence intensity |

---

### Outline
`【外描边 Outline】粗细、颜色`

A dedicated `Outline` Pass (`LightMode = SRPDefaultUnlit`) that uses `Cull Front` to **cull front faces (i.e. render only back faces)** and expands vertices outward along the normal to create the outline. Multiple outline direction sources and width modes are supported.  

![](Documents~/Feature_Outline.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_OUTLINE_ON` (Pass toggle) | Enables the outline |
| Outline Type | `_FloatOutlineType` | Outline direction source: `VertexNormal` / `VertexColor` / `VertexTangent` |
| Width Type | `_FloatOutlineWidthType` → `_OUTLINE_WIDTH_SAME` / `_OUTLINE_WIDTH_SCALING` | `Same` uniform width (all vertices expand by the same distance) / `Scaling` varying width (scaled by `dot(vertex direction, expansion direction) + 0.3`, thicker on convex areas and thinner in concave ones; it is a fixed per-vertex value and is **view-independent**) |
| Color | `_ColorOutlineColor` | Outline color |
| Width | `_FloatOutlineWidth` | Outline width |
| Base Map Blend / Intensity | `_ToggleOutlineBaseMapBlend` / `_FloatOutlineBaseMapBlendIntensity` | Blends the outline color with the base map color to make the outline look more natural |
| Outline Texture / Intensity | `_TexOutlineMap` → `_OUTLINE_MAP_ON` / `_FloatOutlineMapIntensity` | Modulates the outline color with a dedicated outline texture (colored outlines/patterns/noise); assigning a texture enables it automatically |

> Whether the outline is `affected by light and shadow` is controlled by `Light Setting → Light Toggles → Outline` (`_ToggleGlobalLightOutline` / `_GlobalLightOutlineMixedIntensity`); the shadow part reuses the `Shadow Receive` toggle.  
> The outline also supports `Alpha Clipping`, `Clip` and `Stencil` (see the Basic panel); however it is fixed to `Cull Front` and is unaffected by `Render Face`.  

---

### Rim Light
`【边缘光 RimLight】颜色、大小、遮罩`

Adds a glowing edge along the object's silhouette, with support for intensity, inside extension distance and hard edge control.  
There are two **detection methods** for the edge (`Fresnel` / `Depth Difference`), chosen between as an either-or. They are only responsible for producing the same edge signal; all downstream processing — color, intensity, inside distance, hard edge, shade mask, mask map and so on — is **completely shared**.  

![](Documents~/Feature_RimLight.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_ToggleRimLight` → `_RIMLIGHT_ON` | Enables the rim light |
| Color | `_ColorRimLightColor` | Rim light color (HDR, alpha controls opacity) |
| Intensity | `_FloatRimLightIntensity` | Rim light range/intensity |
| Inside Distance | `_FloatRimLightInsideDistance` | How far the rim light extends inward; larger values make the range narrower |
| Hard Edge | `_ToggleRimLightHard` | Hardens the edge (removes the gradient) |
| Detection Method | `_FloatRimLightType` → `_RIMLIGHT_DEPTH_ON` | `Fresnel` (0, default) / `Depth Difference` (1), see [Rim Light Type](#rim-light-type) below |

#### Rim Light Type

| Method | Keyword | Principle | Characteristics |
| :-- | :-- | :-- | :-- |
| `Fresnel` (default) | no keyword | Based on the `angle between the normal and the view direction (NdotV)`; the closer to a grazing angle, the brighter | Soft gradient that follows surface normal detail; no extra dependencies |
| `Depth Difference` | `_RIMLIGHT_DEPTH_ON` | Samples scene depth offset by a number of pixels in screen space along the normal orientation; if the offset point is farther away it is judged to be a silhouette edge facing the camera | Clean, uniform-width silhouettes unaffected by surface normal detail; **requires Depth Texture to be enabled** |

Parameters exclusive to the `Fresnel` method:  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Normal Source / Blend Intensity | `_FloatRimLightNormalSource` / `_FloatRimLightNormalMapBlend` | Rim light normal source: `Geometry Normal` / `Normal Map` / `Blend` (only the Blend option uses the blend intensity) |

Parameters exclusive to the `Depth Difference` method:  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Sampling Width (pixels) | `_FloatRimLightDepthWidth` | The pixel distance of the offset sampling, i.e. the edge width. Scaled resolution-independently against a `1080p` baseline and clamped to 128 pixels |
| Depth Threshold | `_FloatRimLightDepthThreshold` | In world units. The depth difference must exceed this value to count as an edge, used to suppress depth noise inside the model |
| Threshold Soft Transition | `_FloatRimLightDepthThresholdSoft` | The width of the soft transition around the threshold; larger values make the edge softer |

> ⚠ The `Depth Difference` method relies on `_CameraDepthTexture`, so **`Depth Texture` must be enabled in the URP Asset**, otherwise there will be no effect or even overall brightening. The editor shows a red warning when this method is selected.  
> The current pixel depth is taken from the fragment's own `positionCS.z` (independent of whether the depth texture already contains this object), and the offset point is sampled with `Load` using integer pixel coordinates, avoiding the UV vertical-flip differences between graphics APIs.  
> The offset is **only along the horizontal screen direction** (taking the sign of the view-space normal's x), so it mainly detects vertical silhouettes on the left and right — this matches the reference implementation and also avoids the wrong offset direction for top/bottom silhouettes caused by the `SV_Position` Y axis pointing down in D3D and up in GL.  
> The `Depth Difference` method does not use the `Normal Source` setting; when the `Fresnel` method's `Normal Source` is set to `Normal Map` or `Blend` but no normal map is assigned, the editor shows a red warning that it has no effect. That selection is built with a branchless selector using `lerp + step` (the same technique as "Outline Type") and generates no extra keyword variants.  

#### Shade Mask
Masks the rim light on the `opposite side of the main light`, preventing unnatural edge glow on backlit/shadowed parts. After masking, a dedicated rim light color can be overlaid on the shaded areas.  

![](Documents~/Feature_RimLightShadeMask.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Shade Mask Toggle | `_ToggleRimLightShadeMask` → `_RIMLIGHT_SHADEMASK_ON` | Enables the shade mask |
| Mask Intensity / Mask Offset | `_FloatRimLightShadeMaskIntensity` / `_FloatRimLightShadeMaskOffset` | Mask intensity and the offset of its affected range |
| Shade Color Toggle | `_ToggleRimLightShadeColor` → `_RIMLIGHT_SHADEMASK_COLOR_ON` | Overlays a dedicated rim light color on the shaded areas |
| Shade Color / Intensity / Hard Edge | `_ColorRimLightShadeColor` / `_FloatRimLightShadeColorIntensity` / `_ToggleRimLightShadeColorHard` | Settings related to the shaded-area rim light color |

#### Mask Map
Uses a mask texture sharing the base map's UV (sampling the R channel) to control the rim light's distribution and intensity per pixel.  

![](Documents~/Feature_RimLightMaskMap.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Mask Map | `_TexRimLightMaskMap` → `_RIMLIGHT_MASKMAP_ON` | Rim light mask texture (the keyword is enabled automatically when a texture is present) |
| Mask Intensity | `_FloatRimLightMaskMapIntensity` | Mask influence intensity (`-1~1`) |

---

### Emissive
`【自发光 Emissive】遮罩、颜色(HDR)、动画`

Uses an emissive texture to overlay a glow color unaffected by lighting, supporting both static glow and animated glow (UV scrolling / rotation / ping-pong, time-based color change, view-based color change).  

![](Documents~/Feature_Emissive.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_ToggleEmissive` → `_EMISSIVE_ON` | Enables emissive |
| Emissive Map | `_TexEmissiveMap` | `RGB` = glow color (sRGB), `A` = glow intensity (mask) |
| Color | `_ColorEmissiveMapColor` | Emissive color (HDR), **defaults to black = no glow** |

Emissive = `texture RGB × HDR color × intensity (texture A channel)`, added directly to the final color.  

> ⚠ **HDR glow requires Bloom post-processing to be visible**: the Shader outputs untruncated HDR color, and raising the HDR color's `Intensity` by itself only brightens it. To get a bloom (glow bleed) effect you need to enable `Bloom` in the scene's `Global Volume` and make sure `HDR` is checked in the URP Asset.  

#### Emissive Animation
When `Emissive Animation - Toggle` is off, it is in **static mode** (only `texture × color × A channel`); turning it on enters **animation mode**, where the following parameters take effect.  

![](Documents~/Feature_EmissiveAnim.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Animation Toggle | `_ToggleEmissiveAnim` → `_EMISSIVE_ANIM` (no keyword = static) | Off = static / On = animated |
| UV Scale Mode | `_FloatEmissiveAnimUVType` | `FullMap` full-coverage UV mapping / `MatCap` spherical mapping by view-space normal, flowing with the view |
| Move Speed | `_FloatEmissiveAnimSpeed` | UV scrolling speed |
| Move Direction U / V | `_FloatEmissiveAnimDirU` / `_FloatEmissiveAnimDirV` | UV scrolling direction |
| Rotation Speed | `_FloatEmissiveAnimRotate` | Rotates around the UV center `(0.5, 0.5)` |
| Ping-Pong Movement | `_ToggleEmissiveAnimPingpong` | Uses `sin` to turn the time curve into a back-and-forth motion |
| Color Change / Change Color / Change Speed | `_ToggleEmissiveChangeColor` / `_ColorEmissiveChangeColor` / `_FloatEmissiveChangeSpeed` | Transitions back and forth between the emissive color and the change color following a `cos` time curve |
| View Change Color | `_ToggleEmissiveViewChangeColor` / `_ColorEmissiveViewChangeColor` | Transitions between the emissive color and this color based on the angle between the view direction and the normal (Fresnel) |
| ｜Normal Map | `_ToggleNormalMapOnEmissive` | The Fresnel calculation of the view change color uses the normal map |

> In animation mode, the **color** samples the animated UV while the **intensity (A channel)** samples the static UV — so the glowing region stays fixed where it is painted in the texture and only the color/pattern inside it flows.  
> Pixels with intensity below `0.005` do not glow (a `step` threshold), avoiding faint glow caused by dark noise in the texture.  

---

### MatCap
`【材质捕获 MatCap】贴图、混合模式、遮罩`

Spherical environment map (MatCap / Sphere Map): converts the world normal into view space to use as sampling UV, achieving view-dependent material qualities (metal, jade, leather, etc.) and giving rich surface expression without real reflections.  

![](Documents~/Feature_MatCap.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_ToggleMatCap` → `_MATCAP_ON` | Enables MatCap |
| MatCap Map | `_TexMatCapMap` | Spherical environment map (sRGB), **defaults to black = no effect** |
| Color | `_ColorMatCapMapColor` | Custom color (HDR, alpha contributes to intensity) |
| Color Blend Mode | `_FloatMatCapColorBlend` → `_MATCAP_COLORBLEND_MULTIPLY` / `_MATCAP_COLORBLEND_LERP` (no keyword = Additive) | `Additive` (linear dodge) / `Multiply` / `Lerp` interpolated blend |
| Blend Intensity | `_FloatMatCapColorBlendIntensity` | Intensity of the blend mode |
| Rotation | `_FloatMatCapRotate` | Rotates around the UV center (`-1~1` corresponds to `-π~π`) |
| Normal Map | `_ToggleNormalMapOnMatCap` | The sampling orientation uses the normal map |
| Shadow Mask / Intensity | `_ToggleMatCapShadowMask` / `_FloatMatCapShadowMaskIntensity` | Darkens MatCap in the shade 1 region so it dims inside shadows |

> Whether MatCap is `affected by light color` is controlled by `Light Setting → Light Toggles → MatCap` (`_ToggleGlobalLightMatCapMap`).  

#### Mask Map
Uses a mask texture sharing the base map's UV (sampling the R channel) to control MatCap's distribution and intensity per pixel.  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Mask Map | `_TexMatCapMaskMap` | Mask texture (defaults to white = fully visible; always sampled, no keyword needed) |
| Mask Intensity | `_FloatMatCapMaskMapIntensity` | Mask **offset** (`-1~1`): added to the mask value then `saturate`d; at `0` the texture's original value is used, positive values strengthen it overall and negative values weaken it |

---

### Light Setting
`【光照设置 LightSetting】光照开关、光照强度`

Consolidates settings for realtime lighting, environment lighting, exposure, additional lights, per-item light toggles, shadows, built-in lighting and light direction locking.  

![](Documents~/Feature_LightSetting.gif)

**Light Intensity**

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Realtime Light Intensity | `_FloatRealtimeLightIntensity` | Main light intensity |
| Environment Light Intensity | `_FloatEnvLightIntensity` | Ambient light / light probe intensity |

**Exposure Settings**

![](Documents~/Feature_Exposure.gif)

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Global Exposure Intensity | `_FloatGlobalExposureIntensity` | Global exposure |
| Base Map Bright Exposure | `_FloatBaseMapExposureIntensity` | Bright area exposure |
| Base Map Shade 1 Exposure | `_FloatBaseMapShade1ExposureIntensity` | Shade 1 exposure |
| Base Map Shade 2 Exposure | `_FloatBaseMapShade2ExposureIntensity` | Shade 2 exposure |

**Additional Lights (point lights, etc.)**

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Main Toggle | `_ToggleAddLight` → `_ADDLIGHT_ON` | Enables additional lights (point/spot lights) |
| Intensity | `_FloatAddLightIntensity` | Additional light intensity. Performs Half-Lambert directional shading using each light's own direction (lit side brighter, back side darker), then adds `light color × distance attenuation`, avoiding a uniformly washed-out look |

**Light Toggles (per-item light influence)**

You can individually set whether the following items are affected by light color:  

| Item | Toggle Property | Blend Intensity Property |
| :-- | :-- | :-- |
| Base Map | `_ToggleGlobalLightBaseMap` | `_GlobalLightBaseMapMixedIntensity` |
| Shade Map 1 | `_ToggleGlobalLightBaseShade1` | `_GlobalLightBaseShade1MixedIntensity` |
| Shade Map 2 | `_ToggleGlobalLightBaseShade2` | `_GlobalLightBaseShade2MixedIntensity` |
| HighLight | `_ToggleGlobalLightHighLight` | — (simple toggle, no blend intensity) |
| Rim Light | `_ToggleGlobalLightRimLight` | `_GlobalLightRimLightMixedIntensity` |
| Shade Rim Light | `_ToggleGlobalLightRimLightShade` | `_GlobalLightRimLightShadeMixedIntensity` |
| Outline | `_ToggleGlobalLightOutline` | `_GlobalLightOutlineMixedIntensity` |
| MatCap | `_ToggleGlobalLightMatCapMap` | — (simple toggle, no blend intensity) |

Among these, `Outline` makes the outline follow the main light color and darken along with cast shadows.  

**Shadow Settings**

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Shadow Cast | `ShadowCaster` Pass toggle (`Material.SetShaderPassEnabled("ShadowCaster", ...)`, no corresponding material property) | Casts shadows into the scene. With alpha clipping enabled, cut-out areas cast no shadow |
| Shadow Receive / Intensity | `_ToggleShadowReceive` / `_FloatShadowIntensity` | Receives real cast shadows from the scene; the intensity is an offset on the shadow attenuation (negative values brighten/soften shadows, positive values deepen them) |
| Terminator Smoothing / Smoothing Value | `_ToggleShadowTerminatorSmooth` / `_FloatShadowTerminatorSmooth` | Handles aliasing at the light/dark terminator, see the explanation below |
| Self-Shadow Bias : Depth Bias | `_FloatSelfShadowDepthBias` | Pushes the caster along the light direction when casting; increasing it reduces self-shadow acne |
| Self-Shadow Bias : Normal Bias | `_FloatSelfShadowNormalBias` | Shrinks along the normal when casting and scales by the `1-NdotL` slope (strongest at grazing angles); increasing it suppresses terminator break-up |
| Low Quality PCF | `_ToggleShadowLowQualityPCF` | Forces a low-quality (4-tap) PCF kernel to resample the main light shadow, reducing shadow perspective aliasing at character scale |

`Terminator Smoothing` addresses the problem of the light/dark terminator exposing shadow map resolution aliasing:  

| Mode | Behavior |
| :-- | :-- |
| Off (default) | Uses the shadow map directly (original behavior). Cast shadows cover the entire region including the terminator, but the terminator may show aliasing caused by the shadow map resolution |
| On | Blends the `geometric (NdotL) smooth self-shadow envelope` with the `shadow map` by taking the **darker of the two (min)**. The min of two monotonic curves is still monotonic, so it **never produces a bright seam**; the terminator is dominated by the smooth geometry (removing aliasing), darker cast shadows can still come through, and the backlit side is darkened as usual |

> The `Smoothing Value` is the transition half-width of the geometric smooth self-shadow envelope: larger makes the terminator smoother (geometry dominates a wider range), smaller makes the terminator sharper and cast shadows hug the terminator more closely.  
> It relies on the main light shadow keywords `_MAIN_LIGHT_SHADOWS*` (already added to the `ForwardLit` and `Outline` Passes); main light shadows must be enabled in the URP Asset for the body and outline to receive real cast shadows.

`Self-Shadow Bias` and `Low Quality PCF` are **per-material compensations** layered on top of the URP global settings; both default to "do not change the original behavior" and are only meant to be tuned individually for the occasional problematic model:  

| Item | Default | Description |
| :-- | :-- | :-- |
| Self-Shadow Bias (depth / normal) | `0 / 0` = no extra bias | On top of URP's global shadow bias, pushes vertices inside the `ShadowCaster` Pass. Too much depth bias causes light leaking (Peter-Panning); the normal bias scales by slope and mainly suppresses terminator break-up. The slider `[0,1]` corresponds to roughly `0~0.02` world units, which falls within the usable range at character scale |
| Low Quality PCF | `Off` = use URP's original sampling | The wide PCF kernels of Medium/High spread the samples over a larger area of the shadow map, which at character-scale perspective actually magnifies texel stair-stepping; a low-quality kernel fits characters better and gives cleaner edges. Only applies to the **shadow map** path; screen-space shadows (`_MAIN_LIGHT_SHADOWS_SCREEN`) and the no-shadow case fall back to the default sampling |

> ⚠ `Low Quality PCF` still requires `Soft Shadows` to be checked in the URP Asset — the 4-tap texel offsets are only uploaded by the pipeline when soft shadows are enabled; otherwise the 4 sample points coincide, which is equivalent to hard shadows and this toggle will have no visible effect.  
> See `MainLightShadowLowQualityPCF` in `Core/Shaders/ShadowFunction.hlsl` for the implementation.  

**Built-In Light (material-specific)**

![](Documents~/Feature_BuiltInLight.gif)

When enabled, it introduces the material's own light direction and color, keeping the character consistently lit across different scenes.  

Note that both are **blended** rather than directly replacing the scene lighting: the light direction is interpolated between "scene light direction ↔ built-in light direction" by the `Direction Blend Intensity` (default 0.5 = half and half); the light color only participates in the blend after the `Built-In Light Color Toggle` is additionally enabled, otherwise the scene light color is still used entirely. To fully break away from the scene light direction, set `Direction Blend Intensity` to 1.  

| Parameter | Property | Description |
| :-- | :-- | :-- |
| Built-In Light Toggle | `_ToggleBuiltInLight` → `_BUILTINLIGHT_ON` | Enables built-in lighting |
| Direction X / Y / Z | `_FloatBuiltInLightAxisX/Y/Z` | Built-in light direction |
| Direction Blend Intensity | `_FloatBuiltInLightDirBlend` | 0 = scene light direction, 1 = built-in light direction |
| Built-In Light Color Toggle / Color / Blend Intensity | `_ToggleBuiltInLightColor` / `_ColorBuiltInLightColor` / `_FloatBuiltInLightColorBlend` | Dedicated built-in light color |

**Light Direction Lock**

Locks the light height to horizontal so that light and dark vary along the horizontal axis. It can be applied individually to the following items:  

| Item | Property |
| :-- | :-- |
| Base Map | `_ToggleLightHorLockBaseMap` |
| HighLight | `_ToggleLightHorLockHighLight` |
| Rim Light Shade Mask | `_ToggleLightHorLockRimLight` |

---

### Render Pass Overview
In addition to `ForwardLit` (base lighting) and `Outline`, the following Passes are included to make sure objects work correctly within the full URP rendering flow:

| Pass | LightMode | Purpose |
| :-- | :-- | :-- |
| `ShadowCaster` | `ShadowCaster` | Casts shadows into the scene (controlled by the "Shadow Settings → Shadow Cast" toggle). Supports per-vertex light direction for point/spot lights and alpha clipping |
| `DepthOnly` | `DepthOnly` | Writes the camera depth texture `_CameraDepthTexture`, used by depth fog, soft particles, screen-space outlines and so on |
| `DepthNormals` | `DepthNormals` | Writes the camera normals texture `_CameraNormalsTexture`, used by SSAO and other normal-dependent post-processing |

Overview of each Pass's render states:

| Pass | Cull | Alpha Clipping | Clip | Stencil |
| :-- | :-- | :--: | :--: | :--: |
| `ForwardLit` | `[_IntRenderFaceType]` | ✅ | ✅ | ✅ |
| `Outline` | `Front` (fixed) | ✅ | ✅ (hard cull) | ✅ |
| `ShadowCaster` | `[_IntRenderFaceType]` | ✅ | ❌ | ❌ |
| `DepthOnly` | `[_IntRenderFaceType]` | ✅ | ❌ | ❌ |
| `DepthNormals` | `[_IntRenderFaceType]` | ✅ | ❌ | ❌ |

> All three additional Passes support alpha clipping (`_ALPHATEST_ON`), keeping the shadows and depth/normal silhouettes of cut-out objects consistent with the body.  

---

### Debug
`【调试 Debug】仅编辑器用，不影响正式流程`

A debug section located at the **very top** of the Inspector. When `Highlight Locate (Scene)` is enabled, every object in the Scene view using the current material is overlaid in **solid magenta** (visible through occluders), with a bounding box wireframe and a name label drawn, making it easy to quickly locate who uses this material in a complex scene.  

- Accurate down to the **submesh**: on the same object, only the material slots actually using the current material are highlighted (e.g. face skin vs. eyes).
- Supports `SkinnedMeshRenderer` (bakes the mesh in its current pose).
- Purely an editor feature: it does not modify materials/Shaders/the render pipeline, only draws an overlay in the Scene view, and does not affect the Game view or the actual runtime flow.
- The toggle is editor-session-level state and is not written into the material; it turns off automatically after script recompilation or domain reload.

Source: `Assets/PluginsDeveloper/BlurToonURP/Core/Editor/MaterialDebugHighlight.cs`

---

### Tools
| Script | Description |
| :-- | :-- |
| `LightController.cs` | Attached to a light, it lets the light rotate automatically at a configured Euler angle speed (with start delay and duration), commonly used for effect demos and recordings. |

---