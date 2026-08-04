using Microsoft.CodeAnalysis;

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

    /// <summary>
    /// PSG0005: Class with [BindableValidator] must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithBindableValidator = new(
        id: "PSG0005",
        title: "Class with [BindableValidator] must be partial",
        messageFormat: "The class '{0}' has [BindableValidator] but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [BindableValidator] attribute merges generated members or a generated base declaration into the target type, which must be partial.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG0006: [BindableValidator] is only supported on classes.
    /// </summary>
    public static readonly DiagnosticDescriptor BindableValidatorOnNonClass = new(
        id: "PSG0006",
        title: "[BindableValidator] is only supported on classes",
        messageFormat: "The type '{0}' has [BindableValidator] but is not a class; remove the attribute or change the declaration to a partial class",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[BindableValidator] generates class partials that inherit BindableValidator or implement INotifyDataErrorInfo; it cannot be applied to structs, records, or interfaces.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG0007: Class with [NavigationAware] must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithNavigationAware = new(
        id: "PSG0007",
        title: "Class with [NavigationAware] must be partial",
        messageFormat: "The class '{0}' has [NavigationAware] but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [NavigationAware] attribute generates INavigationAware members into the target type, which must be partial.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG0008: Class with [DialogAware] must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor NonPartialClassWithDialogAware = new(
        id: "PSG0008",
        title: "Class with [DialogAware] must be partial",
        messageFormat: "The class '{0}' has [DialogAware] but is not declared as partial",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [DialogAware] attribute generates IDialogAware members into the target type, which must be partial.",
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

    public static readonly DiagnosticDescriptor NotifyCanExecuteChangedForCommandNotFound = new(
        id: "PSG2005",
        title: "[NotifyCanExecuteChangedFor] command not found",
        messageFormat: "The command '{0}' referenced by [NotifyCanExecuteChangedFor] was not found on '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The name passed to [NotifyCanExecuteChangedFor] should match either an existing member on the containing type, or the generated command property of a method annotated with [DelegateCommand] or [AsyncDelegateCommand] (e.g. method 'Save' or 'SaveAsync' yields 'SaveCommand'; an explicit CommandName on the attribute is also honored).",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor CanExecuteMemberIncompatibleSignature = new(
        id: "PSG2006",
        title: "CanExecute member has incompatible signature",
        messageFormat: "The CanExecute member '{0}' on '{1}' does not match the expected signature for this command (expects bool-returning method or Func delegate compatible with the execute method parameters)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "For parameterless execute methods, CanExecute should be a bool-returning method with no parameters or a Func<bool> member. For commands with one argument type T, CanExecute should be bool M(T) or Func<T, bool>.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor AsyncDelegateCommandPackageRequired = new(
        id: "PSG3002",
        title: "AsyncDelegateCommand package required for Prism prior to 9.0",
        messageFormat: "Prism.Commands.AsyncDelegateCommand was not found but async commands are used; install NuGet '{0}' and, for Prism.Core 8.1.97, install 'MvvmAIO.Prism.Bcl.Commands'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Use MvvmAIO.Prism.SourceGenerators for analyzer + attributes, and install MvvmAIO.Prism.Bcl.Commands manually when targeting Prism.Core 8.1.97. Alternatively upgrade to Prism 9+.",
        helpLinkUri: HelpLink);

    // --- Container registration diagnostics ---

    /// <summary>
    /// PSG4001: ServiceType is not assignable from the implementation type.
    /// </summary>
    public static readonly DiagnosticDescriptor ServiceTypeNotAssignable = new(
        id: "PSG4001",
        title: "ServiceType is not assignable from implementation type",
        messageFormat: "The type '{0}' does not implement or inherit from ServiceType '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "When using a registration attribute with ServiceType, the decorated class should implement or inherit from the service type.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG4002: ViewModelType could not be resolved.
    /// </summary>
    public static readonly DiagnosticDescriptor ViewModelTypeNotFound = new(
        id: "PSG4002",
        title: "ViewModelType could not be resolved",
        messageFormat: "The ViewModelType on '{0}' could not be resolved; the registration will be skipped",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The ViewModelType property on [RegisterForNavigation] or [RegisterDialog] must reference a valid, resolvable type.",
        helpLinkUri: HelpLink);

    // --- Validation diagnostics ---

    /// <summary>
    /// PSG5001: [NotifyDataErrorInfo] requires the containing type to inherit from BindableValidator.
    /// </summary>
    public static readonly DiagnosticDescriptor NotifyDataErrorInfoOnNonValidator = new(
        id: "PSG5001",
        title: "[NotifyDataErrorInfo] requires BindableValidator base type",
        messageFormat: "The type '{0}' uses [NotifyDataErrorInfo] but does not inherit from BindableValidator or use [BindableValidator]; validation calls will not be emitted",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[NotifyDataErrorInfo] is only effective when the containing type inherits from Prism.SourceGenerators.BindableValidator or is annotated with [BindableValidator]. Otherwise the generated setter will not call ValidateProperty.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG6001: Field-backed [ObservableProperty] can be converted to a partial property (C# 13+).
    /// </summary>
    public static readonly DiagnosticDescriptor UsePartialPropertyForObservableProperty = new(
        id: "PSG6001",
        title: "Use partial property for [ObservableProperty]",
        messageFormat: "The field '{0}' can be converted to a partial property when using C# 13 or later",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "Partial properties with the field keyword provide a cleaner API than backing fields. Apply the code fix when LangVersion is 13.0 or higher.",
        helpLinkUri: HelpLink);

    // --- Region navigation diagnostics ---

    public static readonly DiagnosticDescriptor RegionManagerMemberNotFound = new(
        id: "PSG7001",
        title: "IRegionManager member not found",
        messageFormat: "The type '{0}' uses region navigation generation but no accessible IRegionManager field or property was found; declare one or set RegionManagerMember on the attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[NavigateCommand] and [NavigateOnChanged] require an IRegionManager instance on the containing type.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NavigateCommandRegionRequired = new(
        id: "PSG7002",
        title: "Region is required for [NavigateCommand]",
        messageFormat: "The method '{0}' has [NavigateCommand] but Region is missing or empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Set Region to the Prism region name passed to RequestNavigate.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NavigateCommandTargetRequired = new(
        id: "PSG7003",
        title: "Target is required for [NavigateCommand]",
        messageFormat: "The method '{0}' has [NavigateCommand] but Target is missing or empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Set Target to the view name passed to RequestNavigate.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NavigateOnChangedRequiresObservableProperty = new(
        id: "PSG7004",
        title: "[NavigateOnChanged] requires [ObservableProperty]",
        messageFormat: "The member '{0}' has [NavigateOnChanged] but is not annotated with [ObservableProperty]",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[NavigateOnChanged] must be applied to the same field or partial property as [ObservableProperty].",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor NavigateOnChangedTargetMemberRequired = new(
        id: "PSG7005",
        title: "TargetMember is required for [NavigateOnChanged]",
        messageFormat: "The member '{0}' has [NavigateOnChanged] but TargetMember is missing or empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Set TargetMember to the member path on the changed value used as the navigation target.",
        helpLinkUri: HelpLink);

    // --- Dialog service diagnostics ---

    public static readonly DiagnosticDescriptor DialogServiceMemberNotFound = new(
        id: "PSG7101",
        title: "IDialogService member not found",
        messageFormat: "The type '{0}' uses [ShowDialogCommand] but no accessible IDialogService field or property was found; declare one or set DialogServiceMember on the attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[ShowDialogCommand] requires an IDialogService instance on the containing type.",
        helpLinkUri: HelpLink);

    public static readonly DiagnosticDescriptor ShowDialogCommandNameRequired = new(
        id: "PSG7102",
        title: "Name is required for [ShowDialogCommand]",
        messageFormat: "The method '{0}' has [ShowDialogCommand] but Name is missing or empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Set Name to the dialog name registered with RegisterDialog.",
        helpLinkUri: HelpLink);

    // --- FromNavigationParameter diagnostics ---

    /// <summary>
    /// PSG7006: [FromNavigationParameter] can only be applied to fields or properties.
    /// </summary>
    public static readonly DiagnosticDescriptor FromNavigationParameterOnInvalidTarget = new(
        id: "PSG7006",
        title: "[FromNavigationParameter] can only be applied to fields or properties",
        messageFormat: "The member '{0}' has [FromNavigationParameter] but is not a field or property; remove the attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[FromNavigationParameter] must be applied to a field or partial property annotated with [ObservableProperty].",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG7007: [FromNavigationParameter] requires [ObservableProperty] on the same member.
    /// </summary>
    public static readonly DiagnosticDescriptor FromNavigationParameterRequiresObservableProperty = new(
        id: "PSG7007",
        title: "[FromNavigationParameter] requires [ObservableProperty]",
        messageFormat: "The member '{0}' has [FromNavigationParameter] but is not annotated with [ObservableProperty]; parameter binding will not be emitted",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[FromNavigationParameter] must be applied to the same field or partial property as [ObservableProperty] so the generated setter can be used for assignment.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG7008: [FromNavigationParameter] key cannot be empty.
    /// </summary>
    public static readonly DiagnosticDescriptor FromNavigationParameterEmptyKey = new(
        id: "PSG7008",
        title: "[FromNavigationParameter] key cannot be empty",
        messageFormat: "The member '{0}' has [FromNavigationParameter] with an empty Key; omit Key to use the property name or provide a non-empty value",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Key property of [FromNavigationParameter] must be a non-empty string, or omitted to default to the property name.",
        helpLinkUri: HelpLink);

    // --- FromDialogParameter diagnostics ---

    /// <summary>
    /// PSG7103: [FromDialogParameter] can only be applied to fields or properties.
    /// </summary>
    public static readonly DiagnosticDescriptor FromDialogParameterOnInvalidTarget = new(
        id: "PSG7103",
        title: "[FromDialogParameter] can only be applied to fields or properties",
        messageFormat: "The member '{0}' has [FromDialogParameter] but is not a field or property; remove the attribute",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "[FromDialogParameter] must be applied to a field or partial property annotated with [ObservableProperty].",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG7104: [FromDialogParameter] requires [ObservableProperty] on the same member.
    /// </summary>
    public static readonly DiagnosticDescriptor FromDialogParameterRequiresObservableProperty = new(
        id: "PSG7104",
        title: "[FromDialogParameter] requires [ObservableProperty]",
        messageFormat: "The member '{0}' has [FromDialogParameter] but is not annotated with [ObservableProperty]; parameter binding will not be emitted",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "[FromDialogParameter] must be applied to the same field or partial property as [ObservableProperty] so the generated setter can be used for assignment.",
        helpLinkUri: HelpLink);

    /// <summary>
    /// PSG7105: [FromDialogParameter] key cannot be empty.
    /// </summary>
    public static readonly DiagnosticDescriptor FromDialogParameterEmptyKey = new(
        id: "PSG7105",
        title: "[FromDialogParameter] key cannot be empty",
        messageFormat: "The member '{0}' has [FromDialogParameter] with an empty Key; omit Key to use the property name or provide a non-empty value",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Key property of [FromDialogParameter] must be a non-empty string, or omitted to default to the property name.",
        helpLinkUri: HelpLink);
}
