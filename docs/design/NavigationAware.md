# Design Doc: NavigationAware

> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## 概述

`NavigationAwareGenerator` + `NavigationAwareSyntax` / `NavigationAwareMetadataExtractor` 生成区域导航生命周期；参数绑定与 `ObservablePropertyGenerator` 协作。

## 实现概览

- `NavigationAwareGenerator.cs` — 接口成员与 `*Core` partial
- `ParameterBinding` — `[FromNavigationParameter]` 提取与 `TryGetValue` 语句；Kind = Navigation
- `PrismRegionsModel` / 程序集探测 — 选用 `INavigationAware` 命名空间

## API 与契约

| 特性 | 说明 |
|------|------|
| `[NavigationAware]` | 类级；生成 `INavigationAware` 成员与 `*Core` partial 钩子 |
| `[FromNavigationParameter(key)]` | field / partial property 与 `[ObservableProperty]` 联用；在 `OnNavigatedTo` 前绑定 |

生成 `OnNavigatedTo`、`OnNavigatedFrom`、`IsNavigationTarget` 及对应 `*Core` partial；参数通过 `TryGetValue<T>` 写入属性 setter。

### 诊断

| ID | 级别 |
|----|------|
| PSG0007 | Error — 类非 partial |
| PSG7006–PSG7008 | `[FromNavigationParameter]` 目标 / ObservableProperty / 空 key |

### 不变量

1. 接口命名空间由引用程序集探测：Prism 8 为 `Prism.Regions`，Prism 9 为 `Prism.Navigation.Regions`。
2. 参数绑定在 `OnNavigatedToCore` **之前**执行。
3. Parameter Binding 的 **Blocking Diagnostic**（Error）抑制整个 Aware 表面；**Warning**（PSG7007）只省略该 binding，仍发出 `INavigationAware`。

### 不在范围内

- 视图注册（`RegisterForNavigation` 在 View code-behind）。

## 参考

- `RegionNavigationGeneratorTests.cs`、`NavigationDialogGeneratorTests.cs`
