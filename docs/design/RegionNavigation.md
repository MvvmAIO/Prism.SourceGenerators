# Design Doc: RegionNavigation

## 概述

`RegionNavigationGenerator` 处理 `[NavigateCommand]` 与 `[NavigateOnChanged]`。

## 实现概览

- `RegionNavigationGenerator.cs`、`RegionNavigationSyntax.cs`、`RegionNavigationMetadataExtractor.cs`
- `IRegionManager` 解析：字段、属性、主构造函数参数
- `NavigateOnChanged` 挂钩 `ObservableProperty` setter 末尾

## API 与契约

| 特性 | 说明 |
|------|------|
| `[NavigateCommand(Region, Target)]` | 生成 `DelegateCommand`，调用 `IRegionManager.RequestNavigate`；命令名遵循共享 Command Naming（剥 `Async` 后追加 `Command`） |
| `[NavigateOnChanged(TargetMember = ...)]` | 与 `[ObservableProperty]` 联用；值变化时导航 |

依赖类型上可访问的 `IRegionManager`（字段、属性或构造函数注入）。

### 诊断

| ID | 范围 |
|----|------|
| PSG7001 | 缺少 `IRegionManager` |
| PSG7002–7005 | Region / Target / NavigateOnChanged 规则 |

### 不变量

1. 仅区域导航（Region-first）；非 MAUI `INavigationService`。
2. 诊断使用 **PSG7xxx**。

### 不在范围内

- Region 名称常量生成（ROADMAP F2）。

## 参考

- `RegionNavigationGeneratorTests.cs`
