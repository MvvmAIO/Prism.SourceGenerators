using Microsoft.CodeAnalysis;

#pragma warning disable RS2008

namespace Prism.SourceGenerators.Diagnostics;

/// <summary>
/// Diagnostic descriptors for the Prism source generators.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Prism.SourceGenerators";
    private const string HelpLink = "https://github.com/MvvmAIO/Prism.SourceGenerators/blob/master/README.md#diagnostics";

    /// <summary>
    /// PSG0001: Class with [ObservableProperty] members must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithObservableProperty = new(
        id: "PSG0001",
        title: "Class with [ObservableProperty] members must be partial",
        messageFormat: "The class '{0}' contains members with [ObservableProperty] but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "When [ObservableProperty] is used, the containing class must be partial so source-generated members can be merged correctly.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG0002: Class with [DelegateCommand] method must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithDelegateCommand = new(
        id: "PSG0002",
        title: "Class with command generation attribute must be partial",
        messageFormat: "The class '{0}' contains methods with command generation attributes ([DelegateCommand] or [AsyncDelegateCommand]) but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Classes containing [DelegateCommand] or [AsyncDelegateCommand] methods must be partial so generated command properties can be emitted.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG0003: Property with [ObservableProperty] must be declared as partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialPropertyWithObservableProperty = new(
        id: "PSG0003",
        title: "Property with [ObservableProperty] must be partial",
        messageFormat: "The property '{0}' has [ObservableProperty] but is not declared as partial; add the 'partial' modifier to both the property and its containing class",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Property-targeted [ObservableProperty] requires a partial property declaration (and a partial containing class).",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG0004: Class with [BindableBase] must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithBindableBase = new(
        id: "PSG0004",
        title: "Class with [BindableBase] must be partial",
        messageFormat: "The class '{0}' has [BindableBase] but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [BindableBase] attribute generates INotifyPropertyChanged implementation into the target type, which must be partial.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor InvalidDelegateCommandMethodSignature = new(
        id: "PSG1001",
        title: "Invalid [DelegateCommand] method signature",
        messageFormat: "The method '{0}' has an unsupported signature for [DelegateCommand]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[DelegateCommand] supports void methods with zero or one parameter, and async Task methods with supported command signatures.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor InvalidAsyncDelegateCommandMethodSignature = new(
        id: "PSG1002",
        title: "Invalid [AsyncDelegateCommand] method signature",
        messageFormat: "The method '{0}' has an unsupported signature for [AsyncDelegateCommand]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[AsyncDelegateCommand] supports Task-returning methods with up to one command argument (plus optional CancellationToken).",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor CatchHandlerNotFound = new(
        id: "PSG2001",
        title: "Catch handler not found",
        messageFormat: "The Catch handler '{0}' was not found on '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The Catch named argument should reference an existing method, field, or property on the containing type.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor CatchHandlerInvalidSignature = new(
        id: "PSG2002",
        title: "Catch handler has incompatible signature",
        messageFormat: "The Catch handler '{0}' on '{1}' must accept Exception (or derived) to be used safely",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Catch handlers should be methods with one Exception-compatible parameter, or Action<Exception> members.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor CanExecuteMemberNotFound = new(
        id: "PSG2003",
        title: "CanExecute member not found",
        messageFormat: "The CanExecute member '{0}' was not found on '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The CanExecute named argument should reference an existing method or property on the containing type.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor ObservesPropertyNotFound = new(
        id: "PSG2004",
        title: "Observed property not found",
        messageFormat: "The observed property '{0}' was not found on '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The property name passed to [ObservesProperty] should exist on the containing type or one of its base types.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor AsyncDelegateCommandPackageRequired = new(
        id: "PSG3002",
        title: "AsyncDelegateCommand package required for Prism prior to 9.0",
        messageFormat: "Prism.Commands.AsyncDelegateCommand was not found but async commands are used; reference NuGet package '{0}' so MvvmAIO.Prism.Core / MvvmAIO.Prism.Core.Prism8 are applied (Prism.Core 8.1.97 — remove when upgrading to Prism 9+)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Use the MvvmAIO.Prism.SourceGenerators NuGet package (not a project reference to the generator alone) so MSBuild adds MvvmAIO.Prism.Core and, for Prism.Core 8.1.97, MvvmAIO.Prism.Core.Prism8. Alternatively upgrade to Prism 9+.",
        helpLinkUri: HelpLink);
}
