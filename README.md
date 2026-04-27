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

## Building

```bash
dotnet build Prism.SourceGenerators.slnx
```

## Requirements

- .NET SDK 9.0.200+ or .NET 10 SDK
- Visual Studio 2022 17.13+ / Rider / VS Code with C# Dev Kit (for `.slnx` support)
