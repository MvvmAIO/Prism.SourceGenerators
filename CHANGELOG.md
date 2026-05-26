# Changelog

All notable changes to this project are documented in this file.

## Unreleased

### Changed

- README install examples: **MvvmAIO.Prism.SourceGenerators** **0.5.0**.
- **`MvvmAIO.Prism.Bcl.Commands`** csproj default version aligned to **0.5.0** (matches Nuke publish `--version`; no API changes). Package README install examples updated.

## [0.5.0] - 2026-05-26

### Added

- **`[DelegateCommand]` / `[AsyncDelegateCommand]`** now support execute methods returning **`Task<TResult>`** (result is awaited; not surfaced on the command). Closes [#56](https://github.com/MvvmAIO/Prism.SourceGenerators/issues/56).
- Additional **Roslyn 5.0** smoke tests for delegate commands.

### Changed

- Pack: suppress **NU5128** for bundled `MvvmAIO.Prism.Core` under `lib/netstandard2.0` (not a separate nuspec dependency).

## [0.4.3] - 2026-05-25

### Changed

- **`AGENTS.md`** is now the canonical project constraint document (build/CI, `.slnx`, `.Temp/`, GitHub workflow, generator checklist). Cursor rules under `.cursor/rules/` defer to it.
- README install examples updated to **MvvmAIO.Prism.SourceGenerators** 0.4.2.

## [0.4.2] - 2026-05-25

### Added

- **`AGENTS.md`** — Cursor Cloud / automated agent development guide (build commands, layout, gotchas).
- **`Prism.SourceGenerators.Tests.Roslyn5000`** — smoke tests running generators against the Roslyn 5.0 analyzer build.

### Changed

- **Dependency updates:** Polyfill 10.7.0, Verify.XunitV3 31.17.0, Microsoft.SourceLink.GitHub 10.0.300, NuGet.Packaging 7.6.0, System.Security.Cryptography.Xml 10.0.8 (Nuke build).
- **`MvvmAIO.Prism.Bcl.Commands`** 0.4.2 — SourceLink 10.0.300 (no API changes).

## [0.4.1] - 2026-05-16

### Fixed
- **Code fix provider** now covers **PSG0005** (`[BindableValidator]` class not partial) in addition to PSG0001–PSG0004. The IDE quick-fix bulb (Ctrl+. / Alt+Enter) will offer "Add 'partial' modifier" for all five diagnostics, and "Fix all in document/project/solution" works accordingly.

## [0.4.0] - 2026-05-14

### Added
- **Validation support** via `[NotifyDataErrorInfo]` attribute and `BindableValidator` base class. Properties annotated with `[ObservableProperty]` and `[NotifyDataErrorInfo]` (or on a class with `[NotifyDataErrorInfo]`) automatically call `ValidateProperty(value, nameof(Property))` in the generated setter. `BindableValidator` implements `INotifyDataErrorInfo`, providing `ValidateProperty()`, `ValidateAllProperties()`, `ClearErrors()`, and `ClearAllErrors()` methods backed by `System.ComponentModel.DataAnnotations`.
- New diagnostic **PSG5001** (Warning): `[NotifyDataErrorInfo]` is used but the containing type does not inherit from `BindableValidator`; validation calls will not be emitted.
- New `[BindableValidator]` attribute: annotate a class with `[BindableValidator]` to generate the full `BindableValidator` base class implementation inline, without manual inheritance.

### Changed
- **Breaking:** `ObservableValidator` was renamed to **`BindableValidator`** (full metadata name `Prism.SourceGenerators.BindableValidator`). Update view model base types accordingly. Diagnostic **PSG5001** title and message now refer to `BindableValidator`.
- **`MvvmAIO.Prism.Bcl.Commands`** now targets **`netstandard2.0`**, **`netstandard2.1`**, **`net8.0`**, and **`net10.0`** (previously `netstandard2.0` only).

### Fixed
- **Field-target `[ObservableProperty]`** forwards **untargeted** attributes (e.g. `System.ComponentModel.DataAnnotations`) onto the generated property, not only `[property: …]` lists, so `Validator` / **`BindableValidator`** see validation metadata. Attribute lists with an explicit **`[field: …]`** target remain on the backing field only.
- **Partial property `[ObservableProperty]`**: attributes inheriting from **`System.ComponentModel.DataAnnotations.ValidationAttribute`** (e.g. `[Required]`, `[EmailAddress]`) are **not** re-emitted on the generated implementing partial; they remain on the user's partial declaration only, avoiding **CS0579** while keeping **`Validator`** metadata correct.

## [0.3.1] - 2026-05-05

### Fixed
- **MSB3277** assembly conflict: `System.Threading.Tasks.Extensions` now correctly pinned for `netstandard2.0` target in `Prism.Bcl.Commands`.
- `Microsoft.CodeAnalysis.CSharp` / `.Workspaces` package versions realigned across all multi-Roslyn projects to avoid version drift in CI.

### Changed
- **`MvvmAIO.Prism.Bcl.Commands`** NuGet package now ships a **package-scoped README** (`Prism.Bcl.Commands/README.md`) instead of the monorepo root README, so the gallery page describes Prism 8 async commands only.
- Avalonia sample projects moved to the dedicated **`Prism.SourceGenerators.Samples`** repository.
- Dependabot configured for NuGet and GitHub Actions version updates; Prism.Core version pinned from auto-updates.
- `Microsoft.SourceLink.GitHub` bumped from `8.0.0` to `10.0.203`.
- `Polyfill` consuming guidance aligned with SimonCropp/Polyfill recommendations.

## [0.3.0] - 2026-05-05

### Added
- **`ContainerRegistryRegistrationGenerator`**: new `[Register]` attribute generates `IContainerRegistry` registration calls automatically. Supports `Name`, singleton/transient control, Prism 8 compatibility, and diagnostics for duplicate or invalid registrations.
- **`[NotifyCanExecuteChangedFor(nameof(XxxCommand), …)]`** attribute (in **`MvvmAIO.Prism.Core`**) for use alongside `[ObservableProperty]`. The generated property setter calls `XxxCommand?.RaiseCanExecuteChanged()` for each named command after raising `PropertyChanged`. Names are validated against existing members or the generated command of a `[DelegateCommand]`/`[AsyncDelegateCommand]` method on the same type.
- New diagnostic **PSG2005** (Warning): `[NotifyCanExecuteChangedFor]` references a name that cannot be resolved to a command property. The setter is still emitted so the project keeps compiling once the typo is fixed.
- New diagnostic **PSG2006** (Warning): `CanExecute` references a member that exists but is not usable as `Func<bool>` / `Func<T, bool>` / `bool M()` / `bool M(T)` for the annotated execute method (wrong return type or parameters).
- **`PropertyAccess`** enum and **`PropertyAccess`** named/positional argument on `[ObservableProperty]` for **field** targets: choose the generated property's accessibility (`public` default, or `internal`, `protected`, `private`, `protected internal`, `private protected`). Partial property targets keep using the accessibility written on the property declaration.
- `[DelegateCommand]` / `[AsyncDelegateCommand]` now accept execute methods returning **`ValueTask`** or **`ValueTask<TResult>`** (in addition to non-generic `Task`). The generator emits a lambda that calls `.AsTask()` so existing Prism `AsyncDelegateCommand` / `AsyncDelegateCommand<T>` constructors keep working. Methods that take `CancellationToken` cannot combine with `ValueTask` / `ValueTask<TResult>` in this shape (**PSG1001**).
- New **code fix provider** for **PSG0001–PSG0004**: the IDE quick-fix bulb (Ctrl+. / Alt+Enter) now offers an **"Add 'partial' modifier"** action on the offending class or property. Supports "Fix all in document/project/solution" workflows.
- `[ObservableProperty]` now forwards user-supplied attributes onto the generated property. For **field** targets, untargeted attributes and `[property: Xxx]` lists are forwarded (generator-owned attributes are filtered). For **partial property** targets, the same applies **except** attributes inheriting `ValidationAttribute`, which stay on the user's partial declaration only to avoid duplicate metadata (**CS0579**). Forwarded attributes use fully-qualified type names.
- **`[ObservableProperty]`** emits `OnXxxChanging` partial methods alongside `OnXxxChanged`, and calls them before `SetProperty` in the generated setter.
- **`INotifyPropertyChanging`** behavior aligned with CommunityToolkit.Mvvm: `[BindableBase]` always emits `INotifyPropertyChanging` when not already in the type hierarchy; `[ObservableProperty]` always emits `OnXxxChanging` partials and a guarded `RaisePropertyChanging(nameof(…))` call. Types using `[ObservableProperty]` without `[BindableBase]` receive a companion `*.ObservablePropertyChanging.g.cs`. Set `FeatureSwitches.EnableINotifyPropertyChangingSupport = false` to disable at runtime.
- Type-shape matrix tests added for broad generator coverage.

### Changed
- All generators refactored to emit code via **syntax trees** instead of raw string interpolation.
- `Prism.Core` directory renamed to `Prism.SourceGenerators.Core`.

### Fixed
- `[ObservableProperty]` generated setter now routes through **`SetProperty`** correctly (#19).
- `[ObservableProperty]` preserves nullable reference type annotations in generated `OnXxxChanged` / `OnXxxChanging` API (#23).
- Duplicate `IContainerRegistry` registrations prevented when a class carries multiple different `[Register]` attribute types.

## [0.2.2] - 2026-05-02

### Fixed
- CI workflow updated to support **separate NuGet API keys** for `MvvmAIO.Prism.SourceGenerators` and `MvvmAIO.Prism.Bcl.Commands` package publishing.

## [0.2.0] - 2026-05-01

### Changed
- **Breaking:** AsyncDelegateCommand is no longer embedded in the analyzer package.
- Main package **`MvvmAIO.Prism.SourceGenerators`** now contains analyzers + **`MvvmAIO.Prism.Core`** only.
- Prism 8 async command compatibility is split to a separate package **`MvvmAIO.Prism.Bcl.Commands`**.
- Packaging targets now inject only **`MvvmAIO.Prism.Core`** (no Prism8 command auto-injection).
- Added integration coverage in **`Prism.SourceGenerators.Integration.Tests`** for PSG3002 scenarios.

### Added
- New project **`Prism.Bcl.Commands`** producing **`MvvmAIO.Prism.Bcl.Commands`** for Prism.Core 8.1.97 async commands.
- New integration tests validating:
  - Prism.Core 8 without BCL package reports PSG3002.
  - Adding BCL package resolves PSG3002.

### Fixed
- Corrected package identity references to **`MvvmAIO.Prism.SourceGenerators`** across packaging, samples, diagnostics, tests, and docs.
- Fixed changelog formatting corruption and restored version history entries.

## [0.1.7] - 2026-05-01

### Fixed
- **MSB4086** while loading WPF/other projects in the IDE when `CscToolPath` / compiler file version is unavailable during design-time evaluation.
- Roslyn folder selection conditions in targets now guard numeric comparisons with non-empty checks, falling back safely to **roslyn4.12**.

## [0.1.6] - 2026-05-01

### Changed
- **Breaking:** AsyncDelegateCommand is no longer embedded in the analyzer. MvvmAIO.Prism.SourceGenerators contains analyzers + MvvmAIO.Prism.Core only; Prism.Core 8.1.97 consumers should install MvvmAIO.Prism.Bcl.Commands manually. Missing assemblies while async commands are used still reports PSG3002 (replaces PSG3001).

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

