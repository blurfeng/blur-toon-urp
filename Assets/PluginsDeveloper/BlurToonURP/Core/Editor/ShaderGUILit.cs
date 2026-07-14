using UnityEditor;
using UnityEngine;

namespace BlurToonURP.EditorGUIx
{
    public class ShaderGUILit : ShaderGUIBase
    {
        protected override void OnGUIDraw()
        {
            base.OnGUIDraw();

            //Debug 功能区（仅编辑器，不影响正式流程），放在顶部
            MaterialDebugHighlight.OnInspectorGUI(Material);

            EditorGUIx.FoldoutPanel("【基础设置 Basic】表面类型、渲染面、透明度裁切、裁剪、模板测试", PanelMainBasic);
            EditorGUIx.FoldoutPanel("【基础贴图 BaseMap】基础贴图及暗部贴图", PanelMainBasicMap);
            EditorGUIx.FoldoutPanel("【法线贴图 NormalMap】强度、效果开关", PanelMainNormalMap);
            EditorGUIx.FoldoutPanel("【镜面高光 HighLight】高光颜色、大小、遮罩", PanelMainHighLight);
            EditorGUIx.FoldoutPanel("【外描边 Outline】粗细、颜色", PanelMainOutline);
            EditorGUIx.FoldoutPanel("【边缘光 RimLight】颜色、大小、遮罩", PanelMainRimLight);
            EditorGUIx.FoldoutPanel("【光照设置 LightSetting】光照开关、光照强度", PanelMainGlobalLight);
            
        }

        #region BaseMap 基础贴图
        private static readonly GUIContent ContentBaseMap = new GUIContent("基础贴图", "基础色 : 贴图采样色(sRGB) × 自定义色(RGB), 默认:白色)");
        private static readonly GUIContent ContentBaseMapShadeThresholdMap = new GUIContent("暗部阈值贴图", "通过阈值贴图控制暗部1的分布与强度。暗部强度 : 纹理采样(linear)");

        /// <summary>
        /// 关键词 暗部阈值贴图
        /// </summary>
        private const string MatKeywordShadeThresholdMap = "_BASEMAP_SHADE_THRESHOLDMAP_ON";

