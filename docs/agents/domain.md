# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

- **`CONTEXT.md`** at the repo root (glossary: Parameter Binding, Blocking Diagnostic, Parameter Binding Kind).
- **`AGENTS.md`** at the repo root (project constraints, layout, generator checklist, documentation map).
- **`docs/DOCUMENTATION.md`** — documentation carriers and sync checklist.
- **`docs/adr/`** — ADRs that touch the area you're about to work in.
- **`docs/design/`** — Design Doc for the generator domain under change.
- Sibling routing for issues: **`docs/agents/issue-tracker.md`**.

If a listed optional file doesn't exist, **proceed silently**. Don't flag absence; don't suggest creating domain files upfront unless the task requires an ADR / Design Doc update per `AGENTS.md`.

## File structure

```
/
├── CONTEXT.md
├── AGENTS.md
├── docs/
│   ├── agents/          ← skills config (this folder)
│   ├── adr/
│   ├── design/
│   ├── DOCUMENTATION.md
│   ├── DEVELOPMENT.md
│   └── ROADMAP.md
├── Prism.SourceGenerators/          ← shared generator sources
├── Prism.SourceGenerators.Core/     ← attributes (MvvmAIO.Prism.Core)
└── Prism.SourceGenerators.Roslyn*/  ← per-Roslyn analyzer builds
```

User-facing guides and runnable samples are **not** in this tree — see sibling clones:

- `Prism.SourceGenerators.Docs` → `MvvmAIO/Prism.SourceGenerators.Docs`
- `Prism.SourceGenerators.Samples` → `MvvmAIO/Prism.SourceGenerators.Samples`

## Use the glossary's vocabulary

Prefer terms already used in `AGENTS.md`, Design Docs, and ADRs (`ObservableProperty`, `BindableBase`, `BindableValidator`, `PSG####`, Roslyn band names). Don't invent parallel names for the same concept.

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding.
