# MvvmAIO Prism Source Generators

Roslyn source generators for [Prism](https://github.com/PrismLibrary/Prism) MVVM: `[ObservableProperty]`, `[DelegateCommand]` / `[AsyncDelegateCommand]`, `[BindableBase]`, and related attributes.

**Repository:** [MvvmAIO/Prism.SourceGenerators](https://github.com/MvvmAIO/Prism.SourceGenerators)  
**Full documentation (README):** [English](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.md) · [简体中文](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.zh-CN.md) · [日本語](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.ja.md)

## Wiki map

| Topic | Page |
|--------|------|
| Install & requirements | [Getting Started](Getting-Started) |
| `[ObservableProperty]` & notifications | [ObservableProperty](ObservableProperty) |
| Commands & execution | [Commands](Commands) |
| Analyzer IDs (PSGxxxx) | [Diagnostics](Diagnostics) |
| Build, Nuke, samples, Prism 8 BCL | [Build and samples](Build-and-samples) |

## CI

[![.NET](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml/badge.svg?branch=master)](https://github.com/MvvmAIO/Prism.SourceGenerators/actions/workflows/dotnet.yml)

## Publishing this wiki from the main repo

The markdown in the main repository’s `wiki/` folder is the **source** for GitHub Wiki. After enabling Wiki under **Settings → General → Features**:

1. Clone the wiki git repository (empty on first use is OK after Wiki is enabled).
2. Copy the contents of the `wiki/` folder from the default branch into that clone.
3. Commit and push to `https://github.com/MvvmAIO/Prism.SourceGenerators.wiki.git`.

See [Build and samples](Build-and-samples) for the exact commands.
