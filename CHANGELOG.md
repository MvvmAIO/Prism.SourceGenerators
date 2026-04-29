# Changelog

All notable changes to this project are documented in this file.

## [0.1.2] - 2026-04-29

### Added
- Multi-Roslyn analyzer package layout for Roslyn 4.0 / 4.3 / 4.12 / 5.0.
- Build and test CI workflow with test result artifact and dynamic test badge.
- xUnit v3 test runner migration and Verify.XunitV3 support.
- Avalonia sample applications for Prism 8 and Prism 9.
- Prism.DryIoc.Avalonia sample shell with sidebar navigation.
- Packaging `build/` and `buildTransitive/` targets to select analyzer by compiler version.

### Changed
- Updated diagnostics documentation and package installation guidance in README files.
- Added SourceLink and deterministic CI build settings for package output.

### Fixed
- Resolved Polyfill System.Memory version warning by upgrading to a supported version.
