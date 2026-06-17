; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 0.6.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSG0007 | Prism.SourceGenerators | Error | Class with [NavigationAware] must be partial
PSG0008 | Prism.SourceGenerators | Error | Class with [DialogAware] must be partial
PSG6001 | Prism.SourceGenerators | Info | Use partial property for [ObservableProperty] (C# 13+)

## Release 0.4.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSG0005 | Prism.SourceGenerators | Error | Class with [BindableValidator] must be partial
PSG0006 | Prism.SourceGenerators | Error | [BindableValidator] is only supported on classes
PSG5001 | Prism.SourceGenerators | Warning | [NotifyDataErrorInfo] requires BindableValidator base type

## Release 0.3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSG2005 | Prism.SourceGenerators | Warning | [NotifyCanExecuteChangedFor] command not found
PSG2006 | Prism.SourceGenerators | Warning | CanExecute member has incompatible signature
PSG4001 | Prism.SourceGenerators | Warning | ServiceType is not assignable from implementation type
PSG4002 | Prism.SourceGenerators | Warning | ViewModelType could not be resolved

## Release 0.1.6

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSG3002 | Prism.SourceGenerators | Error | AsyncDelegateCommand package required for Prism prior to 9.0

## Release 0.1.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSG0001 | Prism.SourceGenerators | Error | Class with [ObservableProperty] members must be partial
PSG0002 | Prism.SourceGenerators | Error | Class with command generation attribute must be partial
PSG0003 | Prism.SourceGenerators | Error | Property with [ObservableProperty] must be partial
PSG0004 | Prism.SourceGenerators | Error | Class with [BindableBase] must be partial
PSG1001 | Prism.SourceGenerators | Error | Invalid [DelegateCommand] method signature
PSG1002 | Prism.SourceGenerators | Error | Invalid [AsyncDelegateCommand] method signature
PSG2001 | Prism.SourceGenerators | Warning | Catch handler not found
PSG2002 | Prism.SourceGenerators | Warning | Catch handler has incompatible signature
PSG2003 | Prism.SourceGenerators | Warning | CanExecute member not found
PSG2004 | Prism.SourceGenerators | Warning | Observed property not found
