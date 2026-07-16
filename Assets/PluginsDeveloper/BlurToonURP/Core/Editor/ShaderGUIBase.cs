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
            //逐个选中材质做 per-material 同步（支持多选编辑）；与 ValidateMaterial 复用同一逻辑。
            if (Materials != null)
                foreach (var m in Materials)
                    if (m != null) SyncMaterialKeywords(m);

            EditorGUI.BeginChangeCheck();

            OnGUIDraw();

            if (EditorGUI.EndChangeCheck())
                materialEditor.PropertiesChanged();
        }

        /// <summary>
        /// Unity 在“材质被创建/加载/属性变更”时回调，即使从未打开该材质的 Inspector 也会触发。
        /// <para>在此无条件重放“关键词 / Pass 关键词”映射，修复原先“只有打开并刷新 Inspector 才会同步关键词”的问题：
        /// 脚本创建、批量导入、AssetDatabase 复制或直接改属性得到的材质，其关键词也能与开关属性保持一致，
        /// 无需手动逐个点开材质面板“治好”。与 OnGUI 复用同一套 per-material 同步逻辑（<see cref="SyncMaterialKeywords"/>）。</para>
        /// </summary>
        public override void ValidateMaterial(Material material)
        {
            base.ValidateMaterial(material);
            SyncMaterialKeywords(material);
        }

        /// <summary>
        /// 子类重写此方法用于绘制GUI
        /// </summary>
        protected virtual void OnGUIDraw()
        {

        }

        /// <summary>
        /// 子类重写：把单个材质的“开关/类型/贴图”属性无条件同步为对应的“关键词 / Pass 关键词”映射。
        /// <para>OnGUI（逐个选中材质）与 ValidateMaterial（Unity 回调的单个材质）都会调用，须幂等，
        /// 且只依赖传入的 <paramref name="material"/> 自身状态、不读取 GUI 上下文，以便无 Inspector 时也能正确执行。
        /// 用于修复“关键词同步只在折叠面板展开时才执行 / 只在打开 Inspector 时才执行”导致的开关与关键词不一致。</para>
        /// </summary>
        protected virtual void SyncMaterialKeywords(Material material)
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

        #region 关键词/Pass 同步辅助方法

        //--- 单材质版：只依赖传入材质自身状态，OnGUI 与 ValidateMaterial 共用，务必保持幂等 ---

        /// <summary>
        /// 按材质自身“开关浮点属性值”启用/禁用关键词。
        /// </summary>
        /// <param name="material">目标材质</param>
        /// <param name="keyword">要开关的关键词</param>
        /// <param name="toggleFloatProperty">驱动该关键词的浮点属性名（如 "_ToggleRimLight"）</param>
        /// <param name="onValue">开启对应的属性值，默认 1</param>
        protected static void SetKeyword(Material material, string keyword, string toggleFloatProperty, float onValue = 1f)
        {
            if (material == null) return;
            bool on = material.HasProperty(toggleFloatProperty) && Mathf.Approximately(material.GetFloat(toggleFloatProperty), onValue);
            if (on) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        /// <summary>
        /// 按材质自身“Pass 开关状态”启用/禁用关键词。
        /// </summary>
        protected static void SetKeywordByPass(Material material, string keyword, string passName)
        {
            if (material == null) return;
            if (material.GetShaderPassEnabled(passName)) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        /// <summary>
        /// 按材质自身“是否指定了该贴图”启用/禁用关键词。
        /// </summary>
        protected static void SetKeywordByTexture(Material material, string keyword, string textureProperty)
        {
            if (material == null) return;
            if (material.HasProperty(textureProperty) && material.GetTexture(textureProperty) != null) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        //--- 多选版：作用到全部选中材质，供各折叠面板即时同步用（保留各材质自身差异，不会用激活材质覆盖其它） ---

        /// <summary>
        /// 按“每个材质自身的开关浮点属性值”将关键词应用到所有选中材质。
        /// </summary>
        protected void ApplyKeyword(string keyword, string toggleFloatProperty, float onValue = 1f)
        {
            if (Materials == null) return;
            foreach (var m in Materials)
                SetKeyword(m, keyword, toggleFloatProperty, onValue);
        }

        /// <summary>
        /// 按“每个材质自身的Pass开关状态”将关键词应用到所有选中材质。
        /// </summary>
        protected void ApplyKeywordByPass(string keyword, string passName)
        {
            if (Materials == null) return;
            foreach (var m in Materials)
                SetKeywordByPass(m, keyword, passName);
        }

        /// <summary>
        /// 按“每个材质自身是否指定了该贴图”将关键词应用到所有选中材质。
        /// </summary>
        protected void ApplyKeywordByTexture(string keyword, string textureProperty)
        {
            if (Materials == null) return;
            foreach (var m in Materials)
                SetKeywordByTexture(m, keyword, textureProperty);
        }

        #endregion
    }
}