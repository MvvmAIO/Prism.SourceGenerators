# Getting started

## Install

Add the NuGet package to your app or library (replace version with the latest on NuGet):

```xml
<PackageReference Include="MvvmAIO.Prism.SourceGenerators" Version="0.2.0" />
```

Or:

```bash
dotnet add package MvvmAIO.Prism.SourceGenerators
```

The package ships **analyzers** plus **`MvvmAIO.Prism.Core`** (attributes used by the generator). MSBuild selects a Roslyn-matched analyzer assembly (4.0.1 / 4.3.1 / 4.12 / 5.0) automatically.

## Requirements

- **.NET 10 SDK** for building this repository; consumer projects should match your own target frameworks.
- **IDE:** Visual Studio 2022 **17.13+**, Rider, or VS Code with C# Dev Kit (`.slnx` support when opening the upstream solution).

## Prism 8 vs Prism 9

| Scenario | Packages |
|----------|-----------|
| **Prism 9+** with native `AsyncDelegateCommand` | `MvvmAIO.Prism.SourceGenerators` only |
| **Prism.Core 8.1.97** and async commands | Also install **`MvvmAIO.Prism.Bcl.Commands`** |

If async commands are used on Prism 8 without the BCL package, the analyzer reports **PSG3002**. See [Commands](Commands) and [Build and samples](Build-and-samples).

## Minimal example

```csharp
using Prism.Mvvm;
using Prism.SourceGenerators;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello";

    [DelegateCommand]
    private void Increment() { /* ... */ }
}
```

The containing type must be **`partial`** so the generator can emit members into a second part.

## Next steps

- [ObservableProperty](ObservableProperty) — field vs partial property, hooks, forwarding attributes  
- [Commands](Commands) — `[DelegateCommand]`, `[AsyncDelegateCommand]`, `[ObservesProperty]`  
- [Diagnostics](Diagnostics) — PSGxxxx reference  
