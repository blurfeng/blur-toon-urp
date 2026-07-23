using UnityEditor;
using UnityEngine;

namespace BlurToonURP.EditorGUIx
{
    /// <summary>
    /// 逐对象阴影投射者的检视面板。
    ///
    /// 除了默认属性外，额外显示“登记状态”——投射者是通过静态表向渲染 Feature 登记的，
    /// 一旦登记失败（组件未启用、处于预制体资产中、域重载异常等）整套功能会静默失效、
    /// 没有任何报错。把状态直接摆在面板上，避免再次出现“看起来配好了但就是不生效”。
    /// </summary>
    [CustomEditor(typeof(BlurToonPerObjectShadowCaster))]
    [CanEditMultipleObjects]
    public class BlurToonPerObjectShadowCasterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            var caster = (BlurToonPerObjectShadowCaster)target;
            bool registered = false;
            var activeCasters = BlurToonPerObjectShadowCaster.ActiveCasters;
            for (int i = 0; i < activeCasters.Count; i++)
            {
                if (ReferenceEquals(activeCasters[i], caster))
                {
                    registered = true;
                    break;
                }
            }

            if (registered)
            {
                EditorGUILayout.HelpBox(
                    $"已登记。当前场景共 {activeCasters.Count} 个已启用的投射者。\n" +
                    "若阴影仍不生效，检查：URP Renderer 上是否添加了 BlurToon Per Object Shadow；主光 Shadow Type 是否为 No Shadows；材质的“阴影投射”是否被关闭。",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "未登记，逐对象阴影对本对象不会生效。\n" +
                    "常见原因：组件或所在物体被禁用；当前正在编辑预制体资产而非场景实例。\n" +
                    "可先尝试禁用后重新启用本组件。",
                    MessageType.Warning);
            }

            if (!caster.TryGetWorldBoundingSphere(out Vector3 center, out float radius))
            {
                EditorGUILayout.HelpBox(
                    "找不到有效的渲染器，无法计算包围球。请确认子层级下存在已启用的 Renderer，" +
                    "或在上方手动指定渲染器列表。",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField("包围球",
                    $"中心 {center.ToString("F2")}   半径 {radius:F2} m");
            }
        }
    }
}
