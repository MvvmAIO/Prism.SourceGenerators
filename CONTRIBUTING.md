# Contributing to Prism.SourceGenerators

Thank you for your interest in this project. The following guidelines help keep reviews predictable and CI green.

**Automated agents (Cursor, cloud agents):** the canonical constraint document is **[AGENTS.md](AGENTS.md)** at the repository root. It merges repository rules, build commands, and workflow expectations in one place; `.cursor/rules/` defers to it.

---

## Ground rules

- Be respectful and assume good intent.
- For **substantive** work (new generator behavior, packaging changes, non-trivial refactors), prefer an [**issue**](https://github.com/MvvmAIO/Prism.SourceGenerators/issues) (or link an existing one) **before** large implementation efforts, so maintainers can confirm direction.
- Open **pull requests against `master`**. Link the issue in the PR body (`Fixes #123`, `Closes #456`, or `Ref #789`).
- Maintainers merge with **Squash and merge** for routine PRs. Write commit messages and PR titles so the squashed commit message stays readable.
- **CI must pass** before merge unless a maintainer explicitly agrees to an exception.

The **canonical product manual** is the static documentation site **[Prism.SourceGenerators.Docs](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)** (repo **[Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)**). This repository’s **README** files and **[GitHub Wiki](https://github.com/MvvmAIO/Prism.SourceGenerators/wiki)** (tracked under [`wiki/`](https://github.com/MvvmAIO/Prism.SourceGenerators/tree/master/wiki)) are **short entry points** only.

**Maintainer documentation (documentation-driven development):** see **[`docs/DOCUMENTATION.md`](docs/DOCUMENTATION.md)** for document types (RFC, ADR, Spec, Design Doc, Plan, Review), lifecycle, and the **docs-before-code** workflow. Index: [`docs/README.md`](docs/README.md). Substantive generator or API work should update the relevant **Spec** and **Design Doc** in the same PR when behavior changes.

Technical discussion in issues and PRs may be in English or Chinese.

---

## Development environment

| Requirement | Notes |
|-------------|--------|
| **.NET 10 SDK** | Required to build this repository. |
| **IDE** | Visual Studio **2022 17.13+**, **Rider**, or **VS Code with C# Dev Kit** — solution entry is **`.slnx`**. |

Clone and restore as usual:

```bash
git clone https://github.com/MvvmAIO/Prism.SourceGenerators.git
cd Prism.SourceGenerators
dotnet build Prism.SourceGenerators.slnx
```

---

## Repository layout (short)

| Area | Purpose |
|------|---------|
| `Prism.SourceGenerators/` | Shared generator sources (`.shproj` / `.projitems`). |
| `Prism.SourceGenerators.Roslyn4001` … `Roslyn5000` | Analyzer builds pinned to different **Roslyn** API bands; packaged under `analyzers/dotnet/roslyn*` in the NuGet layout. |
| `Prism.SourceGenerators.Core` | **`MvvmAIO.Prism.Core`** — attributes referenced by user code and the generator. |
| `Prism.SourceGenerators.Package` | NuGet package project for **`MvvmAIO.Prism.SourceGenerators`**. |
| `Prism.Bcl.Commands` | Optional **`MvvmAIO.Prism.Bcl.Commands`** package for Prism 8 async commands. |
| `Prism.SourceGenerators.Tests` / `…Integration.Tests` | Unit and integration tests. |
| Samples | [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) (separate repo). |
| `build/` | [**Nuke**](https://nuke.build/) automation (`build.slnx`, `build/_build.csproj`). |

When you change generator behavior, consider **all Roslyn** flavor projects if the API surface differs, and run the **full CI target** locally (see below).

---

## Build and test

**Fast loop** (compile everything the IDE would compile):

```bash
dotnet build Prism.SourceGenerators.slnx
```

**Same pipeline as GitHub Actions** (recommended before opening a PR):

```bash
dotnet run --project build/_build.csproj -- --target Ci --configuration Release
```

Other useful Nuke targets (see `build/` and README **Nuke Build**):

```bash
dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version 0.2.0
```

---

## Tests and snapshots

- **Unit tests** use **xUnit v3** and often **Verify** (`Verify.XunitV3`). If intentional output changes, refresh snapshots using the workflow documented for Verify (accept changes in the test harness / diff tool).
- **Integration tests** cover packaging and analyzer scenarios (e.g. **PSG3002** with and without **`MvvmAIO.Prism.Bcl.Commands`**).

If a PR changes emitted source or diagnostics, update or add tests and any **`.verified.`** files deliberately—do not bulk-delete snapshots without review.

---

## Documentation-driven development

Maintainer docs live under **`docs/`**. See **[`docs/DOCUMENTATION.md`](docs/DOCUMENTATION.md)** for the full system (RFC, ADR, Spec, Design Doc, Plan, Review, Roadmap).

| Change | Docs to update |
|--------|----------------|
| New generator or PSG ID | RFC + ADR → **`docs/spec/`** + **`docs/design/`** + `AnalyzerReleases` + Docs site |
| Behavior change (existing generator) | **`docs/spec/`** / **`docs/design/`** in the same PR |
| Large multi-PR feature | **`docs/plans/`** + main Issue |
| Release | **`docs/PUBLISHING.md`** checklist + Release Review in **`docs/review/`** |

Backlog: [`docs/ROADMAP.md`](docs/ROADMAP.md). **Prism.SourceGenerators.Docs** remains the **consumer** manual; `docs/spec/` is the **maintainer contract**.

---

## Code and review expectations

- Match **existing style** in touched files (naming, nullable annotations, `#nullable`, language features already in use).
- Prefer **focused** changes: one logical concern per PR when practical.
- **User-visible** behavior (new diagnostics, generator output, package layout) should be reflected in **`CHANGELOG.md`** under **Unreleased** (or the appropriate section maintainers use).
- New **analyzer IDs** (`PSGxxxx`) should appear in **README** diagnostic tables, **`docs/spec/`**, and **Prism.SourceGenerators.Docs** when messages or IDs change.

---

## Pull requests

- Use the [pull request template](.github/pull_request_template.md) and tick the checklist.
- Describe **what** changed and **why**; link issues and note breaking changes explicitly.
- Keep commits readable; final history is usually **squash** into one commit on `master`.

---

## Security

Do not commit **secrets** (NuGet API keys, PATs, private feed URLs with credentials). Use GitHub **encrypted secrets** or local environment variables for publish operations.

---

## Questions

Open a [discussion-style issue](https://github.com/MvvmAIO/Prism.SourceGenerators/issues/new/choose) or ask in your PR. For consumer usage (not hacking on this repo), see the **[documentation site](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)**, then **[README.md](README.md)** and the **[Wiki](https://github.com/MvvmAIO/Prism.SourceGenerators/wiki)** for brief notes.
