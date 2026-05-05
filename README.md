# Prism.SourceGenerators

**English** | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

参与贡献见 [CONTRIBUTING.md](CONTRIBUTING.md)。

Roslyn source generators for the [Prism](https://github.com/PrismLibrary/Prism) MVVM library.

## CI Status

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)
[![Tests](https://img.shields.io/endpoint?url=https://raw.githubusercontent.com/MvvmAIO/Prism.SourceGenerators/master/.github/badges/tests.json)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

- Open the workflow page above to see the latest pipeline status.
- The `Tests` badge displays the latest passed/failed/skipped counts.
- The run also uploads a `test-results` artifact (`.trx`) for detailed test reports.

## Project Structure

```
Prism.SourceGenerators/                        # Shared project (.shproj/.projitems/.props + source code)
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Core/                   # MvvmAIO.Prism.Core (attributes), bundled in MvvmAIO.Prism.SourceGenerators
Prism.Bcl.Commands/                            # MvvmAIO.Prism.Bcl.Commands (Prism 8 AsyncDelegateCommand package, install manually)
```

Samples (Avalonia): [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) — Prism 8 / Prism 9 demo apps consuming **`MvvmAIO.Prism.SourceGenerators`** from NuGet.

## Generators

### `[ObservableProperty]`

Generates observable properties for classes inheriting from `BindableBase`. Supports two usage modes depending on the C# language version.

#### Field target (all C# versions)

Annotate a private field with `[ObservableProperty]` to generate a property that calls `SetProperty` in the setter. By default the generated property is **`public`**. Pass **`PropertyAccess`** (positional or named `PropertyAccess = …`) to choose another accessibility (`internal`, `protected`, `private`, `protected internal`, `private protected`).

```csharp
// C# 12 or earlier
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty(PropertyAccess.Internal)]
    // or: [ObservableProperty(PropertyAccess = PropertyAccess.Internal)]
    private int _count;

    // Generated: setter calls OnTitleChanging*, then BindableBase.SetProperty(ref _title, value, () => { OnTitleChanged*; }),
    // then optional RaisePropertyChanged for [NotifyPropertyChangedFor] / command refresh attributes.
}
```

For **partial property** targets, the accessibility on the property declaration is used; `PropertyAccess` is ignored.

#### Partial property target (C# 13+ with `field` keyword)

Annotate a `partial` property with `[ObservableProperty]` to generate the implementing declaration using the `field` keyword (semi-auto property).

```csharp
// C# 13+ / .NET 9+ (requires LangVersion 13.0+ or preview)
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";

    // Generated: same SetProperty(ref field, value, onChanged) pattern with OnChanging/OnChanged hooks.
}
```

The partial property approach eliminates the need for a separate backing field and provides a cleaner API surface. Both modes can coexist in the same project.

#### OnChanging / OnChanged partial methods

For every `[ObservableProperty]`, the generator emits four `partial` method declarations that you can optionally implement to react to changes. `OnXxxChanging` hooks run **before** the backing field is updated; `OnXxxChanged` hooks run **after**:

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // Generated declarations (implement any subset):
    // partial void OnAgeChanging(int value);
    // partial void OnAgeChanging(int oldValue, int newValue);
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanging(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age about to change from {oldValue} to {newValue}");
    }

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age changed from {oldValue} to {newValue}");
    }
}
```

The generated setter uses `EqualityComparer<T>.Default.Equals` for an early-out. When the value differs, it calls both `OnChanging` overloads, then `SetProperty(ref storage, value, onChanged)` so overrides of `SetProperty` run on the same path as hand-written Prism properties. The `onChanged` callback invokes both `OnChanged` overloads, then `SetProperty` raises `PropertyChanged` for the generated property. Any `[NotifyPropertyChangedFor]` names and `[NotifyCanExecuteChangedFor]` command refreshes are emitted after that call.

### `[NotifyPropertyChangedFor]`

Apply to a field or partial property alongside `[ObservableProperty]` to automatically raise `PropertyChanged` for additional dependent properties when the annotated property changes.

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _firstName = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    private string _lastName = "";

    public string FullName => $"{FirstName} {LastName}";
}
```

Supports multiple property names via `[NotifyPropertyChangedFor(nameof(A), nameof(B))]` or multiple attribute instances.

