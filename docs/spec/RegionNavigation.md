# Spec: RegionNavigation

> **版本**：v0.8.1
> **关联 Design Doc**：[design/RegionNavigation.md](../design/RegionNavigation.md)
> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## API 面

| 特性 | 说明 |
|------|------|
| `[NavigateCommand(Region, Target)]` | 生成 `DelegateCommand` → `IRegionManager.RequestNavigate` |
| `[NavigateOnChanged(TargetMember = ...)]` | 与 `[ObservableProperty]` 联用；值变化时导航 |

依赖：类型上可访问的 `IRegionManager`（字段 / 属性 / 构造函数注入）。

## 诊断 ID

| ID | 范围 |
|----|------|
| PSG7001 | 缺少 `IRegionManager` |
| PSG7002–7005 | Region / Target / NavigateOnChanged 规则 |

## 不变量

1. 仅区域导航（Region-first）；非 MAUI `INavigationService`。
2. 诊断带 **PSG7xxx**。

## 不在范围内

- Region 名称常量生成（ROADMAP F2）
