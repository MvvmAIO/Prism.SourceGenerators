# Design Doc: DelegateCommand

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

## API 与契约

| 特性 | 说明 |
|------|------|
| `[DelegateCommand]` | `void` / `Task` / `ValueTask` / `Task<TResult>` 等生成 `DelegateCommand` / `AsyncDelegateCommand` |
| `[AsyncDelegateCommand]` | 支持 `EnableParallelExecution`、`CancelAfter`、`Catch` 等进阶选项 |
| `[ObservesProperty]` | `CanExecute` 观察属性变化 |

生成 `{Method}Command` 属性（C# 14+ 使用 `field ??=`），并接线 `ObservesCanExecute` 与 `RaiseCanExecuteChanged`。

命令属性名由共享 **Command Naming**（`NamingHelpers.GetCommandName`）派生：去掉末尾 `Async`，再一律追加 `Command`（与 `[NavigateCommand]` / `[ShowDialogCommand]` 相同）。可用特性上的 `CommandName` 覆盖。

### 诊断

| ID | 级别 | 触发条件 |
|----|------|----------|
| PSG0002 | Error | 含命令方法的类非 partial |
| PSG1001 | Error | `[DelegateCommand]` 签名无效 |
| PSG1002 | Error | `[AsyncDelegateCommand]` 签名无效 |
| PSG2001–2004 | Warning | Catch / CanExecute / Observes 未解析 |
| PSG2006 | Warning | CanExecute 签名不兼容 |
| PSG3002 | Error | 缺少 `AsyncDelegateCommand`（Prism 8 未装 Bcl.Commands） |

### 不变量

1. Prism 9+ 使用框架 `AsyncDelegateCommand`；Prism 8 使用 **Bcl.Commands**。
2. `CancellationToken` 与 `ValueTask` / `Task<TResult>` 组合受限（PSG1001）。
3. `ValueTask` 经 `.AsTask()` 接入 Prism 构造函数。

### 不在范围内

- `ICommand` 以外的 UI 绑定框架。

## 参考

- `Prism.SourceGenerators.Integration.Tests` — PSG3002
