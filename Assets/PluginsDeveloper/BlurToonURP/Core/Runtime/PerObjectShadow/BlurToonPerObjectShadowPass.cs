using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BlurToonURP
{
    /// <summary>
    /// 逐对象阴影渲染 Pass。
    ///
    /// 每帧把筛选出的投射者各自渲染进图集的一块瓦片，视锥在 XY 上紧贴其包围球，
    /// 纹素密度比级联阴影高数倍，因此 NPR 的硬边不会暴露纹素阶梯。
    ///
    /// 瓦片里只画投射者自身的渲染器（逐个 cmd.DrawRenderer 提交，不走 context.DrawShadows），
    /// 所以它只提供自阴影；角色收到的场景投影仍来自 URP 级联图，两者由 CombineMode 合并。
    ///
    /// 渲染用的是材质自带的 ShadowCaster Pass，因此不需要为 Lit.shader 新增任何 Pass；
    /// 材质上关闭“阴影投射”的对象同样不会进入本图集。
    /// </summary>
    public class BlurToonPerObjectShadowPass : ScriptableRenderPass
    {
        /// <summary>与 PerObjectShadowFunction.hlsl 中的数组长度必须一致。</summary>
        public const int MaxCasterCount = 16;

        private const string k_ProfilerTag = "BlurToon Per Object Shadow";
        private const string k_KeywordName = "_BLURTOON_PER_OBJECT_SHADOW";
        /// <summary>材质中用于投射阴影的 Pass 名（与 Lit.shader 中的 Name "ShadowCaster" 一致）。</summary>
        private const string k_ShadowCasterPassName = "ShadowCaster";

        private static readonly int s_AtlasId = Shader.PropertyToID("_BlurToonPerObjShadowAtlas");
        private static readonly int s_MatricesId = Shader.PropertyToID("_BlurToonPerObjShadowMatrices");
        private static readonly int s_TilesId = Shader.PropertyToID("_BlurToonPerObjShadowTiles");
        private static readonly int s_SpheresId = Shader.PropertyToID("_BlurToonPerObjShadowSpheres");
        private static readonly int s_ParamsId = Shader.PropertyToID("_BlurToonPerObjShadowParams");
        private static readonly int s_AtlasSizeId = Shader.PropertyToID("_BlurToonPerObjShadowAtlasSize");
        private static readonly int s_DebugId = Shader.PropertyToID("_BlurToonPerObjShadowDebug");
        private static readonly int s_AtlasRawId = Shader.PropertyToID("_BlurToonPerObjShadowAtlasRaw");
        private static readonly int s_CombineId = Shader.PropertyToID("_BlurToonPerObjShadowCombine");

        //URP ShadowCaster Pass 读取的全局量
        private static readonly int s_ShadowBiasId = Shader.PropertyToID("_ShadowBias");
        private static readonly int s_LightDirectionId = Shader.PropertyToID("_LightDirection");

        private readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(k_ProfilerTag);

        /// <summary>排序委托提前缓存，避免每帧为 lambda 产生一次委托分配。</summary>
        private static readonly System.Comparison<CasterSlice> s_PriorityComparison =
            (a, b) => a.Priority.CompareTo(b.Priority);

        /// <summary>本帧入选的投射者及其派生数据。</summary>
        private struct CasterSlice
        {
            public BlurToonPerObjectShadowCaster Caster;
            public Vector3 Center;
            public float Radius;
            public float Priority;
            public Matrix4x4 ViewMatrix;
            public Matrix4x4 ProjectionMatrix;
            public Matrix4x4 ShadowMatrix;
            public Vector3 ShadowForward;
            public int TileX;
            public int TileY;
        }

        private readonly List<CasterSlice> m_Slices = new List<CasterSlice>(MaxCasterCount);
        private readonly Matrix4x4[] m_ShadowMatrices = new Matrix4x4[MaxCasterCount];
        private readonly Vector4[] m_TileRects = new Vector4[MaxCasterCount];
        /// <summary>投射者的世界包围球（xyz=中心 w=半径），供着色器计算 SceneAndSelf 的自剔除距离。</summary>
        private readonly Vector4[] m_CasterSpheres = new Vector4[MaxCasterCount];
        private readonly Plane[] m_CameraFrustumPlanes = new Plane[6];

        private RTHandle m_Atlas;
        private BlurToonPerObjectShadowSettings m_Settings;
        private int m_TileResolution;
        /// <summary>本帧图集每边的瓦片数，按实际入选的投射者数量动态划分。</summary>
        private int m_GridDimension = 1;
        private int m_AtlasResolution;
        private int m_MainLightIndex = -1;
        private bool m_HasWork;

        public BlurToonPerObjectShadowPass()
        {
            //必须早于不透明物体渲染。放在 URP 主光阴影之后，两张图互不干扰。
            renderPassEvent = RenderPassEvent.AfterRenderingShadows;
        }

        /// <summary>
        /// 每帧在 AddRenderPasses 中调用：筛选投射者并预计算矩阵。
        /// 返回 false 表示本帧无有效投射者（Pass 仍会入队，用于关闭着色器关键词）。
        /// </summary>
        public bool Setup(BlurToonPerObjectShadowSettings settings, ref RenderingData renderingData)
        {
            m_Settings = settings;
            m_Slices.Clear();
            m_HasWork = false;
            m_MainLightIndex = renderingData.lightData.mainLightIndex;

            if (m_Settings == null || m_MainLightIndex < 0)
                return false;

            //DrawShadows 依赖剔除结果中该光源的投射者列表，主光必须是投射阴影的光源
            VisibleLight mainLight = renderingData.lightData.visibleLights[m_MainLightIndex];
            if (mainLight.light == null || mainLight.light.shadows == LightShadows.None)
                return false;

            IReadOnlyList<BlurToonPerObjectShadowCaster> casters = BlurToonPerObjectShadowCaster.ActiveCasters;
            if (casters.Count == 0)
                return false;

            m_AtlasResolution = (int)m_Settings.atlasSize;
            int capacity = Mathf.Min(m_Settings.GetEffectiveCasterCapacity(), MaxCasterCount);
            if (capacity <= 0)
                return false;

            Camera camera = renderingData.cameraData.camera;
            Vector3 cameraPosition = camera.transform.position;
            Vector3 cameraForward = camera.transform.forward;
            Vector3 mainLightForward = mainLight.localToWorldMatrix.GetColumn(2);

            GeometryUtility.CalculateFrustumPlanes(camera, m_CameraFrustumPlanes);

            float maxDistanceSq = m_Settings.maxDistance * m_Settings.maxDistance;

            for (int i = 0; i < casters.Count; i++)
            {
                BlurToonPerObjectShadowCaster caster = casters[i];
                if (caster == null)
                    continue;

                if (!caster.TryGetWorldBoundingSphere(out Vector3 center, out float radius))
                    continue;

                //距离剔除：超出范围的对象回退到 URP 级联阴影
                if ((center - cameraPosition).sqrMagnitude > maxDistanceSq)
                    continue;

                //视锥剔除：画面外的对象不占用瓦片
                var bounds = new Bounds(center, Vector3.one * (radius * 2f));
                if (!GeometryUtility.TestPlanesAABB(m_CameraFrustumPlanes, bounds))
                    continue;

                var slice = new CasterSlice
                {
                    Caster = caster,
                    Center = center,
                    Radius = radius,
                    Priority = BlurToonPerObjectShadowUtils.ComputePriority(center, cameraPosition, cameraForward)
                               + caster.PriorityBias,
                };

                m_Slices.Add(slice);
            }

            if (m_Slices.Count == 0)
                return false;

            //按优先级升序，超出容量的丢弃
            m_Slices.Sort(s_PriorityComparison);
            if (m_Slices.Count > capacity)
                m_Slices.RemoveRange(capacity, m_Slices.Count - capacity);

            //网格按“本帧实际入选的投射者数量”划分，而不是按配置上限。
            //画面里只有一个角色时它就独占整张图集，纹素密度相对 Max Caster Count=4 直接翻倍，
            //这是提升有效分辨率最省事的一档，不必加大图集内存。
            //代价：入选数量跨过 1/4/16 的分界时瓦片尺寸会变，那一帧阴影质量有一次突变。
            m_GridDimension = BlurToonPerObjectShadowSettings.GetTileGridDimension(m_Slices.Count);
            m_TileResolution = m_AtlasResolution / m_GridDimension;
            if (m_TileResolution <= 0)
                return false;

            //为入选者分配瓦片并构造矩阵
            int gridDimension = m_GridDimension;
            for (int i = 0; i < m_Slices.Count; i++)
            {
                CasterSlice slice = m_Slices[i];

                slice.ShadowForward = BlurToonPerObjectShadowUtils.ComputeShadowForward(
                    m_Settings, mainLightForward, slice.Center, cameraPosition);

                BlurToonPerObjectShadowUtils.BuildMatrices(
                    slice.ShadowForward, slice.Center, slice.Radius,
                    m_Settings.casterExtrusion, m_TileResolution, m_Settings.stabilizeTexelSnapping,
                    out slice.ViewMatrix, out slice.ProjectionMatrix);

                slice.ShadowMatrix = BlurToonPerObjectShadowUtils.BuildShadowSamplingMatrix(
                    slice.ProjectionMatrix, slice.ViewMatrix);

                slice.TileX = i % gridDimension;
                slice.TileY = i / gridDimension;

                m_Slices[i] = slice;
            }

            m_HasWork = true;
            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!m_HasWork)
                return;

            ShadowUtils.ShadowRTReAllocateIfNeeded(
                ref m_Atlas, m_AtlasResolution, m_AtlasResolution, 16,
                name: "_BlurToonPerObjShadowAtlas");

            ConfigureTarget(m_Atlas);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            //无投射者：关掉关键词让着色器完全回退到 URP 阴影，零开销
            if (!m_HasWork || m_Atlas == null)
            {
                CoreUtils.SetKeyword(cmd, k_KeywordName, false);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                return;
            }

            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                //硬件光栅化深度偏移：不位移顶点，因此不会随光源方向形变 caster 轮廓，
                //阴影边界不会在表面上滑动（顶点位移式的深度偏移正是边缘滑动的来源之一）
                cmd.SetGlobalDepthBias(m_Settings.depthBias, m_Settings.slopeBias);

                int gridDimension = m_GridDimension;

                for (int i = 0; i < m_Slices.Count; i++)
                {
                    CasterSlice slice = m_Slices[i];

                    //ShadowCaster Pass 用来做法线偏移的全局量。ViewBlend 模式下阴影方向与主光方向不同，
                    //必须按本瓦片的阴影方向设置，否则内缩方向错误。
                    Vector4 shadowBias = BlurToonPerObjectShadowUtils.GetShadowBias(
                        slice.Radius, m_TileResolution, m_Settings.normalBias);
                    cmd.SetGlobalVector(s_ShadowBiasId, shadowBias);
                    //约定为“指向光源”的方向
                    Vector3 towardLight = -slice.ShadowForward;
                    cmd.SetGlobalVector(s_LightDirectionId,
                        new Vector4(towardLight.x, towardLight.y, towardLight.z, 0f));

                    cmd.SetViewport(new Rect(
                        slice.TileX * m_TileResolution, slice.TileY * m_TileResolution,
                        m_TileResolution, m_TileResolution));
                    cmd.SetViewProjectionMatrices(slice.ViewMatrix, slice.ProjectionMatrix);

                    //显式提交投射者自己的渲染器，不走 context.DrawShadows。
                    //DrawShadows 依赖 Unity 的阴影投射者剔除（剔除平面 / 剔除球 / cullingResults 中的投射者列表），
                    //这套机制在不同光源角度下是否放行难以预测也无法观测——实测会出现某些角度下遮挡几何整个没被画进瓦片，
                    //表现为自投影突然消失（而 URP 原生阴影正常）。逐渲染器提交是确定性的：要画什么就是什么。
                    //代价是瓦片里不再包含场景遮挡物，场景投影改由下方的 CombineMode 决定如何与 URP 结果合并。
                    DrawCasterRenderers(cmd, slice);

                    m_ShadowMatrices[i] = slice.ShadowMatrix;
                    //z：图集 UV 缩放（网格为正方形，xy 共用一个值）
                    //w：该瓦片一个纹素的世界尺寸，供着色器做接收端法线偏移——掠射角下必须按纹素尺度补偿
                    m_TileRects[i] = new Vector4(
                        (float)slice.TileX / gridDimension,
                        (float)slice.TileY / gridDimension,
                        1f / gridDimension,
                        (2f * slice.Radius) / m_TileResolution);

                    //用未吸附的真实包围球：自剔除要的是“角色实际占据的那块空间”，
                    //而 slice.ViewMatrix 里的中心已被纹素吸附偏移过，不能拿来当球心。
                    m_CasterSpheres[i] = new Vector4(
                        slice.Center.x, slice.Center.y, slice.Center.z, slice.Radius);
                }

                //未使用的槽位置零，着色器按 count 遍历，这里只是避免残留脏数据
                for (int i = m_Slices.Count; i < MaxCasterCount; i++)
                {
                    m_ShadowMatrices[i] = Matrix4x4.zero;
                    m_TileRects[i] = Vector4.zero;
                    m_CasterSpheres[i] = Vector4.zero;
                }

                cmd.SetGlobalDepthBias(0f, 0f);

                //恢复相机的视图/投影矩阵，避免影响后续 Pass
                cmd.SetViewProjectionMatrices(
                    renderingData.cameraData.GetViewMatrix(),
                    renderingData.cameraData.GetProjectionMatrix());

                //距离淡出：与 URP 的阴影淡出同构，atten = distance * scale + bias 后 saturate
                float fadeStart = Mathf.Max(0f, m_Settings.maxDistance - Mathf.Max(0.01f, m_Settings.fadeRange));
                float fadeScale = 1f / Mathf.Max(0.01f, m_Settings.maxDistance - fadeStart);
                float fadeBias = -fadeStart * fadeScale;

                cmd.SetGlobalTexture(s_AtlasId, m_Atlas);
                //同一张图集再以普通纹理名绑定一次，供调试模式读取原始深度（比较采样器取不回深度值本身）
                cmd.SetGlobalTexture(s_AtlasRawId, m_Atlas);
                cmd.SetGlobalMatrixArray(s_MatricesId, m_ShadowMatrices);
                cmd.SetGlobalVectorArray(s_TilesId, m_TileRects);
                cmd.SetGlobalVectorArray(s_SpheresId, m_CasterSpheres);
                cmd.SetGlobalVector(s_ParamsId, new Vector4(
                    m_Slices.Count, m_Settings.strength, fadeScale, fadeBias));
                //z：瓦片 UV 内缩量，避免比较采样跨到相邻瓦片。w：接收端法线偏移（单位为瓦片纹素）
                cmd.SetGlobalVector(s_AtlasSizeId, new Vector4(
                    1f / m_AtlasResolution, m_AtlasResolution, 1f / m_TileResolution,
                    m_Settings.receiverNormalOffset));
                cmd.SetGlobalVector(s_DebugId, new Vector4((int)m_Settings.debugMode, 0f, 0f, 0f));
                //y = SceneAndSelf 的自剔除距离倍率，着色器按“沿光线离开包围球所需距离”逐像素算出基准值再乘它
                cmd.SetGlobalVector(s_CombineId, new Vector4(
                    (int)m_Settings.combineMode, m_Settings.selfRejectScale, 0f, 0f));

                CoreUtils.SetKeyword(cmd, k_KeywordName, true);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <summary>
        /// 把一个投射者的全部渲染器画进当前瓦片，使用各自材质的 ShadowCaster Pass。
        /// 材质上关闭了“阴影投射”的部件同样不会进入瓦片，语义与场景阴影一致。
        /// </summary>
        private void DrawCasterRenderers(CommandBuffer cmd, CasterSlice slice)
        {
            if (slice.Caster == null)
                return;

            Renderer[] renderers = slice.Caster.GetRenderers();
            if (renderers == null)
                return;

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                if (renderer.shadowCastingMode == ShadowCastingMode.Off)
                    continue;

                Material[] materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;

                for (int m = 0; m < materials.Length; m++)
                {
                    Material material = materials[m];
                    if (material == null)
                        continue;

                    //材质编辑器用 SetShaderPassEnabled 控制“阴影投射”开关，这里必须尊重它
                    if (!material.GetShaderPassEnabled(k_ShadowCasterPassName))
                        continue;

                    int passIndex = material.FindPass(k_ShadowCasterPassName);
                    if (passIndex < 0)
                        continue;

                    cmd.DrawRenderer(renderer, material, m, passIndex);
                }
            }
        }

        public void Dispose()
        {
            m_Atlas?.Release();
            m_Atlas = null;
        }
    }
}
