using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Generates <see cref="BindableValidator"/> support for types annotated with <c>[BindableValidator]</c>.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class BindableValidatorGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<BindableValidatorGenerationInfo>> classInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    BindableValidatorMetadataExtractor.BindableValidatorAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, token) => BindableValidatorMetadataExtractor.ExtractGenerationInfo(ctx, token));

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
                CompilationUnitSyntax compilationUnit = BindableValidatorSyntax.CreateCompilationUnit(info);
                spc.AddSource($"{info.Hierarchy.FilenameHint}.BindableValidator.g.cs", compilationUnit);
            });
    }
}
