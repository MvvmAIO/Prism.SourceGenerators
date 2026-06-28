using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

internal static class DialogAwareMetadataExtractor
{
    internal const string DialogAwareAttributeMetadataName = "Prism.SourceGenerators.DialogAwareAttribute";

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
        if (diagnostics.Length > 0)
        {
            return new Result<DialogAwareGenerationInfo>(default!, diagnostics);
        }

        string title = GetTitleFromAttribute(context.Attributes);
        return new Result<DialogAwareGenerationInfo>(
            new DialogAwareGenerationInfo(
                HierarchyInfo.From(classSymbol),
                title,
                dialogsNamespace,
                PrismDialogsModel.UsesDialogCloseListener(compilation, dialogsNamespace),
                PrismDialogsModel.DialogAwareHasTitle(compilation, dialogsNamespace)),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

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
