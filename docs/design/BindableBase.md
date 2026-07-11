# Design Doc: BindableBase

## 概述

`BindableBaseGenerator` 为无 `BindableBase` 基类的 partial 类型生成 INPC 基础设施。

## 实现概览

- `BindableBaseGenerator.cs`
- 与 `PropertyChangingGenerator` 协调 `INotifyPropertyChanging`

## API 与契约

`[BindableBase]` 于未继承 `Prism.Mvvm.BindableBase` 的 **partial class**：生成 `INotifyPropertyChanged`、`SetProperty`、`RaisePropertyChanged` 与 `OnPropertyChanged`。若已继承 `BindableBase` 或层级中已有 `INotifyPropertyChanged`，则不生成。

### 诊断

| ID | 说明 |
|----|------|
| PSG0004 | 类非 partial |

### 不变量

- 始终考虑 `INotifyPropertyChanging`；可通过 `FeatureSwitches` 关闭运行时行为。

## 参考

- `BindableBaseGenerator` 单元测试
