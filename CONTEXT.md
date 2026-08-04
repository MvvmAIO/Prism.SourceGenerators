# Prism.SourceGenerators

Roslyn source generators for Prism MVVM: compile-time BindableBase patterns, commands, container registration, validation, region navigation, and dialogs.

## Language

**Parameter Binding**:
Typed assignment of a navigation or dialog parameter onto an `[ObservableProperty]` member via `[FromNavigationParameter]` / `[FromDialogParameter]`, using `TryGetValue<T>` before the corresponding `*Core` hook.
_Avoid_: parameter injection, model binding, FromRoute

**Blocking Diagnostic**:
A diagnostic whose default severity is Error; it prevents emitting the enclosing Aware surface. Warnings (e.g. PSG7007 / PSG7104) do not block that surface — they only omit the offending Parameter Binding.
_Avoid_: fatal diagnostic, hard error (unless quoting Roslyn severity)

**Parameter Binding Kind**:
Which From* attribute and PSG diagnostic trio apply — Navigation (`[FromNavigationParameter]`, PSG7006–7008) or Dialog (`[FromDialogParameter]`, PSG7103–7105). Same Parameter Binding module, different Kind preset.
_Avoid_: binding mode, binding source enum (unless a code name)
