# Design Doc: ObservableProperty

> **关联 Spec**：[spec/ObservableProperty.md](../spec/ObservableProperty.md)
> **关联 ADR**：[ADR-005](../adr/ADR-005-syntax-tree-emission.md)、[ADR-006](../adr/ADR-006-incremental-generators.md)

## 概述

`ObservablePropertyGenerator` 是核心生成器，兼管特性转发、`PropertyChangingGenerator` 协作与 partial property / field 双模式。

## 实现概览

| 组件 | 路径 |
|------|------|
| 主生成器 | `ObservablePropertyGenerator.cs` |
| 属性变更前 | `PropertyChangingGenerator.cs` |
| 特性过滤 | `IsGeneratorOwnedObservablePropertyAttribute`（含 From*Parameter 抑制） |

### 增量管线

1. `ForAttributeWithMetadataName` 收集 `[ObservableProperty]` 目标。
2. 区分 field vs partial property（语言版本探测）。
3. 构建 `PropertyDeclarationSyntax` + `SetProperty` 调用链。
4. 合并 `NotifyPropertyChangedFor` / `NotifyCanExecuteChangedFor` / 验证调用。

### 特性转发

- field：无目标 / `[property:]` → 生成属性；`[field:]` 保留。
- partial property：`ValidationAttribute` 子类不复制到实现 partial（避免 CS0579）；`FromNavigationParameter` / `FromDialogParameter` 同理（0.8.1）。

## 设计权衡

- **对齐 CommunityToolkit.Mvvm** 的 `OnChanging` / `INotifyPropertyChanging` 行为，但 setter 走 Prism `SetProperty`。
- **PropertyAccess** 仅 field 模式；partial property 用声明修饰符。

## 已知局限

- 转发特性参数表达式按原文发射，复杂表达式可能需完全限定类型名。

## 参考

- `Prism.SourceGenerators.Tests/MatrixTests.cs`
