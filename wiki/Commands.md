# Commands

Types with **`[DelegateCommand]`** or **`[AsyncDelegateCommand]`** must be **`partial`**.

## DelegateCommand

**`[DelegateCommand]`** on a method generates a command property:

| Execute method | Generated type |
|----------------|----------------|
| `void` / `void M(T)` | `DelegateCommand` / `DelegateCommand<T>` |
| `Task`, `ValueTask`, `ValueTask<TResult>` (see restrictions) | `AsyncDelegateCommand` / `AsyncDelegateCommand<T>` |

**`Task<TResult>`** is **not** supported as the execute return type for this attribute path.

**`CancellationToken` + `ValueTask` / `ValueTask<TResult>`** combinations are rejected (**PSG1001**) with the current emission shape.

`ValueTask` / `ValueTask<TResult>` are adapted with **`.AsTask()`** so Prism’s **`Func<Task>`** / **`Func<T, Task>`** constructors are used.

Use **`CanExecute = nameof(SomeBoolMethod)`** for **`Func<bool>`** / **`Func<T, bool>`**-compatible members. Wrong shape → **PSG2006**.

### C# language version

- **C# 14+:** command property may use the **`field`** keyword (no separate backing field).
- **Earlier:** traditional **`_command` backing field**.

## AsyncDelegateCommand

**`[AsyncDelegateCommand]`** is the dedicated attribute for async methods with Prism-style options (e.g. **`EnableParallelExecution`**, **`CancelAfter`**, **`Catch`**, **`CancellationTokenSourceFactory`**, **`ObservesCanExecute`**).

On **Prism 9+**, framework types are used. On **Prism.Core 8.1.97**, install **`MvvmAIO.Prism.Bcl.Commands`** for the same surface area.

## ObservesProperty

**`[ObservesProperty(nameof(...))]`** re-evaluates **`CanExecute`** when the listed properties change. Works with both **`[DelegateCommand]`** and **`[AsyncDelegateCommand]`**.

## Prism 8 and PSG3002

If **`AsyncDelegateCommand`** types are missing at compile time (Prism 8 without **`MvvmAIO.Prism.Bcl.Commands`**), the analyzer reports **PSG3002**. Install the BCL package or upgrade to Prism 9+.

More detail: [Getting Started](Getting-Started), [Build and samples](Build-and-samples).
