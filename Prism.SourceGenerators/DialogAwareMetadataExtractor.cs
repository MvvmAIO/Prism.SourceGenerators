using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

internal static class DialogAwareMetadataExtractor
{
    internal const string DialogAwareAttributeMetadataName = "Prism.SourceGenerators.DialogAwareAttribute";
    internal const string FromDialogParameterAttributeMetadataName = "Prism.SourceGenerators.FromDialogParameterAttribute";
    private const string ObservablePropertyAttributeMetadataName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    internal static Result<DialogAwareGenerationInfo> ExtractGenerationInfo(
        GeneratorAttributeSyntaxContext context,
        System.Threading.CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new Result<DialogAwareGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        string? dialogsNamespace = PrismDialogsModel.ResolveDialogsNamespace(compilation);
        if (dialogsNamespace is null)
        {
            return new Result<DialogAwareGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        ImmutableArray<DiagnosticInfo> diagnostics = ValidatePartial(classSymbol);

        (EquatableArray<ParameterBindingInfo> bindings, ImmutableArray<DiagnosticInfo> bindingDiagnostics) =
            ExtractParameterBindings(classSymbol);

        ImmutableArray<DiagnosticInfo> allDiagnostics = diagnostics.AddRange(bindingDiagnostics);
        if (allDiagnostics.Length > 0)
        {
            return new Result<DialogAwareGenerationInfo>(default!, allDiagnostics);
        }

        string title = GetTitleFromAttribute(context.Attributes);
        return new Result<DialogAwareGenerationInfo>(
            new DialogAwareGenerationInfo(
                HierarchyInfo.From(classSymbol),
                title,
                dialogsNamespace,
                PrismDialogsModel.UsesDialogCloseListener(compilation, dialogsNamespace),
                PrismDialogsModel.DialogAwareHasTitle(compilation, dialogsNamespace),
                bindings),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static (EquatableArray<ParameterBindingInfo> Bindings, ImmutableArray<DiagnosticInfo> Diagnostics)
        ExtractParameterBindings(INamedTypeSymbol classSymbol)
    {
        var bindingBuilder = ImmutableArray.CreateBuilder<ParameterBindingInfo>();
        var diagnosticBuilder = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (ISymbol member in classSymbol.GetAllMembers())
        {
            AttributeData? attr = null;
            foreach (AttributeData a in member.GetAttributes())
            {
                if (a.AttributeClass?.ToDisplayString() == FromDialogParameterAttributeMetadataName)
                {
                    attr = a;
                    break;
                }
            }

            if (attr is null)
            {
                continue;
            }

            // PSG7103: must be field or property
            if (member is not IFieldSymbol and not IPropertySymbol)
            {
                diagnosticBuilder.Add(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.FromDialogParameterOnInvalidTarget,
                        member,
                        member.Name));
                continue;
            }

            // PSG7104: requires [ObservableProperty] on the same member
            if (!HasObservableProperty(member))
            {
                diagnosticBuilder.Add(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.FromDialogParameterRequiresObservableProperty,
                        member,
                        member.Name));
                continue;
            }

            // Resolve key (explicit or default to property name)
            string? explicitKey = attr.TryGetNamedString("Key");
            if (explicitKey is null && attr.TryGetConstructorArgument<string>(0, out string? constructorKey))
            {
                explicitKey = constructorKey;
            }
            string propertyName;
            string propertyType;

            if (member is IFieldSymbol field)
            {
                propertyName = NamingHelpers.GetPropertyNameFromField(field.Name);
                propertyType = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            else
            {
                IPropertySymbol property = (IPropertySymbol)member;
                propertyName = property.Name;
                propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            // PSG7105: empty key
            if (explicitKey is not null && string.IsNullOrWhiteSpace(explicitKey))
            {
                diagnosticBuilder.Add(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.FromDialogParameterEmptyKey,
                        member,
                        member.Name));
                continue;
            }

            string parameterKey = explicitKey ?? propertyName;
            bindingBuilder.Add(new ParameterBindingInfo(propertyName, propertyType, parameterKey));
        }

        return (bindingBuilder.ToImmutable().AsEquatableArray(), diagnosticBuilder.ToImmutable());
    }

    private static bool HasObservableProperty(ISymbol symbol) =>
        symbol.GetAttributes().Any(static a =>
            a.AttributeClass?.ToDisplayString() == ObservablePropertyAttributeMetadataName);

    private static string GetTitleFromAttribute(ImmutableArray<AttributeData> attributes)
    {
        foreach (AttributeData attribute in attributes)
        {
            if (attribute.AttributeClass?.ToDisplayString() != DialogAwareAttributeMetadataName)
            {
                continue;
            }

            foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
            {
                if (named.Key == "Title" && named.Value.Value is string title)
                {
                    return title;
                }
            }
        }

        return string.Empty;
    }

    private static ImmutableArray<DiagnosticInfo> ValidatePartial(INamedTypeSymbol classSymbol)
    {
        bool isPartial = classSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(syntax => syntax.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword));

        if (isPartial)
        {
            return ImmutableArray<DiagnosticInfo>.Empty;
        }

        return ImmutableArray.Create(
            DiagnosticInfo.Create(
                DiagnosticDescriptors.NonPartialClassWithDialogAware,
                classSymbol,
                classSymbol.Name));
    }
}
