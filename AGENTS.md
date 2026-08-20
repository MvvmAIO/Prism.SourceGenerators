# AGENTS.md

**Canonical project constraints** for human contributors and automated agents (Cursor, cloud agents, CI bots). Follow this file for all work in **MvvmAIO/Prism.SourceGenerators**. Cursor rules under [`.cursor/rules/`](.cursor/rules/) defer here and must not contradict it.

For consumer-facing API and generator behavior, the **[documentation site](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)** remains authoritative; this file governs **how to change the repository**.

---

## Project overview

Roslyn **source generators** for [Prism](https://github.com/PrismLibrary/Prism) MVVM: compile-time emission of `BindableBase` patterns, commands, container registration, and validation. There are **no** runtime services, databases, or containers — validate with `dotnet build`, `dotnet test`, and the Nuke **Ci** target.

| NuGet package | Role |
|---------------|------|
| **MvvmAIO.Prism.SourceGenerators** | Analyzers + bundled **MvvmAIO.Prism.Core** attributes |
| **MvvmAIO.Prism.Bcl.Commands** | Optional Prism 8 `AsyncDelegateCommand` compatibility (separate API key on publish) |

**Related repositories** (separate clones; apply the same *spirit* of constraints where noted):

| Repo | Role |
|------|------|
| [Prism.SourceGenerators.Samples](https://github.com/MvvmAIO/Prism.SourceGenerators.Samples) | Avalonia demos (Prism 8 / 9); consume packages from NuGet |
| [Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs) | Static docs site (NuStreamDocs); sync on user-visible changes |

---

## Development environment

| Requirement | Notes |
|-------------|--------|
| **.NET 10 SDK** | Required; pinned in [`global.json`](global.json) (`10.0.201`, `rollForward: latestFeature`) |
| **.NET 8 SDK** | Test projects target `net8.0` |
| **IDE** | Visual Studio **2022 17.13+**, Rider, or VS Code + C# Dev Kit — entry solution is **`.slnx`** |

Install both SDKs side-by-side; ensure `DOTNET_ROOT` and `PATH` include the SDK directory (relevant for **Cursor Cloud** and headless agents).

---

## Build, test, and release

| Task | Command |
|------|---------|
| Restore | `dotnet restore Prism.SourceGenerators.slnx` |
| Build | `dotnet build Prism.SourceGenerators.slnx` |
| Test | `dotnet test Prism.SourceGenerators.slnx` |
| **Full CI** (recommended before PR) | `dotnet run --project build/_build.csproj -- --target Ci --configuration Release` |
| Pack | `dotnet run --project build/_build.csproj -- --target Pack --configuration Release --version <VER>` |
| Publish NuGet | Tag `v<VER>` or workflow **Publish NuGet** (maintainer actors / secrets) |

See [`README.md`](README.md), [`CONTRIBUTING.md`](CONTRIBUTING.md), and [`wiki/Build-and-samples.md`](wiki/Build-and-samples.md) for packaging and release details.

---

## Repository layout

| Path | Purpose |
|------|---------|
| `Prism.SourceGenerators/` | Shared generator logic (`.shproj` / `.projitems` / `.props`) |
| `Prism.SourceGenerators.Roslyn4001` … `Roslyn5000` | Analyzer builds per Roslyn API band → `analyzers/dotnet/roslyn*` in the NuGet layout |
| `Prism.SourceGenerators.Core` | **MvvmAIO.Prism.Core** — attributes used by consumer code and the generator |
| `Prism.SourceGenerators.Package` | **MvvmAIO.Prism.SourceGenerators** NuGet project |
| `Prism.Bcl.Commands` | **MvvmAIO.Prism.Bcl.Commands** (Prism 8 async commands) |
| `Prism.SourceGenerators.Tests` | xUnit v3 + **Verify** snapshots (Roslyn **4.12** test host) |
| `Prism.SourceGenerators.Tests.Roslyn5000` | Roslyn **5.0** smoke tests |
| `Prism.SourceGenerators.Integration.Tests` | Packaging and analyzer integration scenarios |
| `build/` | [Nuke](https://nuke.build/) — `build.slnx`, `build/_build.csproj` |
| `wiki/` | GitHub Wiki source (Chinese-first notes; not a contract for diagnostics text) |

When changing generator logic, consider **all four** Roslyn flavor projects if the API surface differs. See [`wiki/Architecture.md`](wiki/Architecture.md) for multi-Roslyn versioning and Dependabot ignore policy.

---

## Mandatory project rules

### 1. Solution format — prefer SLNX over SLN

- Use **`.slnx`** (XML solution format) as the **primary** solution for this repository.
- Prefer **`dotnet new slnx`** (or Visual Studio’s SLNX flow) when creating a solution from scratch.
- When adding or removing projects, edit the **existing root** [`Prism.SourceGenerators.slnx`](Prism.SourceGenerators.slnx) (or another `.slnx` in-repo if the workflow requires it), **not** a legacy `.sln`.
- **Do not** create a new **`.sln`** as the default product solution (harder to merge; duplicates the `.slnx` graph).
- **Exception:** a secondary `.sln` is acceptable only when the user **explicitly** requests it (e.g. tooling that cannot consume `.slnx`). Document why it exists if you add one.

### 2. Temporary files and scratch work — `.Temp/`

- Place **throwaway projects** (e.g. `dotnet new` apps to test a generator), **one-off experiments**, and other **local-only disposable** files under the repository root **`.Temp/`** — not beside production projects unless they truly belong in the solution.
- **Git:** `.Temp/` is in [`.gitignore`](.gitignore) and must **never** be committed.
- **Cursor:** `.Temp/` is in [`.cursorignore`](.cursorignore) so scratch content stays out of default indexing.
- **Cleanup:** safe to delete all or part of `.Temp/` anytime; nothing in CI or the product build may depend on it.

### 3. Git and GitHub workflow

For substantive changes that go through GitHub:

1. **Issue** — Open or reference a [GitHub issue](https://github.com/MvvmAIO/Prism.SourceGenerators/issues) describing the problem or feature **before** large implementation efforts.
2. **Pull request** — Implement on a branch; open a PR against **`master`**; link the issue (`Closes #NN`, `Fixes #NN`, or `Ref #NN`).
3. **Merge** — Use **Squash and merge** only for routine PRs (not merge commit or rebase merge unless a maintainer explicitly overrides).
4. **CI** — Must be green before merge unless a maintainer agrees to an exception.

Additional expectations:

- Use the [pull request template](.github/pull_request_template.md).
- Write PR titles and commit messages so the **squashed** commit on `master` stays readable.
- **Do not** commit secrets (NuGet API keys, PATs, credentials). Use GitHub encrypted secrets or local env vars for publish.
- **Do not** run destructive git commands (`push --force` to `master`, `reset --hard`, etc.) unless the user explicitly requests them.
- **Do not** create git commits unless the user explicitly asks.

### 4. Code and review expectations

- Match **existing style** in touched files (naming, nullable annotations, `#nullable`, language features already in use).
- Prefer **focused** changes: one logical concern per PR when practical.
- **Minimize scope** — do not change unrelated code; avoid over-abstraction and unnecessary comments.
- **User-visible** changes (diagnostics, emitted source, package layout) require:
  - [`CHANGELOG.md`](CHANGELOG.md) under **Unreleased** (or the section maintainers use for the release).
  - New **PSGxxxx** IDs in **README** diagnostic tables (English and Chinese READMEs in sync when practical).
  - **[Prism.SourceGenerators.Docs](https://github.com/MvvmAIO/Prism.SourceGenerators.Docs)** diagnostics reference when messages or IDs change (same PR or immediately after).
- **Tests:** add or update meaningful tests; do not add trivial tests that only assert the obvious.
- **Verify snapshots:** if generator output changes intentionally, update `.verified.` files deliberately — see **Tests and snapshots** in [`CONTRIBUTING.md`](CONTRIBUTING.md). Never bulk-delete snapshots without review.

### 5. Dependency and Roslyn alignment

- **`Microsoft.CodeAnalysis.*`**, **`Prism.Core`**, and **`Microsoft.Bcl.AsyncInterfaces`** versions are coordinated manually — see [`Directory.Build.props`](Directory.Build.props), [`Prism.SourceGenerators/Prism.SourceGenerators.props`](Prism.SourceGenerators/Prism.SourceGenerators.props), and [`.github/dependabot.yml`](.github/dependabot.yml) **ignore** list. Do not bump these via blind Dependabot merges without checking all Roslyn variants and integration tests.
- **`PolyfillVersion`** in `Directory.Build.props` must stay **one version** across the repo.
- **`PrismSourceGeneratorsTestsRoslynVersion`** must match the **Roslyn4120** test project reference (currently **4.12.0**). The **Roslyn5000** test project overrides to **5.0.0** locally in its `.csproj`.

---

## Generator work checklist

When editing generators, attributes, or packaging:

- [ ] Consider all **Roslyn4001 / 4031 / 4120 / 5000** projects if APIs differ.
- [ ] Run **Ci** target locally before opening a PR.
- [ ] Update **Verify** snapshots only when output is intentionally changed.
- [ ] Add integration coverage when packaging or **PSG** behavior changes.
- [ ] Sync **Docs** repo diagnostics / getting-started if consumers are affected.
- [ ] Avoid embedding runtime command types in the main analyzer package (Prism 8 commands stay in **Bcl.Commands**).

Diagnostic IDs are defined in [`Prism.SourceGenerators/Diagnostics/DiagnosticDescriptors.cs`](Prism.SourceGenerators/Diagnostics/DiagnosticDescriptors.cs). Compiler output wins over Wiki/README if text diverges.

---

## Known issues and gotchas

- **`System.IO.Hashing`** warning from `Prism.Bcl.Commands` NuGet targets is a known upstream nuisance; it does not fail CI.
- CI compile uses **`TreatWarningsAsErrors=true`** via Nuke; there is no separate **`dotnet format`** step in CI.
- **`MvvmAIO.Prism.Bcl.Commands`** publish uses a **separate** NuGet API key (`NUGET_API_KEY_BCL`) in the publish workflow — a tag release may publish the main package without updating Bcl on nuget.org.
- Partial types are required for generated code — **PSG0001–PSG0005** and IDE **MakePartial** code fixes apply from v0.4.1+.

---

## Documentation map (for agents)

| Surface | Use for |
|---------|---------|
| **[Documentation site](https://mvvmaio.github.io/Prism.SourceGenerators.Docs/)** | Canonical consumer manual, PSG tables, architecture |
| **`CONTEXT.md`** | Domain glossary for agent skills (Parameter Binding, etc.) |
| **`docs/`** (see [docs/README.md](docs/README.md)) | Maintainer documentation: Design Doc, ADR, Roadmap — [docs/DOCUMENTATION.md](docs/DOCUMENTATION.md) defines the documentation model |
| **`docs/agents/`** | Config for user-local skills (issue tracker, triage labels, domain layout) |
| **This repo `README` / `README.zh-CN` / `README.ja`** | Landing-page snippets |
| **`wiki/`** | Short Chinese-oriented notes; not a contract |
| **`CONTRIBUTING.md`** | Human contributor process (aligned with this file) |
| **`CHANGELOG.md`** | Released and unreleased product changes |

---

## 文档体系

完整规则见 **[docs/DOCUMENTATION.md](docs/DOCUMENTATION.md)**。维护者文档保留稳定沉淀的信息；需求、讨论与评审分别由 GitHub Issue、PR 和 Release 承载。

| 类型 | 位置 | 用途 | 关键规则 |
|------|------|------|----------|
| **ADR** | `docs/adr/` | 架构决策（不可变） | 编号不复用；替代时新建 ADR |
| **Design Doc** | `docs/design/` | API、诊断、契约、实现与权衡 | 随代码 PR 同步更新 |
| **Roadmap** | `docs/ROADMAP.md` | Backlog | 维护宏观优先级 |
| **Issue / PR / Release** | GitHub | 任务、审查、版本历史 | 不在仓内重复记录 |

### Agent 文档工作流

| 场景 | 行为 |
|------|------|
| 新增 PSG 诊断 | 更新 Design Doc、`AnalyzerReleases.Unshipped.md`、README 诊断表与 Docs 站 |
| 破坏性架构变更 | 记录 ADR，并更新相关 Design Doc |
| 发版 | 更新版本表、CHANGELOG 与 GitHub Release 信息 |
| 创建 ADR | 使用 `docs/adr/_template.md`；编号见 `docs/adr/README.md` |
| 用户可见变更 | `CHANGELOG.md` + **Prism.SourceGenerators.Docs** |
| 文档文件位置 | 维护者文档放在 `docs/`，勿与 `wiki/` 职责混淆 |

发现与已有 ADR 或 Design Doc 冲突时，先报告再由维护者决定是否 Supersede ADR。

---

## Agent skills

This repository does **not** vendor Skills. Agents should use the maintainer's user-local skills (typically `~/.agents/skills` / Cursor user skills). Do not copy `.agents/skills` or `skills-lock.json` back into this repo.

Config for engineering skills: [`docs/agents/`](docs/agents/) (`issue-tracker.md`, `triage-labels.md`, `domain.md`). Skills such as `/to-tickets`, `/to-spec`, `/triage`, `/wayfinder`, and `/qa` must follow the issue routing there.

### Sibling issue routing

| Change lands in… | Execution issue repo |
|------------------|----------------------|
| This repo (generators, Core, packages, tests, build, maintainer `docs/`, `AGENTS.md`) | `MvvmAIO/Prism.SourceGenerators` |
| User docs site | `MvvmAIO/Prism.SourceGenerators.Docs` |
| Avalonia samples | `MvvmAIO/Prism.SourceGenerators.Samples` |

- **Execution tickets follow the repo they change** — do not open Docs/Samples-only execution issues here.
- **Cross-repo links**: parent / `wayfinder:map` may stay here; checklist URLs + `Relates to` / `Blocked by` on children. Do not duplicate full acceptance criteria across repos.
- A title `Docs: …` in **this** repo means maintainer docs under `docs/`; user-site work belongs in the Docs repo.

---

## Cursor Cloud notes

Cloud agents have no special exemptions from the rules above. Use the **Full CI** command as the pre-PR gate. If the environment lacks .NET 10 or 8, install both before building. For exploratory spikes, use **`.Temp/`** only.
