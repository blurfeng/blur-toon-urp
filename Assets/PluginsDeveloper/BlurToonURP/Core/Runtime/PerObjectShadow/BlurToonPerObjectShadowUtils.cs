using UnityEngine;

namespace BlurToonURP
{
    /// <summary>
    /// 逐对象阴影的矩阵构造与稳定化工具。
    /// </summary>
    public static class BlurToonPerObjectShadowUtils
    {
        /// <summary>
        /// 计算某个投射者本帧使用的阴影方向（“阴影相机”的朝向，即光线传播方向）。
        /// </summary>
        /// <param name="settings">Feature 配置</param>
        /// <param name="mainLightForward">主光的 forward（光线传播方向）</param>
        /// <param name="casterCenter">投射者包围球中心</param>
        /// <param name="cameraPosition">相机世界位置</param>
        public static Vector3 ComputeShadowForward(
            BlurToonPerObjectShadowSettings settings,
            Vector3 mainLightForward,
            Vector3 casterCenter,
            Vector3 cameraPosition)
        {
            if (settings.directionMode == BlurToonShadowDirectionMode.MainLight)
                return mainLightForward.normalized;

            //以“相机→角色”的视线为主方向。注意不用 camera.forward：
            //相机绕角色转动时 camera.forward 会变，而“相机→角色”在角色附近变化平缓，阴影方向更稳。
            Vector3 viewForward = casterCenter - cameraPosition;
            if (viewForward.sqrMagnitude < 1e-8f)
                viewForward = mainLightForward;
            viewForward.Normalize();

            //向量插值而非四元数插值：两方向接近反向时四元数插值会出现跳变
            Vector3 forward = Vector3.Lerp(viewForward, mainLightForward.normalized, settings.viewBlendToLight);
            if (forward.sqrMagnitude < 1e-8f)
                forward = viewForward;
            forward.Normalize();

            //限制俯仰：接近正上方俯视或从正下方仰视时会出现不该有的自阴影，把方向压回允许的角度区间
            float minDeg = Mathf.Min(settings.viewBlendPitchClampDegrees.x, settings.viewBlendPitchClampDegrees.y);
            float maxDeg = Mathf.Max(settings.viewBlendPitchClampDegrees.x, settings.viewBlendPitchClampDegrees.y);
            //夹角越大 cos 越小，故 max 角度对应 cos 下界
            float cosUpper = Mathf.Cos(minDeg * Mathf.Deg2Rad);
            float cosLower = Mathf.Cos(maxDeg * Mathf.Deg2Rad);

            Vector3 up = Vector3.up;
            float cosAngle = Vector3.Dot(forward, up);
            float cosClamped = Mathf.Clamp(cosAngle, cosLower, cosUpper);
            if (!Mathf.Approximately(cosClamped, cosAngle))
            {
                forward += (cosClamped - cosAngle) * up;
                if (forward.sqrMagnitude < 1e-8f)
                    forward = viewForward;
                forward.Normalize();
            }

            return forward;
        }

