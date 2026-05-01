# Changelog

All notable changes to this project are documented in this file.

## Unreleased

## [0.1.6] - 2026-05-01

### Changed
- **Breaking:** AsyncDelegateCommand is no longer embedded in the analyzer. The **`MvvmAIO.Prism.SourceGenerators`** NuGet package ships **`MvvmAIO.Prism.Core`** and **`MvvmAIO.Prism.Core.Prism8`** under `lib/`; MSBuild adds **`MvvmAIO.Prism.Core.Prism8`** when **`Prism.Core` 8.1.97** is on the graph, otherwise **`MvvmAIO.Prism.Core`** only. Missing assemblies while async commands are used still reports **PSG3002** (replaces PSG3001).

### Added
- `Prism.Core` / `Prism.SourceGenerators.Core.Prism8` projects producing **`MvvmAIO.Prism.Core`** and **`MvvmAIO.Prism.Core.Prism8`** assemblies bundled into the single analyzer package.

### Removed
- `PRISM_SOURCEGENERATORS_ATTRIBUTES` conditional compilation on attribute types (**`MvvmAIO.Prism.Core`**).

## [0.1.2] - 2026-04-29

### Added
- Multi-Roslyn analyzer package layout for Roslyn 4.0 / 4.3 / 4.12 / 5.0.
- Build and test CI workflow with test result artifact and dynamic test badge.
- xUnit v3 test runner migration and Verify.XunitV3 support.
- Avalonia sample applications for Prism 8.1.97 and Prism 9.
- Prism.DryIoc.Avalonia sample shell with sidebar navigation.
- Packaging `build/` and `buildTransitive/` targets to select analyzer by compiler version.

### Changed
- Updated diagnostics documentation and package installation guidance in README files.
- Added SourceLink and deterministic CI build settings for package output.

### Fixed
- Resolved Polyfill System.Memory version warning by upgrading to a supported version.
