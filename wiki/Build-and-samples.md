# Build, samples, and Prism.Bcl.Commands

## Repository layout (high level)

| Path | Role |
|------|------|
| `Prism.SourceGenerators/` | Shared generator sources (`.shproj` / `.projitems`) |
| `Prism.SourceGenerators.Roslyn4001` / `4031` / `4120` / `5000` | Analyzer packages built per Roslyn band |
| `Prism.SourceGenerators.Core` | **`MvvmAIO.Prism.Core`** — attributes bundled in the main package |
| `Prism.Bcl.Commands` | **`MvvmAIO.Prism.Bcl.Commands`** — optional Prism 8 async commands |
| `Prism.SourceGenerators.Samples.Prism8` / `Prism9` | Avalonia samples |

## Build the sources

```bash
dotnet build Prism.SourceGenerators.slnx
```

Requires **.NET 10 SDK** and a recent IDE for `.slnx`.

## Nuke automation

Orchestration lives under **`build/`** (see **`build.slnx`**).

```bash
# CI-like: clean, restore, compile, test
dotnet run --project build/_build.csproj -- --target Ci --configuration Release

# Pack NuGet (optional version)
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0

# Publish packages (requires API key)
dotnet run --project build/_build.csproj -- --target Publish --configuration Release --version 0.2.0 --nuget-api-key <NUGET_API_KEY>
```

## Publish GitHub Wiki from `wiki/`

1. On GitHub: **Settings → General → Features → Wikis** — enable Wikis.
2. Clone the wiki repository (HTTPS; authenticate as you usually do for GitHub):

   ```bash
   git clone https://github.com/MvvmAIO/Prism.SourceGenerators.wiki.git
   cd Prism.SourceGenerators.wiki
   ```

3. Copy all files from the main repo’s **`wiki/`** directory into this clone (overwrite `Home.md` / `_Sidebar.md` as needed).
4. Commit and push:

   ```bash
   git add -A
   git commit -m "docs: sync wiki from main repository"
   git push origin master
   ```

Wiki uses **`master`** as the default branch name in many GitHub wiki repos; if your remote uses **`main`**, push accordingly.

## MvvmAIO.Prism.Bcl.Commands

- Separate NuGet for **Prism.Core 8.1.97** consumers who need **`AsyncDelegateCommand`** and related APIs.
- On **netstandard2.0**, the package references **`System.Threading.Tasks.Extensions`** (conditional); see **`Prism.Bcl.Commands.csproj`** in the repo.
