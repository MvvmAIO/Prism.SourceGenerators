# ADR-004: Prism 8 / 9 API 引用探测

| 字段 | 值 |
|------|-----|
| **状态** | Accepted |
| **日期** | 2026-06-17 |
| **关联 Design Doc** | [NavigationAware](../design/NavigationAware.md) / [DialogAware](../design/DialogAware.md) |

## 背景

Prism 9 将区域导航与对话框 API 迁至新命名空间（`Prism.Navigation.Regions`、`Prism.Dialogs`），Prism 8 仍使用 `Prism.Regions`、`Prism.Services.Dialogs`。硬编码单一命名空间无法同时支持两版。

## 决策

生成器在编译期根据**已引用程序集**选择发出类型的完全限定名，而非假定单一 Prism 主版本。`[NavigationAware]`、`[DialogAware]`、`[NavigateCommand]`、`[ShowDialogCommand]` 等均遵循此规则。集成测试契约程序集覆盖 Prism 8 专用 API。

## 后果

- **正面**：单包同时服务 Prism 8 / 9 样本与消费者。
- **负面**：探测逻辑须随 Prism 版本演进维护；MAUI 等非 Region 场景不在范围内。

## 参考

- [NavigationAware](../design/NavigationAware.md)
- [DialogAware](../design/DialogAware.md)
