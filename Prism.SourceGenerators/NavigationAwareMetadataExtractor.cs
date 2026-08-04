using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

internal static class NavigationAwareMetadataExtractor
{
    internal const string NavigationAwareAttributeMetadataName = "Prism.SourceGenerators.NavigationAwareAttribute";

    internal static Result<NavigationAwareGenerationInfo> ExtractGenerationInfo(
        GeneratorAttributeSyntaxContext context,
        System.Threading.CancellationToken token)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return new Result<NavigationAwareGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        string? regionsNamespace = PrismRegionsModel.ResolveRegionsNamespace(compilation);
        if (regionsNamespace is null)
        {
            return new Result<NavigationAwareGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        ImmutableArray<DiagnosticInfo> diagnostics = ValidatePartial(classSymbol);

        (EquatableArray<ParameterBindingInfo> bindings, ImmutableArray<DiagnosticInfo> bindingDiagnostics) =
            ParameterBinding.Extract(classSymbol, ParameterBindingKind.Navigation);

        ImmutableArray<DiagnosticInfo> allDiagnostics = diagnostics.AddRange(bindingDiagnostics);

        return new Result<NavigationAwareGenerationInfo>(
            new NavigationAwareGenerationInfo(
                HierarchyInfo.From(classSymbol),
                regionsNamespace,
                bindings),
            allDiagnostics);
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
}
