# Spec: DialogAware

> **版本**：v0.8.1
> **关联 Design Doc**：[design/DialogAware.md](../design/DialogAware.md)
> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## API 面

| 特性 | 说明 |
|------|------|
| `[DialogAware]` | 生成 `IDialogAware` 成员 + 对话框生命周期 `*Core` partial |
| `[FromDialogParameter(key)]` | 与 ObservableProperty 联用；`OnDialogOpened` 前绑定 |

Prism 9：`Prism.Dialogs` / `DialogCloseListener`；Prism 8：`Prism.Services.Dialogs`。

## 诊断 ID

| ID | 级别 |
|----|------|
| PSG0008 | Error — 类非 partial |
| PSG7103–7105 | `[FromDialogParameter]` 校验 |

## 不变量

1. `Title`、`RequestClose` 等由生成器提供可覆盖 partial。
2. 参数特性在 partial property 模式不转发到实现 partial（0.8.1+）。

## 不在范围内

- `CloseDialog` 辅助方法（ROADMAP F2 候选）
