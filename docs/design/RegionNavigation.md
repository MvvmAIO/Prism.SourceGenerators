# Design Doc: RegionNavigation

> **关联 Spec**：[spec/RegionNavigation.md](../spec/RegionNavigation.md)

## 概述

`RegionNavigationGenerator` 处理 `[NavigateCommand]` 与 `[NavigateOnChanged]`。

## 实现概览

- `RegionNavigationGenerator.cs`、`RegionNavigationSyntax.cs`、`RegionNavigationMetadataExtractor.cs`
- `IRegionManager` 解析：字段、属性、主构造函数参数
- `NavigateOnChanged` 挂钩 `ObservableProperty` setter 末尾

## 参考

- `RegionNavigationGeneratorTests.cs`
