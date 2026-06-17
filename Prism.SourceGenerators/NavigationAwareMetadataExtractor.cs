using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

internal static class NavigationAwareMetadataExtractor
{
    internal const string NavigationAwareAttributeMetadataName = "Prism.SourceGenerators.NavigationAwareAttribute";

    internal static Result<HierarchyInfo> ExtractGenerationInfo(
        GeneratorAttributeSyntaxContext context,
        System.Threading.CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new Result<HierarchyInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        if (!compilationHasNavigationAware(compilation))
        {
            return new Result<HierarchyInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        ImmutableArray<DiagnosticInfo> diagnostics = ValidatePartial(classSymbol);
        if (diagnostics.Length > 0)
        {
            return new Result<HierarchyInfo>(default!, diagnostics);
        }

        return new Result<HierarchyInfo>(HierarchyInfo.From(classSymbol), ImmutableArray<DiagnosticInfo>.Empty);
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
                DiagnosticDescriptors.NonPartialClassWithNavigationAware,
                classSymbol,
                classSymbol.Name));
    }

    private static bool compilationHasNavigationAware(Compilation compilation) =>
        compilation.HasAccessibleTypeWithMetadataName("Prism.Navigation.Regions.INavigationAware");
}
