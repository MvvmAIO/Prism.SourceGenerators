# AGENTS.md

## Cursor Cloud specific instructions

This is a .NET Roslyn source-generator library (no runtime services). The VM comes with .NET 10 SDK and .NET 8 SDK pre-installed, which are the only system requirements.

### Key commands

| Task | Command |
|------|---------|
| Build | `dotnet build Prism.SourceGenerators.slnx` |
| Test | `dotnet test Prism.SourceGenerators.slnx` |
| Full CI (clean + restore + compile + test) | `dotnet run --project build/_build.csproj -- --target Ci --configuration Release` |
| Pack NuGet packages | `dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <VERSION>` |

See `README.md` and `CONTRIBUTING.md` for full build/test/pack documentation.

### Testing notes

- **Unit tests** (`Prism.SourceGenerators.Tests`) use xUnit v3 + Verify for snapshot testing of generated source output. If generator output changes intentionally, update `.verified.` snapshot files.
- **Integration tests** (`Prism.SourceGenerators.Integration.Tests`) cover packaging and analyzer diagnostics.
- The Nuke CI target (`--target Ci`) is the same pipeline GitHub Actions runs; always use it before opening a PR.
- Build produces a single `System.IO.Hashing` warning for `net6.0` TFM in `Prism.Bcl.Commands` — this is expected and not a build failure.
