# Agents

## Cursor Cloud specific instructions

This is a .NET source generators library — there are **no runnable services or web servers**. Development involves building, testing, and packaging NuGet packages.

### SDK requirements

Both **.NET 10 SDK** and **.NET 8 SDK** must be installed side-by-side. The `global.json` pins to .NET 10 (`10.0.201`, `latestPatch` roll-forward). Test projects target `net8.0`.

SDKs are installed to `~/.dotnet`; ensure `DOTNET_ROOT` and `PATH` include that directory (already configured in `~/.bashrc`).

### Key commands

See `README.md` **Building** and **Nuke Build** sections and `CONTRIBUTING.md` **Build and test** section for full details. Quick reference:

| Task | Command |
|------|---------|
| Build | `dotnet build Prism.SourceGenerators.slnx` |
| Test | `dotnet test Prism.SourceGenerators.slnx` |
| Full CI (clean + restore + compile + test) | `dotnet run --project build/_build.csproj -- --target Ci --configuration Release` |
| Pack NuGet | `dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <ver>` |

### Gotchas

- The `Prism.Bcl.Commands` project emits a **System.IO.Hashing** warning for its `net6.0` TFM — this is expected and does not fail CI (Nuke sets `TreatWarningsAsErrors=true` only on the main build, and this warning comes from a NuGet targets file, not from project code).
- Tests use **Verify** (`Verify.XunitV3`) for snapshot testing. If generator output changes intentionally, you must accept new `.verified.` files — see `CONTRIBUTING.md` **Tests and snapshots**.
- The `.Temp/` directory is gitignored; use it for throwaway experiments.
- The solution uses `.slnx` format. Prefer `Prism.SourceGenerators.slnx` for the main dev loop.
