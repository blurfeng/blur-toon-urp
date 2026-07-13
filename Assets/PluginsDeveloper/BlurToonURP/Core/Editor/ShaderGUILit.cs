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

            EditorGUIx.FoldoutPanel("【BaseMap 基础贴图】基础贴图及暗部贴图", PanelMainBasicMap);
            EditorGUIx.FoldoutPanel("【NormalMap 法线贴图】强度、效果开关", PanelMainNormalMap);
            EditorGUIx.FoldoutPanel("【HighLight 镜面高光】高光颜色、大小、遮罩", PanelMainHighLight);
            EditorGUIx.FoldoutPanel("【Outline 外描边】粗细、颜色", PanelMainOutline);
            EditorGUIx.FoldoutPanel("【RimLight 边缘光】颜色、大小、遮罩", PanelMainRimLight);
            EditorGUIx.FoldoutPanel("【LightSetting 光照设置】光照开关、光照强度", PanelMainGlobalLight);
            
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
            EditorGUIx.SwitchButton("边缘光", GetMaterialProperty("_ToggleNormalMapOnRimLight"));
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
        }
        #endregion
        
        #region 主面板-边缘光
        private static readonly GUIContent ContentRimLightShadeMask = new GUIContent("暗部遮罩", "对“主光源反方向”的“边缘光”进行遮罩");
        private static readonly GUIContent ContentRimLightMaskTex = new GUIContent("遮罩贴图", "在遮罩贴图中绘制边缘光的分布与强度，uv坐标与基础贴图相同。");

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
