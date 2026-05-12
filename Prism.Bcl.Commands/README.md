# MvvmAIO.Prism.Bcl.Commands

**Prism 8** companion library that provides **`AsyncDelegateCommand`**, **`AsyncDelegateCommand<T>`**, and **`IAsyncCommand`** for apps using **Prism.Core 8.1.97** together with [MvvmAIO.Prism.SourceGenerators](https://www.nuget.org/packages/MvvmAIO.Prism.SourceGenerators).

> **Prism 9+** already ships these types — you do **not** need this package if you're on Prism 9 or later.

## When Do You Need This?

| Scenario | Need this package? |
|----------|-------------------|
| Prism.Core **8.1.97** + `[AsyncDelegateCommand]` source generator | **Yes** |
| Prism.Core **8.1.97** + hand-written `AsyncDelegateCommand` | **Yes** |
| Prism **9.0+** (any usage) | **No** — types already included |
| Only using synchronous `[DelegateCommand]` on Prism 8 | **No** |

Without this package on Prism 8, the analyzer reports **PSG3002** — `AsyncDelegateCommand` type not found.

## Installation

```xml
<PackageReference Include="MvvmAIO.Prism.Bcl.Commands" Version="0.3.1" />
```

Or:

```bash
dotnet add package MvvmAIO.Prism.Bcl.Commands
```

> You also need [`MvvmAIO.Prism.SourceGenerators`](https://www.nuget.org/packages/MvvmAIO.Prism.SourceGenerators) for the Roslyn generators and attribute definitions.

## What's Included

| Type | Description |
|------|-------------|
| `AsyncDelegateCommand` | Non-generic async command with `Func<Task>` / `Func<CancellationToken, Task>` |
| `AsyncDelegateCommand<T>` | Generic async command with `Func<T, Task>` / `Func<T, CancellationToken, Task>` |
| `IAsyncCommand` | Async command abstraction (`ExecuteAsync` with optional `CancellationToken`) |

### Fluent API

Both `AsyncDelegateCommand` and `AsyncDelegateCommand<T>` support Prism 9–style fluent chaining:

```csharp
[AsyncDelegateCommand(
    CanExecute = nameof(CanSave),
    EnableParallelExecution = true,
    Catch = nameof(HandleError))]
private async Task SaveAsync(CancellationToken ct)
{
    await _repository.SaveAsync(ct);
}

private bool CanSave() => IsValid;
private void HandleError(Exception ex) => Logger.Error(ex);
```

Available fluent methods:
- `.EnableParallelExecution()` — allow concurrent executions
- `.CancelAfter(TimeSpan)` — auto-cancel after timeout
- `.CancellationTokenSourceFactory(Func<CancellationToken>)` — custom token source
- `.Catch(Action<Exception>)` / `.Catch<TException>(Action<TException>)` — error handling
- `.ObservesProperty(() => Property)` — re-evaluate `CanExecute` on property change
- `.ObservesCanExecute(() => BoolProperty)` — bind `CanExecute` to a boolean property

## Compatibility

| | Supported |
|---|-----------|
| Target frameworks | .NET Standard 2.0, .NET 6.0+ |
| Prism.Core | 8.1.97 |
| Dependencies | `Prism.Core` 8.1.97, `System.Threading.Tasks.Extensions` 4.6.3 (netstandard2.0 only) |

## Typical Project Setup (Prism 8)

```xml
<ItemGroup>
    <!-- Source generators + attributes -->
    <PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.2.0" />
    <!-- Async command types for Prism 8 -->
    <PackageReference Include="MvvmAIO.Prism.Bcl.Commands" Version="0.3.1" />
    <!-- Prism framework -->
    <PackageReference Include="Prism.Core" Version="8.1.97" />
</ItemGroup>
```

## Resources

- 📖 [Full documentation (GitHub)](https://github.com/MvvmAIO/Prism.SourceGenerators)
- 📖 [Web docs (Blazor)](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)
- 🧪 [Samples (Avalonia)](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples)
- 📋 [Changelog](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/CHANGELOG.md)
- 🐛 [Issues](https://github.com/MvvmAIO/Prism.SourceGenerators/issues)

## License

MIT — see [LICENSE](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/LICENSE.txt).