### `[NotifyCanExecuteChangedFor]`

Apply to a field or partial property alongside `[ObservableProperty]` to automatically invoke `RaiseCanExecuteChanged()` on the named commands when the property value changes.

```csharp
public partial class EditorViewModel : BindableBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = "";

    [DelegateCommand(CanExecute = nameof(CanSave))]
    private void Save() { /* ... */ }

    private bool CanSave() => !string.IsNullOrEmpty(Name);
}
```

The generated setter calls `SaveCommand?.RaiseCanExecuteChanged()` after `RaisePropertyChanged`. Multiple commands are supported via `[NotifyCanExecuteChangedFor(nameof(A), nameof(B))]` or repeated attributes. Names may reference either an existing member on the type or the generated command of a method annotated with `[DelegateCommand]` / `[AsyncDelegateCommand]` (e.g. method `Save` yields `SaveCommand`). Unresolved names are reported as **PSG2005** (warning) and the setter is still emitted.

### Forwarding attributes to the generated property

For **field** targets, attributes you write with the explicit `[property: Xxx]` target are forwarded onto the generated property:

```csharp
public partial class Vm : BindableBase
{
    [ObservableProperty]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.ComponentModel.DataAnnotations.Required]
    private string _password = "";
}
```

becomes

```csharp
[global::System.Text.Json.Serialization.JsonIgnoreAttribute]
[global::System.ComponentModel.DataAnnotations.RequiredAttribute]
public string Password { get { ... } set { ... } }
```

For **partial property** targets, every attribute you put on the partial declaration is forwarded onto the implementing declaration (with the exception of generator-owned attributes — `[ObservableProperty]`, `[NotifyPropertyChangedFor]`, `[NotifyCanExecuteChangedFor]` — which are stripped). Forwarded attributes are emitted with fully-qualified type names so they do not depend on `using` directives present in the generated file.

> Argument expressions inside the forwarded attribute are emitted verbatim. Use literal/`nameof`/`typeof` arguments or fully-qualified type references in argument positions if your `using` directives aren't visible to the generated file.

### `[DelegateCommand]`

Generates `DelegateCommand` or `AsyncDelegateCommand` properties from methods.

- **Synchronous methods** (`void`) generate `DelegateCommand` / `DelegateCommand<T>`
- **Async methods** returning non-generic **`Task`**, **`ValueTask`**, or **`ValueTask<TResult>`** generate `AsyncDelegateCommand` / `AsyncDelegateCommand<T>`. `ValueTask` / `ValueTask<TResult>` are wired via `.AsTask()` in the generated delegate so Prism’s `Func<Task>` / `Func<T, Task>` constructors are used. **`Task<TResult>`** is not supported for execute methods (unchanged). Execute methods that take a **`CancellationToken`** cannot return `ValueTask` / `ValueTask<TResult>` with the current emission shape (**PSG1001**).
- For Prism &lt; 9.0, use NuGet **`MvvmAIO.Prism.SourceGenerators`**, which adds **`MvvmAIO.Prism.Core`** for source-generator attributes. For Prism.Core 8.1.97 async commands, install **`MvvmAIO.Prism.Bcl.Commands`** manually so `AsyncDelegateCommand` exists. If those assemblies are missing while async commands are used, **PSG3002** is reported.
- **C# 14+**: Command properties use the `field` keyword (no separate backing field)
- **C# 13 and earlier**: Command properties use a traditional backing field

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // Generates: DelegateCommand IncrementCommand
    [DelegateCommand]
    private void Increment() { /* ... */ }

    // Generates: AsyncDelegateCommand LoadDataCommand
    [DelegateCommand]
    private async Task LoadDataAsync() { /* ... */ }

    // With CanExecute support
    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    private void Submit() { /* ... */ }
    private bool CanSubmit() => true;
}
```

#### Generated output comparison

**C# 14+ (LangVersion >= 14)** — uses `field` keyword:
```csharp
// No backing field needed
public DelegateCommand IncrementCommand => field ??= new DelegateCommand(Increment);
```

**C# 13 and earlier** — traditional backing field:
```csharp
private DelegateCommand? _incrementCommand;
public DelegateCommand IncrementCommand => _incrementCommand ??= new DelegateCommand(Increment);
```

### `[AsyncDelegateCommand]`

Dedicated attribute for async methods with advanced Prism-style features.
On Prism 9+, uses the framework types; on Prism 8.1.97, install **`MvvmAIO.Prism.Bcl.Commands`** for the same fluent surface: `EnableParallelExecution`, `CancelAfter`, `Catch`, `CancellationTokenSourceFactory`, and `ObservesCanExecute`.

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    // Parallel execution enabled
    [AsyncDelegateCommand(EnableParallelExecution = true)]
    private async Task FetchDataAsync() { /* ... */ }

    // With error handling and CanExecute
    [AsyncDelegateCommand(CanExecute = nameof(CanSave), Catch = nameof(HandleError))]
    private async Task SaveAsync() { /* ... */ }

    private bool CanSave() => true;
    private void HandleError(Exception ex) { /* ... */ }
}
```

