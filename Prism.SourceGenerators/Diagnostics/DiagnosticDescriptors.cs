using Microsoft.CodeAnalysis;

#pragma warning disable RS2008

namespace Prism.SourceGenerators.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the Prism source generators.
/// </summary>
internal static class DiagnosticDescriptors
{
    /// <summary>
    /// PSG0001: Class with [ObservableProperty] members must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithObservableProperty = new(
        id: "PSG0001",
        title: "Class with [ObservableProperty] members must be partial",
        messageFormat: "The class '{0}' contains members with [ObservableProperty] but is not declared as partial",
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

    /// <summary>
    /// PSG0003: Property with [ObservableProperty] must be declared as partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialPropertyWithObservableProperty = new(
        id: "PSG0003",
        title: "Property with [ObservableProperty] must be partial",
        messageFormat: "The property '{0}' has [ObservableProperty] but is not declared as partial; add the 'partial' modifier to both the property and its containing class",
        category: "Prism.SourceGenerators",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
