using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Emits <c>INotifyPropertyChanging</c> infrastructure in companion partial files:
/// <c>*.BindableBase.PropertyChanging.g.cs</c> for generated <c>[BindableBase]</c> when needed, and
/// <c>*.ObservablePropertyChanging.g.cs</c> for <c>[ObservableProperty]</c> when the type does not already get it from hierarchy or <c>[BindableBase]</c> output.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class PropertyChangingGenerator : IIncrementalGenerator
{
    private const string ObservablePropertyAttributeName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<Result<BindableBaseGenerationInfo>> bindableInfos =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    BindableBaseMetadataExtractor.BindableBaseAttributeMetadataName,
                    static (node, _) => node is ClassDeclarationSyntax,
                    static (ctx, token) => BindableBaseMetadataExtractor.ExtractGenerationInfo(ctx, token));

        context.RegisterSourceOutput(
            bindableInfos.Where(static item =>
                item.Value is not null
                && !item.HasBlockingDiagnostics
                && item.Value!.EmitChangingInterfaceAndMembers),
            static (spc, item) =>
            {
                BindableBaseGenerationInfo info = item.Value!;
                CompilationUnitSyntax compilationUnit = PropertyChangingSyntax.CreateCompilationUnit(info.Hierarchy);
                spc.AddSource($"{info.Hierarchy.FilenameHint}.BindableBase.PropertyChanging.g.cs", compilationUnit);
            });

        IncrementalValuesProvider<Result<ObservablePropertyChangingCandidate>> fieldCandidates =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ObservablePropertyAttributeName,
                    static (node, _) => node is VariableDeclaratorSyntax
                    {
                        Parent: VariableDeclarationSyntax
                        {
                            Parent: FieldDeclarationSyntax
                            {
                                Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                            }
                        }
                    },
                    static (ctx, token) => ExtractObservableChangingFromField(ctx, token));

        IncrementalValuesProvider<Result<ObservablePropertyChangingCandidate>> propertyCandidates =
            context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ObservablePropertyAttributeName,
                    static (node, _) => node is PropertyDeclarationSyntax
                    {
                        Parent: ClassDeclarationSyntax or RecordDeclarationSyntax
                    },
                    static (ctx, token) => ExtractObservableChangingFromProperty(ctx, token));

        RegisterObservablePropertyChangingInfrastructure(context, fieldCandidates, propertyCandidates);
    }

    private static Result<ObservablePropertyChangingCandidate> ExtractObservableChangingFromField(
        GeneratorAttributeSyntaxContext context,
        System.Threading.CancellationToken token)
    {
        IFieldSymbol fieldSymbol = (IFieldSymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = fieldSymbol.ContainingType;

        Compilation compilation = context.SemanticModel.Compilation;
        bool emitFile = IsPartialType(containingType, token)
            && PropertyChangingAnalysis.NeedsObservablePropertyChangingInfrastructure(containingType, compilation);

        return new Result<ObservablePropertyChangingCandidate>(
            new ObservablePropertyChangingCandidate(HierarchyInfo.From(containingType), emitFile),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static Result<ObservablePropertyChangingCandidate> ExtractObservableChangingFromProperty(
        GeneratorAttributeSyntaxContext context,
        System.Threading.CancellationToken token)
    {
        IPropertySymbol propertySymbol = (IPropertySymbol)context.TargetSymbol;
        INamedTypeSymbol containingType = propertySymbol.ContainingType;
        PropertyDeclarationSyntax propertySyntax = (PropertyDeclarationSyntax)context.TargetNode;

        Compilation compilation = context.SemanticModel.Compilation;
        bool emitFile = IsPartialType(containingType, token)
            && propertySyntax.Modifiers.Any(SyntaxKind.PartialKeyword)
            && PropertyChangingAnalysis.NeedsObservablePropertyChangingInfrastructure(containingType, compilation);

        return new Result<ObservablePropertyChangingCandidate>(
            new ObservablePropertyChangingCandidate(HierarchyInfo.From(containingType), emitFile),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static bool IsPartialType(INamedTypeSymbol typeSymbol, System.Threading.CancellationToken token) =>
        typeSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static void RegisterObservablePropertyChangingInfrastructure(
        IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<Result<ObservablePropertyChangingCandidate>> fieldCandidates,
        IncrementalValuesProvider<Result<ObservablePropertyChangingCandidate>> propertyCandidates)
    {
        IncrementalValueProvider<ImmutableArray<ObservablePropertyChangingCandidate>> fields = fieldCandidates
            .Select(static (item, _) => item.Value)
            .Collect();

        IncrementalValueProvider<ImmutableArray<ObservablePropertyChangingCandidate>> props = propertyCandidates
            .Select(static (item, _) => item.Value)
            .Collect();

        IncrementalValueProvider<(ImmutableArray<ObservablePropertyChangingCandidate> Fields, ImmutableArray<ObservablePropertyChangingCandidate> Props)> combined =
            fields.Combine(props);

        context.RegisterSourceOutput(combined, static (spc, tuple) =>
        {
            HashSet<string> emitted = new(StringComparer.Ordinal);

            foreach (ObservablePropertyChangingCandidate candidate in tuple.Fields)
            {
                TryEmitObservablePropertyChangingInfrastructure(spc, candidate, emitted);
            }

            foreach (ObservablePropertyChangingCandidate candidate in tuple.Props)
            {
                TryEmitObservablePropertyChangingInfrastructure(spc, candidate, emitted);
            }
        });
    }

    private static void TryEmitObservablePropertyChangingInfrastructure(
        SourceProductionContext spc,
        ObservablePropertyChangingCandidate candidate,
        HashSet<string> emitted)
    {
        if (!candidate.EmitFile)
            return;

        if (!emitted.Add(candidate.Hierarchy.FilenameHint))
            return;

        CompilationUnitSyntax compilationUnit = PropertyChangingSyntax.CreateCompilationUnit(candidate.Hierarchy);
        spc.AddSource($"{candidate.Hierarchy.FilenameHint}.ObservablePropertyChanging.g.cs", compilationUnit);
    }

    private readonly record struct ObservablePropertyChangingCandidate(HierarchyInfo Hierarchy, bool EmitFile);
}
