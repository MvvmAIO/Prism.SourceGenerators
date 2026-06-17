using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Emits <c>Prism.Services.Dialogs.IDialogAware</c> members for types annotated with <c>[DialogAware]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DialogAwareGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<DialogAwareGenerationInfo>> classInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    DialogAwareMetadataExtractor.DialogAwareAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, token) => DialogAwareMetadataExtractor.ExtractGenerationInfo(ctx, token));

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
                CompilationUnitSyntax compilationUnit = DialogAwareSyntax.CreateCompilationUnit(info);
                spc.AddSource($"{info.Hierarchy.FilenameHint}.DialogAware.g.cs", compilationUnit);
            });
    }
}
