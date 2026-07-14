using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.MessageBox;

namespace BlurToonURP.EditorGUIx
{
    public static class EditorGUIx
    {
        /// <summary>
        /// GUI默认颜色
        /// </summary>
        private static Color ColorDefault { get => GUI.backgroundColor; }
        
        #region Label 标签
        
        private static GUIStyle _boldLabel;
        private static GUIStyle BoldLabel
        {
            get
            {
                if(_boldLabel == null)
                {
                    _boldLabel = new GUIStyle(EditorStyles.boldLabel);
                    _boldLabel.padding.left = 14;
                }

                return _boldLabel;
            }
        }

        public static void LabelItem(string text)
        {
            GUILayout.Label(text, EditorGUIx.BoldLabel);
        }
        
        public static void LabelItem(GUIContent content)
        {
            GUILayout.Label(content, EditorGUIx.BoldLabel);
        }

        #endregion

        #region Switch Button 切换按钮
        
        /// <summary>
        /// GUI 开启色
        /// </summary>
        private static Color ColorOn { get; } = Color.green;

        /// <summary>
        /// GUI 关闭色
        /// </summary>
        private static Color ColorOff { get; } = Color.gray;

        private static readonly GUILayoutOption[] LayoutBtnSmall = new GUILayoutOption[] { GUILayout.Width(50) }; //小尺寸

        /// <summary>
        /// 按钮 开关切换
        /// </summary>
        /// <param name="label"></param>
        /// <param name="matProp"></param>
        public static void SwitchButton(string label, MaterialProperty matProp)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(label);
            ToggleButton(matProp);

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 按钮 开关切换
        /// </summary>
        /// <param name="label"></param>
        /// <param name="matProp"></param>
        public static void SwitchButton(GUIContent label, MaterialProperty matProp)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(label);
            ToggleButton(matProp);

            EditorGUILayout.EndHorizontal();
        }

        private static void ToggleButton(MaterialProperty matProp)
        {
            if (matProp.floatValue == 0)
            {
                GUI.color = ColorOff;
                if (GUILayout.Button("Off", LayoutBtnSmall))
                    matProp.floatValue = 1f;
                GUI.color = ColorDefault;
            }
            else
            {
                GUI.color = ColorOn;
                if (GUILayout.Button("On", LayoutBtnSmall))
                    matProp.floatValue = 0f;
                GUI.color = ColorDefault;
            }
        }

