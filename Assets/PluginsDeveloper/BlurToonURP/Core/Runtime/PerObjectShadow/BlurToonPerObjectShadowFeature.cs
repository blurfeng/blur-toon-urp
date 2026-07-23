using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BlurToonURP
{
    /// <summary>
    /// 逐对象阴影 Renderer Feature。
    ///
    /// 用途：解决角色在 URP 级联阴影图里纹素密度过低导致的两个问题——
    /// 1) NPR 的硬边暴露出阴影图纹素阶梯；
    /// 2) 光源或相机移动时纹素栅格重新量化，边缘逐帧跳变。
    ///
    /// 做法：给每个挂了 BlurToonPerObjectShadowCaster 的对象分配一块紧贴其包围盒的高分辨率瓦片。
    /// 瓦片视锥朝光源方向拉长，因此同时包含对象自身与场景遮挡物，角色只采样这一张图即可。
    ///
    /// 使用步骤：
    /// 1) 把本 Feature 添加到 URP Renderer 资产上（本工程为 Assets/Settings/URP-HighFidelity-Renderer.asset）；
    /// 2) 给角色根节点挂 BlurToonPerObjectShadowCaster；
    /// 3) 确认主光的 Shadow Type 不是 No Shadows（本 Feature 依赖主光的投射者剔除结果）。
    ///
    /// 注意：角色材质上的“阴影投射”开关同时控制它是否进入本图集——关闭后该角色不会有自阴影。
    /// </summary>
    [DisallowMultipleRendererFeature("BlurToon Per Object Shadow")]
    public class BlurToonPerObjectShadowFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private BlurToonPerObjectShadowSettings m_Settings = new BlurToonPerObjectShadowSettings();

        private BlurToonPerObjectShadowPass m_Pass;

        public BlurToonPerObjectShadowSettings Settings => m_Settings;

        public override void Create()
        {
            m_Pass ??= new BlurToonPerObjectShadowPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (m_Pass == null)
                return;

            //只对游戏/场景视图相机生效，反射探针与预览相机不需要逐对象阴影
            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                return;

            //即使本帧没有投射者也要入队：Pass 需要负责关闭着色器关键词，
            //否则关键词会停留在上一帧的开启状态，着色器继续采样已失效的图集。
            m_Pass.Setup(m_Settings, ref renderingData);
            renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            m_Pass = null;
        }
    }
}
