using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SmoothNormalTool
{
    public class SmoothNormalGeneratorWindow : EditorWindow
    {
        // ─────────────────────────────────────────────────────────────
        //  Save state
        // ─────────────────────────────────────────────────────────────
        private enum SaveState { Clean, NeedSave, Saved }
        private SaveState _saveState = SaveState.Clean;

        // ─────────────────────────────────────────────────────────────
        //  Layout
        // ─────────────────────────────────────────────────────────────
        private Vector2 _leftScroll;
        private Vector2 _rightScroll;
        private float _dividerX = 420f;
        private bool _isDraggingDivider;
        
        // ─────────────────────────────────────────────────────────────
        //  Data status
        // ─────────────────────────────────────────────────────────────
        private bool _hasVertexColorData;
        private bool _hasTangentData;
        private bool[] _hasUVData = new bool[4];
        // 顶点色各通道是否含有非默认数据
        private bool _hasVcr, _hasVcg, _hasVcb, _hasVca;
        
        // ─────────────────────────────────────────────────────────────
        //  Styles
        // ─────────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _statusBoxStyle;
        private GUIStyle _dataCardStyle;
        private bool _stylesInitialized;

        // ─────────────────────────────────────────────────────────────
        //  Foldouts
        // ─────────────────────────────────────────────────────────────
        private bool _foldoutMeshInfo = true;
        private bool _foldoutClear;

        // ─────────────────────────────────────────────────────────────
        //  Colors
        // ─────────────────────────────────────────────────────────────
        private static readonly Color ColorAccent = new Color(0.33f, 0.78f, 1f);
        private static readonly Color ColorSuccess = new Color(0.35f, 0.85f, 0.47f);
        private static readonly Color ColorWarning = new Color(1f, 0.78f, 0.25f);
        private static readonly Color ColorGray = new Color(0.4f, 0.42f, 0.48f);
        private static readonly Color ColorCard = new Color(0.18f, 0.20f, 0.24f);
        private static readonly Color ColorBorder = new Color(0.28f, 0.30f, 0.36f);

        // ═══════════════════════════════════════════════════════════════
        [MenuItem("Tools/Smooth Normal Generator")]
        public static void ShowWindow()
        {
            var win = GetWindow<SmoothNormalGeneratorWindow>("平滑法线生成器");
            win.minSize = new Vector2(820, 560);
            win.Show();
        }

        // ═══════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            OnSelectionChanged();
            SetupPreviewRenderer();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            TearDownPreviewRenderer();
        }

        #region UI 主界面
        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginHorizontal();
            {
                // ── Left panel ──────────────────────────────────────
                EditorGUILayout.BeginVertical(GUILayout.Width(_dividerX));
                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                DrawLeftPanel();
                EditorGUILayout.EndScrollView();
                DrawSaveButton();          // 固定在左栏底部，ScrollView 外
                EditorGUILayout.EndVertical();

                // ── Divider ─────────────────────────────────────────
                DrawDivider();

                // ── Right panel ─────────────────────────────────────
                EditorGUILayout.BeginVertical();

                // 数据状态总览放在 ScrollView 内
                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll,
                    GUILayout.ExpandHeight(false), GUILayout.MaxHeight(position.height * 0.4f));
                DrawRightPanelTop();
                EditorGUILayout.EndScrollView();

                // 预览区放在 ScrollView 外，直接占满剩余高度
                DrawPreviewLaunchPanel();

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

            HandleDividerDrag();
        }
        
        private void DrawDivider()
        {
            var dividerRect = new Rect(_dividerX, 0, 4, position.height);
            EditorGUI.DrawRect(dividerRect, ColorBorder);

            // Hover highlight
            if (dividerRect.Contains(Event.current.mousePosition))
            {
                EditorGUI.DrawRect(dividerRect, ColorAccent * 0.6f);
                EditorGUIUtility.AddCursorRect(dividerRect, MouseCursor.ResizeHorizontal);
            }
        }
        
        private void HandleDividerDrag()
        {
            var dividerRect = new Rect(_dividerX - 2, 0, 8, position.height);
            var e = Event.current;

            if (e.type == EventType.MouseDown && dividerRect.Contains(e.mousePosition))
                _isDraggingDivider = true;
            if (e.type == EventType.MouseUp)
                _isDraggingDivider = false;
            if (_isDraggingDivider && e.type == EventType.MouseDrag)
            {
                _dividerX = Mathf.Clamp(e.mousePosition.x, 300, position.width - 250);
                Repaint();
            }
        }
        
        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject)
            {
                var go = Selection.activeGameObject;
                _meshFilter = go.GetComponent<MeshFilter>();
                _skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();

                if (_meshFilter || _skinnedMeshRenderer)
                {
                    _targetObject = go;
                    RefreshTargetMesh();
                }
            }
            Repaint();
        }

        #region UI 左侧界面
        private void DrawLeftPanel()
        {
            DrawWindowHeader();
            GUILayout.Space(12);
            DrawTargetSection();
            GUILayout.Space(6);
            DrawMeshInfoSection();
            GUILayout.Space(6);
            DrawStorageModeSection();
            GUILayout.Space(6);
            DrawGenerateSection();
            GUILayout.Space(6);
        }

        private void DrawSaveButton()
        {
            Color btnColor;
            string btnLabel;
            bool   canSave;

            switch (_saveState)
            {
                case SaveState.NeedSave:
                    btnColor = ColorWarning;
                    btnLabel = "⚠  需要保存";
                    canSave  = true;
                    break;
                case SaveState.Saved:
                    btnColor = ColorSuccess;
                    btnLabel = "✓  保存完成";
                    canSave  = false;
                    break;
                default: // Clean
                    btnColor = ColorGray;
                    btnLabel = "—  无修改";
                    canSave  = false;
                    break;
            }

            EditorGUI.DrawRect(new Rect(0, position.height - 42, _dividerX, 1), ColorBorder);
            GUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            GUI.enabled = canSave;
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize    = 11,
                fontStyle   = FontStyle.Bold,
                fixedHeight = 28,
                normal      = { textColor = canSave ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.55f, 0.58f, 0.62f),
                                background = MakeTex(2, 2, btnColor) },
                hover       = { textColor = new Color(0.05f, 0.05f, 0.08f),
                                background = MakeTex(2, 2, btnColor * 1.12f) },
            };

            if (GUILayout.Button(btnLabel, style))
                SaveMeshAsset();

            GUI.enabled = true;
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        private void SaveMeshAsset()
        {
            if (!_targetMesh) return;
            string path = AssetDatabase.GetAssetPath(_targetMesh);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[SmoothNormal] 目标 Mesh 不是项目资源文件，无法保存。请确保 Mesh 来自 .fbx / .asset 等资源文件。");
                return;
            }

            AssetDatabase.SaveAssetIfDirty(_targetMesh);
            AssetDatabase.Refresh();
            _saveState = SaveState.Saved;
            Repaint();
            Debug.Log($"[SmoothNormal] 已保存 Mesh 资源：{path}");
        }

        /// <summary>标记 Mesh 已被修改，需要保存。</summary>
        private void MarkDirty()
        {
            _saveState = SaveState.NeedSave;
            Repaint();
        }

        private void DrawWindowHeader()
        {
            var rect = EditorGUILayout.BeginVertical();

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);

            // Icon bar
            var iconRect = GUILayoutUtility.GetRect(36, 36, GUILayout.Width(36));
            DrawHexIcon(iconRect, ColorAccent);

            GUILayout.Space(10);
            EditorGUILayout.BeginVertical();
            GUILayout.Space(4);
            GUILayout.Label("平滑法线生成器", _headerStyle);
            GUILayout.Label("Smooth Normal Generator  •  Unity 2022.3", _subHeaderStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.EndVertical();

            // Accent line
            EditorGUI.DrawRect(new Rect(0, rect.yMax + 7, _dividerX, 2), ColorAccent);
        }
        #endregion

        #region 右侧界面
        private void DrawRightPanelTop()
        {
            GUILayout.Space(12);
            DrawSectionHeader("数据通道状态总览", "◈");
            GUILayout.Space(4);
            DrawDataStatusCards();
            GUILayout.Space(8);
        }

        private void DrawDataStatusCards()
        {
            // ── Vertex Color ─────────────────────────────────────────
            DrawBigStatusCard(
                "顶点色  Vertex Color",
                "平滑法线 XY → 顶点色 B, A 通道",
                _hasVertexColorData,
                new[] { ("B 通道", "存储法线 X"), ("A 通道", "存储法线 Y") },
                _targetMesh?.colors32?.Length > 0,
                ColorSuccess
            );

            GUILayout.Space(6);

            // ── Tangent ──────────────────────────────────────────────
            DrawBigStatusCard(
                "切线空间  Tangent Space",
                "平滑法线 → tangent.xyz (切线空间转换)",
                _hasTangentData,
                new[] { ("Tangent XYZ", "切线空间平滑法线"), ("Tangent W", "翻转标记 ±1") },
                _targetMesh?.tangents?.Length > 0,
                ColorWarning
            );

            GUILayout.Space(6);

            // ── UV Channels ──────────────────────────────────────────
            DrawBigStatusCard(
                "UV 通道  UV Channels",
                "平滑法线 XY → UV.xy 通道",
                _hasUVData.Any(v => v),
                new[]
                {
                    ("UV1 (xy)", _hasUVData[0] ? "有数据" : "空"),
                    ("UV2 (xy)", _hasUVData[1] ? "有数据" : "空"),
                    ("UV3 (xy)", _hasUVData[2] ? "有数据" : "空"),
                    ("UV4 (xy)", _hasUVData[3] ? "有数据" : "空"),
                },
                _hasUVData.Any(v => v),
                ColorAccent
            );
        }

        #region UI 数据卡
        private void DrawBigStatusCard(string titleName, string desc, bool hasData, (string label, string note)[] items, bool rawExists, Color accentColor)
        {
            var bgRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, 3, bgRect.height + 10), hasData ? accentColor : ColorBorder);

            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();

            // Title row
            EditorGUILayout.BeginHorizontal();
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11, normal = { textColor = Color.white } };
            GUILayout.Label(titleName, titleStyle);
            GUILayout.FlexibleSpace();

            // Status badge
            var badgeColor = hasData ? ColorSuccess : (rawExists ? ColorWarning : new Color(0.4f, 0.4f, 0.5f));
            var badgeText = hasData ? "● 含平滑法线" : (rawExists ? "○ 有原始数据" : "✕ 空");
            var badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                fontStyle = FontStyle.Bold,
                normal = { textColor = badgeColor },
                alignment = TextAnchor.MiddleRight,
            };
            GUILayout.Label(badgeText, badgeStyle, GUILayout.Width(90));
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();

            // Desc
            var descStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.6f, 0.65f, 0.72f) } };
            GUILayout.Label(desc, descStyle);
            GUILayout.Space(4);

            // Sub-items grid
            EditorGUILayout.BeginHorizontal();
            foreach (var (label, note) in items)
            {
                DrawChannelChip(label, note, hasData, accentColor);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.EndVertical();
        }

        private void DrawChannelChip(string label, string note, bool active, Color accentColor)
        {
            var chipBg = active ? new Color(accentColor.r * 0.2f, accentColor.g * 0.2f, accentColor.b * 0.2f, 0.8f)
                                : new Color(0.12f, 0.13f, 0.16f);
            var chipStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(6, 6, 4, 4),
                margin = new RectOffset(2, 2, 0, 0),
                normal = { background = MakeTex(2, 2, chipBg) }
            };

            EditorGUILayout.BeginVertical(chipStyle, GUILayout.Width(80));
            var lStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 9,
                normal = { textColor = active ? accentColor : new Color(0.5f, 0.5f, 0.6f) },
                alignment = TextAnchor.MiddleCenter,
            };
            var nStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 8,
                normal = { textColor = new Color(0.5f, 0.55f, 0.62f) },
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
            };
            GUILayout.Label(label, lStyle);
            GUILayout.Label(note, nStyle);
            EditorGUILayout.EndVertical();
        }
        #endregion
        #endregion
        #endregion

        #region UI 标题Icon
        private void DrawHexIcon(Rect r, Color c)
        {
            Handles.BeginGUI();
            Handles.color = c;
            var center = new Vector2(r.x + r.width / 2, r.y + r.height / 2);
            float s = r.width * 0.42f;
            var pts = new Vector3[7];
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 3;
                pts[i] = new Vector3(center.x + Mathf.Cos(a) * s, center.y + Mathf.Sin(a) * s, 0);
            }
            pts[6] = pts[0];
            Handles.DrawAAPolyLine(2f, pts);
            // Inner dot
            Handles.DrawSolidDisc(center, Vector3.forward, s * 0.25f);
            Handles.EndGUI();
        }
        #endregion
        
        #region UI 目标对象
        private GameObject _targetObject;
        private Mesh _targetMesh;
        private MeshFilter _meshFilter;
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        
        private void DrawTargetSection()
        {
            DrawSectionHeader("目标对象", "◉");
            EditorGUILayout.BeginVertical(_dataCardStyle);

            EditorGUI.BeginChangeCheck();
            var newObj = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("GameObject", "含有 MeshFilter 或 SkinnedMeshRenderer 的对象"),
                _targetObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && newObj != _targetObject)
            {
                _targetObject = newObj;
                if (_targetObject)
                {
                    _meshFilter = _targetObject.GetComponent<MeshFilter>();
                    _skinnedMeshRenderer = _targetObject.GetComponent<SkinnedMeshRenderer>();
                    RefreshTargetMesh();
                }
                else
                {
                    _meshFilter = null;
                    _skinnedMeshRenderer = null;
                    _targetMesh = null;
                    RefreshDataStatus();
                }
            }

            if (_targetObject)
            {
                EditorGUILayout.BeginHorizontal();
                string rendererType = _meshFilter ? "MeshFilter" :
                                      _skinnedMeshRenderer ? "SkinnedMeshRenderer" : "—";
                DrawTag(rendererType, ColorAccent);
                if (_targetMesh) DrawTag(_targetMesh.name, ColorCard * 1.4f);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("请选择场景中含有网格的 GameObject", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// 刷新 对象数据
        /// </summary>
        private void RefreshDataStatus()
        {
            if (!_targetMesh)
            {
                _hasVertexColorData = false;
                _hasTangentData = false;
                for (int i = 0; i < 4; i++) _hasUVData[i] = false;
                return;
            }

            // Vertex color: check each RGBA channel individually
            var colors = _targetMesh.colors32;
            if (colors != null && colors.Length > 0)
            {
                _hasVcr = colors.Any(c => c.r != 128);
                _hasVcg = colors.Any(c => c.g != 128);
                _hasVcb = colors.Any(c => c.b != 128);
                _hasVca = colors.Any(c => c.a != 128);
            }
            else
            {
                _hasVcr = _hasVcg = _hasVcb = _hasVca = false;
            }
            _hasVertexColorData = _hasVcr || _hasVcg || _hasVcb || _hasVca;

            // Tangent: check if tangents exist and w-component suggests smoothed data
            var tangents = _targetMesh.tangents;
            _hasTangentData = tangents != null && tangents.Length > 0 &&
                              tangents.Any(t => !Mathf.Approximately(t.w, 1f) && !Mathf.Approximately(t.w, -1f));

            // UV channels
            var uvList = new List<Vector4>();
            for (int ch = 0; ch < 4; ch++)
            {
                _targetMesh.GetUVs(ch, uvList);
                _hasUVData[ch] = uvList.Count > 0;
            }
        }
        
        /// <summary>
        /// 刷新 目标Mesh数据。
        /// </summary>
        private void RefreshTargetMesh()
        {
            if (_meshFilter)
                _targetMesh = _meshFilter.sharedMesh;
            else if (_skinnedMeshRenderer)
                _targetMesh = _skinnedMeshRenderer.sharedMesh;
            else
                _targetMesh = null;

            // 切换目标时重置保存状态
            _saveState = SaveState.Clean;

            if (_targetMesh)
            {
                var b = _targetMesh.bounds;
                _previewPivot = b.center;
                _previewZoom  = b.size.magnitude * 1.6f;
            }

            RefreshDataStatus();
        }
        #endregion
        
        #region UI Mesh信息列表
        private void DrawMeshInfoSection()
        {
            _foldoutMeshInfo = DrawFoldout(_foldoutMeshInfo, "网格信息", "▦");
            if (!_foldoutMeshInfo) return;

            EditorGUILayout.BeginVertical(_dataCardStyle);

            if (!_targetMesh)
            {
                GUILayout.Label("无网格数据", _subHeaderStyle);
            }
            else
            {
                DrawInfoRow("顶点数", _targetMesh.vertexCount.ToString("N0"));
                DrawInfoRow("三角面数", (_targetMesh.triangles.Length / 3).ToString("N0"));
                DrawInfoRow("SubMesh 数", _targetMesh.subMeshCount.ToString());
                DrawInfoRow("含法线", _targetMesh.normals?.Length > 0 ? "✓" : "✗");
                DrawInfoRow("含切线", _targetMesh.tangents?.Length > 0 ? "✓" : "✗");
                DrawInfoRow("含顶点色", _targetMesh.colors32?.Length > 0 ? "✓" : "✗");

                var uvList = new List<Vector4>();
                for (int ch = 0; ch < 4; ch++)
                {
                    _targetMesh.GetUVs(ch, uvList);
                    DrawInfoRow($"UV{ch + 1}", uvList.Count > 0 ? $"✓ ({uvList.Count}个)" : "—");
                }
            }

            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region UI 存储方式
        public enum StorageMode { VertexColor, TangentSpace, UV }

        public enum VertexColorChannel
        {
            Rg,   // R=法线X  G=法线Y
            Gb,   // G=法线X  B=法线Y
            Ba,   // B=法线X  A=法线Y
        }
        
        private StorageMode _storageMode = StorageMode.VertexColor;
        // Vertex color channel pair
        private VertexColorChannel _vcChannel = VertexColorChannel.Ba;
        // UV channel options
        private int _uvChannel = 1; // UV2 by default
        private readonly string[] _uvChannelNames = { "UV1 (xy)", "UV2 (xy)", "UV3 (xy)", "UV4 (xy)" };
        
        private void DrawStorageModeSection()
        {
            DrawSectionHeader("存储方式", "◈");
            EditorGUILayout.BeginVertical(_dataCardStyle);

            // Tabs
            EditorGUILayout.BeginHorizontal();
            DrawModeTab("顶点色\nVertex Color", StorageMode.VertexColor);
            DrawModeTab("切线空间\nTangent", StorageMode.TangentSpace);
            DrawModeTab("UV 通道\nUV Channel", StorageMode.UV);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            switch (_storageMode)
            {
                case StorageMode.VertexColor:
                    DrawVertexColorModeUI();
                    break;
                case StorageMode.TangentSpace:
                    DrawTangentModeUI();
                    break;
                case StorageMode.UV:
                    DrawUVModeUI();
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModeTab(string label, StorageMode mode)
        {
            bool active = _storageMode == mode;
            var bgColor = active ? ColorAccent : ColorCard;
            var fgColor = active ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.65f, 0.70f, 0.78f);

            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = active ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = fgColor, background = MakeTex(2, 2, bgColor) },
                hover = { textColor = fgColor, background = MakeTex(2, 2, bgColor * 1.1f) },
                padding = new RectOffset(6, 6, 6, 6),
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
            };

            if (GUILayout.Button(label, style, GUILayout.Height(42))) _storageMode = mode;
        }

        #region UI 存储方式-顶点色
        /// <summary>
        /// 顶点色模式 UI：选择 RG / GB / BA 存储对，并用颜色指示 RGBA 各通道的数据状态。
        /// </summary>
        private void DrawVertexColorModeUI()
        {
            EditorGUILayout.BeginVertical(GetInnerCardStyle());

            // ── 通道选择 ────────────────────────────────────────────
            GUILayout.Label("存储通道对", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.55f, 0.6f, 0.68f) } });
            GUILayout.Space(2);
            EditorGUILayout.BeginHorizontal();
            DrawVcChannelTab("RG", VertexColorChannel.Rg);
            DrawVcChannelTab("GB", VertexColorChannel.Gb);
            DrawVcChannelTab("BA", VertexColorChannel.Ba);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(8);

            // ── RGBA 各通道状态 ──────────────────────────────────────
            GUILayout.Label("顶点色通道数据状态", new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.55f, 0.6f, 0.68f) } });
            GUILayout.Space(2);

            // 当前选中的通道对写入的是哪两个通道
            bool rIsWrite = _vcChannel == VertexColorChannel.Rg;
            bool gIsWrite = _vcChannel == VertexColorChannel.Rg || _vcChannel == VertexColorChannel.Gb;
            bool bIsWrite = _vcChannel == VertexColorChannel.Gb || _vcChannel == VertexColorChannel.Ba;
            bool aIsWrite = _vcChannel == VertexColorChannel.Ba;

            DrawVcChannelStatus("R 通道", _hasVcr, rIsWrite, "法线 X（RG 模式）");
            DrawVcChannelStatus("G 通道", _hasVcg, gIsWrite, "法线 X/Y（RG/GB 模式）");
            DrawVcChannelStatus("B 通道", _hasVcb, bIsWrite, "法线 X/Y（GB/BA 模式）");
            DrawVcChannelStatus("A 通道", _hasVca, aIsWrite, "法线 Y（BA 模式）");

            GUILayout.Space(4);
            EditorGUILayout.HelpBox("选定通道对的 XY 分量将被写入，Z 分量通过重建得到。非激活通道原有数据不受影响。", MessageType.None);

            // ── 清除按钮 ─────────────────────────────────────────────
            GUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            DrawClearChannelButton("清除 RG", _hasVcr || _hasVcg, () => ClearVertexColorChannels(true, true, false, false));
            DrawClearChannelButton("清除 GB", _hasVcg || _hasVcb, () => ClearVertexColorChannels(false, true, true, false));
            DrawClearChannelButton("清除 BA", _hasVcb || _hasVca, () => ClearVertexColorChannels(false, false, true, true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>单个通道选择 Tab 按钮</summary>
        private void DrawVcChannelTab(string label, VertexColorChannel target)
        {
            bool active = _vcChannel == target;
            var bgColor = active ? ColorAccent : new Color(0.22f, 0.24f, 0.28f);
            var fgColor = active ? new Color(0.05f, 0.05f, 0.08f) : new Color(0.65f, 0.70f, 0.78f);
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = active ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = fgColor, background = MakeTex(2, 2, bgColor) },
                hover  = { textColor = fgColor, background = MakeTex(2, 2, bgColor * 1.1f) },
                fixedHeight = 26,
            };
            if (GUILayout.Button(label, style))
                _vcChannel = target;
        }

        /// <summary>绘制单个 RGBA 通道的状态行</summary>
        private void DrawVcChannelStatus(string channelName, bool hasData, bool isWriteTarget, string roleDesc)
        {
            Color dotColor;
            string desc;

            if (isWriteTarget)
            {
                // 当前选中要写入的通道
                dotColor = Color.white;
                desc     = hasData ? $"将覆盖写入  •  {roleDesc}" : $"将写入  •  {roleDesc}";
            }
            else if (hasData)
            {
                dotColor = ColorWarning;   // 黄色：有数据但不是写入目标
                desc     = "有数据（非当前写入通道）";
            }
            else
            {
                dotColor = new Color(0.4f, 0.42f, 0.48f);   // 灰色：空
                desc     = "无数据";
            }

            DrawStatusIndicator(channelName, desc, dotColor);
        }
        #endregion

        #region UI 存储方式-切线空间
        /// <summary>
        /// 切线空间模式 UI，展示 tangent.xyz 存储平滑法线（切线空间）和 tangent.w 存储翻转信息的状态，并提供说明。
        /// </summary>
        private void DrawTangentModeUI()
        {
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            DrawStatusIndicator("Tangent XYZ", "存储平滑法线（切线空间）", _hasTangentData);
            DrawStatusIndicator("Tangent W", "存储翻转信息（±1）", _hasTangentData);
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("将平滑法线转换到切线空间后存入 tangent.xyz，兼容大多数标准 Shader。", MessageType.None);
            GUILayout.Space(4);
            DrawClearChannelButton("清除切线数据", _hasTangentData, ClearTangents);
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region UI 存储方式-UV通道
        /// <summary>
        /// UV 通道模式 UI，提供 UV 通道选择，并展示各通道是否含有数据的状态，同时说明平滑法线 XY 分量存储在选定 UV 通道的 xy 分量中，Z 分量通过重建得到。
        /// </summary>
        private void DrawUVModeUI()
        {
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            _uvChannel = EditorGUILayout.Popup("UV 通道", _uvChannel, _uvChannelNames);
            GUILayout.Space(4);
            for (int i = 0; i < 4; i++)
            {
                bool isSelected = i == _uvChannel;
                bool hasData    = _hasUVData[i];

                string desc;
                Color  dotColor;
                if (isSelected)
                {
                    desc     = "当前选中，将写入此通道";
                    dotColor = Color.white;
                }
                else if (hasData)
                {
                    desc     = "有数据";
                    dotColor = ColorSuccess;
                }
                else
                {
                    desc     = "无数据";
                    dotColor = ColorGray;
                }

                // 状态行 + 右侧清除按钮
                EditorGUILayout.BeginHorizontal();
                DrawStatusIndicator($"UV{i + 1} 通道", desc, dotColor);
                GUILayout.FlexibleSpace();
                int capturedIndex = i;
                GUI.enabled = hasData;
                if (GUILayout.Button("清除", GUILayout.Width(44), GUILayout.Height(16)))
                    ClearUV(capturedIndex);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(4);
            EditorGUILayout.HelpBox("平滑法线 XY 分量存入选定 UV 通道的 xy 分量，Z 分量通过 sqrt 重建。", MessageType.None);
            EditorGUILayout.EndVertical();
        }
        #endregion
        #endregion

        #region UI 生成平滑法线
        /// <summary>
        /// 生成平滑法线的 UI 区域，包含一个大按钮，显示当前选定的存储方式和目标通道信息。按钮仅在有有效目标网格时可点击，点击后调用生成方法。
        /// </summary>
        private void DrawGenerateSection()
        {
            DrawSectionHeader("生成平滑法线", "◈");
            
            EditorGUILayout.BeginVertical(_dataCardStyle);

            bool canGenerate = _targetMesh;
            GUI.enabled = canGenerate;

            // Big generate button
            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 44,
                normal = { textColor = new Color(0.05f, 0.05f, 0.08f), background = MakeTex(2, 2, canGenerate ? ColorAccent : Color.gray) },
                hover = { textColor = new Color(0.05f, 0.05f, 0.08f), background = MakeTex(2, 2, canGenerate ? ColorAccent * 1.1f : Color.gray) },
            };

            string modeLabel = _storageMode == StorageMode.VertexColor ? "顶点色" :
                               _storageMode == StorageMode.TangentSpace ? "切线空间" : $"UV{_uvChannel + 1}";

            if (GUILayout.Button($"▶  生成平滑法线  →  {modeLabel}", btnStyle))
                GenerateSmoothNormals();

            GUI.enabled = true;
            EditorGUILayout.EndVertical();
        }
        #endregion
        
        #region UI 预览描边渲染
        // ─────────────────────────────────────────────────────────────
        //  Inline Preview
        // ─────────────────────────────────────────────────────────────
        private PreviewRenderUtility _previewUtil;
        private Material _previewBaseMat;
        private Material _previewOutlineMat;
        private Material _normalLineMat;

        // camera orbit
        private Vector2 _previewOrbit  = new Vector2(30f, -20f);
        private float   _previewZoom   = 3f;
        private Vector3 _previewPivot  = Vector3.zero;
        private bool    _previewDragging;
        private Vector2 _previewLastMouse;

        // outline params
        private float _outlineWidth  = 0.02f;
        private Color _outlineColor  = Color.white;
        private bool  _showBase      = true;
        private bool  _showOutline   = true;
        private Color _baseColor     = new Color(0.8f, 0.8f, 0.8f);
        private Color _previewBgColor = new Color(0.53f, 0.81f, 0.98f);
        private float _smoothness    = 0.5f;
        private float _metallic;

        // normal visualization
        private bool  _showNormals          = true;
        private float _normalLength         = 0.05f;
        private Color _normalColor          = new Color(0.2f, 1f, 0.4f);

        // original normal visualization
        private bool  _showOriginalNormals;
        private Color _originalNormalColor  = new Color(0.3f, 0.5f, 1f);

        private static readonly int PropSrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int PropDstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int PropCull     = Shader.PropertyToID("_Cull");
        private static readonly int PropZWrite   = Shader.PropertyToID("_ZWrite");

        private static readonly int PropGlossiness  = Shader.PropertyToID("_Glossiness");
        private static readonly int PropMetallic     = Shader.PropertyToID("_Metallic");
        private static readonly int PropOutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int PropOutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int PropStorageMode  = Shader.PropertyToID("_StorageMode");
        private static readonly int PropUVChannel    = Shader.PropertyToID("_UVChannel");
        private static readonly int PropVcChannel    = Shader.PropertyToID("_VCChannel");
        
        private void DrawPreviewLaunchPanel()
        {
            if (_previewUtil == null) SetupPreviewRenderer();

            // Section header（在 ScrollView 外，固定高度）
            GUILayout.Space(4);
            DrawSectionHeader("描边预览", "◉");
            GUILayout.Space(4);

            float paramW   = 220f;
            float totalW   = position.width - _dividerX - 16f;
            float previewW = Mathf.Max(80f, totalW - paramW - 2f);

            // 用 GUILayoutUtility.GetRect + ExpandHeight 让 Layout 分配所有剩余高度
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));

            // ── Viewport ──────────────────────────────────────────────
            var previewRect = GUILayoutUtility.GetRect(previewW, previewW,
                GUILayout.Width(previewW), GUILayout.ExpandHeight(true));
            DrawInlineViewport(previewRect);

            // ── Divider ───────────────────────────────────────────────
            EditorGUI.DrawRect(new Rect(previewRect.xMax, previewRect.y, 2, previewRect.height), ColorBorder);

            // ── Params ────────────────────────────────────────────────
            EditorGUILayout.BeginVertical(GUILayout.Width(paramW));
            DrawInlinePreviewParams();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawInlineViewport(Rect r)
        {
            if (!_targetMesh)
            {
                EditorGUI.DrawRect(r, new Color(0.11f, 0.12f, 0.15f));
                var s = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.4f, 0.45f, 0.5f) },
                };
                GUI.Label(r, "请先选择 Mesh", s);
                return;
            }

            HandlePreviewCameraControl(r);

            _previewUtil.BeginPreview(r, GUIStyle.none);
            _previewUtil.camera.backgroundColor = _previewBgColor;

            var camPos = _previewPivot + Quaternion.Euler(_previewOrbit.y, _previewOrbit.x, 0) * new Vector3(0, 0, _previewZoom);
            _previewUtil.camera.transform.position = camPos;
            _previewUtil.camera.transform.LookAt(_previewPivot);

            if (_showBase && _previewBaseMat)
            {
                UpdatePreviewBaseMat();
                _previewUtil.DrawMesh(_targetMesh, Matrix4x4.identity, _previewBaseMat, 0);
            }
            if (_showOutline && _previewOutlineMat)
            {
                UpdatePreviewOutlineMat();
                _previewUtil.DrawMesh(_targetMesh, Matrix4x4.identity, _previewOutlineMat, 0);
            }

            _previewUtil.camera.Render();
            var tex = _previewUtil.EndPreview();
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, false);

            // Overlay: mode badge
            var badgeRect = new Rect(r.x + 6, r.y + 6, 150, 20);
            EditorGUI.DrawRect(badgeRect, new Color(0.05f, 0.06f, 0.08f, 0.82f));
            string modeLabel = _storageMode == StorageMode.VertexColor ? "顶点色 模式" :
                               _storageMode == StorageMode.TangentSpace ? "切线空间 模式" :
                               $"UV{_uvChannel + 1} 模式";
            var bs = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = ColorAccent },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            GUI.Label(new Rect(badgeRect.x + 6, badgeRect.y, badgeRect.width, badgeRect.height), $"● {modeLabel}", bs);

            // Overlay: smooth normals
            if (_showNormals)
                DrawNormalsOverlay(r, GetDecodedSmoothNormals(), _normalColor);

            // Overlay: original normals
            if (_showOriginalNormals)
                DrawNormalsOverlay(r, _targetMesh.normals, _originalNormalColor);

            // Overlay: hint
            var hintRect = new Rect(r.x, r.yMax - 22, r.width, 22);
            EditorGUI.DrawRect(hintRect, new Color(0.05f, 0.06f, 0.08f, 0.72f));
            var hs = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 0.55f, 0.62f) },
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label(hintRect, "左键旋转  |  滚轮缩放  |  中键平移", hs);
        }

        /// <summary>
        /// 用 GL 在预览视口上叠加绘制法线方向线段。
        /// normals 为对象空间法线数组，与 mesh.vertices 一一对应。
        /// </summary>
        private void DrawNormalsOverlay(Rect r, Vector3[] normals, Color color)
        {
            if (_previewUtil?.camera == null || _targetMesh == null) return;
            if (Event.current.type != EventType.Repaint) return;
            if (normals == null || normals.Length != _targetMesh.vertexCount) return;

            var verts = _targetMesh.vertices;
            var cam   = _previewUtil.camera;
            int step  = Mathf.Max(1, verts.Length / 512);

            // 懒初始化 GL 画线材质
            if (!_normalLineMat)
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                _normalLineMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _normalLineMat.SetInt(PropSrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _normalLineMat.SetInt(PropDstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _normalLineMat.SetInt(PropCull,     (int)UnityEngine.Rendering.CullMode.Off);
                _normalLineMat.SetInt(PropZWrite,   0);
            }

            _normalLineMat.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, position.width, position.height, 0);
            GL.Begin(GL.LINES);
            GL.Color(color);

            for (int i = 0; i < verts.Length; i += step)
            {
                Vector3 vpO = cam.WorldToViewportPoint(verts[i]);
                Vector3 vpE = cam.WorldToViewportPoint(verts[i] + normals[i] * _normalLength);

                if (vpO.z <= 0 || vpE.z <= 0) continue;

                GL.Vertex3(r.x + vpO.x * r.width, r.y + (1f - vpO.y) * r.height, 0);
                GL.Vertex3(r.x + vpE.x * r.width, r.y + (1f - vpE.y) * r.height, 0);
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// 从 Mesh 按当前存储模式和通道选择，CPU 解码平滑法线（对象空间）。
        /// </summary>
        /// <summary>
        /// 修正重建出的 Z 符号：XY 压缩存储时 Z 总被重建为正值，
        /// 用原始顶点法线做点积验证，若方向相反则翻转 Z。
        /// </summary>
        private static Vector3 FixNormalZ(Vector3 smoothN, Vector3 vertexNormal)
        {
            if (Vector3.Dot(smoothN, vertexNormal.normalized) < 0f)
                smoothN.z = -smoothN.z;
            return smoothN.normalized;
        }

        /// <summary>
        /// 用 Gram-Schmidt 从顶点法线重建正交切线帧（与 Shader 侧保持一致）。
        /// tangent.xyz 已被 ConvertToTangentSpace 覆盖为切线空间平滑法线，
        /// 不能再用作 TBN 的 T 轴，必须重建。
        /// </summary>
        private static Vector3 DecodeTangentSpaceGs(Vector3 tsNormal, Vector3 vertexNormal, float tangentW)
        {
            var n  = vertexNormal.normalized;
            var up = Mathf.Abs(n.y) < 0.999f ? Vector3.up : Vector3.right;
            var t  = Vector3.Cross(up, n).normalized;
            var b  = Vector3.Cross(n, t) * tangentW;
            return (t * tsNormal.x + b * tsNormal.y + n * tsNormal.z).normalized;
        }

        private Vector3[] GetDecodedSmoothNormals()
        {
            if (_targetMesh == null) return null;
            int vCount       = _targetMesh.vertexCount;
            var result       = new Vector3[vCount];
            var meshNormals  = _targetMesh.normals;   // 原始顶点法线，用于 Z 符号修正

            switch (_storageMode)
            {
                // ── 顶点色 ───────────────────────────────────────────
                case StorageMode.VertexColor:
                {
                    var colors = _targetMesh.colors32;
                    if (colors == null || colors.Length != vCount) return null;
                    for (int i = 0; i < vCount; i++)
                    {
                        float nx, ny;
                        var c = colors[i];
                        switch (_vcChannel)
                        {
                            case VertexColorChannel.Rg:
                                nx = c.r / 127.5f - 1f; ny = c.g / 127.5f - 1f; break;
                            case VertexColorChannel.Gb:
                                nx = c.g / 127.5f - 1f; ny = c.b / 127.5f - 1f; break;
                            default: // Ba
                                nx = c.b / 127.5f - 1f; ny = c.a / 127.5f - 1f; break;
                        }
                        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                        // Z 重建后修正符号，确保与外表面法线同向
                        result[i] = FixNormalZ(new Vector3(nx, ny, nz), meshNormals[i]);
                    }
                    break;
                }

                // ── 切线空间 ─────────────────────────────────────────
                case StorageMode.TangentSpace:
                {
                    var tangents = _targetMesh.tangents;
                    if (tangents == null || tangents.Length != vCount) return null;
                    if (meshNormals == null || meshNormals.Length != vCount) return null;
                    for (int i = 0; i < vCount; i++)
                    {
                        var tan    = tangents[i];
                        var tsN    = new Vector3(tan.x, tan.y, tan.z); // tangent.xyz = 切线空间平滑法线
                        // 用 Gram-Schmidt 重建 TBN，与 Shader 侧逻辑完全一致
                        result[i] = DecodeTangentSpaceGs(tsN, meshNormals[i], tan.w);
                    }
                    break;
                }

                // ── UV 通道 ──────────────────────────────────────────
                case StorageMode.UV:
                {
                    var uvList = new List<Vector2>();
                    _targetMesh.GetUVs(_uvChannel, uvList);
                    if (uvList.Count != vCount) return null;
                    for (int i = 0; i < vCount; i++)
                    {
                        float nx = uvList[i].x;
                        float ny = uvList[i].y;
                        float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                        // Z 重建后修正符号
                        result[i] = FixNormalZ(new Vector3(nx, ny, nz), meshNormals[i]);
                    }
                    break;
                }
            }

            return result;
        }

        private void HandlePreviewCameraControl(Rect r)
        {
            var e = Event.current;
            if (!r.Contains(e.mousePosition)) return;

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                _previewDragging = true;
                _previewLastMouse = e.mousePosition;
                e.Use();
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                _previewDragging = false;
                e.Use();
            }
            if (_previewDragging && e.type == EventType.MouseDrag && e.button == 0)
            {
                var delta = e.mousePosition - _previewLastMouse;
                _previewOrbit.x += delta.x * 0.5f;
                _previewOrbit.y += delta.y * 0.5f;
                _previewOrbit.y  = Mathf.Clamp(_previewOrbit.y, -89f, 89f);
                _previewLastMouse = e.mousePosition;
                Repaint(); e.Use();
            }
            if (e.type == EventType.ScrollWheel)
            {
                _previewZoom = Mathf.Clamp(_previewZoom + e.delta.y * _previewZoom * 0.05f, 0.1f, 100f);
                Repaint(); e.Use();
            }
            if (e.type == EventType.MouseDrag && e.button == 2)
            {
                var delta = e.delta * (0.004f * _previewZoom);
                var cam = _previewUtil?.camera;
                if (cam)
                    _previewPivot -= cam.transform.right * delta.x - cam.transform.up * delta.y;
                Repaint(); e.Use();
            }
        }

        private void DrawInlinePreviewParams()
        {
            // 描边参数
            GUILayout.Space(6);
            DrawPreviewParamHeader("描边参数");
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            _showOutline = EditorGUILayout.Toggle("显示描边", _showOutline);
            GUI.enabled = _showOutline;
            EditorGUI.BeginChangeCheck();
            _outlineColor = EditorGUILayout.ColorField("描边颜色", _outlineColor);
            _outlineWidth = EditorGUILayout.Slider("描边宽度", _outlineWidth, 0.001f, 0.15f);
            if (EditorGUI.EndChangeCheck()) Repaint();
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            // 模型参数
            GUILayout.Space(4);
            DrawPreviewParamHeader("模型参数");
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            _showBase = EditorGUILayout.Toggle("显示模型", _showBase);
            GUI.enabled = _showBase;
            EditorGUI.BeginChangeCheck();
            _baseColor  = EditorGUILayout.ColorField("基础颜色", _baseColor);
            _smoothness = EditorGUILayout.Slider("光滑度", _smoothness, 0f, 1f);
            _metallic   = EditorGUILayout.Slider("金属度", _metallic, 0f, 1f);
            if (EditorGUI.EndChangeCheck()) Repaint();
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            // 视口参数
            GUILayout.Space(4);
            DrawPreviewParamHeader("视口参数");
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            EditorGUI.BeginChangeCheck();
            _previewBgColor = EditorGUILayout.ColorField("背景颜色", _previewBgColor);
            if (EditorGUI.EndChangeCheck()) Repaint();
            EditorGUILayout.EndVertical();

            // 法线可视化
            GUILayout.Space(4);
            DrawPreviewParamHeader("法线可视化");
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            EditorGUI.BeginChangeCheck();

            // 平滑法线
            _showNormals  = EditorGUILayout.Toggle("显示平滑法线", _showNormals);
            GUI.enabled   = _showNormals;
            _normalLength = EditorGUILayout.Slider("法线长度", _normalLength, 0.005f, 0.5f);
            _normalColor  = EditorGUILayout.ColorField("平滑法线颜色", _normalColor);
            GUI.enabled   = true;

            EditorGUILayout.Space(2);

            // 原始法线
            _showOriginalNormals = EditorGUILayout.Toggle("显示原始法线", _showOriginalNormals);
            GUI.enabled          = _showOriginalNormals;
            _originalNormalColor = EditorGUILayout.ColorField("原始法线颜色", _originalNormalColor);
            GUI.enabled          = true;

            if (EditorGUI.EndChangeCheck()) Repaint();
            EditorGUILayout.EndVertical();

            // 相机控制
            GUILayout.Space(4);
            DrawPreviewParamHeader("相机控制");
            EditorGUILayout.BeginVertical(GetInnerCardStyle());
            EditorGUI.BeginChangeCheck();
            _previewOrbit.x = EditorGUILayout.Slider("水平旋转", _previewOrbit.x, -180f, 180f);
            _previewOrbit.y = EditorGUILayout.Slider("垂直旋转", _previewOrbit.y, -89f, 89f);
            _previewZoom    = EditorGUILayout.Slider("距离", _previewZoom, 0.1f, 20f);
            if (EditorGUI.EndChangeCheck()) Repaint();
            if (GUILayout.Button("重置视角"))
            {
                _previewOrbit = new Vector2(30f, -20f);
                if (_targetMesh) { _previewPivot = _targetMesh.bounds.center; _previewZoom = _targetMesh.bounds.size.magnitude * 1.6f; }
                Repaint();
            }
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
        }

        private void DrawPreviewParamHeader(string headerText)
        {
            var s = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal = { textColor = ColorAccent },
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(2);
            GUILayout.Label(headerText, s);
            EditorGUILayout.EndHorizontal();
        }

        // ─────────────────────────────────────────────────────────────
        private void SetupPreviewRenderer()
        {
            _previewUtil = new PreviewRenderUtility();
            _previewUtil.camera.fieldOfView    = 30f;
            _previewUtil.camera.nearClipPlane  = 0.01f;
            _previewUtil.camera.farClipPlane   = 1000f;
            _previewUtil.camera.backgroundColor = _previewBgColor;
            _previewUtil.camera.clearFlags     = CameraClearFlags.SolidColor;
            _previewUtil.lights[0].intensity   = 1.1f;
            _previewUtil.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
            _previewUtil.lights[1].intensity   = 0.4f;
            BuildPreviewMaterials();
        }

        private void TearDownPreviewRenderer()
        {
            if (_previewUtil != null) { _previewUtil.Cleanup(); _previewUtil = null; }
            if (_previewBaseMat)    DestroyImmediate(_previewBaseMat);
            if (_previewOutlineMat) DestroyImmediate(_previewOutlineMat);
            if (_normalLineMat)     DestroyImmediate(_normalLineMat);
        }

        private static readonly int PropBaseColor  = Shader.PropertyToID("_BaseColor");
        private static readonly int PropSmoothness = Shader.PropertyToID("_Smoothness");

        // 按渲染管线寻找可用的 Lit shader
        private static Shader FindLitShader()
        {
            var s = Shader.Find("Universal Render Pipeline/Lit");
            if (s) return s;
            s = Shader.Find("Standard");
            if (s) return s;
            return Shader.Find("Unlit/Color");
        }

        private void BuildPreviewMaterials()
        {
            var litShader = FindLitShader();
            _previewBaseMat = new Material(litShader);
            ApplyBaseMatParams();

            var outlineShader = Shader.Find("SmoothNormalTool/OutlinePreview") ?? Shader.Find("Unlit/Color");
            _previewOutlineMat = new Material(outlineShader);
            if (_previewOutlineMat.HasProperty(PropOutlineColor)) _previewOutlineMat.SetColor(PropOutlineColor, _outlineColor);
            if (_previewOutlineMat.HasProperty(PropOutlineWidth)) _previewOutlineMat.SetFloat(PropOutlineWidth, _outlineWidth);
        }

        private void ApplyBaseMatParams()
        {
            if (!_previewBaseMat) return;
            // 颜色：URP 用 _BaseColor，Built-in 用 _Color（即 .color）
            if (_previewBaseMat.HasProperty(PropBaseColor))
                _previewBaseMat.SetColor(PropBaseColor, _baseColor);
            else
                _previewBaseMat.color = _baseColor;

            // 光滑度：URP 用 _Smoothness，Built-in 用 _Glossiness
            if (_previewBaseMat.HasProperty(PropSmoothness))
                _previewBaseMat.SetFloat(PropSmoothness, _smoothness);
            else if (_previewBaseMat.HasProperty(PropGlossiness))
                _previewBaseMat.SetFloat(PropGlossiness, _smoothness);

            if (_previewBaseMat.HasProperty(PropMetallic))
                _previewBaseMat.SetFloat(PropMetallic, _metallic);
        }

        private void UpdatePreviewBaseMat()
        {
            ApplyBaseMatParams();
        }

        private void UpdatePreviewOutlineMat()
        {
            if (!_previewOutlineMat) return;
            if (_previewOutlineMat.HasProperty(PropOutlineColor)) _previewOutlineMat.SetColor(PropOutlineColor, _outlineColor);
            if (_previewOutlineMat.HasProperty(PropOutlineWidth)) _previewOutlineMat.SetFloat(PropOutlineWidth, _outlineWidth);
            if (_previewOutlineMat.HasProperty(PropStorageMode))  _previewOutlineMat.SetFloat(PropStorageMode,  (float)_storageMode);
            if (_previewOutlineMat.HasProperty(PropUVChannel))    _previewOutlineMat.SetFloat(PropUVChannel,    _uvChannel);
            if (_previewOutlineMat.HasProperty(PropVcChannel))    _previewOutlineMat.SetFloat(PropVcChannel,    (float)_vcChannel);
        }
        #endregion

        #region 平滑法线 写入与清除
        private void GenerateSmoothNormals()
        {
            if (!_targetMesh) return;

            Undo.RecordObject(_targetMesh, "Generate Smooth Normals");

            var smoothNormals = SmoothNormalCalculator.Calculate(_targetMesh);

            switch (_storageMode)
            {
                case StorageMode.VertexColor:
                    StorageWriter.WriteToVertexColor(_targetMesh, smoothNormals, _vcChannel);
                    break;
                case StorageMode.TangentSpace:
                    StorageWriter.WriteToTangent(_targetMesh, smoothNormals);
                    break;
                case StorageMode.UV:
                    StorageWriter.WriteToUV(_targetMesh, smoothNormals, _uvChannel);
                    break;
            }

            EditorUtility.SetDirty(_targetMesh);
            RefreshDataStatus();
            MarkDirty();
            Repaint();


            Debug.Log($"[SmoothNormal] 生成完成 → 模式: {_storageMode}, Mesh: {_targetMesh.name}, 顶点数: {_targetMesh.vertexCount}");
        }

        // ─────────────────────────────────────────────────────────────
        private void ClearVertexColorChannels(bool clearR, bool clearG, bool clearB, bool clearA)
        {
            if (!_targetMesh) return;
            Undo.RecordObject(_targetMesh, "Clear Vertex Color Channels");
            int vCount = _targetMesh.vertexCount;
            var existing = _targetMesh.colors32;
            bool hasExisting = existing != null && existing.Length == vCount;
            var colors = new Color32[vCount];
            for (int i = 0; i < vCount; i++)
            {
                byte r = (hasExisting && !clearR) ? existing[i].r : (byte)128;
                byte g = (hasExisting && !clearG) ? existing[i].g : (byte)128;
                byte b = (hasExisting && !clearB) ? existing[i].b : (byte)128;
                byte a = (hasExisting && !clearA) ? existing[i].a : (byte)128;
                colors[i] = new Color32(r, g, b, a);
            }
            _targetMesh.colors32 = colors;
            EditorUtility.SetDirty(_targetMesh);
            RefreshDataStatus();
            MarkDirty();
            Repaint();
        }
        private void ClearTangents()
        {
            if (!_targetMesh) return;
            Undo.RecordObject(_targetMesh, "Clear Tangents");
            _targetMesh.tangents = null;
            EditorUtility.SetDirty(_targetMesh);
            RefreshDataStatus();
            MarkDirty();
            Repaint();
        }
        private void ClearUV(int ch)
        {
            if (!_targetMesh) return;
            Undo.RecordObject(_targetMesh, $"Clear UV{ch + 1}");
            _targetMesh.SetUVs(ch, (List<Vector2>)null);
            EditorUtility.SetDirty(_targetMesh);
            RefreshDataStatus();
            MarkDirty();
            Repaint();
        }
        #endregion
        
        #region 辅助方法
        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                normal = { textColor = new Color(0.5f, 0.6f, 0.7f) },
            };

            _dataCardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(8, 8, 2, 2),
            };

            _stylesInitialized = true;
        }

        private void DrawSectionHeader(string titleName, string icon)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            var s = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                normal = { textColor = ColorAccent },
            };
            GUILayout.Label($"{icon}  {titleName}", s);
            EditorGUILayout.EndHorizontal();
        }

        private bool DrawFoldout(bool state, string titleName, string icon)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            var s = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = ColorAccent },
                onNormal = { textColor = ColorAccent },
            };
            bool result = EditorGUILayout.Foldout(state, $"{icon}  {titleName}", true, s);
            EditorGUILayout.EndHorizontal();
            return result;
        }

        private void DrawInfoRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            var lStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white } };
            var vStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.white }, alignment = TextAnchor.MiddleRight };
            GUILayout.Label(label, lStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(value, vStyle, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            // 细分割线
            var lineRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(lineRect, new Color(0.28f, 0.30f, 0.36f, 0.6f));
        }

        private void DrawStatusIndicator(string label, string desc, bool active)
        {
            DrawStatusIndicator(label, desc, active ? ColorSuccess : new Color(0.4f, 0.42f, 0.48f));
        }

        private void DrawStatusIndicator(string label, string desc, Color dotColor)
        {
            EditorGUILayout.BeginHorizontal();
            var dotStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = dotColor }, fontSize = 14 };
            GUILayout.Label("●", dotStyle, GUILayout.Width(18));
            var lStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            GUILayout.Label(label, lStyle, GUILayout.Width(100));
            var dStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = dotColor } };
            GUILayout.Label(desc, dStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTag(string text, Color bg)
        {
            var s = new GUIStyle(GUI.skin.box)
            {
                fontSize = 9,
                padding = new RectOffset(5, 5, 2, 2),
                margin = new RectOffset(2, 2, 2, 2),
                normal = { background = MakeTex(2, 2, bg), textColor = Color.white },
            };
            GUILayout.Label(text, s);
        }

        private void DrawClearChannelButton(string label, bool enabled, System.Action onClick)
        {
            GUI.enabled = enabled;
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                normal = { textColor = enabled ? new Color(1f, 0.55f, 0.45f) : new Color(0.4f, 0.42f, 0.48f) },
            };
            if (GUILayout.Button(label, style))
                onClick?.Invoke();
            GUI.enabled = true;
        }

        private GUIStyle GetInnerCardStyle() => new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(8, 8, 6, 6),
        };

        private static Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();
        private static Texture2D MakeTex(int w, int h, Color col)
        {
            if (_texCache.TryGetValue(col, out var cached) && cached) return cached;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
            tex.SetPixels(pixels);
            tex.Apply();
            _texCache[col] = tex;
            return tex;
        }
        #endregion
    }
}
