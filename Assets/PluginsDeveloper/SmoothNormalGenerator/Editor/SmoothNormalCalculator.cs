using System.Collections.Generic;
using UnityEngine;

namespace SmoothNormalTool
{
    /// <summary>
    /// 平滑法线计算核心：将同一位置的顶点面法线按角度加权平均后归一化，用于描边顶点偏移。
    /// </summary>
    public static class SmoothNormalCalculator
    {
        /// <summary>
        /// 计算每个顶点的平滑法线（对象空间）。
        /// 算法：对每个三角形求面法线，再按各顶点处的夹角加权，
        /// 将同位置顶点的加权面法线累加后归一化。
        /// </summary>
        public static Vector3[] Calculate(Mesh mesh)
        {
            // ── Step 1: 建立 位置 → 加权面法线列表 ───────────────────
            var faceNormalMap = CreateWeightedFaceNormalMap(mesh);

            // ── Step 2: 对每个顶点，累加同位置的加权面法线并归一化 ───
            return CalculateAverageNormals(faceNormalMap, mesh);
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// 遍历所有三角形，为每个顶点位置收集「角度加权后的面法线」列表。
        /// 边向量乘以 1000 以提升叉积数值精度；面法线先归一化再乘角度权重。
        /// </summary>
        private static Dictionary<Vector3, List<Vector3>> CreateWeightedFaceNormalMap(Mesh mesh)
        {
            var vertices  = mesh.vertices;
            var normals   = mesh.normals;
            var triangles = mesh.triangles;
            int triCount  = triangles.Length;

            var map = new Dictionary<Vector3, List<Vector3>>();

            for (int i = 0; i < triCount; i += 3)
            {
                int idx0 = triangles[i];
                int idx1 = triangles[i + 1];
                int idx2 = triangles[i + 2];

                Vector3 p0 = vertices[idx0];
                Vector3 p1 = vertices[idx1];
                Vector3 p2 = vertices[idx2];

                // 边向量 ×1000 提升精度
                Vector3 edge1 = (p1 - p0) * 1000f;
                Vector3 edge2 = (p2 - p0) * 1000f;

                Vector3 faceNormal = Vector3.Cross(edge1, edge2);
                if (faceNormal.sqrMagnitude < 1e-10f) continue;
                faceNormal.Normalize();

                // ── 朝向修正 ─────────────────────────────────────────
                // 叉积的朝向由三角形绕序决定。背面三角形的绕序相反，
                // 导致叉积与实际外表面法线反向。
                // 用三顶点原始法线的均值来验证并修正面法线方向。
                Vector3 avgVertNormal = normals[idx0] + normals[idx1] + normals[idx2];
                if (Vector3.Dot(faceNormal, avgVertNormal) < 0f)
                    faceNormal = -faceNormal;

                // 各顶点处的内角权重（弧度）
                float w0 = AngleRadius(p1 - p0, p2 - p0);
                float w1 = AngleRadius(p2 - p1, p0 - p1);
                float w2 = AngleRadius(p0 - p2, p1 - p2);

                AddToMap(map, p0, faceNormal * w0);
                AddToMap(map, p1, faceNormal * w1);
                AddToMap(map, p2, faceNormal * w2);
            }

            return map;
        }

        /// <summary>累加到 map，若 key 不存在则新建列表。</summary>
        private static void AddToMap(Dictionary<Vector3, List<Vector3>> map,
                                     Vector3 position, Vector3 weightedNormal)
        {
            if (!map.TryGetValue(position, out var list))
            {
                list = new List<Vector3>(4);
                map[position] = list;
            }
            list.Add(weightedNormal);
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// 将 faceNormalMap 中每个位置的加权法线求和并归一化，
        /// 写入对应顶点索引的结果数组。
        /// </summary>
        private static Vector3[] CalculateAverageNormals(
            Dictionary<Vector3, List<Vector3>> faceNormalMap, Mesh mesh)
        {
            var vertices      = mesh.vertices;
            var originalNorms = mesh.normals;
            int vCount        = vertices.Length;
            var result        = new Vector3[vCount];

            for (int i = 0; i < vCount; i++)
            {
                if (!faceNormalMap.TryGetValue(vertices[i], out var list) || list.Count == 0)
                {
                    result[i] = originalNorms[i]; // fallback
                    continue;
                }

                Vector3 sum = list[0];
                for (int j = 1; j < list.Count; j++)
                    sum += list[j];

                result[i] = sum.normalized;
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>两向量夹角（弧度），安全 Clamp 防止 acos 溢出。</summary>
        private static float AngleRadius(Vector3 a, Vector3 b)
        {
            float sqA = a.sqrMagnitude;
            float sqB = b.sqrMagnitude;
            if (sqA < 1e-10f || sqB < 1e-10f) return 0f;
            float dot = Vector3.Dot(a, b) / Mathf.Sqrt(sqA * sqB);
            return Mathf.Acos(Mathf.Clamp(dot, -1f, 1f));
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary>将平滑法线从对象空间转换到切线空间（TBN 转置）。</summary>
        public static Vector3[] ConvertToTangentSpace(Mesh mesh, Vector3[] smoothNormals)
        {
            var normals  = mesh.normals;
            var tangents = mesh.tangents;
            int vCount   = smoothNormals.Length;
            var result   = new Vector3[vCount];

            for (int i = 0; i < vCount; i++)
            {
                var n    = normals[i].normalized;
                var tVec = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z).normalized;
                var b    = Vector3.Cross(n, tVec) * tangents[i].w;

                result[i] = new Vector3(
                    Vector3.Dot(smoothNormals[i], tVec),
                    Vector3.Dot(smoothNormals[i], b),
                    Vector3.Dot(smoothNormals[i], n)
                ).normalized;
            }

            return result;
        }
    }
}
