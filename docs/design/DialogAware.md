# Design Doc: DialogAware

> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## 概述

`DialogAwareGenerator` 生成 `IDialogAware` 实现；Prism 9 路径涉及 `DialogCloseListener`。

## 实现概览

- `DialogAwareGenerator.cs`、`DialogAwareMetadataExtractor.cs`
- `ParameterBinding` — `[FromDialogParameter]` 提取与 `TryGetValue` 语句；Kind = Dialog

## 已知局限

- 尚无生成 `CloseDialog` 辅助（ROADMAP F2）。

## API 与契约

| 特性 | 说明 |
|------|------|
| `[DialogAware]` | 生成 `IDialogAware` 成员与对话框生命周期 `*Core` partial |
| `[FromDialogParameter(key)]` | 与 ObservableProperty 联用；在 `OnDialogOpened` 前绑定 |

Prism 9 使用 `Prism.Dialogs` / `DialogCloseListener`；Prism 8 使用 `Prism.Services.Dialogs`。

### 诊断

| ID | 级别 |
|----|------|
| PSG0008 | Error — 类非 partial |
| PSG7103–7105 | `[FromDialogParameter]` 校验 |

### 不变量

1. `Title`、`RequestClose` 等由生成器提供可覆盖 partial。
2. 参数特性在 partial property 模式不转发到实现 partial（0.8.1+）。
3. Parameter Binding 的 **Blocking Diagnostic**（Error）抑制整个 Aware 表面；**Warning**（PSG7104）只省略该 binding，仍发出 `IDialogAware`。

### 不在范围内

- `CloseDialog` 辅助方法（ROADMAP F2 候选）。

## 参考

- Samples `ConfirmDialogViewModel`
