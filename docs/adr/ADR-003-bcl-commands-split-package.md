# ADR-003: AsyncDelegateCommand 拆分为 Bcl.Commands 独立包

| 字段 | 值 |
|------|-----|
| **状态** | Accepted |
| **日期** | 2026-05-01 |
| **关联 RFC** | 无 — 直接决策（CHANGELOG 0.2.0） |

## 背景

Prism 9+ 内置 `AsyncDelegateCommand`；Prism.Core 8.1.97 无此类型。早期版本将 BCL 命令类型嵌入分析器包，导致包职责混乱且 Prism 9 用户重复携带类型。

## 决策

- **MvvmAIO.Prism.SourceGenerators** 仅含分析器 + **MvvmAIO.Prism.Core** 特性。
- Prism 8 异步命令由独立 NuGet **MvvmAIO.Prism.Bcl.Commands** 提供；消费者手动安装。
- 使用异步命令但缺少类型时报告 **PSG3002**（非嵌入回主包）。

## 后果

- **正面**：包边界清晰；Prism 9 零额外命令依赖。
- **负面**：Prism 8 用户须安装第二个包；发布需独立 API Key。

## 参考

- [CHANGELOG 0.2.0](../../CHANGELOG.md)
- [Prism.Bcl.Commands/README.md](../../Prism.Bcl.Commands/README.md)
