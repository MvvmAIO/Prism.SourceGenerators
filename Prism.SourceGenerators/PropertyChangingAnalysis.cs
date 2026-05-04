using Microsoft.CodeAnalysis;

namespace Prism.SourceGenerators;

/// <summary>
/// Shared rules for when <c>INotifyPropertyChanging</c> infrastructure must be emitted for <c>[ObservableProperty]</c> targets.
/// </summary>
internal static class PropertyChangingAnalysis
{
    private const string BindableBaseAttributeMetadataName = "Prism.SourceGenerators.BindableBaseAttribute";
    private const string PrismMvvmBindableBaseMetadataName = "Prism.Mvvm.BindableBase";

    /// <summary>
    /// Emit <c>*.ObservablePropertyChanging.g.cs</c> when the type has <c>[ObservableProperty]</c> but does not already
    /// get <c>INotifyPropertyChanging</c> from the hierarchy or from <c>*.BindableBase.PropertyChanging.g.cs</c> emitted for <c>[BindableBase]</c>.
    /// </summary>
    public static bool NeedsObservablePropertyChangingInfrastructure(INamedTypeSymbol containingType, Compilation compilation)
    {
        if (BindableBaseMetadataExtractor.TypeOrBaseImplementsINotifyPropertyChanging(containingType, compilation))
            return false;

        if (TypeHasBindableBaseAttribute(containingType, compilation)
            && !InheritsPrismMvvmBindableBase(containingType, compilation))
        {
            return false;
        }

        return true;
    }

    private static bool TypeHasBindableBaseAttribute(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? attrType = compilation.GetTypeByMetadataName(BindableBaseAttributeMetadataName);
        if (attrType is null)
            return false;

        foreach (AttributeData attr in type.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attrType))
                return true;
        }

        return false;
    }

    private static bool InheritsPrismMvvmBindableBase(INamedTypeSymbol type, Compilation compilation)
    {
        INamedTypeSymbol? prismBb = compilation.GetTypeByMetadataName(PrismMvvmBindableBaseMetadataName);
        if (prismBb is null)
            return false;

        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, prismBb))
                return true;
        }

        return false;
    }
}
