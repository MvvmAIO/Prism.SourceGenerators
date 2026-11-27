# Prism.SourceGenerators

Roslyn incremental source generators for [Prism](https://github.com/PrismLibrary/Prism) MVVM (`MvvmAIO.Prism.SourceGenerators` and optional Prism 8 `MvvmAIO.Prism.Bcl.Commands`). Companion repositories: [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) and [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs).

## Build & Verify

```powershell
# Recommended pre-PR gate (matches CI):
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# Quick solution build & test:
dotnet test Prism.SourceGenerators.slnx
```

- Requires **.NET 8** and **.NET 10** SDKs (`global.json` pins the 10.0 band).
- Pack / Publish: `--target Pack --configuration Release --version <VER>`; NuGet publishing runs via `v*` tag or maintainer `workflow_dispatch`.

## Architecture & Invariants

- **Multi-Roslyn layout**: `Prism.SourceGenerators/` holds shared `.shproj` generator sources compiled across four Roslyn target bands (`Roslyn4001` through `Roslyn5000`); consider all four projects when generator APIs differ.
- **Prism 8 runtime split**: Keep `AsyncDelegateCommand` compatibility in `Prism.Bcl.Commands`; never embed runtime command types inside the main analyzer package (`Prism.SourceGenerators.Package`).
- **Coordinated dependencies**: `Microsoft.CodeAnalysis.*`, `Prism.Core`, and `Microsoft.Bcl.AsyncInterfaces` are manually coordinated (see `Directory.Build.props`, `Prism.SourceGenerators.props`, and `.github/dependabot.yml` ignores). Keep `PolyfillVersion` unified repo-wide and `PrismSourceGeneratorsTestsRoslynVersion` aligned with the `Roslyn4120` test host (`Roslyn5000` tests override to `5.0.0`).
- **Partial types**: Consumer types targeted by generators must be `partial` (`PSG0001–PSG0005` + `MakePartial` code fixes).
- **Single-concern PRs**: Keep changes focused; file Docs/Samples-only execution issues in the companion repositories rather than here.

## Gotchas

- CI enforces `TreatWarningsAsErrors=true` via Nuke; there is no separate `dotnet format` CI gate.
- The `System.IO.Hashing` warning from `Prism.Bcl.Commands` NuGet targets is a known upstream nuisance and does not fail CI.
- `MvvmAIO.Prism.Bcl.Commands` publishes with a separate NuGet API key (`NUGET_API_KEY_BCL`), so a main-package tag release does not automatically republish `Bcl.Commands`.
- Review Verify `*.received.*` snapshots carefully when generator output changes intentionally; never bulk-delete `.verified.*` files without inspection.

## Docs & Agent Config

- Domain glossary: [`CONTEXT.md`](CONTEXT.md)
- Maintainer docs: [`docs/DOCUMENTATION.md`](docs/DOCUMENTATION.md), [`docs/adr/`](docs/adr/), [`docs/design/`](docs/design/), [`docs/ROADMAP.md`](docs/ROADMAP.md), [`CONTRIBUTING.md`](CONTRIBUTING.md), [`CHANGELOG.md`](CHANGELOG.md)
- Consumer docs: [Prism.SourceGenerators.Docs](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)
- Issue routing & triage labels: [`docs/agents/`](docs/agents/)

## Repo-Specific Conventions

- Default branch is **`master`**; use **Squash and merge** for routine PRs.
- Commit messages, issues, and PRs: **English**.
- When adding or changing diagnostics (`DiagnosticDescriptors.cs`) or user-visible generator behavior, update `AnalyzerReleases.Unshipped.md`, the corresponding Design Doc, README diagnostic tables, `CHANGELOG.md` (`[Unreleased]`), and `Prism.SourceGenerators.Docs`.
- Prefer `.slnx` (`Prism.SourceGenerators.slnx`) over adding a legacy `.sln`.
- Do not commit, push, bump versions, or create tags unless explicitly requested.
