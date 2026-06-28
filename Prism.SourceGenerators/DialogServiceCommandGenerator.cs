using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Emits <c>DelegateCommand</c> properties that call <c>IDialogService.ShowDialog</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class DialogServiceCommandGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<ShowDialogCommandGenerationInfo>> showDialogCommands =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    DialogServiceCommandMetadataExtractor.ShowDialogCommandAttributeMetadataName,
                    static (node, _) => node is MethodDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (ctx, token) => DialogServiceCommandMetadataExtractor.ExtractShowDialogCommand(ctx, token));

        context.RegisterSourceOutput(
            showDialogCommands.Where(static item => !item.Errors.IsEmpty),
            static (spc, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    spc.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        context.RegisterSourceOutput(
            showDialogCommands
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (spc, info) =>
            {
                CompilationUnitSyntax compilationUnit = DialogServiceCommandSyntax.CreateCompilationUnit(info);
                spc.AddSource($"{info.Hierarchy.FilenameHint}.{info.CommandName}.g.cs", compilationUnit);
            });
    }
}
