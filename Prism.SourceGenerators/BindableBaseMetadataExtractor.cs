using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Shared extraction for <c>[BindableBase]</c> (used by <see cref="BindableBaseGenerator"/> and <see cref="PropertyChangingGenerator"/>).
/// </summary>
internal static class BindableBaseMetadataExtractor
{
    public const string BindableBaseAttributeMetadataName = "Prism.SourceGenerators.BindableBaseAttribute";

    private const string BindableBaseFullName = "Prism.Mvvm.BindableBase";
    private const string INotifyPropertyChangingMetadataName = "System.ComponentModel.INotifyPropertyChanging";

    public static Result<BindableBaseGenerationInfo> ExtractGenerationInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        INamedTypeSymbol classSymbol = (INamedTypeSymbol)context.TargetSymbol;

        bool isPartial = classSymbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax(token))
            .OfType<TypeDeclarationSyntax>()
            .Any(static t => t.Modifiers.Any(SyntaxKind.PartialKeyword));

        if (!isPartial)
        {
            return new Result<BindableBaseGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NonPartialClassWithBindableBase,
                        classSymbol,
                        classSymbol.Name)));
        }

        Compilation compilation = context.SemanticModel.Compilation;

        if (BindableValidatorMetadataExtractor.TypeHasBindableValidatorAttribute(classSymbol, compilation))
        {
            return new Result<BindableBaseGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        bool inheritsBindableBase = false;
        for (INamedTypeSymbol? baseType = classSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (baseType.ToDisplayString() == BindableBaseFullName)
            {
                inheritsBindableBase = true;
                break;
            }
        }

        if (inheritsBindableBase)
        {
            return new Result<BindableBaseGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        bool implementsINPC = classSymbol.AllInterfaces.Any(
            static i => i.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged");

        if (implementsINPC)
        {
            bool baseImplementsINPC = false;
            for (INamedTypeSymbol? baseType = classSymbol.BaseType; baseType is not null; baseType = baseType.BaseType)
            {
                if (baseType.AllInterfaces.Any(
                    static i => i.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged"))
                {
                    baseImplementsINPC = true;
                    break;
                }
            }

            if (baseImplementsINPC)
            {
                return new Result<BindableBaseGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
            }
        }

        bool emitChangingInterfaceAndMembers = !TypeOrBaseImplementsINotifyPropertyChanging(classSymbol, compilation);
        HierarchyInfo hierarchy = HierarchyInfo.From(classSymbol);

        return new Result<BindableBaseGenerationInfo>(
            new BindableBaseGenerationInfo(hierarchy, emitChangingInterfaceAndMembers),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    public static bool TypeOrBaseImplementsINotifyPropertyChanging(INamedTypeSymbol classSymbol, Compilation compilation)
    {
        INamedTypeSymbol? iface = compilation.GetTypeByMetadataName(INotifyPropertyChangingMetadataName);
        if (iface is null)
            return false;

        return classSymbol.AllInterfaces.Contains(iface, SymbolEqualityComparer.Default);
    }
}
