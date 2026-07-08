# Design Doc: DialogAware

> **关联 Spec**：[spec/DialogAware.md](../spec/DialogAware.md)
> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## 概述

`DialogAwareGenerator` 生成 `IDialogAware` 实现；Prism 9 路径涉及 `DialogCloseListener`。

## 实现概览

- `DialogAwareGenerator.cs`、`DialogAwareMetadataExtractor.cs`
- `FromDialogParameter` 在 `OnDialogOpened` 前绑定

## 已知局限

- 尚无生成 `CloseDialog` 辅助（ROADMAP F2）。

## 参考

- Samples `ConfirmDialogViewModel`