        /// <summary>
        /// 为一个投射者构造视图矩阵与正交投影矩阵。
        ///
        /// 视锥在 XY 上紧贴包围球（边长 = 2*radius），在 Z 上朝光源方向拉长 extrusion。
        /// 瓦片里只画投射者自身，extrusion 仅用于加大近平面余量（容纳超出包围球的长武器、披风等），
        /// 不是为了纳入场景遮挡物。
        /// </summary>
        /// <param name="shadowForward">阴影方向（光线传播方向）</param>
        /// <param name="center">包围球中心</param>
        /// <param name="radius">包围球半径（应已量化，保证逐帧尺寸稳定）</param>
        /// <param name="extrusion">朝光源方向额外拉长的距离</param>
        /// <param name="tileResolution">瓦片分辨率（用于纹素吸附）</param>
        /// <param name="snapToTexel">是否吸附到纹素栅格</param>
        public static void BuildMatrices(
            Vector3 shadowForward,
            Vector3 center,
            float radius,
            float extrusion,
            int tileResolution,
            bool snapToTexel,
            out Matrix4x4 viewMatrix,
            out Matrix4x4 projectionMatrix)
        {
            //构造阴影相机的旋转。shadowForward 接近世界上方向时换一个参考上方向，避免 LookRotation 退化。
            Vector3 up = Mathf.Abs(Vector3.Dot(shadowForward, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
            Quaternion rotation = Quaternion.LookRotation(shadowForward, up);

            //纹素吸附：把包围球中心在“阴影空间”内量化到整纹素。
            //只有当正交尺寸恒定（半径已量化）时纹素大小才恒定，吸附才能真正消除边缘爬行。
            if (snapToTexel && tileResolution > 0)
            {
                float texelWorldSize = (2f * radius) / tileResolution;
                if (texelWorldSize > 0f)
                {
                    Quaternion inverseRotation = Quaternion.Inverse(rotation);
                    Vector3 centerInShadowSpace = inverseRotation * center;
                    centerInShadowSpace.x = Mathf.Floor(centerInShadowSpace.x / texelWorldSize) * texelWorldSize;
                    centerInShadowSpace.y = Mathf.Floor(centerInShadowSpace.y / texelWorldSize) * texelWorldSize;
                    center = rotation * centerInShadowSpace;
                }
            }

            //把阴影相机沿反光方向后退，使近平面之后留出足够空间。
            //这里必须多留一个 radius 的安全余量（而不是刚好贴住包围球）：
            //URP 的 ShadowCaster Pass 会做 positionCS.z = min(z, NEAR_CLIP)（pancaking），
            //凡是落在近平面之前的几何都会被压平到近平面上并照常投影。
            //若近平面正好与包围球相切（extrusion=0 时就是如此），角色朝光源一侧的表面深度≈近平面，
            //自身被压平后会遮挡它后面的所有部分 —— 表现为整片突然变暗或深度比较在阈值上乱跳。
            //留出一个 radius 后，投射者永远不会被自己压平，extrusion=0 也成为合法配置。
            float backDistance = radius * 2f + extrusion;
            Vector3 shadowCameraPosition = center - shadowForward * backDistance;

            //Unity 的视图矩阵约定：相机看向 -Z，故 z 轴取负缩放后求逆（等价于 Camera.worldToCameraMatrix）
            viewMatrix = Matrix4x4.TRS(shadowCameraPosition, rotation, new Vector3(1f, 1f, -1f)).inverse;

            //正交投影：XY 紧贴包围球，Z 从近平面 0 到远平面（后退距离 + 包围球直径）
            float zFar = backDistance + radius * 2f;
            projectionMatrix = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0f, zFar);
        }

        /// <summary>
        /// 由视图/投影矩阵构造“世界坐标 → 阴影瓦片 [0,1]³ 坐标”的采样矩阵。
        ///
        /// 与 URP ShadowUtils.GetShadowTransform 保持一致的两处处理：
        /// 1) Matrix4x4.Ortho 产出的是未做 z 反转的投影矩阵，反转 Z 深度缓冲的平台需手动翻转第三行；
        /// 2) 追加 0.5 缩放偏移把裁剪空间 [-1,1] 映射到纹理空间 [0,1]，省掉着色器里的一次 MAD。
        ///
        /// 注意：此处不烘焙瓦片在图集中的位置，返回的是瓦片自身的 [0,1]³ 局部坐标——
        /// 着色器需要先用它做越界判定，再套用瓦片的图集 UV 变换。
        /// </summary>
        public static Matrix4x4 BuildShadowSamplingMatrix(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
        {
            Matrix4x4 proj = projectionMatrix;

            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20;
                proj.m21 = -proj.m21;
                proj.m22 = -proj.m22;
                proj.m23 = -proj.m23;
            }

            Matrix4x4 worldToShadow = proj * viewMatrix;

            Matrix4x4 textureScaleAndBias = Matrix4x4.identity;
            textureScaleAndBias.m00 = 0.5f;
            textureScaleAndBias.m11 = 0.5f;
            textureScaleAndBias.m22 = 0.5f;
            textureScaleAndBias.m03 = 0.5f;
            textureScaleAndBias.m13 = 0.5f;
            textureScaleAndBias.m23 = 0.5f;

            return textureScaleAndBias * worldToShadow;
        }

        /// <summary>
        /// 计算 URP ShadowCaster Pass 所需的 _ShadowBias 向量。
        ///
        /// 与 URP ShadowUtils.GetShadowBias 同一套换算：偏移量以“阴影图纹素在世界空间的大小”为单位。
        /// 本实现只使用法线偏移（y 分量）；深度偏移交给硬件光栅化偏移（SetGlobalDepthBias），
        /// 因为顶点位移式的深度偏移会随光源方向改变 caster 轮廓，正是要避免的边缘滑动来源。
        /// </summary>
        /// <param name="radius">包围球半径（正交视锥半宽）</param>
        /// <param name="tileResolution">瓦片分辨率</param>
        /// <param name="normalBias">法线偏移，单位为纹素</param>
        public static Vector4 GetShadowBias(float radius, int tileResolution, float normalBias)
        {
            if (tileResolution <= 0)
                return Vector4.zero;

            //正交视锥是正方形，边长即 2*radius
            float frustumSize = 2f * radius;
            float texelSize = frustumSize / tileResolution;

            //URP 约定偏移为负值（沿光方向推入 / 沿法线内缩）
            return new Vector4(0f, -normalBias * texelSize, 0f, 0f);
        }

        /// <summary>
        /// 计算投射者的排序权重，越小越优先。
        /// 距离相机越近、越靠近画面中心的投射者优先获得瓦片。
        /// </summary>
        public static float ComputePriority(Vector3 casterCenter, Vector3 cameraPosition, Vector3 cameraForward)
        {
            Vector3 toCaster = casterCenter - cameraPosition;
            float distanceSq = toCaster.sqrMagnitude;

            Vector3 direction = toCaster.sqrMagnitude > 1e-8f ? toCaster.normalized : cameraForward;
            float cosAngle = Vector3.Dot(cameraForward, direction);

            //距离项归一化到 100m 量级，与“偏离画面中心”项（0~1）量级相当
            return Mathf.Clamp01(distanceSq / 1e4f) + (1f - cosAngle) * 0.5f;
        }
    }
}
