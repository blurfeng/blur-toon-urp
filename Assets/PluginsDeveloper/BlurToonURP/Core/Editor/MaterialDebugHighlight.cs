using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlurToonURP.EditorGUIx
{
    /// <summary>
    /// 材质调试：在 Scene 视图中高亮显示“正在使用当前材质”的物体，便于快速定位。
    /// <para>纯 Editor 功能：不修改材质/Shader/渲染管线，只在 Scene 视图叠加绘制，不影响正式运行流程与 Game 视图。</para>
    /// </summary>
    public static class MaterialDebugHighlight
    {
        //高亮开关（编辑器会话级状态，不写入材质；脚本重编译/域重载后自动关闭）
        private static bool s_enabled;
        //当前要高亮的材质（跟随 Inspector 正在显示的材质）
        private static Material s_targetMaterial;

        //叠加绘制用的临时材质与蒙皮烘焙网格（HideAndDontSave，随退订清理）
        private static Material s_overlayMat;
        private static Mesh s_skinnedBakeMesh;
        private static GUIStyle s_labelStyle;
        //复用列表：当前物体上“使用该材质”的子网格索引
        private static readonly List<int> s_matchSubmeshes = new List<int>();

        //高亮颜色（品红）：填充为不透明实心（类似 Shader 报错色，最醒目），线框与标签同色
        private static readonly Color HighlightFillColor = new Color(1f, 0f, 1f, 1f);
        private static readonly Color HighlightWireColor = new Color(1f, 0f, 1f, 0.9f);

        [InitializeOnLoadMethod]
        private static void Init()
        {
            //域重载前清理，避免临时对象泄漏，同时复位开关
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
        }

        private static void OnBeforeReload()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Cleanup();
            s_enabled = false;
        }

        /// <summary>
        /// 在 Inspector 顶部绘制 Debug 功能区。由 ShaderGUI 在绘制主体前调用。
        /// </summary>
        /// <param name="target">当前正在编辑的材质</param>
        public static void OnInspectorGUI(Material target)
        {
            //记录当前材质，供 Scene 绘制回调使用
            s_targetMaterial = target;

            EditorGUIx.FoldoutPanel("【调试 Debug】仅编辑器用，不影响正式流程", () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    "高亮定位（Scene）",
                    "在 Scene 视图中把所有使用当前材质的物体叠加为品红色并框选，便于快速定位。仅 Scene 视图可见，不修改材质与正式渲染。"));

                var guiColor = GUI.color;
                GUI.color = s_enabled ? Color.green : Color.gray;
                if (GUILayout.Button(s_enabled ? "On" : "Off", GUILayout.Width(50)))
                    SetEnabled(!s_enabled);
                GUI.color = guiColor;
                EditorGUILayout.EndHorizontal();

                if (s_enabled)
                    EditorGUILayout.HelpBox("已开启：Scene 视图中品红色区域即为使用该材质的物体（可穿透遮挡显示）。", MessageType.Info);
            });
        }

        private static void SetEnabled(bool on)
        {
            if (s_enabled == on) return;
            s_enabled = on;

            //订阅/退订 Scene 视图绘制回调
            SceneView.duringSceneGui -= OnSceneGUI;
            if (s_enabled)
                SceneView.duringSceneGui += OnSceneGUI;
            else
                Cleanup();

            SceneView.RepaintAll();
        }

        private static void Cleanup()
        {
            if (s_overlayMat != null) { Object.DestroyImmediate(s_overlayMat); s_overlayMat = null; }
            if (s_skinnedBakeMesh != null) { Object.DestroyImmediate(s_skinnedBakeMesh); s_skinnedBakeMesh = null; }
        }

        //叠加用材质：内置 Colored 着色器，穿透遮挡 + 不透明实心填充
        private static Material OverlayMat
        {
            get
            {
                if (s_overlayMat == null)
                {
                    s_overlayMat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                    s_overlayMat.SetInt("_ZTest", (int)CompareFunction.Always); //穿透遮挡显示
                    s_overlayMat.SetInt("_ZWrite", 0);
                    s_overlayMat.SetInt("_Cull", (int)CullMode.Off);
                    s_overlayMat.SetInt("_SrcBlend", (int)BlendMode.One); //不透明实心
                    s_overlayMat.SetInt("_DstBlend", (int)BlendMode.Zero);
                }
                return s_overlayMat;
            }
        }

        private static GUIStyle LabelStyle
        {
            get
            {
                if (s_labelStyle == null)
                {
                    s_labelStyle = new GUIStyle(EditorStyles.boldLabel);
                    s_labelStyle.normal.textColor = HighlightWireColor;
                }
                return s_labelStyle;
            }
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!s_enabled || s_targetMaterial == null) return;
            //所有绘制都在 Repaint 事件进行（Handles/Graphics.DrawMeshNow 仅在此时有效）
            if (Event.current.type != EventType.Repaint) return;

            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!UsesTargetMaterial(r)) continue;

                DrawRendererHighlight(r);
            }
        }

        private static bool UsesTargetMaterial(Renderer r)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
                if (mats[i] == s_targetMaterial) return true;
            return false;
        }

        private static void DrawRendererHighlight(Renderer r)
        {
            //取网格与变换（蒙皮取当前姿势烘焙网格）
            Mesh mesh = null;
            Matrix4x4 matrix = r.localToWorldMatrix;

            if (r is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                if (s_skinnedBakeMesh == null)
                    s_skinnedBakeMesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                //不含缩放烘焙，配合 localToWorldMatrix（含缩放）使用，兼容任意缩放
                smr.BakeMesh(s_skinnedBakeMesh, false);
                mesh = s_skinnedBakeMesh;
            }
            else
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null) mesh = mf.sharedMesh;
            }

            //1) 实心品红填充：只绘制“确实使用当前材质”的子网格，精准区分同物体上的不同材质（如面部皮肤 vs 眼睛）
            if (mesh != null)
            {
                GetMatchingSubmeshes(r, mesh, s_matchSubmeshes);
                if (s_matchSubmeshes.Count > 0)
                {
                    var mat = OverlayMat;
                    mat.SetColor("_Color", HighlightFillColor);
                    if (mat.SetPass(0))
                    {
                        for (int i = 0; i < s_matchSubmeshes.Count; i++)
                            Graphics.DrawMeshNow(mesh, matrix, s_matchSubmeshes[i]);
                    }
                }
            }

            //2) 包围盒线框 + 名称标签（画在填充之上，便于远处/离屏时也能定位到物体）
            var bounds = r.bounds;
            var prevZTest = Handles.zTest;
            Handles.zTest = CompareFunction.Always;
            Handles.color = HighlightWireColor;
            Handles.DrawWireCube(bounds.center, bounds.size);
            Handles.zTest = prevZTest;
            Handles.Label(bounds.center + Vector3.up * (bounds.extents.y + 0.05f), r.gameObject.name, LabelStyle);
        }

        /// <summary>
        /// 收集“使用了当前材质”的材质槽对应的子网格索引（材质槽 i ↔ 子网格 i）。
        /// </summary>
        private static void GetMatchingSubmeshes(Renderer r, Mesh mesh, List<int> result)
        {
            result.Clear();
            var mats = r.sharedMaterials;
            int subCount = Mathf.Max(1, mesh.subMeshCount);
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != s_targetMaterial) continue;
                int sub = Mathf.Min(i, subCount - 1); //材质数多于子网格时，多出的材质渲染最后一个子网格
                if (!result.Contains(sub)) result.Add(sub);
            }
        }
    }
}
