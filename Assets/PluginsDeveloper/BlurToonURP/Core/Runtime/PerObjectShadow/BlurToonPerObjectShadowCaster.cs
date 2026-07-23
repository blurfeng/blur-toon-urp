using System.Collections.Generic;
using UnityEngine;

namespace BlurToonURP
{
    /// <summary>
    /// 逐对象阴影投射者。挂在角色根节点上，向 BlurToonPerObjectShadowFeature 登记自己。
    ///
    /// 为什么需要它：
    /// URP 的级联阴影图要覆盖整个场景，角色只能分到极少的纹素。以本工程的配置为例
    /// （阴影距离 150、4 级联、图集 4096 → 每级 2048），级联 0 的包围球直径约 13~14m，
    /// 摊到 2048 只有约 6.4mm/纹素，1.8m 高的角色仅占 280 纹素左右。
    /// 角色在 1080p 上占 900px 高时，一个阴影纹素≈3 个屏幕像素 —— NPR 的硬边直接暴露成阶梯，
    /// 且光源旋转会让纹素栅格整体重新量化，边缘逐帧跳变。
    ///
    /// 本组件让该对象独占一块紧贴自身包围盒的瓦片：1024 的瓦片贴合 2.2m 的角色约 2.1mm/纹素，
    /// 密度提升 3 倍以上；配合瓦片尺寸量化与纹素吸附，角色移动时边缘也不再爬行。
    /// 硬边本身不做任何柔化，NPR 风格完全保留。
    /// </summary>
    //必须 ExecuteAlways：不加的话 OnEnable/OnDisable 只在播放模式下调用，
    //编辑模式（Scene 视图 / 未运行的 Game 视图）里投射者永远登记不上，逐对象阴影完全不生效。
    [ExecuteAlways]
    [AddComponentMenu("BlurToonURP/Per Object Shadow Caster (逐对象阴影投射者)")]
    [DisallowMultipleComponent]
    public class BlurToonPerObjectShadowCaster : MonoBehaviour
    {
        /// <summary>
        /// 当前已启用的投射者。渲染 Feature 每帧遍历此表做剔除与排序。
        /// 用静态表而不是每帧 FindObjectsOfType，避免每帧的全场景查找开销。
        /// </summary>
        private static readonly List<BlurToonPerObjectShadowCaster> s_ActiveCasters =
            new List<BlurToonPerObjectShadowCaster>();

        public static IReadOnlyList<BlurToonPerObjectShadowCaster> ActiveCasters => s_ActiveCasters;

        [SerializeField]
        [Tooltip("参与包围盒计算的渲染器。留空则在启用时自动收集子层级下的全部渲染器。")]
        private Renderer[] m_Renderers = null;

        [SerializeField]
        [Tooltip("包围盒外扩（米）。适当外扩可避免动作幅度大时肢体擦出瓦片边界导致阴影被裁掉。")]
        [Range(0f, 1f)]
        private float m_BoundsPadding = 0.1f;

        [SerializeField]
        [Tooltip("包围半径的量化步长（米）。骨骼动画会让包围盒逐帧变化，若正交投影尺寸也跟着变，" +
                 "纹素大小就会逐帧改变，纹素吸附将完全失效。把半径向上取整到该步长可锁定投影尺寸，" +
                 "是消除角色运动时阴影边缘爬行的关键。设为 0 表示不量化（不推荐）。")]
        [Range(0f, 2f)]
        private float m_RadiusQuantize = 0.25f;

        [SerializeField]
        [Tooltip("优先级偏移。同屏投射者数量超过上限时，按“距离相机近 + 处于画面中心”排序取前 N 个，" +
                 "此值直接加到排序权重上（越小越优先）。主角可设为负值以确保永远拿得到瓦片。")]
        private float m_PriorityBias = 0f;

        /// <summary>缓存的渲染器列表，避免每帧 GetComponentsInChildren 的分配。</summary>
        private Renderer[] m_CachedRenderers;

        public float PriorityBias => m_PriorityBias;

        /// <summary>
        /// 本投射者需要画进阴影瓦片的渲染器。渲染 Pass 直接按此列表逐个提交，
        /// 不依赖 Unity 的阴影投射者剔除机制——那套机制在不同光源角度下是否放行难以预测，
        /// 曾导致遮挡几何在某些角度整个消失、自投影随之丢失。
        /// </summary>
        public Renderer[] GetRenderers()
        {
            if (m_CachedRenderers == null)
                RefreshRenderers();
            return m_CachedRenderers;
        }

        /// <summary>
        /// 是否是场景中的实例。未打开的预制体资产也会收到 OnValidate，
        /// 若把它登记进来会白白占掉一块瓦片（它的渲染器根本不在场景里）。
        /// </summary>
        private bool IsSceneInstance => gameObject.scene.IsValid();

        private void Register()
        {
            if (!IsSceneInstance)
                return;

            RefreshRenderers();
            if (!s_ActiveCasters.Contains(this))
                s_ActiveCasters.Add(this);
        }

        private void Unregister()
        {
            s_ActiveCasters.Remove(this);
        }

        private void OnEnable()
        {
            Register();
        }

        private void OnDisable()
        {
            Unregister();
        }

        private void OnDestroy()
        {
            //删除组件时 OnDisable 通常会先触发，这里兜底防止静态表残留已销毁的引用
            Unregister();
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器兜底：增删组件、撤销重做、域重载等路径都可能绕过 OnEnable，
        /// OnValidate 在这些时机都会被调用，用它保证静态表与场景状态一致。
        /// </summary>
        private void OnValidate()
        {
            if (isActiveAndEnabled)
                Register();
            else
                Unregister();
        }
#endif

        /// <summary>
        /// 重新收集渲染器。运行时增删角色部件（换装等）后需要手动调用。
        /// </summary>
        public void RefreshRenderers()
        {
            m_CachedRenderers = (m_Renderers != null && m_Renderers.Length > 0)
                ? m_Renderers
                : GetComponentsInChildren<Renderer>(false);
        }

        /// <summary>
        /// 计算当前帧的世界空间包围球。
        /// 返回 false 表示没有有效渲染器，本帧应跳过该投射者。
        /// </summary>
        /// <param name="center">包围球中心（世界空间）</param>
        /// <param name="radius">包围球半径（已含外扩与量化）</param>
        public bool TryGetWorldBoundingSphere(out Vector3 center, out float radius)
        {
            center = default;
            radius = 0f;

            if (m_CachedRenderers == null || m_CachedRenderers.Length == 0)
                RefreshRenderers();

            bool hasBounds = false;
            Bounds bounds = default;

            for (int i = 0; i < m_CachedRenderers.Length; i++)
            {
                Renderer r = m_CachedRenderers[i];
                //渲染器可能在运行时被销毁或禁用；禁用的部件不该撑大包围盒
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                    continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (!hasBounds)
                return false;

            center = bounds.center;
            //用 AABB 对角线的一半作为包围球半径：保证任意光照方向下正交投影都能完整罩住对象，
            //不必随光源方向重新拟合（重新拟合会让投影尺寸随光源变化，正是要避免的抖动来源）。
            radius = bounds.extents.magnitude + m_BoundsPadding;

            //半径向上量化，锁定正交投影尺寸 → 纹素大小恒定 → 纹素吸附才有意义
            if (m_RadiusQuantize > 0f)
                radius = Mathf.Ceil(radius / m_RadiusQuantize) * m_RadiusQuantize;

            return radius > 0f;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!TryGetWorldBoundingSphere(out Vector3 center, out float radius))
                return;

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.75f);
            Gizmos.DrawWireSphere(center, radius);
        }
#endif
    }
}
