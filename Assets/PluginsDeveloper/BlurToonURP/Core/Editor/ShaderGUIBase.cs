using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace BlurToonURP.EditorGUIx
{
    public class ShaderGUIBase : ShaderGUI
    {
        /// <summary>
        /// 当前的材质球编辑器
        /// </summary>
        public MaterialEditor MaterialEditor {  get; private set; }

        /// <summary>
        /// 当前的材质球（多选时为激活的那一个，仅用于读取状态/驱动界面显示）
        /// </summary>
        protected Material Material { get; private set; }

        /// <summary>
        /// 当前所有被编辑的材质球（支持多选编辑）。
        /// 写入关键词/Pass开关时应作用到此数组的全部材质，而不是只作用于 <see cref="Material"/>。
        /// </summary>
        protected Material[] Materials { get; private set; }

        private MaterialProperty[] _materialProperties;
        private readonly Dictionary<string, MaterialProperty> _materialPropertyDic = new Dictionary<string, MaterialProperty>();

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            //base.OnGUI(materialEditor, properties);

            EditorGUIUtility.fieldWidth = 0f;

            //获取材质球及属性列表
            MaterialEditor = materialEditor;
            Material = materialEditor.target as Material;
            //收集所有选中的材质球，用于多选编辑时同步关键词与Pass开关
            var targets = materialEditor.targets;
            Materials = new Material[targets.Length];
            for (int i = 0; i < targets.Length; i++)
                Materials[i] = targets[i] as Material;
            RefreshMatProperty(properties);

            //无条件同步关键词/Pass映射：不依赖折叠面板是否展开。
            //否则新建材质在面板折叠状态下，“界面开关(默认开)”与“关键词(默认关)”不一致，特性会静默失效，需手动展开面板才被“治好”。
            SyncKeywords();

            EditorGUI.BeginChangeCheck();

            OnGUIDraw();

            if (EditorGUI.EndChangeCheck())
                materialEditor.PropertiesChanged();
        }

        /// <summary>
        /// 子类重写此方法用于绘制GUI
        /// </summary>
        protected virtual void OnGUIDraw()
        {

        }

        /// <summary>
        /// 子类重写：无条件同步材质的“关键词/Pass 关键词”映射。
        /// <para>每次 OnGUI 都会调用，与各折叠面板内部的同步调用等价且幂等；
        /// 用于修复“关键词同步只在对应折叠面板展开时才执行”，导致新建/未展开材质开关与关键词不一致的问题。</para>
        /// </summary>
        protected virtual void SyncKeywords()
        {

        }

        /// <summary>
        /// 刷新材质球属性列表并重建查找用的Dic。
        /// <para>必须“每次 OnGUI 都刷新”：Unity 每帧会重新生成反映当前材质数值的 MaterialProperty 快照，
        /// 若只在首帧缓存旧快照，则 Undo/Redo 后材质数据虽已回退，但界面读取的仍是旧快照，
        /// 导致 Inspector 显示不刷新（重开 Inspector 会新建 ShaderGUI 实例才恢复）。</para>
        /// </summary>
        /// <param name="properties"></param>
        private void RefreshMatProperty(MaterialProperty[] properties)
        {
            _materialProperties = properties;
            if (_materialProperties == null) return;

            _materialPropertyDic.Clear();
            for (int i = 0; i < _materialProperties.Length; i++)
            {
                var item = _materialProperties[i];
                _materialPropertyDic[item.name] = item;
            }
        }

        /// <summary>
        /// 获取材质球属性，优先从Dic获取。
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected MaterialProperty GetMaterialProperty(string name)
        {
            if (!_materialPropertyDic.TryGetValue(name, out MaterialProperty matP))
            {
                matP = FindProperty(name, _materialProperties);
                if (matP == null)
                    Debug.LogError($"BlurToonURP ShaderGUIBase.GetMaterialProperty() Error! can't find MaterialProperty by name : {name}");
                else
                    _materialPropertyDic.Add(name, matP);
            }

            return matP;
        }

        #region 多选编辑辅助方法（关键词/Pass 需手动作用到全部选中材质）

        /// <summary>
        /// 按“每个材质自身的开关浮点属性值”将关键词应用到所有选中材质。
        /// 支持多选批量设置，同时保留各材质自身的差异（不会用激活材质覆盖其它材质）。
        /// </summary>
        /// <param name="keyword">要开关的关键词</param>
        /// <param name="toggleFloatProperty">驱动该关键词的浮点属性名（如 "_ToggleRimLight"）</param>
        /// <param name="onValue">开启对应的属性值，默认 1</param>
        protected void ApplyKeyword(string keyword, string toggleFloatProperty, float onValue = 1f)
        {
            if (Materials == null) return;
            foreach (var m in Materials)
            {
                if (m == null) continue;
                bool on = m.HasProperty(toggleFloatProperty) && Mathf.Approximately(m.GetFloat(toggleFloatProperty), onValue);
                if (on) m.EnableKeyword(keyword);
                else m.DisableKeyword(keyword);
            }
        }

        /// <summary>
        /// 按“每个材质自身的Pass开关状态”将关键词应用到所有选中材质。
        /// </summary>
        protected void ApplyKeywordByPass(string keyword, string passName)
        {
            if (Materials == null) return;
            foreach (var m in Materials)
            {
                if (m == null) continue;
                if (m.GetShaderPassEnabled(passName)) m.EnableKeyword(keyword);
                else m.DisableKeyword(keyword);
            }
        }

        /// <summary>
        /// 按“每个材质自身是否指定了该贴图”将关键词应用到所有选中材质。
        /// </summary>
        protected void ApplyKeywordByTexture(string keyword, string textureProperty)
        {
            if (Materials == null) return;
            foreach (var m in Materials)
            {
                if (m == null) continue;
                if (m.HasProperty(textureProperty) && m.GetTexture(textureProperty) != null) m.EnableKeyword(keyword);
                else m.DisableKeyword(keyword);
            }
        }

        #endregion
    }
}