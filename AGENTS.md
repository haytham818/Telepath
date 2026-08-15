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
| `Telepath.Godot` | `Telepath.Godot` | `addons/Telepath/Godot/` | Godot 交互：节点适配、生命周期、编辑器扩展
| `Telepath.SourceGenerator` | `Telepath.SourceGenerator` | `addons/Telepath/SourceGenerator/` | Roslyn 代码生成
| `Telepath` | `Telepath` | `Telepath` |  演示

### 项目规则

- **Core 禁止** 引用 `Godot` / `GodotSharp`，也禁止 `GODOT` 条件编译
- Godot 可以依赖 Core；反向禁止
- SourceGenerator **不** 引用 Core / Godot（按属性元数据名识别）
- 编辑器插件属于 Godot 层（`#if TOOLS`），不单独属于某一程序集

### 项目详细信息
见 doc/
