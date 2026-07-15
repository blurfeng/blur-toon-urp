# SmoothNormalTool — Unity 2022.3 编辑器工具

为 Mesh 快速生成**平滑法线**，用于顶点偏移描边，支持三种存储方式。

---

## 目录结构

```
SmoothNormalTool/
├── Editor/
│   ├── SmoothNormalGeneratorWindow.cs   ← 主编辑器窗口
│   ├── SmoothNormalCalculator.cs        ← 平滑法线计算核心
│   ├── StorageWriter.cs                 ← 数据写入（顶点色/切线/UV）
│   ├── SmoothNormalPreviewWindow.cs     ← 描边实时预览窗口
│   ├── OutlineShaderGUI.cs             ← 描边材质自定义 Inspector
│   └── SmoothNormalTool.Editor.asmdef
├── Shaders/
│   ├── Outline.shader                   ← 生产用描边 Shader（支持三种模式）
│   └── OutlinePreview.shader            ← 编辑器预览专用 Shader
└── README.md
```

---

## 安装

1. 将 `SmoothNormalTool` 文件夹整体拷贝到你的 Unity 项目 `Assets/` 下任意位置。
2. Unity 会自动编译，无需额外依赖。
3. 菜单栏出现 **Tools → Smooth Normal Generator** 即为安装成功。

---

## 使用方法

### 1. 打开工具窗口

```
Unity 菜单栏 → Tools → Smooth Normal Generator
```

### 2. 选择目标对象

- 在 **Hierarchy** 中点击一个含有 `MeshFilter` 或 `SkinnedMeshRenderer` 的 GameObject，工具会自动读取。
- 也可以手动拖拽到"目标对象"字段。

### 3. 选择存储方式

| 模式 | 存储位置 | 适用场景 |
|------|---------|---------|
| **顶点色** | `vertex.color.BA` | 顶点色未被其他效果占用时首选 |
| **切线空间** | `tangent.xyz` | 需要切线空间法线贴图兼容时 |
| **UV 通道** | `UV1~UV4.xy` | 其他通道已被占用，或需要精度时 |

### 4. 生成平滑法线

点击 **"▶ 生成平滑法线"** 按钮，数据会直接写入 `sharedMesh`，支持 Undo。

### 5. 查看数据状态

右侧面板实时显示三种存储通道的状态：
- `● 含平滑法线` — 已检测到平滑法线数据
- `○ 有原始数据` — 通道有数据但不是平滑法线
- `✕ 空` — 该通道无数据

### 6. 描边预览

点击 **"🔍 打开描边预览窗口"** 打开实时预览：
- 鼠标左键拖动旋转视角
- 滚轮缩放
- 中键平移
- 右侧面板调节描边宽度、颜色、背景色等参数

---

## 在游戏中使用描边

### 方法一：使用内置 Outline.shader

1. 为你的模型创建新材质，选择 Shader `SmoothNormalTool/Outline`。
2. 在材质 Inspector 中选择**平滑法线来源**（与生成时选择的模式一致）。
3. 设置描边颜色和宽度。

> ⚠️ `Outline.shader` 是两 Pass 的描边 Shader（Pass0 描边 + Pass1 正常渲染）。
> 如果你用自己的主材质，可以将 Pass0 OUTLINE 复制到你的 Shader 中。

### 方法二：单独描边 Pass

把 `Outline.shader` 的 OUTLINE Pass 代码复制到现有 Shader，仅保留描边渲染。

---

## Shader 中读取平滑法线

### 顶点色模式

```hlsl
// 从顶点色 B/A 重建平滑法线（对象空间）
float nx = color.b * 2.0 - 1.0;
float ny = color.a * 2.0 - 1.0;
float nz = sqrt(max(0, 1.0 - nx*nx - ny*ny));
float3 smoothNormal = normalize(float3(nx, ny, nz));
```

### 切线空间模式

```hlsl
// tangent.xyz 已是切线空间平滑法线，转回对象空间
float3 N = normalize(v.normal);
float3 T = normalize(v.tangent.xyz);
float3 B = normalize(cross(N, T) * v.tangent.w);
float3 ts = normalize(v.tangent.xyz);
float3 smoothNormal = normalize(T*ts.x + B*ts.y + N*ts.z);
```

### UV 通道模式

```hlsl
// 从 UV2 xy 重建（以 UV2 为例）
float nx = v.uv1.x;  // TEXCOORD1 = UV2
float ny = v.uv1.y;
float nz = sqrt(max(0, 1.0 - nx*nx - ny*ny));
float3 smoothNormal = normalize(float3(nx, ny, nz));
```

---

## 注意事项

- 平滑法线计算基于**顶点位置相等**的判断（精度 0.0001 单位），对于高精度模型效果最佳。
- 修改 sharedMesh 会影响所有使用该 Mesh 的对象，建议先复制 Mesh。
- `OutlinePreview.shader` 仅用于编辑器预览，不要在生产中使用。
- 如果 `OutlinePreview.shader` 无法找到（Shader.Find 返回 null），预览将使用 Unlit/Color 作为 Fallback，描边不会偏移但颜色仍然可见。

---

## 清除数据

在主窗口展开**"清除数据"**折叠面板，可以单独清除各通道数据（支持 Undo）。

---

## 许可

MIT License — 自由使用于商业和非商业项目。
