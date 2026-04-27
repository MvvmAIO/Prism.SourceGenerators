# Prism.SourceGenerators

Roslyn source generators for the [Prism](https://github.com/PrismLibrary/Prism) MVVM library.

## Project Structure

```
Prism.SourceGenerators/                        # Shared project (.shproj/.projitems/.props + source code)
Prism.SourceGenerators.Roslyn4001/             # Roslyn 4.0.1
Prism.SourceGenerators.Roslyn4031/             # Roslyn 4.3.1
Prism.SourceGenerators.Roslyn4120/             # Roslyn 4.12.0
Prism.SourceGenerators.Roslyn5000/             # Roslyn 5.0.0
Prism.SourceGenerators.Samples/               # WPF sample application
```

## Generators

### `[ObservableProperty]`

Generates observable properties for classes inheriting from `BindableBase`. Annotate a field with `[ObservableProperty]` and the generator will create a public property that calls `SetProperty` in the setter.

**Before (manual):**
```csharp
public class MainViewModel : BindableBase
{
    private string _title = "Hello";
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
```

**After (with source generator):**
```csharp
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    // The 'Title' property is auto-generated with SetProperty
}
```

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

## Diagnostics

| ID | Description |
|----|-------------|
| PSG0001 | Class with `[ObservableProperty]` field must be `partial` |
| PSG0002 | Class with `[DelegateCommand]` method must be `partial` |

## Building

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Requirements

- .NET SDK 9.0.200+ or .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit (for `.slnx` support)
