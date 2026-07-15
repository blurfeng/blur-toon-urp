using UnityEditor;
using UnityEngine;

namespace SmoothNormalTool
{
    /// <summary>
    /// 描边 Shader 的自定义材质 Inspector 界面。
    /// </summary>
    public class OutlineShaderGUI : ShaderGUI
    {
        private static readonly Color ColorAccent = new Color(0.33f, 0.78f, 1f);

        public override void OnGUI(MaterialEditor matEditor, MaterialProperty[] props)
        {
            var mat = matEditor.target as Material;
            if (!mat) return;

            EditorGUILayout.Space(4);
            DrawHeader("基础设置");
            DrawProp(matEditor, props, "_BaseColor",  "基础颜色");
            DrawProp(matEditor, props, "_MainTex",    "贴图");

            EditorGUILayout.Space(8);
            DrawHeader("描边设置");
            DrawProp(matEditor, props, "_OutlineColor", "描边颜色");
            DrawProp(matEditor, props, "_OutlineWidth",  "描边宽度");

            EditorGUILayout.Space(8);
            DrawHeader("平滑法线来源");
            DrawProp(matEditor, props, "_SmoothNormalSrc", "存储通道");

            EditorGUILayout.Space(4);
            var srcProp = FindProperty("_SmoothNormalSrc", props);
            DrawSourceHint((int)srcProp.floatValue);

            EditorGUILayout.Space(8);
            matEditor.RenderQueueField();
        }

        private void DrawHeader(string title)
        {
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal   = { textColor = ColorAccent },
            };
            EditorGUILayout.LabelField($"── {title}", style);
        }

        private void DrawProp(MaterialEditor me, MaterialProperty[] props, string name, string label)
        {
            var prop = FindProperty(name, props, false);
            if (prop != null)
                me.ShaderProperty(prop, label);
        }

        private void DrawSourceHint(int mode)
        {
            string hint = mode switch
            {
                0 => "读取顶点色 B 通道 (X) 和 A 通道 (Y)，Z 分量由 XY 重建。",
                1 => "读取 tangent.xyz 中存储的切线空间平滑法线，自动转换回对象空间。",
                2 => "读取 UV2 (TEXCOORD1) 的 xy 存储的平滑法线。",
                3 => "读取 UV3 (TEXCOORD2) 的 xy 存储的平滑法线。",
                4 => "读取 UV4 (TEXCOORD3) 的 xy 存储的平滑法线。",
                5 => "读取 UV5 (TEXCOORD3.zw) —— 注意：与UV4共享寄存器。",
                _ => "未知模式"
            };
            EditorGUILayout.HelpBox(hint, MessageType.Info);
        }
    }
}
