# Design Doc: 架构与打包

> **关联 ADR**：[ADR-001](../adr/ADR-001-multi-roslyn-analyzer-layout.md)、[ADR-002](../adr/ADR-002-shared-project-shproj.md)、[ADR-003](../adr/ADR-003-bcl-commands-split-package.md)

## 概述

本仓库产出两个 NuGet 包的分析器与特性布局，以及测试/CI 如何固定 Roslyn 版本。

## 项目划分

| 项目 | 产出 |
|------|------|
| `Prism.SourceGenerators/`（共享项） | 生成器 C# 源码 |
| `Roslyn4001` … `Roslyn5000` | 各 API 带的 `Prism.SourceGenerators.dll` |
| `Prism.SourceGenerators.Core` | `MvvmAIO.Prism.Core.dll` → `lib/netstandard2.0` |
| `Prism.SourceGenerators.Package` | 元包 + targets |
| `Prism.Bcl.Commands` | 独立 async 命令包 |

## MSBuild 选择 Roslyn 目录

`MvvmAIO.Prism.SourceGenerators.targets` 读取编译器文件版本 → `roslyn4.0` / `4.3` / `4.12` / `5.0`；失败回退 **4.12**。

本地 `ProjectReference` 生成器时设 `MvvmAIOPrismSourceGeneratorsImportAnalyzers=false` 避免重复 analyzer。

## Dependabot 策略

忽略：`Prism.Core`、`Microsoft.CodeAnalysis.*`、`Microsoft.Bcl.AsyncInterfaces`。`PolyfillVersion` 与 `PrismSourceGeneratorsTestsRoslynVersion` 在 `Directory.Build.props` 集中维护。

## 已知局限

- `System.IO.Hashing` 来自 Bcl.Commands targets 的上游警告，不阻断 CI。
- Bcl.Commands 与主包可独立发版（不同 API Key）。

## 参考

- [wiki/Architecture.md](../../wiki/Architecture.md)（Wiki 同步副本）
- [DEVELOPMENT.md](../DEVELOPMENT.md)
