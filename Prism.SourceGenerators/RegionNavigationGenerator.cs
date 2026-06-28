using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Emits region navigation commands and <c>[NavigateOnChanged]</c> hooks for <c>IRegionManager.RequestNavigate</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class RegionNavigationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<NavigateCommandGenerationInfo>> navigateCommands =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    RegionNavigationMetadataExtractor.NavigateCommandAttributeMetadataName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (ctx, token) => RegionNavigationMetadataExtractor.ExtractNavigateCommand(ctx, token));

        RegisterNavigateCommandOutput(context, navigateCommands);

        IncrementalValuesProvider<Result<NavigateOnChangedGenerationInfo>> navigateOnChanged =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    RegionNavigationMetadataExtractor.NavigateOnChangedAttributeMetadataName,
                    static (node, _) => node is VariableDeclaratorSyntax or PropertyDeclarationSyntax,
                    static (ctx, token) => RegionNavigationMetadataExtractor.ExtractNavigateOnChanged(ctx, token));

        context.RegisterSourceOutput(
            navigateOnChanged.Where(static item => !item.Errors.IsEmpty),
            static (spc, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        context.RegisterSourceOutput(
            navigateOnChanged
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (spc, info) =>
            {
                CompilationUnitSyntax compilationUnit = RegionNavigationSyntax.CreateNavigateOnChangedCompilationUnit(info);
                spc.AddSource($"{info.Hierarchy.FilenameHint}.{info.PropertyName}.NavigateOnChanged.g.cs", compilationUnit);
            });
    }

    private static void RegisterNavigateCommandOutput(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result<NavigateCommandGenerationInfo>> navigateCommands)
    {
        context.RegisterSourceOutput(
            navigateCommands.Where(static item => !item.Errors.IsEmpty),
            static (spc, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        context.RegisterSourceOutput(
            navigateCommands
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (spc, info) =>
            {
                CompilationUnitSyntax compilationUnit = RegionNavigationSyntax.CreateNavigateCommandCompilationUnit(info);
                spc.AddSource($"{info.Hierarchy.FilenameHint}.{info.CommandName}.g.cs", compilationUnit);
            });
    }
}
