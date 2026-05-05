# MvvmAIO Prism Source Generators Wiki

面向 **[Prism](https://github.com/PrismLibrary/Prism)** 的 Roslyn **源生成器**：在编译期生成与手写一致的 `SetProperty`、`DelegateCommand` / `AsyncDelegateCommand` 等样板代码，减少重复并保持与 **Prism.Mvvm.BindableBase** 同一套语义。

**主仓库**：[MvvmAIO/Prism.SourceGenerators](https://github.com/MvvmAIO/Prism.SourceGenerators)  
**NuGet 主包**：[MvvmAIO.Prism.SourceGenerators](https://www.nuget.org/packages/MvvmAIO.Prism.SourceGenerators)  
**Prism 8 异步命令扩展包**：[MvvmAIO.Prism.Bcl.Commands](https://www.nuget.org/packages/MvvmAIO.Prism.Bcl.Commands)  
**Avalonia 示例（独立仓库）**：[Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples)

---

## 本 Wiki 与 README 的分工

| 内容 | 建议阅读 |
|------|----------|
| 特性说明、长表格、英文为主 | 仓库根目录 [README.md](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.md) |
| 简体中文与 README 平行叙述 | [README.zh-CN.md](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.zh-CN.md) |
| **中文导读、架构、排错、FAQ、与发布相关的操作** | **本 Wiki 各页（下方导航）** |

---

## 文档地图

1. **[快速开始](Getting-Started)** — 安装、Prism 版本与包组合、第一个 ViewModel、必备 `partial`  
2. **[可观察属性](ObservableProperty)** — 字段 / 部分属性、`PropertyAccess`、钩子、`Notify*`、属性转发、`INotifyPropertyChanging`  
3. **[命令](Commands)** — `[DelegateCommand]` / `[AsyncDelegateCommand]`、`ValueTask`、CanExecute、`[ObservesProperty]`、Prism 8 与 PSG3002  
4. **[诊断与排错](Diagnostics)** — PSGxxxx 一览与处理建议  
5. **[架构与打包](Architecture)** — 多 Roslyn 版本、MSBuild 如何选择分析器、`MvvmAIO.Prism.Core` 注入逻辑  
6. **[构建、示例与 Wiki 维护](Build-and-samples)** — `slnx`、Nuke、独立示例仓库链接、如何把 `wiki/` 推送到本 Wiki 仓库  
7. **[常见问题（FAQ）](FAQ)** — 高频疑问集中解答  

---

## CI 状态

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)  
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/MvvmAIO/Prism.SourceGenerators/master/.github/badges/tests.json)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

构建产物中的 **`test-results`**（`.trx`）可用于在 CI 或本机分析失败用例。

---

## 版本与变更

权威变更记录见仓库 **[CHANGELOG.md](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/CHANGELOG.md)**（含 **Unreleased** 中与 `INotifyPropertyChanging`、**`PropertyAccess`**、`ValueTask` 等相关的说明）。
