using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Prism.SourceGenerators.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PrismAttributeUsageAnalyzer : DiagnosticAnalyzer
{
    private const string ObservablePropertyAttributeName = "Prism.SourceGenerators.ObservablePropertyAttribute";
    private const string DelegateCommandAttributeName = "Prism.SourceGenerators.DelegateCommandAttribute";
    private const string AsyncDelegateCommandAttributeName = "Prism.SourceGenerators.AsyncDelegateCommandAttribute";
    private const string BindableBaseAttributeName = "Prism.SourceGenerators.BindableBaseAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.NonPartialClassWithObservableProperty,
            DiagnosticDescriptors.NonPartialClassWithDelegateCommand,
            DiagnosticDescriptors.NonPartialPropertyWithObservableProperty,
            DiagnosticDescriptors.NonPartialClassWithBindableBase);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type || type.TypeKind != TypeKind.Class)
            return;

        bool isPartial = IsPartialType(type);
        if (isPartial)
            return;

        if (HasAttribute(type, BindableBaseAttributeName))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NonPartialClassWithBindableBase,
                type.Locations.FirstOrDefault(),
                type.Name));
        }

        if (type.GetMembers().OfType<IFieldSymbol>().Any(field => HasAttribute(field, ObservablePropertyAttributeName))
            || type.GetMembers().OfType<IPropertySymbol>().Any(property => HasAttribute(property, ObservablePropertyAttributeName)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NonPartialClassWithObservableProperty,
                type.Locations.FirstOrDefault(),
                type.Name));
        }

        if (type.GetMembers().OfType<IMethodSymbol>().Any(method =>
            HasAttribute(method, DelegateCommandAttributeName) || HasAttribute(method, AsyncDelegateCommandAttributeName)))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NonPartialClassWithDelegateCommand,
                type.Locations.FirstOrDefault(),
                type.Name));
        }
    }

    private static void AnalyzeProperty(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IPropertySymbol property || !HasAttribute(property, ObservablePropertyAttributeName))
            return;

        if (!IsPartialType(property.ContainingType))
            return;

        PropertyDeclarationSyntax? propertyDeclaration = property.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax(context.CancellationToken))
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        bool isPartialProperty = propertyDeclaration?.Modifiers.Any(SyntaxKind.PartialKeyword) == true;
        if (!isPartialProperty)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.NonPartialPropertyWithObservableProperty,
                property.Locations.FirstOrDefault(),
                property.Name));
        }
    }

    private static bool HasAttribute(ISymbol symbol, string attributeName) =>
        symbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == attributeName);

    private static bool IsPartialType(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(syntax => syntax.Modifiers.Any(SyntaxKind.PartialKeyword));
    }
}
