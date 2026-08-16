# AGENTS.md

面向 Coding Agent 的仓库说明

## 项目概览

Telepath 是一个基于 R3 的 MVVM Godot UI 框架

- 引擎: Godot 4.x
- 语言: C# / .NET 8

**用 Godot 打开 `samples/Showcase`，不要打开仓库根。** 仓库根不是 Godot 工程。

## 分层与依赖方向

```
Telepath.Showcase (演示宿主) → Telepath.Godot → Telepath.Core → R3
                             ↘ Telepath.SourceGenerator (Analyzer only)
```

| 项目 | 命名空间 | 路径 | 职责
|------|------|------|---------
| `Telepath.Core` | `Telepath.Core` | `src/Telepath.Core/` | 平台无关：ViewModel、命令、绑定抽象、转换器接口
| `Telepath.Godot` | `Telepath.Godot` | `src/Telepath.Godot/` | Godot 交互：节点适配、生命周期、R3 胶水
| `Telepath.SourceGenerator` | `Telepath.SourceGenerator` | `src/Telepath.SourceGenerator/` | Roslyn 代码生成
| `Telepath.Showcase` | `Telepath.Showcase` | `samples/Showcase/` | 演示 Godot 工程；薄 addon（插件 + Autoload 脚本）

宿主 addon 源码在 `src/Telepath.Godot/Addon/`（`plugin.cfg`、`FrameProviderDispatcher`）和 `src/Telepath.Godot/Editor/`，**排除出** `Telepath.Godot` 编译，经符号链接到 `samples/Showcase/addons/Telepath/`，由 Showcase 宿主编译。

### 项目规则

- **Core 禁止** 引用 `Godot` / `GodotSharp`，也禁止 `GODOT` 条件编译
- Godot 可以依赖 Core；反向禁止
- SourceGenerator **不** 引用 Core / Godot（按属性元数据名识别）
- 库源码在 `src/`，不放进 Godot 工程树；`Telepath.Godot` 不声明 View 类的 `GodotObject` 子类
- Godot 只在宿主主程序集解析 C# 脚本：挂到场景上的脚本（含 `TelepathEditorPlugin`、`FrameProviderDispatcher`、具体 View）由 `Telepath.Showcase` 编译。addon 根文件来自 `src/Telepath.Godot/Addon/`，编辑器脚本在 `src/Telepath.Godot/Editor/`，都不编进库 DLL；`samples/Showcase/addons/Telepath/` 是符号链接
- 具体 View 脚本直接继承非泛型 Godot 节点，使用 `[TelepathView<TViewModel>]`，并在用户源码中声明 `public override partial void _Notification(int what);`。绑定优先写在场景 `metadata/telepath_bindings`（编辑器 Dock）；`[BindTo]` 是逃逸口
- 不要给 View 加 `[Tool]`；Dock 靠脚本路径解析类型
- Godot 的 `MethodName` / 方法表只由 Godot SDK 生成；Telepath 生成器只实现 partial 通知桥
- 进树接绑定、出树断绑定、`NotificationPredelete` 才 `ViewModel.Dispose()`（仅自己 `CreateViewModel()` 的；注入的项 VM 由集合拥有者释放）；不要 `viewModel.AddTo(node)`

### 项目详细信息
见 doc/
