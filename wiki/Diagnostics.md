# Diagnostics (PSGxxxx)

Quick reference for analyzer diagnostics. Severity and messages follow the shipped analyzer; see IDE tooltips for exact text.

| ID | Summary |
|----|---------|
| **PSG0001** | Type with `[ObservableProperty]` must be `partial` |
| **PSG0002** | Type with `[DelegateCommand]` / `[AsyncDelegateCommand]` must be `partial` |
| **PSG0003** | `[ObservableProperty]` on a property must be `partial` |
| **PSG0004** | Type with `[BindableBase]` must be `partial` |
| **PSG1001** | Method signature invalid for `[DelegateCommand]` |
| **PSG1002** | Method signature invalid for `[AsyncDelegateCommand]` |
| **PSG2001** | Catch handler member not found |
| **PSG2002** | Catch handler signature incompatible |
| **PSG2003** | CanExecute member not found |
| **PSG2004** | Observed property not found |
| **PSG2005** | `[NotifyCanExecuteChangedFor]` names a command that was not found (warning) |
| **PSG2006** | `CanExecute` names a member whose signature is not compatible (warning) |
| **PSG3002** | `AsyncDelegateCommand` not found — install **`MvvmAIO.Prism.SourceGenerators`** and, on Prism 8, **`MvvmAIO.Prism.Bcl.Commands`** (or use Prism 9+) |

## Code fixes

**PSG0001–PSG0004** have an IDE fixer to add the missing **`partial`** modifier (including “Fix all in document/project/solution”).

## Full table

The repository README keeps the authoritative table next to feature documentation:  
[README — Diagnostics](https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.md#diagnostics)
