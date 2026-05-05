# 命令：`[DelegateCommand]` 与 `[AsyncDelegateCommand]`

承载命令的类型必须是 **`partial class`**（**PSG0002**）。

---

## 一、`[DelegateCommand]`：从方法生成命令属性

| Execute 方法形态 | 生成的命令类型 |
|------------------|----------------|
| `void` / `void M(T)` | `DelegateCommand` / `DelegateCommand<T>` |
| `async Task` / `Task`（无泛型结果） | `AsyncDelegateCommand` / `AsyncDelegateCommand<T>` |
| 返回 **`ValueTask`** / **`ValueTask<TResult>`** | 同上；生成代码通过 **`.AsTask()`** 接到 Prism 的 **`Func<Task>`** / **`Func<T, Task>`** 构造函数 |

**不支持：** execute 返回 **`Task<TResult>`**（带泛型结果的任务）作为该路径的异步 execute。

**限制（PSG1001）：** execute 方法若带 **`CancellationToken`** 参数，则**不能**与 **`ValueTask` / `ValueTask<TResult>`** 的返回类型组合（当前代码生成形状如此）。

### `CanExecute`

使用 **`CanExecute = nameof(SomeMethod)`**，解析为 **`Func<bool>`**、**`Func<T, bool>`** 或与参数个数匹配的 **`bool M()`** / **`bool M(T)`**。签名不匹配 → **PSG2006**（警告）。

### C# 语言版本与生成形态

- **C# 14+（LangVersion ≥ 14）**：命令属性可使用 **`field`**，例如 `public DelegateCommand XCommand => field ??= new DelegateCommand(X);`
- **更早版本**：传统 **`_xCommand`** 后备字段 + 惰性初始化

---

## 二、`[AsyncDelegateCommand]`：Prism 风格高级异步命令

用于需要 **`EnableParallelExecution`**、**`CancelAfter`**、**`Catch`**、**`CancellationTokenSourceFactory`**、**`ObservesCanExecute`** 等与 Prism 9 文档一致的 API 表面。

- **Prism 9+**：使用框架程序集中的类型。
- **Prism.Core 8.1.97**：必须安装 **`MvvmAIO.Prism.Bcl.Commands`**，否则缺少 **`AsyncDelegateCommand`** 类型 → **PSG3002**。

---

## 三、`[ObservesProperty]`

当列出的属性变化时，自动触发 **`CanExecute`** 的重新求值。可与 **`[DelegateCommand]`** 或 **`[AsyncDelegateCommand]`** 组合使用。

```csharp
[ObservableProperty]
private bool _isValid;

[DelegateCommand(CanExecute = nameof(CanSubmit))]
[ObservesProperty(nameof(IsValid))]
private void Submit() { /* ... */ }
```

多个属性：`[ObservesProperty(nameof(A), nameof(B))]`。

---

## 四、Prism 8 与 **PSG3002**  checklist

1. 已引用 **`MvvmAIO.Prism.SourceGenerators`**（带来 **`MvvmAIO.Prism.Core`** 特性）。  
2. 若使用 **异步命令** 且 Prism 为 **8.x**：已安装 **`MvvmAIO.Prism.Bcl.Commands`**。  
3. 仍报 **PSG3002**：检查是否错误地移除了 BCL 包、或包版本与 **`Prism.Core`** 不兼容。

---

## 五、相关诊断

| ID | 场景 |
|----|--------|
| PSG1001 | `[DelegateCommand]` 方法签名无效（含 `CancellationToken` + `ValueTask` 等组合） |
| PSG1002 | `[AsyncDelegateCommand]` 方法签名无效 |
| PSG2003 | `CanExecute` 成员未找到 |
| PSG2004 | `[ObservesProperty]` 指向的属性未找到 |
| PSG2006 | `CanExecute` 成员存在但签名与命令不匹配 |
| PSG3002 | 缺少 `AsyncDelegateCommand` 类型 |

详见 [诊断与排错](Diagnostics)。
