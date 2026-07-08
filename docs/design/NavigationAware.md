# Design Doc: NavigationAware

> **关联 Spec**：[spec/NavigationAware.md](../spec/NavigationAware.md)
> **关联 RFC**：[NavigationDialogAdvanced](../rfc/archive/NavigationDialogAdvanced.md)
> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## 概述

`NavigationAwareGenerator` + `NavigationAwareSyntax` / `NavigationAwareMetadataExtractor` 生成区域导航生命周期；参数绑定与 `ObservablePropertyGenerator` 协作。

## 实现概览

- `NavigationAwareGenerator.cs` — 接口成员与 `*Core` partial
- `FromNavigationParameter` 绑定语句插入 `OnNavigatedTo` 开头
- `PrismRegionsModel` / 程序集探测 — 选用 `INavigationAware` 命名空间

## 参考

- `RegionNavigationGeneratorTests.cs`、`NavigationDialogGeneratorTests.cs`
