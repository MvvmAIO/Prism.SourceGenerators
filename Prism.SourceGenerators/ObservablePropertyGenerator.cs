using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// A source generator that generates observable properties for classes inheriting from <c>Prism.Mvvm.BindableBase</c>
/// or using generated <c>[BindableBase]</c> MVVM helpers.
/// <para>
/// Attributes are supplied by the <c>MvvmAIO.Prism.Core</c> assembly (referenced by the <c>MvvmAIO.Prism.SourceGenerators</c> NuGet package).
/// Supports two usage modes:
/// <list type="bullet">
/// <item><b>Field target</b> (all C# versions): Apply <c>[ObservableProperty]</c> to a private field to generate
/// a property (default <c>public</c>; optional <c>PropertyAccess</c> on the attribute) that calls
/// <c>SetProperty</c> in the setter.</item>
/// <item><b>Partial property target</b> (C# 13+): Apply <c>[ObservableProperty]</c> to a <c>partial</c> property
/// to generate the implementing declaration using the <c>field</c> keyword (semi-auto property).</item>
/// </list>
/// For any type with <c>[ObservableProperty]</c> members, setters always emit a guarded <c>RaisePropertyChanging</c> call.
/// If the type hierarchy does not already implement <c>INotifyPropertyChanging</c> (and the type is not covered by generated <c>[BindableBase]</c> companion output), <see cref="PropertyChangingGenerator"/> adds <c>*.ObservablePropertyChanging.g.cs</c> with the interface and helpers.
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePropertyGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Prism.SourceGenerators.ObservablePropertyAttribute";
    private const string NotifyPropertyChangedForAttributeName = "Prism.SourceGenerators.NotifyPropertyChangedForAttribute";
    private const string NotifyCanExecuteChangedForAttributeName = "Prism.SourceGenerators.NotifyCanExecuteChangedForAttribute";
    private const string DelegateCommandAttributeName = "Prism.SourceGenerators.DelegateCommandAttribute";
    private const string AsyncDelegateCommandAttributeName = "Prism.SourceGenerators.AsyncDelegateCommandAttribute";
    private const string NotifyDataErrorInfoAttributeName = "Prism.SourceGenerators.NotifyDataErrorInfoAttribute";
    private const string BindableValidatorMetadataName = "Prism.SourceGenerators.BindableValidator";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // --- Pipeline 1: Field targets (traditional, all C# versions) ---
        IncrementalValuesProvider<Result<PropertyGenerationInfo>> fieldInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is VariableDeclaratorSyntax
                    {
                        Parent: VariableDeclarationSyntax
                        {
                            Parent: FieldDeclarationSyntax
                            {
                                Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                            }
                        }
                    },
                    static (context, token) => ExtractFieldInfo(context, token));

        RegisterDiagnosticsAndSource(context, fieldInfos);

        // --- Pipeline 2: Property targets (partial property + field keyword, C# 13+) ---
        IncrementalValuesProvider<Result<PropertyGenerationInfo>> propertyInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeName,
                    static (node, _) => node is PropertyDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (context, token) => ExtractPropertyInfo(context, token));

        RegisterDiagnosticsAndSource(context, propertyInfos);
    }

    private static void RegisterDiagnosticsAndSource(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result<PropertyGenerationInfo>> infos)
    {
        // Report diagnostics
        context.RegisterSourceOutput(
            infos.Where(static item => !item.Errors.IsEmpty),
            static (context, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        // Generate source for valid items
        context.RegisterSourceOutput(
            infos
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (context, info) =>
            {
                string accessModifier = GetAccessModifierString(info.DeclaredAccessibility);
                string setterModifier = info.SetterAccessibility != Accessibility.NotApplicable
                    ? GetAccessModifierString(info.SetterAccessibility) + " "
                    : "";

                CompilationUnitSyntax compilationUnit = ObservablePropertySyntax.CreateCompilationUnit(
                    info,
                    accessModifier,
                    setterModifier);

                context.AddSource(
                    $"{info.Hierarchy.FilenameHint}.{info.PropertyName}.g.cs",
                    compilationUnit);
            });
    }

    private static Result<PropertyGenerationInfo> ExtractFieldInfo(
        GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken token)
    {
        IFieldSymbol fieldSymbol = (IFieldSymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = fieldSymbol.ContainingType;

        bool isPartial = containingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial)
        {
            return new Result<PropertyGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialClassWithObservableProperty,
                        containingType,
                        containingType.Name)));
        }

        string fieldName = fieldSymbol.Name;
        string propertyName = GetPropertyName(fieldName);
        string fieldType = fieldSymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
        HierarchyInfo hierarchy = HierarchyInfo.From(containingType);
        ImmutableArray<string> notifyProps = CollectNotifyPropertyChangedFor(fieldSymbol);
        ImmutableArray<string> notifyCommands = CollectNotifyCanExecuteChangedFor(fieldSymbol);
        ImmutableArray<DiagnosticInfo> commandDiagnostics = ValidateCanExecuteCommands(notifyCommands, containingType, fieldSymbol);
        ImmutableArray<string> forwardedAttributes = CollectForwardedAttributesFromField(
            (VariableDeclaratorSyntax)context.TargetNode, context.SemanticModel, token);

        Accessibility generatedPropertyAccessibility = GetFieldTargetPropertyAccessibility(
            fieldSymbol,
            context.SemanticModel.Compilation);

        bool notifyDataErrorInfo = HasNotifyDataErrorInfo(fieldSymbol, containingType);
        ImmutableArray<DiagnosticInfo> validationDiagnostics = ValidateNotifyDataErrorInfo(
            notifyDataErrorInfo, containingType, fieldSymbol, context.SemanticModel.Compilation);

        if (!validationDiagnostics.IsEmpty)
            notifyDataErrorInfo = false;

        ImmutableArray<DiagnosticInfo> allDiagnostics = commandDiagnostics.AddRange(validationDiagnostics);

        return new Result<PropertyGenerationInfo>(
            new PropertyGenerationInfo(hierarchy, fieldName, propertyName, fieldType,
                IsPartialProperty: false, generatedPropertyAccessibility, Accessibility.NotApplicable, notifyProps, notifyCommands, forwardedAttributes, notifyDataErrorInfo),
            allDiagnostics);
    }

    private static Result<PropertyGenerationInfo> ExtractPropertyInfo(
        GeneratorAttributeSyntaxContext context, System.Threading.CancellationToken token)
    {
        IPropertySymbol propertySymbol = (IPropertySymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = propertySymbol.ContainingType;

        // Check containing type is partial
        bool isTypePartial = containingType.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isTypePartial)
        {
            return new Result<PropertyGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialClassWithObservableProperty,
                        containingType,
                        containingType.Name)));
        }

        // Check property is partial
        PropertyDeclarationSyntax propertySyntax = (PropertyDeclarationSyntax)context.TargetNode;
        bool isPropertyPartial = propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword);

        if (!isPropertyPartial)
        {
            return new Result<PropertyGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialPropertyWithObservableProperty,
                        propertySymbol,
                        propertySymbol.Name)));
        }

        string propertyName = propertySymbol.Name;
        string fieldType = propertySymbol.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
        HierarchyInfo hierarchy = HierarchyInfo.From(containingType);

        Accessibility setterAccessibility = propertySymbol.SetMethod?.DeclaredAccessibility ?? Accessibility.NotApplicable;
        if (setterAccessibility == propertySymbol.DeclaredAccessibility)
            setterAccessibility = Accessibility.NotApplicable;

        ImmutableArray<string> notifyProps = CollectNotifyPropertyChangedFor(propertySymbol);
        ImmutableArray<string> notifyCommands = CollectNotifyCanExecuteChangedFor(propertySymbol);
        ImmutableArray<DiagnosticInfo> commandDiagnostics = ValidateCanExecuteCommands(notifyCommands, containingType, propertySymbol);
        ImmutableArray<string> forwardedAttributes = CollectForwardedAttributesFromProperty(
            propertySyntax, context.SemanticModel, token);

        bool notifyDataErrorInfo = HasNotifyDataErrorInfo(propertySymbol, containingType);
        ImmutableArray<DiagnosticInfo> validationDiagnostics = ValidateNotifyDataErrorInfo(
            notifyDataErrorInfo, containingType, propertySymbol, context.SemanticModel.Compilation);

        if (!validationDiagnostics.IsEmpty)
            notifyDataErrorInfo = false;

        ImmutableArray<DiagnosticInfo> allDiagnostics = commandDiagnostics.AddRange(validationDiagnostics);

        return new Result<PropertyGenerationInfo>(
            new PropertyGenerationInfo(hierarchy, propertyName, propertyName, fieldType,
                IsPartialProperty: true, propertySymbol.DeclaredAccessibility, setterAccessibility, notifyProps, notifyCommands, forwardedAttributes, notifyDataErrorInfo),
            allDiagnostics);
    }

    private static ImmutableArray<string> CollectNotifyPropertyChangedFor(ISymbol symbol) =>
        CollectNamesFromAttribute(symbol, NotifyPropertyChangedForAttributeName);

    private static ImmutableArray<string> CollectNotifyCanExecuteChangedFor(ISymbol symbol) =>
        CollectNamesFromAttribute(symbol, NotifyCanExecuteChangedForAttributeName);

    private static ImmutableArray<string> CollectNamesFromAttribute(ISymbol symbol, string attributeFullName)
    {
        ImmutableArray<string>.Builder? builder = null;

        foreach (AttributeData attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != attributeFullName)
                continue;

            if (attr.ConstructorArguments.Length >= 1 && attr.ConstructorArguments[0].Value is string firstName)
            {
                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add(firstName);

                if (attr.ConstructorArguments.Length >= 2 && !attr.ConstructorArguments[1].IsNull)
                {
                    foreach (var item in attr.ConstructorArguments[1].Values)
                    {
                        if (item.Value is string otherName)
                            builder.Add(otherName);
                    }
                }
            }
        }

        return builder?.ToImmutable() ?? ImmutableArray<string>.Empty;
    }

    private static ImmutableArray<DiagnosticInfo> ValidateCanExecuteCommands(
        ImmutableArray<string> commandNames,
        INamedTypeSymbol containingType,
        ISymbol attributedSymbol)
    {
        if (commandNames.IsDefaultOrEmpty)
            return ImmutableArray<DiagnosticInfo>.Empty;

        ImmutableArray<DiagnosticInfo>.Builder? builder = null;

        foreach (string commandName in commandNames)
        {
            if (CommandMemberExists(containingType, commandName))
                continue;

            builder ??= ImmutableArray.CreateBuilder<DiagnosticInfo>();
            builder.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.NotifyCanExecuteChangedForCommandNotFound,
                attributedSymbol,
                commandName,
                containingType.Name));
        }

        return builder?.ToImmutable() ?? ImmutableArray<DiagnosticInfo>.Empty;
    }

    private static bool CommandMemberExists(INamedTypeSymbol type, string commandName)
    {
        // Walk type + base types looking for an existing member with this name (e.g. user-defined SaveCommand).
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (current.GetMembers(commandName).Length > 0)
                return true;
        }

        // Otherwise, check whether a [DelegateCommand]/[AsyncDelegateCommand] method on the type
        // would generate this command property (e.g. method 'Save' generates 'SaveCommand').
        if (commandName.EndsWith("Command", System.StringComparison.Ordinal))
        {
            string methodName = commandName.Substring(0, commandName.Length - "Command".Length);
            if (methodName.Length > 0)
            {
                foreach (ISymbol member in type.GetMembers(methodName))
                {
                    if (member is IMethodSymbol method && HasCommandAttribute(method))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool HasCommandAttribute(IMethodSymbol method)
    {
        foreach (AttributeData attr in method.GetAttributes())
        {
            string? name = attr.AttributeClass?.ToDisplayString();
            if (name == DelegateCommandAttributeName || name == AsyncDelegateCommandAttributeName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Collects attributes on a field that should appear on the generated property: untargeted lists and
    /// <c>[property: …]</c>. Lists explicitly targeting <c>field</c> only are skipped; other explicit targets
    /// (e.g. <c>method</c>) are skipped. Generator-owned attributes are excluded so DataAnnotations and similar
    /// metadata on the field are forwarded for reflection-based APIs (e.g. <c>Validator.TryValidateObject</c>).
    /// </summary>
    private static ImmutableArray<string> CollectForwardedAttributesFromField(
        VariableDeclaratorSyntax declarator,
        SemanticModel semanticModel,
        System.Threading.CancellationToken token)
    {
        if (declarator.Parent?.Parent is not FieldDeclarationSyntax field)
            return ImmutableArray<string>.Empty;

        ImmutableArray<string>.Builder? builder = null;

        foreach (AttributeListSyntax list in field.AttributeLists)
        {
            // [field: …] stays on the user's backing field only.
            if (list.Target is { } target && target.Identifier.IsKind(SyntaxKind.FieldKeyword))
                continue;

            // Only forward lists meant for the generated property (untargeted or [property: …]).
            if (list.Target is { } nonPropertyTarget
                && !nonPropertyTarget.Identifier.IsKind(SyntaxKind.PropertyKeyword))
            {
                continue;
            }

            foreach (AttributeSyntax attribute in list.Attributes)
            {
                string? rendered = RenderForwardedAttribute(attribute, semanticModel, token);
                if (rendered is null)
                    continue;

                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add(rendered);
            }
        }

        return builder?.ToImmutable() ?? ImmutableArray<string>.Empty;
    }

    /// <summary>
    /// Collects attributes attached to a partial property declaration so they can be forwarded onto the
    /// generated implementing declaration. Generator-owned attributes (<c>[ObservableProperty]</c>,
    /// <c>[NotifyPropertyChangedFor]</c>, <c>[NotifyCanExecuteChangedFor]</c>, <c>[NotifyDataErrorInfo]</c>) are
    /// omitted. Attributes inheriting from <c>System.ComponentModel.DataAnnotations.ValidationAttribute</c> are
    /// <b>not</b> forwarded: they remain on your partial declaration only, avoiding duplicate metadata on the
    /// generated implementing partial (CS0579). Other attributes (e.g. <c>[JsonIgnore]</c>) are still forwarded.
    /// </summary>
    private static ImmutableArray<string> CollectForwardedAttributesFromProperty(
        PropertyDeclarationSyntax property,
        SemanticModel semanticModel,
        System.Threading.CancellationToken token)
    {
        ImmutableArray<string>.Builder? builder = null;
        Compilation compilation = semanticModel.Compilation;

        foreach (AttributeListSyntax list in property.AttributeLists)
        {
            // Partial property declarations don't permit explicit non-property targets, but be defensive.
            if (list.Target is { } target
                && !target.Identifier.IsKind(SyntaxKind.PropertyKeyword)
                && !target.Identifier.IsKind(SyntaxKind.None))
            {
                continue;
            }

            foreach (AttributeSyntax attribute in list.Attributes)
            {
                INamedTypeSymbol? attributeType = ResolveAttributeType(attribute, semanticModel, token);
                if (attributeType is not null && InheritsFromDataAnnotationsValidationAttribute(attributeType, compilation))
                    continue;

                string? rendered = RenderForwardedAttribute(attribute, semanticModel, token);
                if (rendered is null)
                    continue;

                builder ??= ImmutableArray.CreateBuilder<string>();
                builder.Add(rendered);
            }
        }

        return builder?.ToImmutable() ?? ImmutableArray<string>.Empty;
    }

    /// <summary>
    /// Returns whether <paramref name="attributeClass"/> inherits from
    /// <c>System.ComponentModel.DataAnnotations.ValidationAttribute</c> (e.g. <c>Required</c>, <c>EmailAddress</c>).
    /// Uses symbol equality when <see cref="Compilation.GetTypeByMetadataName"/> resolves the base type, and
    /// falls back to fully-qualified display names on the inheritance chain so skipping still works when that
    /// lookup returns null (some compilation / reference layouts).
    /// </summary>
    private static bool InheritsFromDataAnnotationsValidationAttribute(INamedTypeSymbol attributeClass, Compilation compilation)
    {
        INamedTypeSymbol? validationBase = compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.ValidationAttribute");
        const string validationFullyQualifiedDisplay = "global::System.ComponentModel.DataAnnotations.ValidationAttribute";

        for (INamedTypeSymbol? t = attributeClass; t is not null; t = t.BaseType)
        {
            if (validationBase is not null && SymbolEqualityComparer.Default.Equals(t, validationBase))
                return true;

            if (t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == validationFullyQualifiedDisplay)
                return true;
        }

        return false;
    }

    private static bool IsGeneratorOwnedObservablePropertyAttribute(string fullyQualifiedMetadataName) =>
        fullyQualifiedMetadataName == "global::" + AttributeName
        || fullyQualifiedMetadataName == "global::" + NotifyPropertyChangedForAttributeName
        || fullyQualifiedMetadataName == "global::" + NotifyCanExecuteChangedForAttributeName
        || fullyQualifiedMetadataName == "global::" + NotifyDataErrorInfoAttributeName;

    private static string? RenderForwardedAttribute(
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        System.Threading.CancellationToken token)
    {
        INamedTypeSymbol? attributeType = ResolveAttributeType(attribute, semanticModel, token);
        if (attributeType is null)
            return null;

        string fqn = attributeType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        if (IsGeneratorOwnedObservablePropertyAttribute(fqn))
            return null;

        string argsText = attribute.ArgumentList?.ToString() ?? "";
        return $"[{fqn}{argsText}]";
    }

    private static INamedTypeSymbol? ResolveAttributeType(
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        System.Threading.CancellationToken token)
    {
        SymbolInfo info = semanticModel.GetSymbolInfo(attribute, token);
        return info.Symbol switch
        {
            IMethodSymbol ctor => ctor.ContainingType,
            INamedTypeSymbol type => type,
            _ => info.CandidateSymbols.OfType<IMethodSymbol>().Select(static m => m.ContainingType).FirstOrDefault(),
        };
    }

    private const string ObservablePropertyAttributeMetadataName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    /// <summary>
    /// Reads <c>PropertyAccess</c> from the attribute on a field target. Ordinal values must stay aligned with
    /// <c>MvvmAIO.Prism.Core</c> <c>PropertyAccess</c> (generator does not reference that assembly).
    /// </summary>
    private static Accessibility GetFieldTargetPropertyAccessibility(IFieldSymbol fieldSymbol, Compilation compilation)
    {
        INamedTypeSymbol? attrType = compilation.GetTypeByMetadataName(ObservablePropertyAttributeMetadataName);
        if (attrType is null)
            return Accessibility.Public;

        foreach (AttributeData attr in fieldSymbol.GetAttributes())
        {
            if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrType))
                continue;

            return ReadPropertyAccessFromObservablePropertyAttribute(attr);
        }

        return Accessibility.Public;
    }

    private static Accessibility ReadPropertyAccessFromObservablePropertyAttribute(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length >= 1
            && attr.ConstructorArguments[0] is { Value: int ctorOrdinal })
        {
            return MapPropertyAccessOrdinalToAccessibility(ctorOrdinal);
        }

        foreach (System.Collections.Generic.KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
        {
            if (named.Key == "PropertyAccess" && named.Value.Value is int namedOrdinal)
                return MapPropertyAccessOrdinalToAccessibility(namedOrdinal);
        }

        return Accessibility.Public;
    }

    private static Accessibility MapPropertyAccessOrdinalToAccessibility(int value) =>
        value switch
        {
            0 => Accessibility.Public,
            1 => Accessibility.Internal,
            2 => Accessibility.Protected,
            3 => Accessibility.Private,
            4 => Accessibility.ProtectedOrInternal,
            5 => Accessibility.ProtectedAndInternal,
            _ => Accessibility.Public,
        };

    private static string GetAccessModifierString(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => "public"
        };
    }

    private static string GetPropertyName(string fieldName)
    {
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToUpperInvariant(fieldName[2]) + fieldName.Substring(3);
        if (fieldName.StartsWith("_") && fieldName.Length > 1)
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    /// <summary>
    /// Determines whether the member or its containing type has <c>[NotifyDataErrorInfo]</c>.
    /// The attribute can be applied at field/property level or at class level.
    /// </summary>
    private static bool HasNotifyDataErrorInfo(ISymbol memberSymbol, INamedTypeSymbol containingType)
    {
        // Check member-level attribute
        foreach (AttributeData attr in memberSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == NotifyDataErrorInfoAttributeName)
                return true;
        }

        // Check class-level attribute (walk up the hierarchy for inherited class-level)
        for (INamedTypeSymbol? current = containingType; current is not null; current = current.BaseType)
        {
            foreach (AttributeData attr in current.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == NotifyDataErrorInfoAttributeName)
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that when <c>[NotifyDataErrorInfo]</c> is used, the containing type inherits from
    /// <c>BindableValidator</c>. If not, reports <see cref="DiagnosticDescriptors.NotifyDataErrorInfoOnNonValidator"/>.
    /// </summary>
    private static ImmutableArray<DiagnosticInfo> ValidateNotifyDataErrorInfo(
        bool notifyDataErrorInfo,
        INamedTypeSymbol containingType,
        ISymbol attributedSymbol,
        Compilation compilation)
    {
        if (!notifyDataErrorInfo)
            return ImmutableArray<DiagnosticInfo>.Empty;

        if (InheritsFromBindableValidator(containingType, compilation))
            return ImmutableArray<DiagnosticInfo>.Empty;

        return ImmutableArray.Create(
            DiagnosticInfo.Create(
                DiagnosticDescriptors.NotifyDataErrorInfoOnNonValidator,
                attributedSymbol,
                containingType.Name));
    }

    /// <summary>
    /// Checks whether a type inherits from <c>Prism.SourceGenerators.BindableValidator</c>.
    /// </summary>
    private static bool InheritsFromBindableValidator(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? validatorType = compilation.GetTypeByMetadataName(BindableValidatorMetadataName);
        if (validatorType is null)
            return false;

        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, validatorType))
                return true;
        }

        return false;
    }
}

