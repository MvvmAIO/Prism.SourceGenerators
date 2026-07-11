# ADR-001: 多 Roslyn 分析器布局

| 字段 | 值 |
|------|-----|
| **状态** | Accepted |
| **日期** | 2026-04-29 |
| **关联上下文** | 无 — 直接决策 |

## 背景

消费者使用不同版本的 C# 编译器（VS 2022、.NET SDK、Rider），Roslyn 分析器 API 在 4.0 / 4.3 / 4.12 / 5.0 之间存在破坏性差异。单一 analyzer DLL 无法在所有宿主上稳定加载。

## 决策

为同一套生成器源码编译 **四套** `Prism.SourceGenerators.dll`，分别打入 NuGet 的 `analyzers/dotnet/roslyn4.0|4.3|4.12|5.0/cs/`。MSBuild targets 根据 `CscToolPath` 文件版本选择路径，无法解析时回退 **roslyn4.12**。

## 后果

- **正面**：广泛 IDE/SDK 兼容；与设计时 MSB4086 防御一致。
- **负面**：维护四套工程；`Microsoft.CodeAnalysis.*` 版本须手动协调（Dependabot ignore）。
- **测试**：主快照在 Roslyn4120；Roslyn5000 独立冒烟项目。

## 参考

- [design/Architecture.md](../design/Architecture.md)
- [wiki/Architecture.md](../../wiki/Architecture.md)