        /// <summary>
        /// 按钮 开关切换 Pass是否开启（支持多选编辑，点击时作用到全部材质）
        /// </summary>
        /// <param name="label"></param>
        /// <param name="materials">全部被编辑的材质球</param>
        /// <param name="shaderPassName"></param>
        public static void SwitchButtonPass(GUIContent label, Material[] materials, string shaderPassName)
        {
            if (materials == null || materials.Length == 0 || materials[0] == null) return;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(label);

            //以激活材质（第一个）的状态显示按钮
            if (materials[0].GetShaderPassEnabled(shaderPassName))
            {
                GUI.color = ColorOn;
                if (GUILayout.Button("On", LayoutBtnSmall))
                    foreach (var m in materials) { if (m != null) m.SetShaderPassEnabled(shaderPassName, false); }
                GUI.color = ColorDefault;
            }
            else
            {
                GUI.color = ColorOff;
                if (GUILayout.Button("Off", LayoutBtnSmall))
                    foreach (var m in materials) { if (m != null) m.SetShaderPassEnabled(shaderPassName, true); }
                GUI.color = ColorDefault;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 切换开关按钮 及一个可调节的float
        /// </summary>
        /// <param name="shaderGUI"></param>
        /// <param name="label"></param>
        /// <param name="matProp"></param>
        /// <param name="subLabel"></param>
        /// <param name="subMatProp"></param>
        public static void SwitchButtonAndSubFloat(this ShaderGUIBase shaderGUI, string label, MaterialProperty matProp, string subLabel, MaterialProperty subMatProp)
        {
            //暗部贴图1受光照影响开关及混合强度
            EditorGUIx.SwitchButton(label, matProp);
            if (matProp.floatValue.Equals(1))
            {
                EditorGUI.indentLevel++;
                shaderGUI.MaterialEditor.RangeProperty(subMatProp, subLabel);
                EditorGUI.indentLevel--;
            }
        }
        
        /// <summary>
        /// 切换开关按钮 及一个可调节的float
        /// </summary>
        /// <param name="shaderGUI"></param>
        /// <param name="label"></param>
        /// <param name="matProp"></param>
        /// <param name="subLabel"></param>
        /// <param name="subMatProp"></param>
        public static void SwitchButtonAndSubFloat(this ShaderGUIBase shaderGUI, GUIContent label, MaterialProperty matProp, string subLabel, MaterialProperty subMatProp)
        {
            //暗部贴图1受光照影响开关及混合强度
            EditorGUIx.SwitchButton(label, matProp);
            if (matProp.floatValue.Equals(1))
            {
                EditorGUI.indentLevel++;
                shaderGUI.MaterialEditor.RangeProperty(subMatProp, subLabel);
                EditorGUI.indentLevel--;
            }
        }
        #endregion
        
        #region Dropdown 下拉弹窗
        /// <summary>
        /// 下拉弹窗
        /// </summary>
        /// <param name="label"></param>
        /// <param name="property"></param>
        /// <param name="type"></param>
        /// <param name="materialEditor"></param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static void DropdownEnum(GUIContent label, MaterialProperty property, System.Type type, MaterialEditor materialEditor)
        {
            if (property == null)
            {
                LabelTextColor("下拉弹窗错误！ >> 无效的材质球属性", Color.red);
                return;
            }

            EditorGUI.showMixedValue = property.hasMixedValue;

            //下拉弹窗
            var enumNames = System.Enum.GetNames(type);
            var mode = property.floatValue;
            EditorGUI.BeginChangeCheck();
            mode = EditorGUILayout.Popup(label, (int)mode, enumNames);
            if (EditorGUI.EndChangeCheck())
            {
                //更新材质球数值
                materialEditor.RegisterPropertyChangeUndo(label.text);
                property.floatValue = mode;
            }

            EditorGUI.showMixedValue = false;
        }
        #endregion

        #region Foldout 折叠面板
        /// <summary>
        /// 折叠面板 UI风格
        /// </summary>
        public enum EFoldoutStyleType
        {
            /// <summary>
            /// 主面板
            /// </summary>
            Main,

            /// <summary>
            /// 子面板
            /// </summary>
            Sub
        }

        private static GUIStyle _foldoutMain;
        private static GUIStyle FoldoutMain
        {
            get
            {
                if(_foldoutMain == null)
                {
                    _foldoutMain = new GUIStyle("ShurikenModuleTitle");
                    _foldoutMain.font = new GUIStyle(EditorStyles.boldLabel).font;
                    _foldoutMain.border = new RectOffset(7, 7, 4, 4);
                    _foldoutMain.fixedHeight = 22;
                    _foldoutMain.contentOffset = new Vector2(20f, -2f);
                }

                return _foldoutMain;
            }
        }

        private static GUIStyle _foldoutSub;
        private static GUIStyle FoldoutSub
        {
            get
            {
                if (_foldoutSub == null)
                {
                    _foldoutSub = new GUIStyle("ShurikenModuleTitle");
                    _foldoutSub.font = new GUIStyle(EditorStyles.boldLabel).font;
                    _foldoutSub.border = new RectOffset(7, 7, 4, 4);
                    _foldoutSub.fixedHeight = 22;
                    _foldoutSub.contentOffset = new Vector2(20f, -2f);
                    //foldoutSub.padding = new RectOffset(5, 7, 4, 4);
                }

                return _foldoutSub;
            }
        }

        //折叠面板的打开状态
        private static readonly Dictionary<string, bool> FoldoutIsOpenDic = new Dictionary<string, bool>();

        /// <summary>
        /// 折叠面板
        /// </summary>
        /// <param name="title">作为记录打开状态的Key</param>
        /// <param name="panelGUI">折叠面板</param>
        /// <param name="styleType"></param>
        public static void FoldoutPanel(string title, System.Action panelGUI, EFoldoutStyleType styleType = EFoldoutStyleType.Main)
        {
            GUIStyle style;
            Rect rect;
            float toggleRectXoffset = 0f;

            //折叠面板类型
            switch (styleType)
            {
                case EFoldoutStyleType.Main:
                    style = FoldoutMain;
                    rect = GUILayoutUtility.GetRect(16f, 22f, style);
                    toggleRectXoffset = 4f;
                    break;
                case EFoldoutStyleType.Sub:
                    style = FoldoutSub;
                    rect = GUILayoutUtility.GetRect(16f, 22f, style);
                    rect.x += 14f;
                    rect.width -= 14f;
                    toggleRectXoffset = 4f;
                    break;
                default:
                    style= new GUIStyle("ShurikenModuleTitle");
                    rect = GUILayoutUtility.GetRect(16f, 22f, style);
                    break;
            }
            
            GUI.Box(rect, title, style);

            //当前面板的打开状态
            if (!FoldoutIsOpenDic.TryGetValue(title, out bool isOpen))
                FoldoutIsOpenDic.Add(title, isOpen);

            //当前事件处理
            var e = Event.current;
            if (e.type == EventType.Repaint)
            {
                var toggleRect = new Rect(rect.x + toggleRectXoffset, rect.y + 2f, 13f, 13f);
                EditorStyles.foldout.Draw(toggleRect, false, false, isOpen, false);
            }
            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                FoldoutIsOpenDic[title] = !isOpen;
                e.Use();
            }

            //是否打开面板
            if (isOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space();
                panelGUI?.Invoke();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
        #endregion
        
        #region Text 彩色文本
        /// <summary>
        /// 标签 彩色
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="style"></param>
        private static void LabelTextColor(string text, Color color, GUIStyle style = null)
        {
            if (style == null)
                style = EditorStyles.label;

            GUI.color = color;
            GUILayout.Label(text, EditorStyles.boldLabel);
            GUI.color = ColorDefault;
        }

        private static GUIStyle _errorLabel;
        private static GUIStyle ErrorLabel
        {
            get
            {
                if (_errorLabel == null)
                {
                    _errorLabel = new GUIStyle(EditorStyles.wordWrappedMiniLabel);
                    _errorLabel.fontStyle = FontStyle.Bold;
                }

                return _errorLabel;
            }
        }

        /// <summary>
        /// 红字警告提示（如：开关已开启但缺少有效贴图）。会正确保存/还原 GUI.color。
        /// </summary>
        /// <param name="text"></param>
        public static void LabelError(string text)
        {
            var prev = GUI.color;
            GUI.color = new Color(1f, 0.42f, 0.38f);
            GUILayout.Label(text, ErrorLabel);
            GUI.color = prev;
        }
        #endregion
    }
}