        /// <summary>
        /// 主面板 基础贴图
        /// </summary>
        private void PanelMainBasicMap()
        {
            //基础贴图
            EditorGUIx.LabelItem("基础贴图");
            EditorGUILayout.BeginHorizontal();
            MaterialEditor.TexturePropertySingleLine(ContentBaseMap, GetMaterialProperty("_BaseMap"), GetMaterialProperty("_BaseColor"));
            EditorGUILayout.EndHorizontal();
            
            //基础贴图混合颜色
            MaterialEditor.ColorProperty(GetMaterialProperty("_BaseMapBlendColor"), "混合颜色");
            //基础贴图混合颜色强度
            MaterialEditor.RangeProperty(GetMaterialProperty("_BaseMapBlendColorIntensity"), "混合强度");
            
            //暗部颜色1
            EditorGUIx.LabelItem("暗部颜色");
            EditorGUILayout.BeginHorizontal();
            MaterialEditor.ColorProperty(GetMaterialProperty("_Shade1Color"),"暗部1颜色");
            EditorGUILayout.EndHorizontal();

            //暗部颜色2
            EditorGUILayout.BeginHorizontal();
            MaterialEditor.ColorProperty(GetMaterialProperty("_Shade2Color"), "暗部2颜色");
            EditorGUILayout.EndHorizontal();

            //色阶分布
            EditorGUIx.LabelItem("阴影色阶分布与模糊");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBrightShade1Step"), "亮部→暗部1 : 位置");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBrightShade1Blur"), "亮部→暗部1 : 模糊");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatShade1Shade2Step"), "暗部1→暗部2 : 位置");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatShade1Shade2Blur"), "暗部1→暗部2 : 模糊");
            EditorGUILayout.Space();
            
            //暗部阈值贴图
            EditorGUIx.FoldoutPanel("暗部阈值贴图", PanelSubShadeThresholdMap, EditorGUIx.EFoldoutStyleType.Sub);
        }

        /// <summary>
        /// 子界面 暗部阈值贴图
        /// </summary>
        private void PanelSubShadeThresholdMap()
        {
            var matPropToggleShadeThresholdMap = GetMaterialProperty("_ToggleShadeThresholdMap");
            EditorGUIx.SwitchButton("暗部阈值贴图-主开关", matPropToggleShadeThresholdMap);
            //多选编辑：按各材质自身开关值同步关键词
            ApplyKeyword(MatKeywordShadeThresholdMap, "_ToggleShadeThresholdMap");
            if (!matPropToggleShadeThresholdMap.floatValue.Equals(1))
                return;

            //条目 暗部阈值贴图
            var matPropTexShadeThresholdMap = GetMaterialProperty("_TexShadeThresholdMap");
            MaterialEditor.TexturePropertySingleLine(ContentBaseMapShadeThresholdMap, matPropTexShadeThresholdMap);
            MaterialEditor.TextureScaleOffsetProperty(matPropTexShadeThresholdMap);
            //条目 暗部阈值贴图强度
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatShadeThresholdMapIntensity"), "强度");
        }
        #endregion

        #region Basic 基础设置（表面类型 / 渲染面 / 透明度裁切 / 裁剪 / 模板测试）
        private static readonly GUIContent ContentSurfaceType = new GUIContent("表面类型", "Opaque 不透明 / Transparent 透明（标准 Alpha 混合）。切换会自动设置混合模式、深度写入与渲染队列。");
        private static readonly GUIContent ContentRenderFace = new GUIContent("渲染面", "Both 双面 / Back 反面 / Front 正面。作用于本体、阴影、深度等 Pass；描边固定渲染反面，不受此影响。");
        private static readonly GUIContent ContentAlphaClip = new GUIContent("透明度裁切", "按 基础贴图Alpha×基础色Alpha 与阈值裁切像素（Alpha Clip / Cutout）。在本体、描边、阴影、深度所有 Pass 生效。");
        private static readonly GUIContent ContentRenderQueueAuto = new GUIContent("渲染队列自动", "开启时按 表面类型 / 透明度裁切 / 模板类型 自动设置渲染队列；关闭后可在下方手动指定。");
        //裁剪 Clip（溶解）
        private static readonly GUIContent ContentClipType = new GUIContent("裁剪类型", "Off 关闭 / Dither 挖孔（按遮罩剔除像素）/ Alpha 透明度（配合 Transparent 做溶解淡出）。与“透明度裁切”相互独立。");
        private static readonly GUIContent ContentClipMap = new GUIContent("裁剪贴图", "从遮罩贴图 R 通道采样裁剪强度（0-1），UV 与基础贴图相同。");
        private static readonly GUIContent ContentClipBaseMapAlpha = new GUIContent("基础贴图A通道生效", "把基础贴图 Alpha 叠加进裁剪计算（基础贴图为透明贴图时使用）。");
        //模板测试 Stencil
        private static readonly GUIContent ContentStencilType = new GUIContent("模板类型", "Off 关闭 / Discard 丢弃（同组遮罩处不绘制）/ Reserve 保留（写入遮罩，且先于丢弃渲染）。");
        private static readonly GUIContent ContentStencilGroupNum = new GUIContent("模板组序号", "相同序号的材质球才会互相影响（0-255）。");

        /// <summary>
        /// 关键词 透明度裁切 开启
        /// </summary>
        private const string MatKeywordAlphaTest = "_ALPHATEST_ON";
        /// <summary>
        /// 关键词 裁剪 挖孔（无关键词=关闭）
        /// </summary>
        private const string MatKeywordClipDither = "_CLIP_DITHER";
        /// <summary>
        /// 关键词 裁剪 透明度
        /// </summary>
        private const string MatKeywordClipAlpha = "_CLIP_ALPHA";

        /// <summary>
        /// 表面类型
        /// </summary>
        private enum ESurfaceType
        {
            /// <summary>
            /// 不透明
            /// </summary>
            Opaque,

            /// <summary>
            /// 透明（标准 Alpha 混合）
            /// </summary>
            Transparent
        }

        /// <summary>
        /// 渲染面（枚举索引即 Cull 模式值：Both=0 Off、Back=1 Front、Front=2 Back）
        /// </summary>
        private enum ERenderFace
        {
            /// <summary>
            /// 双面（Cull Off）
            /// </summary>
            Both,

            /// <summary>
            /// 反面（Cull Front）
            /// </summary>
            Back,

            /// <summary>
            /// 正面（Cull Back）
            /// </summary>
            Front
        }

        /// <summary>
        /// 裁剪类型（溶解）
        /// </summary>
        private enum EClipType
        {
            /// <summary>
            /// 关闭
            /// </summary>
            Off,

            /// <summary>
            /// 挖孔
            /// </summary>
            Dither,

            /// <summary>
            /// 透明度
            /// </summary>
            Alpha
        }

        /// <summary>
        /// 模板测试类型
        /// </summary>
        private enum EStencilType
        {
            /// <summary>
            /// 关闭
            /// </summary>
            Off,

            /// <summary>
            /// 丢弃（同组遮罩处不绘制）
            /// </summary>
            Discard,

            /// <summary>
            /// 保留（写入遮罩，先于丢弃渲染）
            /// </summary>
            Reserve
        }

        /// <summary>
        /// 主面板 基础设置（表面类型 / 渲染面 / 透明度裁切 / 裁剪 / 模板测试）
        /// </summary>
        private void PanelMainBasic()
        {
            //条目 表面类型
            EditorGUIx.DropdownEnum(ContentSurfaceType, GetMaterialProperty("_Surface"), typeof(ESurfaceType), MaterialEditor);

            //条目 渲染面（Both/Back/Front，枚举索引即 Cull 模式值）
            EditorGUIx.DropdownEnum(ContentRenderFace, GetMaterialProperty("_IntRenderFaceType"), typeof(ERenderFace), MaterialEditor);

            //条目 透明度裁切
            var matPropToggleAlphaClip = GetMaterialProperty("_ToggleAlphaClip");
            EditorGUIx.SwitchButton(ContentAlphaClip, matPropToggleAlphaClip);
            //多选编辑：按各材质自身开关值同步关键词
            ApplyKeyword(MatKeywordAlphaTest, "_ToggleAlphaClip");
            if (matPropToggleAlphaClip.floatValue.Equals(1))
            {
                EditorGUI.indentLevel++;
                //条目 裁切阈值
                MaterialEditor.RangeProperty(GetMaterialProperty("_Cutoff"), "| 裁切阈值");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            //子面板 渲染队列
            EditorGUIx.FoldoutPanel("渲染队列 RenderQueue", PanelSubRenderQueue, EditorGUIx.EFoldoutStyleType.Sub);
            //子面板 裁剪 Clip（溶解）
            EditorGUIx.FoldoutPanel("裁剪 Clip（溶解）", PanelSubClip, EditorGUIx.EFoldoutStyleType.Sub);
            //子面板 模板测试 Stencil
            EditorGUIx.FoldoutPanel("模板测试 Stencil", PanelSubStencil, EditorGUIx.EFoldoutStyleType.Sub);

            //按“表面类型 + 是否裁切 + 模板类型”设置各材质的混合因子/深度写入/渲染队列/渲染类型标签
            ApplySurfaceType();
            //按“模板类型”预设各材质的 Comp/Pass/Fail
            ApplyStencil();
        }

        /// <summary>
        /// 子面板 渲染队列：自动（按表面类型/裁切/模板推导）或手动指定
        /// </summary>
        private void PanelSubRenderQueue()
        {
            //条目 渲染队列自动开关
            var matPropToggleAuto = GetMaterialProperty("_ToggleRenderQueueAuto");
            EditorGUIx.SwitchButton(ContentRenderQueueAuto, matPropToggleAuto);

            //自动时禁用手动输入；关闭自动后可手动指定（作用到全部选中材质）
            EditorGUI.BeginDisabledGroup(matPropToggleAuto.floatValue.Equals(1));
            EditorGUI.BeginChangeCheck();
            int queue = EditorGUILayout.IntField("渲染队列", Material.renderQueue);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var m in Materials)
                    if (m != null) m.renderQueue = queue;
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 子面板 裁剪 Clip（溶解）：Off / 挖孔 / 透明度
        /// </summary>
        private void PanelSubClip()
        {
            //条目 裁剪类型
            var matPropClipType = GetMaterialProperty("_IntClipType");
            EditorGUIx.DropdownEnum(ContentClipType, matPropClipType, typeof(EClipType), MaterialEditor);
            //多选编辑：按各材质自身类型同步关键词（两者都关=Off）
            ApplyKeyword(MatKeywordClipDither, "_IntClipType", (float)EClipType.Dither);
            ApplyKeyword(MatKeywordClipAlpha, "_IntClipType", (float)EClipType.Alpha);

            //关闭时不显示详细设置
            if (matPropClipType.floatValue.Equals((float)EClipType.Off))
                return;

            //条目 裁剪贴图
            var matPropTexClip = GetMaterialProperty("_TexClipMaskMap");
            MaterialEditor.TexturePropertySingleLine(ContentClipMap, matPropTexClip);
            MaterialEditor.TextureScaleOffsetProperty(matPropTexClip);

            if (matPropClipType.floatValue.Equals((float)EClipType.Dither))
            {
                //条目 挖孔强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatClipIntensity"), "裁剪强度");
            }
            else //Alpha
            {
                EditorGUIx.LabelItem(new GUIContent("透明度效果", "改变透明度的溶解，“表面类型”设为 Transparent 才会呈现透明淡出。"));
                //条目 透明度强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatClipTransIntensity"), "透明强度");
                //条目 基础贴图A通道生效
                EditorGUIx.SwitchButton(ContentClipBaseMapAlpha, GetMaterialProperty("_ToggleClipTransBaseMapAlpha"));
            }
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 子面板 模板测试 Stencil：Off / 丢弃 / 保留
        /// </summary>
        private void PanelSubStencil()
        {
            EditorGUIx.LabelItem(new GUIContent("模板测试 : 丢弃、保留", "相同“模板组序号”的材质球才会互相影响。"));

            //条目 模板类型（Comp/Pass/Fail 由 ApplyStencil() 按类型预设）
            EditorGUIx.DropdownEnum(ContentStencilType, GetMaterialProperty("_IntStencilType"), typeof(EStencilType), MaterialEditor);

            //条目 模板组序号（0-255，多选安全：MaterialProperty 会作用到全部选中材质）
            var matPropStencilNum = GetMaterialProperty("_FloatStencilNum");
            EditorGUI.showMixedValue = matPropStencilNum.hasMixedValue;
            EditorGUI.BeginChangeCheck();
            int num = EditorGUILayout.IntField(ContentStencilGroupNum, (int)matPropStencilNum.floatValue);
            if (EditorGUI.EndChangeCheck())
                matPropStencilNum.floatValue = Mathf.Clamp(num, 0, 255);
            EditorGUI.showMixedValue = false;
            EditorGUILayout.Space();
        }

        /// <summary>
        /// 按各材质自身的“表面类型/透明度裁切”设置渲染状态（混合因子、深度写入、渲染队列、RenderType 标签）。
        /// <para>多选编辑时作用到全部选中材质；这些是由属性推导出的“派生渲染状态”，与关键词同理需每次 OnGUI 同步；
        /// 仅在与当前值不同时才写入，避免无意义地反复标记材质为已修改。</para>
        /// </summary>
        private void ApplySurfaceType()
        {
            if (Materials == null) return;
            foreach (var m in Materials)
            {
                if (m == null) continue;

                bool transparent = m.HasProperty("_Surface") && m.GetFloat("_Surface") >= 0.5f;
                bool alphaClip = m.HasProperty("_ToggleAlphaClip") && m.GetFloat("_ToggleAlphaClip") >= 0.5f;
                //渲染队列自动：关闭后保留用户手动设置的队列，不由此处覆盖
                bool autoQueue = !m.HasProperty("_ToggleRenderQueueAuto") || m.GetFloat("_ToggleRenderQueueAuto") >= 0.5f;

                int src, dst, zwrite, queue;
                string renderType;
                if (transparent)
                {
                    //透明：标准 Alpha 混合，关闭深度写入，进入 Transparent 队列
                    src = (int)UnityEngine.Rendering.BlendMode.SrcAlpha;
                    dst = (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                    zwrite = 0;
                    queue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    renderType = "Transparent";
                }
                else
                {
                    //不透明：不混合、写入深度
                    src = (int)UnityEngine.Rendering.BlendMode.One;
                    dst = (int)UnityEngine.Rendering.BlendMode.Zero;
                    zwrite = 1;
                    //渲染队列：模板“保留(写入遮罩)”需先于“丢弃(按遮罩剔除)”渲染；其余按是否裁切进入 AlphaTest / Geometry
                    int stencilType = m.HasProperty("_IntStencilType") ? (int)m.GetFloat("_IntStencilType") : 0;
                    if (stencilType == (int)EStencilType.Reserve)
                        queue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest - 1;
                    else if (stencilType == (int)EStencilType.Discard)
                        queue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    else
                        queue = alphaClip
                            ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                            : (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    renderType = alphaClip ? "TransparentCutout" : "Opaque";
                }

                if (!Mathf.Approximately(m.GetFloat("_SrcBlend"), src)) m.SetFloat("_SrcBlend", src);
                if (!Mathf.Approximately(m.GetFloat("_DstBlend"), dst)) m.SetFloat("_DstBlend", dst);
                if (!Mathf.Approximately(m.GetFloat("_ZWrite"), zwrite)) m.SetFloat("_ZWrite", zwrite);
                if (autoQueue && m.renderQueue != queue) m.renderQueue = queue;
                if (m.GetTag("RenderType", false, "") != renderType) m.SetOverrideTag("RenderType", renderType);
            }
        }

        /// <summary>
        /// 按各材质自身的“模板类型”预设模板测试的比较规则与写入操作（Comp/Pass/Fail）。
        /// <para>Off = 关闭模板测试（Comp Disabled）；Discard 丢弃 = 仅在缓冲区值≠序号处绘制（Comp NotEqual）；
        /// Reserve 保留 = 始终绘制并把序号写入缓冲区（Comp Always + Pass/Fail Replace）。
        /// 与派生渲染状态同理需每次 OnGUI 同步，仅在与当前值不同时才写入。</para>
        /// </summary>
        private void ApplyStencil()
        {
            if (Materials == null) return;
            foreach (var m in Materials)
            {
                if (m == null) continue;

                int type = m.HasProperty("_IntStencilType") ? (int)m.GetFloat("_IntStencilType") : 0;
                //枚举值对应 UnityEngine.Rendering.CompareFunction / StencilOp
                float comp, pass, fail;
                switch (type)
                {
                    case (int)EStencilType.Discard: //丢弃：Comp=NotEqual(6) Pass/Fail=Keep(0)
                        comp = (float)UnityEngine.Rendering.CompareFunction.NotEqual;
                        pass = (float)UnityEngine.Rendering.StencilOp.Keep;
                        fail = (float)UnityEngine.Rendering.StencilOp.Keep;
                        break;
                    case (int)EStencilType.Reserve: //保留：Comp=Always(8) Pass/Fail=Replace(2)
                        comp = (float)UnityEngine.Rendering.CompareFunction.Always;
                        pass = (float)UnityEngine.Rendering.StencilOp.Replace;
                        fail = (float)UnityEngine.Rendering.StencilOp.Replace;
                        break;
                    default: //Off：Comp=Disabled(0)，关闭模板测试
                        comp = (float)UnityEngine.Rendering.CompareFunction.Disabled;
                        pass = (float)UnityEngine.Rendering.StencilOp.Keep;
                        fail = (float)UnityEngine.Rendering.StencilOp.Keep;
                        break;
                }

                if (!Mathf.Approximately(m.GetFloat("_FloatStencilComp"), comp)) m.SetFloat("_FloatStencilComp", comp);
                if (!Mathf.Approximately(m.GetFloat("_FloatStencilPass"), pass)) m.SetFloat("_FloatStencilPass", pass);
                if (!Mathf.Approximately(m.GetFloat("_FloatStencilFail"), fail)) m.SetFloat("_FloatStencilFail", fail);
            }
        }
        #endregion

        #region 主面板-法线贴图
        private static GUIContent m_ContentBaseNormalMap = new GUIContent("法线贴图", "法线偏移 : 贴图采样矢量(sRGB)进行法线偏移");
        
        /// <summary>
        /// 主界面 法线贴图
        /// </summary>
        /// <param name="material"></param>
        private void PanelMainNormalMap()
        {
            //条目 法线贴图&强度 缩放%位移
            var matPropTexNormalMap = GetMaterialProperty("_BumpMap");
            MaterialEditor.TexturePropertySingleLine(m_ContentBaseNormalMap, matPropTexNormalMap, GetMaterialProperty("_BumpScale"));
            MaterialEditor.TextureScaleOffsetProperty(matPropTexNormalMap);

            EditorGUIx.LabelItem("法线贴图的有效开关");
            EditorGUIx.SwitchButton("基础贴图", GetMaterialProperty("_ToggleNormalMapOnBaseMap"));
            EditorGUIx.SwitchButton("高光", GetMaterialProperty("_ToggleNormalMapOnHighLight"));
            //边缘光的法线来源已改为【边缘光】面板中的专属「法线来源」配置，此处不再提供共用开关。
        }
        #endregion

        #region HighLight 镜面高光
        private static readonly GUIContent ContentHighLightMaskTex = new GUIContent("遮罩贴图", "在遮罩贴图中绘制高光的分布与强度（采样R通道），uv坐标与基础贴图相同。");

        /// <summary>
        /// 关键词 镜面高光 开启
        /// </summary>
        private const string MatKeywordHighLightOn = "_HIGHLIGHT_ON";
        /// <summary>
        /// 关键词 镜面高光 遮罩贴图 开启
        /// </summary>
        private const string MatKeywordHighLightMaskMapOn = "_HIGHLIGHT_MASKMAP_ON";

        /// <summary>
        /// 主面板 镜面高光
        /// </summary>
        private void PanelMainHighLight()
        {
            //条目 高光主开关
            var matPropToggleHighLight = GetMaterialProperty("_ToggleHighLight");
            EditorGUIx.SwitchButton("镜面高光-主开关", matPropToggleHighLight);
            //多选编辑：按各材质自身开关值同步关键词
            ApplyKeyword(MatKeywordHighLightOn, "_ToggleHighLight");
            if (!matPropToggleHighLight.floatValue.Equals(1))
                return;

            EditorGUIx.LabelItem("高光 设置");
            //条目 颜色
            MaterialEditor.ColorProperty(GetMaterialProperty("_ColorHighLightColor"), "颜色");
            //条目 强度
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatHighLightIntensity"), "强度");
            //条目 大小（范围）
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatHighLightSize"), "大小");
            //条目 边缘羽化
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatHighLightBlur"), "边缘羽化");
            EditorGUILayout.Space();

            //子面板 遮罩贴图
            EditorGUIx.FoldoutPanel("遮罩贴图", () =>
            {
                EditorGUIx.LabelItem(new GUIContent("遮罩绘制高光", "绘制所有UV位置的高光遮罩，值越大高光越明显。"));
                //条目 高光遮罩贴图
                var matPropTexHighLightMaskMap = GetMaterialProperty("_TexHighLightMaskMap");
                MaterialEditor.TexturePropertySingleLine(ContentHighLightMaskTex, matPropTexHighLightMaskMap);
                MaterialEditor.TextureScaleOffsetProperty(matPropTexHighLightMaskMap);
                //设置 关键词（多选编辑：按各材质是否指定遮罩贴图同步）
                ApplyKeywordByTexture(MatKeywordHighLightMaskMapOn, "_TexHighLightMaskMap");

                //条目 遮罩强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatHighLightMaskMapIntensity"), "遮罩强度");
            }
            , EditorGUIx.EFoldoutStyleType.Sub);
        }
        #endregion

        #region Outline 外描边
        private static readonly GUIContent ContentOutline = new GUIContent("外描边-主开关", "设置外描边开启或关闭。");
        private static readonly GUIContent ContentOutlineType = new GUIContent("描边类型", "法线(顶点色法线)外扩描边。 VertexNormal : 顶点法线，VertexColor : 顶点颜色");
        private static readonly GUIContent ContentOutlineWidthType = new GUIContent("宽度类型", "Same : 相同宽度，Scaling : 变化宽度");
        private static readonly GUIContent ContentOutlineBaseMapBlend = new GUIContent("基础贴图混合", "与基础贴图的颜色进行混合，使描边色更加自然。");
        private static readonly GUIContent ContentOutlineMap = new GUIContent("描边纹理", "描边专用纹理，调制描边颜色（彩色描边/图案/噪声等），UV 与基础贴图相同。指定贴图后自动生效。");

        /// <summary>
        /// 关键词 外描边 开启
        /// </summary>
        private const string MatKeywordOutlineOn = "_OUTLINE_ON";
        /// <summary>
        /// 关键词 外描边 相同宽度
        /// </summary>
        private const string MatKeywordOutlineSameWidth = "_OUTLINE_WIDTH_SAME";
        /// <summary>
        /// 关键词 外描边 变化宽度
        /// </summary>
        private const string MatKeywordOutlineScaling = "_OUTLINE_WIDTH_SCALING";
        /// <summary>
        /// 通道名称 外描边
        /// </summary>
        private const string MatPassNameOutline = "Outline";
        /// <summary>
        /// 关键词 描边纹理贴图 开启
        /// </summary>
        private const string MatKeywordOutlineMapOn = "_OUTLINE_MAP_ON";

        /// <summary>
        /// 描边类型
        /// </summary>
        private enum EOutlineType
        {
            /// <summary>
            /// 顶点法线
            /// </summary>
            VertexNormal,
            
            /// <summary>
            /// 顶点颜色
            /// </summary>
            VertexColor,
            
            /// <summary>
            /// 顶点切线
            /// </summary>
            VertexTangent
        }

        /// <summary>
        /// 描边宽度类型
        /// </summary>
        private enum EOutlineWidthType
        {
            /// <summary>
            /// 相同宽度
            /// </summary>
            Same,
            /// <summary>
            /// 变化宽度
            /// </summary>
            Scaling
        }

        /// <summary>
        /// 主面板 外描边
        /// </summary>
        private void PanelMainOutline()
        {
            //条目 主开关
            EditorGUIx.SwitchButtonPass(ContentOutline, Materials, MatPassNameOutline);
            //设置 关键词（多选编辑：按各材质自身Pass状态同步）
            ApplyKeywordByPass(MatKeywordOutlineOn, MatPassNameOutline);
            if (!Material.GetShaderPassEnabled(MatPassNameOutline))
                return;

            //条目 描边类型
            EditorGUIx.DropdownEnum(ContentOutlineType, GetMaterialProperty("_FloatOutlineType"), typeof(EOutlineType), MaterialEditor);

            //条目 描边宽度类型
            var matPropFloatOutlineWidthType = GetMaterialProperty("_FloatOutlineWidthType");
            EditorGUIx.DropdownEnum(ContentOutlineWidthType, matPropFloatOutlineWidthType, typeof(EOutlineWidthType), MaterialEditor);
            //应用材质球属性-描边宽度类型（多选编辑：按各材质自身宽度类型同步，两者互斥）
            ApplyKeyword(MatKeywordOutlineSameWidth, "_FloatOutlineWidthType", (float)EOutlineWidthType.Same);
            ApplyKeyword(MatKeywordOutlineScaling, "_FloatOutlineWidthType", (float)EOutlineWidthType.Scaling);

            //条目 外描边颜色
            MaterialEditor.ColorProperty(GetMaterialProperty("_ColorOutlineColor"), "颜色");
            //条目 外描边宽度
            MaterialEditor.FloatProperty(GetMaterialProperty("_FloatOutlineWidth"), "宽度");
            EditorGUILayout.Space();

            //条目 基础贴图颜色混合
            var matPropToggleOutlineBaseMapBlend = GetMaterialProperty("_ToggleOutlineBaseMapBlend");
            EditorGUIx.SwitchButton(ContentOutlineBaseMapBlend, matPropToggleOutlineBaseMapBlend);
            if (matPropToggleOutlineBaseMapBlend.floatValue.Equals(1))
            {
                EditorGUI.indentLevel++;
                //条目 基础贴图颜色混合强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatOutlineBaseMapBlendIntensity"), "| 混合强度");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            //子面板 描边纹理贴图
            EditorGUIx.FoldoutPanel("描边纹理贴图", () =>
            {
                EditorGUIx.LabelItem(new GUIContent("纹理调制描边", "用一张描边专用纹理调制描边颜色，可做彩色描边、图案、噪声等。UV 与基础贴图相同。"));
                //条目 描边纹理贴图
                var matPropTexOutlineMap = GetMaterialProperty("_TexOutlineMap");
                MaterialEditor.TexturePropertySingleLine(ContentOutlineMap, matPropTexOutlineMap);
                MaterialEditor.TextureScaleOffsetProperty(matPropTexOutlineMap);
                //设置 关键词（多选编辑：按各材质是否指定描边纹理同步）
                ApplyKeywordByTexture(MatKeywordOutlineMapOn, "_TexOutlineMap");

                //条目 混合强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatOutlineMapIntensity"), "混合强度");
            }
            , EditorGUIx.EFoldoutStyleType.Sub);
        }
        #endregion
        
        #region 主面板-边缘光
        private static readonly GUIContent ContentRimLightShadeMask = new GUIContent("暗部遮罩", "对“主光源反方向”的“边缘光”进行遮罩");
        private static readonly GUIContent ContentRimLightMaskTex = new GUIContent("遮罩贴图", "在遮罩贴图中绘制边缘光的分布与强度，uv坐标与基础贴图相同。");
        private static readonly GUIContent ContentRimLightNormalSource = new GUIContent("法线来源", "边缘光使用的法线来源：几何法线（较平滑）/ 法线贴图（含细节）/ 混合（两者按强度插值）。");

        /// <summary>
        /// 边缘光 法线来源
        /// </summary>
        private enum ERimLightNormalSource
        {
            /// <summary>
            /// 几何法线（顶点法线插值）
            /// </summary>
            VertexNormal,

            /// <summary>
            /// 法线贴图
            /// </summary>
            NormalMap,

            /// <summary>
            /// 混合（几何法线 ↔ 法线贴图）
            /// </summary>
            Blend
        }

        /// <summary>
        /// 关键词 边缘光 开启
        /// </summary>
        private const string MatKeywordRimLightOn = "_RIMLIGHT_ON";
        /// <summary>
        /// 关键词 边缘光 暗部遮罩 开启
        /// </summary>
        private const string MatKeywordRimLightShadeMaskOn = "_RIMLIGHT_SHADEMASK_ON";
        /// <summary>
        /// 关键词 边缘光 暗部遮罩颜色 开启
        /// </summary>
        private const string MatKeywordRimLightShadeMaskColorOn = "_RIMLIGHT_SHADEMASK_COLOR_ON";
        /// <summary>
        /// 关键词 边缘光 遮罩贴图 开启
        /// </summary>
        private const string MatKeywordRimLightMaskMapOn = "_RIMLIGHT_MASKMAP_ON";

        /// <summary>
        /// 主面板-边缘光
        /// </summary>
        private void PanelMainRimLight()
        {
            //条目 边缘光开关
            var matPropToggleRimLight = GetMaterialProperty("_ToggleRimLight");
            EditorGUIx.SwitchButton("边缘光-主开关", matPropToggleRimLight);
            //多选编辑：按各材质自身开关值同步关键词
            ApplyKeyword(MatKeywordRimLightOn, "_ToggleRimLight");
            //未开启 不显示详细设置
            if (!matPropToggleRimLight.floatValue.Equals(1))
                return;

            EditorGUIx.LabelItem("边缘光 设置");
            //条目 颜色
            MaterialEditor.ColorProperty(GetMaterialProperty("_ColorRimLightColor"), "颜色");
            //条目 强度
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightIntensity"), "强度");
            //条目 内遮罩大小
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightInsideDistance"), "内部距离");
            //条目
            EditorGUIx.SwitchButton("硬边缘", GetMaterialProperty("_ToggleRimLightHard"));

            //条目 法线来源（边缘光专属：几何法线 / 法线贴图 / 混合）
            var matPropRimLightNormalSource = GetMaterialProperty("_FloatRimLightNormalSource");
            EditorGUIx.DropdownEnum(ContentRimLightNormalSource, matPropRimLightNormalSource, typeof(ERimLightNormalSource), MaterialEditor);
            //仅“混合”模式显示 几何↔法线贴图 的混合强度滑条
            if (matPropRimLightNormalSource.floatValue.Equals((float)ERimLightNormalSource.Blend))
            {
                EditorGUI.indentLevel++;
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightNormalMapBlend"), "| 混合强度");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            //子面板 暗部遮罩
            EditorGUIx.FoldoutPanel("暗部遮罩", () =>
            {
                //条目 暗部遮罩
                var matPropToggleRimLightShadeMask = GetMaterialProperty("_ToggleRimLightShadeMask");
                EditorGUIx.SwitchButton(ContentRimLightShadeMask, matPropToggleRimLightShadeMask);
                //多选编辑：按各材质自身开关值同步关键词（暗部颜色关键词在Shader中嵌套于暗部遮罩内，未开遮罩时无副作用）
                ApplyKeyword(MatKeywordRimLightShadeMaskOn, "_ToggleRimLightShadeMask");
                ApplyKeyword(MatKeywordRimLightShadeMaskColorOn, "_ToggleRimLightShadeColor");

                //暗部遮罩开关折叠
                if (matPropToggleRimLightShadeMask.floatValue.Equals(1))
                {
                    //条目 遮罩强度
                    MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightShadeMaskIntensity"), "遮罩强度");
                    MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightShadeMaskOffset"), "遮罩偏移");

                    //条目 暗部颜色开关
                    var matPropToggleRimLightShadeColor = GetMaterialProperty("_ToggleRimLightShadeColor");
                    EditorGUIx.SwitchButton("暗部颜色", matPropToggleRimLightShadeColor);
                    //暗部颜色开关折叠
                    if (matPropToggleRimLightShadeColor.floatValue.Equals(1))
                    {
                        EditorGUI.indentLevel++;

                        //条目 颜色
                        MaterialEditor.ColorProperty(GetMaterialProperty("_ColorRimLightShadeColor"), "| 颜色");
                        //条目 强度
                        MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightShadeColorIntensity"), "| 强度");
                        //条目 硬边缘
                        EditorGUIx.SwitchButton("| 硬边缘", GetMaterialProperty("_ToggleRimLightShadeColorHard"));

                        EditorGUI.indentLevel--;
                    }
                }

                EditorGUILayout.Space();
            }
            , EditorGUIx.EFoldoutStyleType.Sub);

            //子面板 遮罩贴图
            EditorGUIx.FoldoutPanel("遮罩贴图", () =>
            {
                EditorGUIx.LabelItem(new GUIContent("遮罩绘制边缘光","绘制所有UV位置的边缘光遮罩，值越大边缘光越亮。"));
                //条目 边缘光遮罩贴图
                var matPropTexRimLightMaskMap = GetMaterialProperty("_TexRimLightMaskMap");
                MaterialEditor.TexturePropertySingleLine(ContentRimLightMaskTex, matPropTexRimLightMaskMap);
                MaterialEditor.TextureScaleOffsetProperty(matPropTexRimLightMaskMap);
                //设置 关键词（多选编辑：按各材质是否指定遮罩贴图同步）
                ApplyKeywordByTexture(MatKeywordRimLightMaskMapOn, "_TexRimLightMaskMap");

                //条目 边缘光遮罩强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRimLightMaskMapIntensity"), "遮罩强度");
            }
            , EditorGUIx.EFoldoutStyleType.Sub);
        }
        #endregion
        
        #region Lighting Setting 光照设置
        private static readonly GUIContent ContentGlobalLightGIIntensity = new GUIContent("光照强度", "环境光照强度 : 例如“光照探针”的影响强度。");
        private static readonly GUIContent ContentLightHorLock = new GUIContent("光照水平锁定", "将光照的高度锁定至水平，使暗部在水平轴向进行变化。");
        private static readonly GUIContent ContentLightExposure = new GUIContent("曝光设置", "设定全局曝光强度、局部曝光强度。");

        /// <summary>
        /// 主面板 光照设置
        /// </summary>
        private void PanelMainGlobalLight()
        {
            //条目 全局光照强度
            EditorGUIx.LabelItem(ContentGlobalLightGIIntensity);
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatRealtimeLightIntensity"), "实时光照强度");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatEnvLightIntensity"), "环境光照强度");
            EditorGUILayout.Space();

            //曝光设置
            EditorGUIx.LabelItem(ContentLightExposure);
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatGlobalExposureIntensity"), "全局曝光强度");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBaseMapExposureIntensity"), "基础贴图亮部曝光强度");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBaseMapShade1ExposureIntensity"), "基础贴图暗部1曝光强度");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBaseMapShade2ExposureIntensity"), "基础贴图暗部2曝光强度");
            EditorGUILayout.Space();

            //子面板 附加光照设置
            EditorGUIx.FoldoutPanel("附加光照", PanelSubAddLightSetting, EditorGUIx.EFoldoutStyleType.Sub);
            //子面板 光照开关
            EditorGUIx.FoldoutPanel("光照开关", PanelSubGlobalLightToggle, EditorGUIx.EFoldoutStyleType.Sub);
            //子面板 阴影接收
            EditorGUIx.FoldoutPanel("阴影设置", PanelSubShadowReceive, EditorGUIx.EFoldoutStyleType.Sub);
            //子面板 内置光照
            EditorGUIx.FoldoutPanel("内置光照", PanelSubBuiltInLight, EditorGUIx.EFoldoutStyleType.Sub);
            //子面板 光照方向锁定
            EditorGUIx.FoldoutPanel("光照方向锁定", PanelSubLightDirLock, EditorGUIx.EFoldoutStyleType.Sub);

            EditorGUILayout.Space();
        }

        #region 子面板 附加光照设置
        /// <summary>
        /// 关键词 附加光照 开启
        /// </summary>
        private const string MatKeywordAddLightOn = "_ADDLIGHT_ON";

        /// <summary>
        /// 子界面 附加光照设置
        /// </summary>
        private void PanelSubAddLightSetting()
        {
            //条目 自发光主开关
            var matPropToggleAddLight = GetMaterialProperty("_ToggleAddLight");
            EditorGUIx.SwitchButton("附加光照-主开关", matPropToggleAddLight);
            //多选编辑：按各材质自身开关值同步关键词
            ApplyKeyword(MatKeywordAddLightOn, "_ToggleAddLight");
            if (!matPropToggleAddLight.floatValue.Equals(1))
                return;

            //条目 强度
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatAddLightIntensity"), "强度");
            EditorGUILayout.Space();
        }
        #endregion

        #region 子面板 光照开关
        /// <summary>
        /// 子面板 光照开关
        /// </summary>
        private void PanelSubGlobalLightToggle()
        {
            EditorGUIx.LabelItem(new GUIContent("设置受到光照影响的项目", "开启后，光照颜色会影响最终呈现的颜色效果。"));
            
            //基础贴图受光照影响开关及混合强度
            this.SwitchButtonAndSubFloat(
                "基础贴图", GetMaterialProperty("_ToggleGlobalLightBaseMap"),
                "强度", GetMaterialProperty("_GlobalLightBaseMapMixedIntensity"));
            this.SwitchButtonAndSubFloat(
                "暗部贴图1", GetMaterialProperty("_ToggleGlobalLightBaseShade1"),
                "强度", GetMaterialProperty("_GlobalLightBaseShade1MixedIntensity"));
            this.SwitchButtonAndSubFloat(
                "暗部贴图2", GetMaterialProperty("_ToggleGlobalLightBaseShade2"),
                "强度", GetMaterialProperty("_GlobalLightBaseShade2MixedIntensity"));
            
            this.SwitchButtonAndSubFloat(
                "边缘光", GetMaterialProperty("_ToggleGlobalLightRimLight"),
                "强度", GetMaterialProperty("_GlobalLightRimLightMixedIntensity"));
            this.SwitchButtonAndSubFloat(
                "暗部边缘光", GetMaterialProperty("_ToggleGlobalLightRimLightShade"),
                "强度", GetMaterialProperty("_GlobalLightRimLightShadeMixedIntensity"));

            //描边受光照与阴影影响（阴影部分同时受“阴影设置”里的阴影接收开关控制）
            this.SwitchButtonAndSubFloat(
                "描边", GetMaterialProperty("_ToggleGlobalLightOutline"),
                "强度", GetMaterialProperty("_GlobalLightOutlineMixedIntensity"));
        }
        #endregion

        #region 子面板 阴影设置
        /// <summary>
        /// 通道名称 阴影投射
        /// </summary>
        private const string MatPassNameShadowCaster = "ShadowCaster";

        private static readonly GUIContent ContentGlobalLightShadowCaster = new GUIContent("阴影投射", "向场景投射阴影，可通过“透明度裁切阈值”来调整半透明物体的投影效果。");
        private static readonly GUIContent ContentGlobalLightShadowReceive = new GUIContent("阴影接收", "接收场景的阴影，可通过“强度调整”来改变阴影的最终效果。");
        private static readonly GUIContent ContentShadowTerminatorSmooth = new GUIContent("交界柔化", "关=直接使用阴影图(原始行为)：投射阴影覆盖全部区域，但交界可能有阴影图分辨率锯齿。\n开=几何平滑自阴影与阴影图取“较暗者”融合：自阴影与接收阴影完全融合、不产生亮缝；交界由平滑几何主导(消锯齿)，更暗的投射阴影仍能穿透。“柔化值”越大交界越平滑，越小交界越锐、投射阴影越贴交界。");

        /// <summary>
        /// 子界面 阴影接收
        /// </summary>
        private void PanelSubShadowReceive()
        {
            //条目 阴影投射开关（多选编辑：点击时作用到全部选中材质）
            EditorGUIx.SwitchButtonPass(ContentGlobalLightShadowCaster, Materials, MatPassNameShadowCaster);

            //条目 阴影接收开关
            this.SwitchButtonAndSubFloat(
                ContentGlobalLightShadowReceive, GetMaterialProperty("_ToggleShadowReceive"),
                "强度", GetMaterialProperty("_FloatShadowIntensity"));
            //条目 交界柔化开关 + 柔化值（关=直接用阴影图/原始行为；开=几何平滑自阴影与阴影图min融合，消锯齿且无亮缝）
            this.SwitchButtonAndSubFloat(
                ContentShadowTerminatorSmooth, GetMaterialProperty("_ToggleShadowTerminatorSmooth"),
                "柔化值", GetMaterialProperty("_FloatShadowTerminatorSmooth"));
            EditorGUILayout.Space();
        }
        #endregion

        #region 子面板 内置光照
        private static readonly GUIContent ContentGlobalLightBuiltInLight = new GUIContent("内置光照", "材质球专属的内置光照，开启内置光照时，场景光照将会失效。");
        private static readonly GUIContent ContentGlobalLightBuiltInLightColor = new GUIContent("内置光照颜色", "为内置光照设置单独的颜色，不启用时默认使用当前环境光照颜色。");

        /// <summary>
        /// 关键词 内置光照
        /// </summary>
        private const string MatKeywordBuiltInLight = "_BUILTINLIGHT_ON";

        /// <summary>
        /// 子界面 内置光照
        /// </summary>
        private void PanelSubBuiltInLight()
        {
            //条目 内置光照开关
            var matPropToggleBuiltInLight = GetMaterialProperty("_ToggleBuiltInLight");
            EditorGUIx.SwitchButton(ContentGlobalLightBuiltInLight, matPropToggleBuiltInLight);
            //多选编辑：按各材质自身开关值同步关键词
            ApplyKeyword(MatKeywordBuiltInLight, "_ToggleBuiltInLight");
            //内置光照开关折叠
            if (!matPropToggleBuiltInLight.floatValue.Equals(1))
                return;

            //条目 X轴位置 Y轴位置 Z轴位置
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBuiltInLightAxisX"), "X轴位置");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBuiltInLightAxisY"), "Y轴位置");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBuiltInLightAxisZ"), "Z轴位置");
            MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBuiltInLightDirBlend"), "方向混合强度");
            //条目 内置光照颜色
            var matPropToggleBuiltInLightColor = GetMaterialProperty("_ToggleBuiltInLightColor");
            EditorGUIx.SwitchButton(ContentGlobalLightBuiltInLightColor, matPropToggleBuiltInLightColor);
            //内置光照开关折叠
            if (matPropToggleBuiltInLightColor.floatValue.Equals(1))
            {
                EditorGUI.indentLevel++;
                //条目 内置光照颜色
                MaterialEditor.ColorProperty(GetMaterialProperty("_ColorBuiltInLightColor"), "光照颜色");
                //条目 内置光照强度
                MaterialEditor.RangeProperty(GetMaterialProperty("_FloatBuiltInLightColorBlend"), "混合强度");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }
        #endregion

        #region 子面板 光照方向锁定
        /// <summary>
        /// 子界面 点光源设置
        /// </summary>
        private void PanelSubLightDirLock()
        {
            //光照方向锁定
            EditorGUIx.LabelItem(ContentLightHorLock);
            EditorGUIx.SwitchButton("基础贴图", GetMaterialProperty("_ToggleLightHorLockBaseMap"));
            EditorGUIx.SwitchButton("边缘光暗部遮罩", GetMaterialProperty("_ToggleLightHorLockRimLight"));
            EditorGUILayout.Space();
        }
        #endregion
        #endregion
    }
}
