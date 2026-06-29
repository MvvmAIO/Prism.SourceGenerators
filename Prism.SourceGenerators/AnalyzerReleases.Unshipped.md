; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
PSG7006 | Prism.SourceGenerators | Error | [FromNavigationParameter] can only be applied to fields or properties
PSG7007 | Prism.SourceGenerators | Warning | [FromNavigationParameter] requires [ObservableProperty]
PSG7008 | Prism.SourceGenerators | Error | [FromNavigationParameter] key cannot be empty
PSG7103 | Prism.SourceGenerators | Error | [FromDialogParameter] can only be applied to fields or properties
PSG7104 | Prism.SourceGenerators | Warning | [FromDialogParameter] requires [ObservableProperty]
PSG7105 | Prism.SourceGenerators | Error | [FromDialogParameter] key cannot be empty
