# MvvmAIO.Prism.Bcl.Commands

Small **Prism 8** companion library: **`AsyncDelegateCommand`**, **`AsyncDelegateCommand<T>`**, and **`IAsyncCommand`** for apps that use **Prism.Core 8.1.97** together with **[MvvmAIO.Prism.SourceGenerators](https://www.nuget.org/packages/MvvmAIO.Prism.SourceGenerators)**.

**Prism 9+** already ships these types in the framework; you normally **do not** need this package.

## When to install

- You target **Prism.Core 8.1.97** and use **`[DelegateCommand]`** / **`[AsyncDelegateCommand]`** (or hand-written code) that requires **`AsyncDelegateCommand`**.
- Without this assembly, the analyzer may report **PSG3002** (`AsyncDelegateCommand` not found).

## Install

```xml
<PackageReference Include="MvvmAIO.Prism.Bcl.Commands" Version="0.3.0" />
```

You still need **`MvvmAIO.Prism.SourceGenerators`** for the Roslyn generators and **`MvvmAIO.Prism.Core`** attribute definitions (pulled in by that package).

## Documentation

- Main project (generators, diagnostics, full README): [github.com/MvvmAIO/Prism.SourceGenerators](https://github.com/MvvmAIO/Prism.SourceGenerators)  
- Avalonia samples: [github.com/MvvmAIO/Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples)

## License

MIT — see the [LICENSE](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/LICENSE.txt) file in the source repository.
