using Microsoft.CodeAnalysis;

#pragma warning disable RS2008

namespace Prism.SourceGenerators.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the Prism source generators.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>
    /// PSG0001: Class with [ObservableProperty] field must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithObservableProperty = new(
        id: "PSG0001",
        title: "Class with [ObservableProperty] field must be partial",
        messageFormat: "The class '{0}' contains fields with [ObservableProperty] but is not declared as partial",
        category: "Prism.SourceGenerators",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PSG0002: Class with [DelegateCommand] method must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithDelegateCommand = new(
        id: "PSG0002",
        title: "Class with command generation attribute must be partial",
        messageFormat: "The class '{0}' contains methods with command generation attributes ([DelegateCommand] or [AsyncDelegateCommand]) but is not declared as partial",
        category: "Prism.SourceGenerators",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
