# AGENTS.md

面向 Coding Agent 的仓库说明

## 项目概览

Telepath 是一个基于 R3 的 MVVM Godot UI 框架

- 引擎: Godot 4.x
- 语言: C# / .NET 8

## 分层与依赖方向

```
Telepath (宿主) → Telepath.Godot → Telepath.Core → R3
                ↘ Telepath.SourceGenerator (Analyzer only)
```

| 项目 | 命名空间 | 路径 | 职责
|------|------|------|---------
| `Telepath.Core` | `Telepath.Core` | `addons/Telepath/Core/` | 平台无关：ViewModel、命令、绑定抽象、转换器接口 
| `Telepath.Godot` | `Telepath.Godot` | `addons/Telepath/Godot/` | Godot 交互：节点适配、生命周期、编辑器扩展、R3 胶水 |
| `Telepath.SourceGenerator` | `Telepath.SourceGenerator` | `addons/Telepath/SourceGenerator/` | Roslyn 代码生成
| `Telepath` | `Telepath` | `Telepath` |  演示

### 项目规则

- **Core 禁止** 引用 `Godot` / `GodotSharp`，也禁止 `GODOT` 条件编译
- Godot 可以依赖 Core；反向禁止
- SourceGenerator **不** 引用 Core / Godot（按属性元数据名识别）
- 编辑器插件属于 Godot 层（`#if TOOLS`），不单独属于某一程序集
- Godot 只在宿主主程序集解析 C# 脚本：挂到场景上的脚本（含 `TelepathEditorPlugin`、`FrameProviderDispatcher`、具体 View）由宿主编译；`View` / `View<T>` 等库类型在 `Telepath.Godot`（该项目启用 Godot.SourceGenerators 的 ScriptMethods，以便基类 `_Ready` 被调用）
- 具体 View 脚本必须非泛型（`FooView : View<FooViewModel>`）
- 进树接绑定、出树断绑定、`NotificationPredelete` 才 `ViewModel.Dispose()`；不要 `viewModel.AddTo(node)`

### 项目详细信息
见 doc/
