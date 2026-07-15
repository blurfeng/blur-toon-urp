using System.Collections.Generic;
using UnityEngine;

namespace SmoothNormalTool
{
    /// <summary>
    /// 将平滑法线数据写入不同存储通道。
    /// </summary>
    public static class StorageWriter
    {
        // ═══════════════════════════════════════════════════════════════
        //  顶点色通道（XY 分量存入选定通道对，范围 [-1,1] → [0,255]）
        // ═══════════════════════════════════════════════════════════════
        public static void WriteToVertexColor(Mesh mesh, Vector3[] smoothNormals,
            SmoothNormalGeneratorWindow.VertexColorChannel channel =
            SmoothNormalGeneratorWindow.VertexColorChannel.Ba)
        {
            int vCount = mesh.vertexCount;

            var existingColors = mesh.colors32;
            var colors = new Color32[vCount];
            bool hasExisting = existingColors != null && existingColors.Length == vCount;

            for (int i = 0; i < vCount; i++)
            {
                var n = smoothNormals[i];

                byte r = hasExisting ? existingColors[i].r : (byte)128;
                byte g = hasExisting ? existingColors[i].g : (byte)128;
                byte b = hasExisting ? existingColors[i].b : (byte)128;
                byte a = hasExisting ? existingColors[i].a : (byte)128;

                switch (channel)
                {
                    case SmoothNormalGeneratorWindow.VertexColorChannel.Rg:
                        r = EncodeFloat(n.x);
                        g = EncodeFloat(n.y);
                        break;
                    case SmoothNormalGeneratorWindow.VertexColorChannel.Gb:
                        g = EncodeFloat(n.x);
                        b = EncodeFloat(n.y);
                        break;
                    default: // Ba
                        b = EncodeFloat(n.x);
                        a = EncodeFloat(n.y);
                        break;
                }

                colors[i] = new Color32(r, g, b, a);
            }

            mesh.colors32 = colors;
        }

        // ═══════════════════════════════════════════════════════════════
        //  切线空间（tangent.xyz 存储平滑法线，tangent.w 保持翻转信息）
        // ═══════════════════════════════════════════════════════════════
        public static void WriteToTangent(Mesh mesh, Vector3[] smoothNormals)
        {
            // 先保证有切线
            if (mesh.tangents == null || mesh.tangents.Length != mesh.vertexCount)
                mesh.RecalculateTangents();

            var existingTangents = mesh.tangents;
            var tangentNormals = SmoothNormalCalculator.ConvertToTangentSpace(mesh, smoothNormals);
            int vCount = mesh.vertexCount;

            var newTangents = new Vector4[vCount];
            for (int i = 0; i < vCount; i++)
            {
                var sn = tangentNormals[i];
                float w = existingTangents != null && existingTangents.Length == vCount
                    ? existingTangents[i].w : 1f;

                newTangents[i] = new Vector4(sn.x, sn.y, sn.z, w);
            }

            mesh.tangents = newTangents;
        }

        // ═══════════════════════════════════════════════════════════════
        //  UV 通道（UV.xy 存储平滑法线 X, Y，范围 [-1,1]）
        // ═══════════════════════════════════════════════════════════════
        public static void WriteToUV(Mesh mesh, Vector3[] smoothNormals, int channel)
        {
            channel = Mathf.Clamp(channel, 0, 3);
            int vCount = mesh.vertexCount;

            var uvData = new List<Vector4>(vCount);

            // 读取已有数据以保留 z/w（如果有）
            var existingUVs = new List<Vector4>(vCount);
            mesh.GetUVs(channel, existingUVs);
            bool hasExisting = existingUVs.Count == vCount;

            for (int i = 0; i < vCount; i++)
            {
                var n = smoothNormals[i];
                float z = hasExisting ? existingUVs[i].z : 0f;
                float w = hasExisting ? existingUVs[i].w : 0f;
                uvData.Add(new Vector4(n.x, n.y, z, w));
            }

            mesh.SetUVs(channel, uvData);
        }

        // ─────────────────────────────────────────────────────────────
        /// <summary> 将 [-1, 1] 映射到 [0, 255] </summary>
        private static byte EncodeFloat(float v)
        {
            return (byte)Mathf.RoundToInt((v * 0.5f + 0.5f) * 255f);
        }
    }
}
