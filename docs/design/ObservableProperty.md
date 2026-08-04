# Design Doc: ObservableProperty

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

## API 与契约

| 特性 | 目标 | 说明 |
|------|------|------|
| `[ObservableProperty]` | field 或 partial property | 生成 `SetProperty` 属性；field 模式可用 `PropertyAccess` |
| `[NotifyPropertyChangedFor]` | 同成员 | 额外 `RaisePropertyChanged` |
| `[NotifyCanExecuteChangedFor]` | 同成员 | 命令 `RaiseCanExecuteChanged` |
| `[NotifyDataErrorInfo]` | 成员或类 | 见 [BindableValidator](BindableValidator.md) |

命名空间为 `Prism.SourceGenerators`（**MvvmAIO.Prism.Core**）。生成 getter/setter、`On{Name}Changing` / `On{Name}Changed` 四个 partial 重载、按需的 `*.ObservablePropertyChanging.g.cs`，并转发用户特性。

### 诊断

| ID | 级别 | 触发条件 |
|----|------|----------|
| PSG0001 | Error | 含 `[ObservableProperty]` 的类非 partial |
| PSG0003 | Error | partial property 目标未声明 partial |
| PSG2005 | Warning | `[NotifyCanExecuteChangedFor]` 命令未解析（含 `Save` / `SaveAsync` → `SaveCommand`，以及显式 `CommandName`） |
| PSG6001 | Info | 建议 field → partial property（C# 13+） |

### 不变量

1. Setter 经 `BindableBase.SetProperty`（或生成 BindableBase 路径）更新存储。
2. 相等性快速路径使用 `EqualityComparer<T>.Default`。
3. `OnChanging` 在写入前；`OnChanged` 在 `SetProperty` 回调内。
4. 生成器自有特性不参与转发；`FromNavigationParameter` / `FromDialogParameter` 在 partial property 模式亦不转发。

### 兼容基线

- C# 12−：field 后备。
- C# 13+：partial property + `field`。
- C# 14+：命令侧 `field`（见 [DelegateCommand](DelegateCommand.md)）。

### 不在范围内

- 跨属性验证规则引擎（由 BindableValidator + DataAnnotations 负责）。

## 参考

- `Prism.SourceGenerators.Tests/MatrixTests.cs`
