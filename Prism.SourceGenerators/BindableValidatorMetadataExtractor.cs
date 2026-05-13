using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Extraction for <c>[BindableValidator]</c> (used by <see cref="BindableValidatorGenerator"/>).
/// </summary>
internal static class BindableValidatorMetadataExtractor
{
    public const string BindableValidatorAttributeMetadataName = "Prism.SourceGenerators.BindableValidatorAttribute";

    private const string BindableValidatorTypeMetadataName = "Prism.SourceGenerators.BindableValidator";
    private const string PrismMvvmBindableBaseMetadataName = "Prism.Mvvm.BindableBase";
    private const string INotifyPropertyChangedMetadataName = "System.ComponentModel.INotifyPropertyChanged";
    private const string INotifyDataErrorInfoMetadataName = "System.ComponentModel.INotifyDataErrorInfo";

    public static Result<BindableValidatorGenerationInfo> ExtractGenerationInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        INamedTypeSymbol classSymbol = (INamedTypeSymbol)context.TargetSymbol;

        if (classSymbol.TypeKind != TypeKind.Class)
        {
            return new Result<BindableValidatorGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.BindableValidatorOnNonClass,
                        classSymbol,
                        classSymbol.Name)));
        }

        bool isPartial = classSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial)
        {
            return new Result<BindableValidatorGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialClassWithBindableValidator,
                        classSymbol,
                        classSymbol.Name)));
        }

        Compilation compilation = context.SemanticModel.Compilation;

        if (TypeInheritsBindableValidator(classSymbol, compilation))
        {
            return new Result<BindableValidatorGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        INamedTypeSymbol? indei = compilation.GetTypeByMetadataName(INotifyDataErrorInfoMetadataName);
        if (indei is not null && classSymbol.AllInterfaces.Contains(indei, SymbolEqualityComparer.Default))
        {
            return new Result<BindableValidatorGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        INamedTypeSymbol? baseType = classSymbol.BaseType;
        bool onlyObjectBase = baseType is null || baseType.SpecialType == SpecialType.System_Object;

        BindableValidatorEmitMode mode;
        if (onlyObjectBase)
        {
            mode = BindableValidatorEmitMode.InheritBindableValidator;
        }
        else if (TypeHierarchyProvidesNotifyPropertyChanged(classSymbol, compilation))
        {
            mode = BindableValidatorEmitMode.InlineValidationOnly;
        }
        else
        {
            mode = BindableValidatorEmitMode.InlineFull;
        }

        HierarchyInfo hierarchy = HierarchyInfo.From(classSymbol);
        return new Result<BindableValidatorGenerationInfo>(
            new BindableValidatorGenerationInfo(hierarchy, mode),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    public static bool TypeHasBindableValidatorAttribute(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        INamedTypeSymbol? attrType = compilation.GetTypeByMetadataName(BindableValidatorAttributeMetadataName);
        if (attrType is null)
            return false;

        foreach (AttributeData attr in typeSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrType))
                return true;
        }

        return false;
    }

    private static bool TypeInheritsBindableValidator(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        INamedTypeSymbol? validatorType = compilation.GetTypeByMetadataName(BindableValidatorTypeMetadataName);
        if (validatorType is null)
            return false;

        for (INamedTypeSymbol? current = typeSymbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, validatorType))
                return true;
        }

        return false;
    }

    /// <summary>
    /// True when the type or a non-<see cref="object"/> base already participates in <c>INotifyPropertyChanged</c>,
    /// or inherits Prism's <c>BindableBase</c> (which provides the standard notification pattern).
    /// </summary>
    private static bool TypeHierarchyProvidesNotifyPropertyChanged(INamedTypeSymbol classSymbol, Compilation compilation)
    {
        INamedTypeSymbol? inpc = compilation.GetTypeByMetadataName(INotifyPropertyChangedMetadataName);
        INamedTypeSymbol? prismBb = compilation.GetTypeByMetadataName(PrismMvvmBindableBaseMetadataName);

        for (INamedTypeSymbol? current = classSymbol.BaseType;
             current is not null && current.SpecialType != SpecialType.System_Object;
             current = current.BaseType)
        {
            if (prismBb is not null && SymbolEqualityComparer.Default.Equals(current, prismBb))
                return true;

            if (inpc is not null && current.AllInterfaces.Contains(inpc, SymbolEqualityComparer.Default))
                return true;
        }

        return false;
    }
}