### `[ObservesProperty]`

Automatically re-evaluates `CanExecute` when the specified properties change.
Works with both `[DelegateCommand]` and `[AsyncDelegateCommand]`.

```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private bool _isValid;

    [DelegateCommand(CanExecute = nameof(CanSubmit))]
    [ObservesProperty(nameof(IsValid))]
    private void Submit() { /* ... */ }

    // Multiple properties
    [AsyncDelegateCommand(CanExecute = nameof(CanSave))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync() { /* ... */ }
}
```

### `[BindableBase]`

Apply to a class that does **not** inherit from `Prism.Mvvm.BindableBase` to automatically generate an `INotifyPropertyChanged` implementation. The generated code includes `PropertyChanged` event, `SetProperty<T>`, `RaisePropertyChanged`, and `OnPropertyChanged` methods.

```csharp
using Prism.SourceGenerators;

[BindableBase]
public partial class SimpleViewModel
{
    private string _message = "Hello!";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }
}
```

If the class already inherits from `BindableBase` or a base class that implements `INotifyPropertyChanged`, no code is generated.

## Diagnostics

| ID | Description |
|----|-------------|
| PSG0001 | Class with `[ObservableProperty]` members must be `partial` |
| PSG0002 | Class with `[DelegateCommand]` / `[AsyncDelegateCommand]` method must be `partial` |
| PSG0003 | Property with `[ObservableProperty]` must be declared as `partial` |
| PSG0004 | Class with `[BindableBase]` must be `partial` |
| PSG1001 | Method signature is invalid for `[DelegateCommand]` |
| PSG1002 | Method signature is invalid for `[AsyncDelegateCommand]` |
| PSG2001 | Catch handler member was not found |
| PSG2002 | Catch handler signature is not compatible |
| PSG2003 | CanExecute member was not found |
| PSG2004 | Observed property was not found |
| PSG2005 | `[NotifyCanExecuteChangedFor]` references a command that was not found |
| PSG2006 | `CanExecute` names a member whose signature is not compatible with the command |
| PSG3002 | `AsyncDelegateCommand` not found; install **`MvvmAIO.Prism.SourceGenerators`** and, on Prism.Core 8.1.97, **`MvvmAIO.Prism.Bcl.Commands`** (or upgrade to Prism 9+) |

> **Quick fix:** PSG0001–PSG0004 all have an IDE code fix that inserts the missing `partial` modifier (Ctrl+. / Alt+Enter on the squiggle, or "Fix all in document/project/solution" to apply across the whole codebase).

## Installation

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.2.0" />
```

Or:

```bash
dotnet add package MvvmAIO.Prism.SourceGenerators
```

## Building

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Nuke Build

This repository uses [Nuke](https://nuke.build/) as the build orchestration layer for local automation and CI.

- Main source solution: `Prism.SourceGenerators.slnx`
- Build automation solution: `build.slnx` (contains only `build/_build.csproj`)

Common commands:

```bash
# CI pipeline locally (clean + restore + compile + test)
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# Pack NuGet package (optionally override version)
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0

# Publish NuGet packages (MvvmAIO.Prism.SourceGenerators + MvvmAIO.Prism.Bcl.Commands)
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.2.0 --nuget-api-key <NUGET_API_KEY>
```

## Requirements

- .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit (for `.slnx` support)


