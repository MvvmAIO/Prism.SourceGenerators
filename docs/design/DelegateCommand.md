# Design Doc: DelegateCommand

> **关联 Spec**：[spec/DelegateCommand.md](../spec/DelegateCommand.md)
> **关联 ADR**：[ADR-003](../adr/ADR-003-bcl-commands-split-package.md)

## 概述

`DelegateCommandGenerator` 统一处理 `[DelegateCommand]` 与 `[AsyncDelegateCommand]`，解析 execute 签名、CanExecute、Catch、ObservesProperty。

## 实现概览

- 文件：`DelegateCommandGenerator.cs`
- Prism 版本探测决定 `AsyncDelegateCommand` 类型来源（框架 vs Bcl.Commands）
- C# 14+ 命令属性使用 `field ??=` 懒初始化

## 设计权衡

- `Task<TResult>` 经 async lambda 等待，结果不暴露到命令面。
- `ValueTask` 统一 `.AsTask()` 适配 Prism 构造函数。

## 已知局限

- `CancellationToken` + `Task<TResult>` / `ValueTask` 组合拒绝（PSG1001）。

## 参考

- `Prism.SourceGenerators.Integration.Tests` — PSG3002
