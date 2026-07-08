# Design Doc: DialogServiceCommand

> **关联 Spec**：[spec/DialogServiceCommand.md](../spec/DialogServiceCommand.md)

## 概述

`DialogServiceCommandGenerator` 生成 `ShowDialog` 命令与可选 `On{Name}DialogClosed` partial。

## 实现概览

- `DialogServiceCommandGenerator.cs`、`DialogServiceCommandSyntax.cs`
- `ShowDialogCommandGenerationInfo` 模型

## 参考

- `DialogServiceCommandGeneratorTests.cs`
