# Changelog

All notable changes to this project are documented in this file.

## Unreleased`n`n## [0.2.0] - 2026-05-01

### Fixed
- **MSB4086** loading WPF/other projects in the IDE when `CscToolPath` / compiler file version is not available yet: Roslyn-folder conditions in **`MvvmAIO.Prism.SourceGenerators.targets`** now require non-empty major (and minor where `<=` is used) before numeric comparison, so design-time evaluation falls back to **roslyn4.12**.

## [0.1.6] - 2026-05-01

### Changed
- **Breaking:** AsyncDelegateCommand is no longer embedded in the analyzer. MvvmAIO.Prism.Prism.SourceGenerators contains analyzers + MvvmAIO.Prism.Core only; Prism.Core 8.1.97 consumers should install MvvmAIO.Prism.Bcl.Commands manually. Missing assemblies while async commands are used still reports PSG3002 (replaces PSG3001).

### Added
- Prism.Bcl.Commands project producing **`MvvmAIO.Prism.Bcl.Commands`** as a separate NuGet package for Prism.Core 8.1.97 async commands.

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

