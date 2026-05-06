# AGENTS.md

## Cursor Cloud specific instructions

This is a .NET source-generator library (Roslyn analyzers for Prism MVVM). There are no runtime services, databases, or containers — all validation is done through `dotnet build` and `dotnet test`.

### SDK requirements

- .NET 10 SDK (pinned via `global.json` with `latestPatch` roll-forward from 10.0.201)
- .NET 8 SDK (test projects target `net8.0`)
- Both are installed to `/usr/share/dotnet`; PATH and DOTNET_ROOT are configured in `~/.bashrc`

### Key commands

| Task | Command |
|------|---------|
| Restore | `dotnet restore Prism.SourceGenerators.slnx` |
| Build | `dotnet build Prism.SourceGenerators.slnx` |
| Test | `dotnet test Prism.SourceGenerators.slnx` |
| Full CI pipeline | `dotnet run --project build/_build.csproj -- --target Ci --configuration Release` |
| Pack NuGet | `dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <VER>` |

### Non-obvious notes

- The solution file uses the `.slnx` format (XML-based, newer than `.sln`).
- The build uses Nuke orchestration (`build/_build.csproj`); the CI target runs Clean → Restore → Compile → Test with `TreatWarningsAsErrors=true`.
- There are four Roslyn-flavored generator projects (Roslyn4001, Roslyn4031, Roslyn4120, Roslyn5000) sharing code via a `.shproj`. Changes to generator logic typically need consideration across all flavors.
- Tests use **Verify** (snapshot testing). If generator output changes intentionally, accept new `.verified.` files — do not bulk-delete without review.
- The `System.IO.Hashing` warning on `Prism.Bcl.Commands` (net6.0 target) is a known upstream nuisance and does not fail the build.
- No lint tool beyond the C# compiler with `TreatWarningsAsErrors`; there is no separate `dotnet format` step in CI.
