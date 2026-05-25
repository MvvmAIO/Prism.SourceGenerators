# AGENTS.md

## Cursor Cloud specific instructions

This is a .NET Roslyn source-generator library for [Prism](https://github.com/PrismLibrary/Prism) MVVM. There are no runtime services, databases, or containers — validate changes with `dotnet build` and `dotnet test`.

### SDK requirements

- **.NET 10 SDK** — required (pinned in `global.json`: `10.0.201`, `rollForward: latestFeature`)
- **.NET 8 SDK** — unit and integration test projects target `net8.0`
- Install both side-by-side; ensure `DOTNET_ROOT` and `PATH` include the SDK install directory

### Key commands

| Task | Command |
|------|---------|
| Restore | `dotnet restore Prism.SourceGenerators.slnx` |
| Build | `dotnet build Prism.SourceGenerators.slnx` |
| Test | `dotnet test Prism.SourceGenerators.slnx` |
| Full CI | `dotnet run --project build/_build.csproj -- --target Ci --configuration Release` |
| Pack NuGet | `dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <VER>` |

See `README.md` and `CONTRIBUTING.md` for issues, PR workflow, and packaging.

### Repository layout

| Area | Purpose |
|------|---------|
| `Prism.SourceGenerators/` | Shared generator sources (`.shproj` / `.projitems`) |
| `Prism.SourceGenerators.Roslyn4001` … `Roslyn5000` | Analyzer builds per Roslyn API band |
| `Prism.SourceGenerators.Core` | `MvvmAIO.Prism.Core` attributes |
| `Prism.SourceGenerators.Tests` | xUnit v3 + **Verify** snapshot tests (Roslyn **4.12** host) |
| `Prism.SourceGenerators.Tests.Roslyn5000` | Roslyn **5.0** smoke tests |
| `Prism.SourceGenerators.Integration.Tests` | Packaging and analyzer scenarios |
| `build/` | Nuke orchestration (`build.slnx`, `_build.csproj`) |

**Samples** and the **documentation site** live in separate repos: [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples), [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs).

### Gotchas

- Prefer **`.slnx`** over `.sln` for solutions (see `.cursor/rules/prefer-slnx-solution-format.mdc`).
- Generator logic changes may need consideration across all four Roslyn flavor projects.
- Intentional output changes: update `.verified.` snapshot files deliberately — see `CONTRIBUTING.md` **Tests and snapshots**.
- Use **`.Temp/`** for throwaway experiments (gitignored; see `.cursor/rules/temp-directory.mdc`).
- A **`System.IO.Hashing`** warning from `Prism.Bcl.Commands` NuGet targets is a known upstream nuisance and does not fail CI.
- CI uses `TreatWarningsAsErrors=true` on the Nuke compile path; there is no separate `dotnet format` step.
