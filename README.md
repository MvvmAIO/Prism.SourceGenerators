# Prism.SourceGenerators

**English** | [简体中文](README.zh-CN.md) | [日本語](README.ja.md)

Roslyn source generators for the [Prism](https://github.com/PrismLibrary/Prism) MVVM library.

## Project Structure

```
Prism.SourceGenerators/                        # Shared project (.shproj/.projitems/.props + source code)
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Samples.Prism9/         # WPF sample (Prism 9.0, native AsyncDelegateCommand)
Prism.SourceGenerators.Samples.Prism8/         # WPF sample (Prism 8.x, polyfill AsyncDelegateCommand)
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
- For Prism < 9.0 (which lacks `AsyncDelegateCommand`), a polyfill is generated automatically
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

Dedicated attribute for async methods with advanced Prism 9.0+ features.
Supports fluent configuration: `EnableParallelExecution`, `CancelAfter`, `Catch`, `CancellationTokenSourceFactory`.

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

## Diagnostics

| ID | Description |
|----|-------------|
| PSG0001 | Class with `[ObservableProperty]` members must be `partial` |
| PSG0002 | Class with `[DelegateCommand]` / `[AsyncDelegateCommand]` method must be `partial` |
| PSG0003 | Property with `[ObservableProperty]` must be declared as `partial` |

## Building

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Requirements

- .NET SDK 9.0.200+ or .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit (for `.slnx` support)
