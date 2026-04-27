# Prism.SourceGenerators

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

### `[DelegateCommand]`

Generates `DelegateCommand` or `AsyncDelegateCommand` properties from methods.

- **Synchronous methods** (`void`) generate `DelegateCommand` / `DelegateCommand<T>`
- **Async methods** (`Task`) generate `AsyncDelegateCommand` / `AsyncDelegateCommand<T>`
- For Prism < 9.0 (which lacks `AsyncDelegateCommand`), a polyfill is generated automatically

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
| PSG0001 | Class with `[ObservableProperty]` field must be `partial` |
| PSG0002 | Class with `[DelegateCommand]` / `[AsyncDelegateCommand]` method must be `partial` |
| PSG0003 | Property with `[ObservableProperty]` must be declared as `partial` |

## Building

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Requirements

- .NET SDK 9.0.200+ or .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit (for `.slnx` support)
