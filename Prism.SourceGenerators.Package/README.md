# MvvmAIO.Prism.SourceGenerators

Roslyn **source generators** for the [Prism](https://github.com/PrismLibrary/Prism) MVVM library — generate observable properties, delegate commands, and validation boilerplate at compile time.

[![CI](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

## Installation

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.7.0" />
```

> **Prism.Core 8.1.97 + async commands?** Also install [`MvvmAIO.Prism.Bcl.Commands`](https://www.nuget.org/packages/MvvmAIO.Prism.Bcl.Commands) for `AsyncDelegateCommand`. Prism 9+ already ships these types.

## Features at a Glance

| Attribute | What it generates |
|-----------|-------------------|
| `[ObservableProperty]` | Property with `SetProperty` call, `OnChanging`/`OnChanged` hooks, `INotifyPropertyChanging` support |
| `[DelegateCommand]` | `DelegateCommand` / `DelegateCommand<T>` property |
| `[AsyncDelegateCommand]` | `AsyncDelegateCommand` / `AsyncDelegateCommand<T>` with fluent chaining |
| `[NotifyPropertyChangedFor]` | Extra `PropertyChanged` notifications for dependent properties |
| `[NotifyCanExecuteChangedFor]` | Auto `RaiseCanExecuteChanged()` on related commands |
| `[ObservesProperty]` | `.ObservesProperty(() => Prop)` on generated commands |
| `[NotifyDataErrorInfo]` | `ValidateProperty()` in setter via `INotifyDataErrorInfo` |
| `[BindableBase]` | Full `INotifyPropertyChanged` implementation on any class |

## Quick Start

### Observable Properties

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // Field target (all C# versions)
    [ObservableProperty]
    private string _title = "Hello";

    // Partial property target (C# 13+)
    [ObservableProperty]
    public partial int Count { get; set; }
}
```

### Commands

```csharp
public partial class MainViewModel : BindableBase
{
    [DelegateCommand]
    private void Save() { /* ... */ }

    [AsyncDelegateCommand(CanExecute = nameof(CanLoad))]
    private async Task LoadAsync(CancellationToken ct) { /* ... */ }

    private bool CanLoad() => !IsBusy;
}
```

### Validation

```csharp
using System.ComponentModel.DataAnnotations;
using Prism.SourceGenerators;

public partial class FormViewModel : BindableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required, MinLength(2)]
    public partial string Username { get; set; }

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required, EmailAddress]
    public partial string Email { get; set; }
}
```

The generated setter calls `ValidateProperty(value, nameof(Property))` after setting the value. `BindableValidator` implements `INotifyDataErrorInfo` with `ValidateProperty()`, `ValidateAllProperties()`, `ClearErrors()`, and `ClearAllErrors()`.

### Property Change Notifications & Command Refresh

```csharp
public partial class EditorViewModel : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _firstName = "";

    [DelegateCommand(CanExecute = nameof(CanSave))]
    [ObservesProperty(nameof(FirstName))]
    private void Save() { /* ... */ }

    private bool CanSave() => !string.IsNullOrEmpty(FirstName);
    public string FullName => $"{FirstName}";
}
```

### BindableBase Generation

```csharp
[BindableBase]
public partial class LightViewModel
{
    // INotifyPropertyChanged + INotifyPropertyChanging + SetProperty<T> + RaisePropertyChanged
    // all generated — no need to inherit from Prism.Mvvm.BindableBase
}
```

## Diagnostics & Code Fixes

| ID | Severity | Description |
|----|----------|-------------|
| PSG0001 | Error | Class with `[ObservableProperty]` must be `partial` |
| PSG0002 | Error | Class with `[DelegateCommand]`/`[AsyncDelegateCommand]` must be `partial` |
| PSG0003 | Error | Property with `[ObservableProperty]` must be `partial` |
| PSG0004 | Error | Class with `[BindableBase]` must be `partial` |
| PSG1001 | Error | Invalid `[DelegateCommand]` method signature |
| PSG1002 | Error | Invalid `[AsyncDelegateCommand]` method signature |
| PSG2001 | Warning | Catch handler not found |
| PSG2002 | Warning | Catch handler has incompatible signature |
| PSG2003 | Warning | CanExecute member not found |
| PSG2004 | Warning | Observed property not found |
| PSG2005 | Warning | `[NotifyCanExecuteChangedFor]` command not found |
| PSG2006 | Warning | CanExecute signature incompatible with command |
| PSG3002 | Error | `AsyncDelegateCommand` type not found (install `MvvmAIO.Prism.Bcl.Commands` for Prism 8) |
| PSG4001 | Warning | ServiceType not assignable from implementation |
| PSG4002 | Warning | ViewModelType could not be resolved |
| PSG5001 | Warning | `[NotifyDataErrorInfo]` requires `BindableValidator` base type |

**PSG0001–PSG0004** have IDE quick fixes: **Ctrl+.** → add `partial` (supports "Fix all in document/project/solution").

## Compatibility

| | Supported |
|---|-----------|
| .NET | 6.0+ / .NET Standard 2.0 |
| Prism | 8.1.97, 9.0+ |
| C# | 10+ (partial properties require C# 13+) |
| Roslyn | 4.0.1, 4.3.1, 4.12.0, 5.0.0 |
| IDE | Visual Studio 2022 17.13+, Rider, VS Code + C# Dev Kit |

## Package Contents

This NuGet package includes:

- **Roslyn analyzers** (multi-targeted: Roslyn 4.0 / 4.3 / 4.12 / 5.0)
- **MvvmAIO.Prism.Core** library (attributes: `[ObservableProperty]`, `[DelegateCommand]`, `[AsyncDelegateCommand]`, `[BindableBase]`, `[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]`, `[ObservesProperty]`, `[NotifyDataErrorInfo]`, `BindableValidator` base class)
- This is a **development dependency** — no runtime DLLs are added to your output

## Resources

- 📖 [Full documentation (GitHub)](https://github.com/MvvmAIO/Prism.SourceGenerators)
- 📖 [Web docs (Blazor)](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)
- 🧪 [Samples (Avalonia)](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples)
- 📋 [Changelog](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/CHANGELOG.md)
- 🐛 [Issues](https://github.com/MvvmAIO/Prism.SourceGenerators/issues)

## License

MIT — see [LICENSE](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/LICENSE.txt).
