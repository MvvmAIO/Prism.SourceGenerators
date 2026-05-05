using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// A source generator that generates <c>INotifyPropertyChanged</c> and <c>SetProperty</c> / <c>RaisePropertyChanged</c> helpers,
/// matching CommunityToolkit.Mvvm <c>ObservableObject</c>, for classes annotated with <c>[BindableBase]</c> that do not inherit <c>Prism.Mvvm.BindableBase</c>.
/// <para>
/// When the type hierarchy does not already implement <c>INotifyPropertyChanging</c>, <see cref="PropertyChangingGenerator"/> emits a companion
/// <c>*.BindableBase.PropertyChanging.g.cs</c> partial. Two-parameter <c>SetProperty</c> calls <c>RaisePropertyChanging</c>; that behavior is gated at runtime by
/// <c>Prism.SourceGenerators.__Internals.FeatureSwitches.EnableINotifyPropertyChangingSupport</c> (default <see langword="true"/>, like the toolkit).
/// </para>
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class BindableBaseGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<BindableBaseGenerationInfo>> classInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    BindableBaseMetadataExtractor.BindableBaseAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, token) => BindableBaseMetadataExtractor.ExtractGenerationInfo(ctx, token));

        // Report diagnostics
        context.RegisterSourceOutput(
            classInfos.Where(static item => !item.Errors.IsEmpty),
            static (context, result) =>
            {
                foreach (DiagnosticInfo diagnostic in result.Errors.AsImmutableArray())
                {
                    context.ReportDiagnostic(diagnostic.ToDiagnostic());
                }
            });

        // Generate source
        context.RegisterSourceOutput(
            classInfos
                .Where(static item => item.Value is not null && !item.HasBlockingDiagnostics)
                .Select(static (item, _) => item.Value!),
            static (context, info) =>
            {
                CompilationUnitSyntax compilationUnit = BindableBaseSyntax.CreateCompilationUnit(info.Hierarchy);
                context.AddSource($"{info.Hierarchy.FilenameHint}.BindableBase.g.cs", compilationUnit);
            });
    }
}
