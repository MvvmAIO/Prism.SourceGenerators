# Spec: DelegateCommand

> **版本**：v0.8.1
> **关联 Design Doc**：[design/DelegateCommand.md](../design/DelegateCommand.md)
> **关联 ADR**：[ADR-003](../adr/ADR-003-bcl-commands-split-package.md)

## API 面

### 特性

| 特性 | 说明 |
|------|------|
| `[DelegateCommand]` | `void` / `Task` / `ValueTask` / `Task<TResult>` 等 → `DelegateCommand` / `AsyncDelegateCommand` |
| `[AsyncDelegateCommand]` | 进阶：`EnableParallelExecution`、`CancelAfter`、`Catch` 等 |
| `[ObservesProperty]` | `CanExecute` 观察属性变化 |

### 生成器产出

- `{Method}Command` 属性（C# 14+ 使用 `field ??=`）
- `ObservesCanExecute` / `RaiseCanExecuteChanged` 接线

## 诊断 ID

| ID | 级别 | 触发条件 |
|----|------|----------|
| PSG0002 | Error | 含命令方法的类非 partial |
| PSG1001 | Error | `[DelegateCommand]` 签名无效 |
| PSG1002 | Error | `[AsyncDelegateCommand]` 签名无效 |
| PSG2001–2004 | Warning | Catch / CanExecute / Observes 未解析 |
| PSG2006 | Warning | CanExecute 签名不兼容 |
| PSG3002 | Error | 缺少 `AsyncDelegateCommand`（Prism 8 未装 Bcl.Commands） |

## 不变量

1. Prism 9+ 使用框架 `AsyncDelegateCommand`；Prism 8 使用 **Bcl.Commands**。
2. `CancellationToken` 与 `ValueTask` / `Task<TResult>` 组合受限（PSG1001）。
3. `ValueTask` 经 `.AsTask()` 接入 Prism 构造函数。

## 不在范围内

- `ICommand` 以外的 UI 绑定框架
