# Prism.SourceGenerators

**English** | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

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
Prism.Core/                                    # MvvmAIO.Prism.Core (attributes), bundled in MvvmAIO.Prism.SourceGenerators
Prism.SourceGenerators.Core.Prism8/           # MvvmAIO.Prism.Core.Prism8 (Prism 8 AsyncDelegateCommand), bundled when Prism.Core 8.1.97
Prism.SourceGenerators.Samples.Prism9/         # Avalonia 12 sample (Prism 9.0, native AsyncDelegateCommand)
Prism.SourceGenerators.Samples.Prism8/         # Avalonia 12 sample (Prism 8.1.97; same MSBuild lib selection as the NuGet package)
```

## Generators

### `[ObservableProperty]`

Generates observable properties for classes inheriting from `BindableBase`. Supports two usage modes depending on the C# language version.

#### Field target (all C# versions)

Annotate a private field with `[ObservableProperty]` to generate a public property that calls `SetProperty` in the setter.

```csharp
// C# 12 or earlier
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    // Generated: public string Title { get => _title; set => SetProperty(ref _title, value); }
}
```

#### Partial property target (C# 13+ with `field` keyword)

Annotate a `partial` property with `[ObservableProperty]` to generate the implementing declaration using the `field` keyword (semi-auto property).

```csharp
// C# 13+ / .NET 9+ (requires LangVersion 13.0+ or preview)
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Hello";

    // Generated: public partial string Title { get => field; set => SetProperty(ref field, value); }
}
```

The partial property approach eliminates the need for a separate backing field and provides a cleaner API surface. Both modes can coexist in the same project.

#### OnChanged partial methods

For every `[ObservableProperty]`, the generator emits two `partial` method declarations that you can optionally implement to react to changes:

```csharp
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    public partial int Age { get; set; }

    // Generated declarations (implement one or both):
    // partial void OnAgeChanged(int value);
    // partial void OnAgeChanged(int oldValue, int newValue);

    partial void OnAgeChanged(int oldValue, int newValue)
    {
        Debug.WriteLine($"Age changed from {oldValue} to {newValue}");
    }
}
```

The generated setter uses `EqualityComparer<T>.Default.Equals` for change detection and calls both `OnChanged` overloads before raising `PropertyChanged`.

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

### `[DelegateCommand]`

Generates `DelegateCommand` or `AsyncDelegateCommand` properties from methods.

- **Synchronous methods** (`void`) generate `DelegateCommand` / `DelegateCommand<T>`
- **Async methods** (`Task`) generate `AsyncDelegateCommand` / `AsyncDelegateCommand<T>`
- For Prism &lt; 9.0, use NuGet **`MvvmAIO.Prism.SourceGenerators`**, which adds **`MvvmAIO.Prism.Core`** and, when **`Prism.Core` 8.1.97** is referenced, **`MvvmAIO.Prism.Core.Prism8`** so `AsyncDelegateCommand` exists. If those assemblies are missing while async commands are used, **PSG3002** is reported.
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
On Prism 9+, uses the framework types; on Prism 8.1.97, the **`MvvmAIO.Prism.SourceGenerators`** package applies **`MvvmAIO.Prism.Core.Prism8`** for the same fluent surface: `EnableParallelExecution`, `CancelAfter`, `Catch`, `CancellationTokenSourceFactory`, and `ObservesCanExecute`.

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
| PSG3002 | `AsyncDelegateCommand` not found; use **`MvvmAIO.Prism.SourceGenerators`** (NuGet) on Prism.Core 8.1.97 (or upgrade to Prism 9+) |

## Installation

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.1.6" />
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
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.1.6

# Publish NuGet package (MvvmAIO.Prism.SourceGenerators, includes MvvmAIO.Prism.Core assemblies)
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.1.6 --nuget-api-key <NUGET_API_KEY>
```

## Requirements

- .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit (for `.slnx` support)
