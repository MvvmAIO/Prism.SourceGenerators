using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Prism.SourceGenerators.Extensions;

namespace Prism.SourceGenerators.Diagnostics;

/// <summary>
/// Suggests converting field-backed <c>[ObservableProperty]</c> members to partial properties when C# 13+ is enabled.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UsePartialPropertyForObservablePropertyAnalyzer : DiagnosticAnalyzer
{
    private const string ObservablePropertyAttributeName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(DiagnosticDescriptors.UsePartialPropertyForObservableProperty);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
    }

    private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not FieldDeclarationSyntax fieldDeclaration)
        {
            return;
        }

        SemanticModel semanticModel = context.SemanticModel;
        if (!SupportsPartialPropertyObservableProperty(semanticModel.Compilation))
        {
            return;
        }

        foreach (VariableDeclaratorSyntax variable in fieldDeclaration.Declaration.Variables)
        {
            ISymbol? symbol = semanticModel.GetDeclaredSymbol(variable, context.CancellationToken);
            if (symbol is not IFieldSymbol field || !HasAttribute(field, ObservablePropertyAttributeName))
            {
                continue;
            }

            INamedTypeSymbol containingType = field.ContainingType;
            if (!IsPartialType(containingType))
            {
                continue;
            }

            if (field.DeclaredAccessibility != Accessibility.Private)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.UsePartialPropertyForObservableProperty,
                variable.Identifier.GetLocation(),
                variable.Identifier.Text));
        }
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == attributeName);

    private static bool IsPartialType(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(syntax => syntax.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static bool SupportsPartialPropertyObservableProperty(Compilation compilation)
    {
        LanguageVersion languageVersion = ((CSharpCompilation)compilation).LanguageVersion;
        return languageVersion == LanguageVersion.Preview || (int)languageVersion >= 1300;
    }
}
