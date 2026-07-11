# Design Doc: DialogServiceCommand

> **关联 ADR**：[ADR-004](../adr/ADR-004-prism8-prism9-api-resolution.md)

## 概述

`DialogServiceCommandGenerator` 生成 `ShowDialog` 命令与可选 `On{Name}DialogClosed` partial。

## 实现概览

- `DialogServiceCommandGenerator.cs`、`DialogServiceCommandSyntax.cs`
- `ShowDialogCommandGenerationInfo` 模型

## API 与契约

| 特性 | 说明 |
|------|------|
| `[ShowDialogCommand(Name = "...")]` | `IDialogService.ShowDialog` 与可选 `On{Name}DialogClosed` partial |

### 诊断

| ID | 说明 |
|----|------|
| PSG7101 | 缺少 `IDialogService` |
| PSG7102 | 对话框 `Name` 必填 |

### 不变量

1. 生成 `DelegateCommand`，非 async 包装，除非 execute 方法本身异步。
2. 探测 Prism 8 / 9 服务接口命名空间。

### 不在范围内

- 对话框 View 注册（`RegisterDialog` 在 View code-behind）。

## 参考

- `DialogServiceCommandGeneratorTests.cs`
