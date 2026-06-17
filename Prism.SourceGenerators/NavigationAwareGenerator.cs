using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Emits <c>Prism.Navigation.Regions.INavigationAware</c> members for types annotated with <c>[NavigationAware]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class NavigationAwareGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<HierarchyInfo>> classInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    NavigationAwareMetadataExtractor.NavigationAwareAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, token) => NavigationAwareMetadataExtractor.ExtractGenerationInfo(ctx, token));

        context.RegisterSourceOutput(
            classInfos.Where(static item => !item.Errors.IsEmpty),
            static (spc, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        context.RegisterSourceOutput(
            classInfos
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (spc, info) =>
            {
                CompilationUnitSyntax compilationUnit = NavigationAwareSyntax.CreateCompilationUnit(info);
                spc.AddSource($"{info.FilenameHint}.NavigationAware.g.cs", compilationUnit);
            });
    }
}
