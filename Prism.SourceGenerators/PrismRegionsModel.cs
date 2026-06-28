using System;
using Microsoft.CodeAnalysis;
using Prism.SourceGenerators.Extensions;

namespace Prism.SourceGenerators;

/// <summary>Resolves Prism 8 vs Prism 9+ regions API namespaces for source generation.</summary>
internal static class PrismRegionsModel
{
    internal const string Prism9RegionsNamespace = "Prism.Navigation.Regions";

    internal const string Prism8RegionsNamespace = "Prism.Regions";

    internal static string? ResolveRegionsNamespace(Compilation compilation)
    {
        if (compilation.GetTypeByMetadataName($"{Prism9RegionsNamespace}.INavigationAware") is not null)
        {
            return Prism9RegionsNamespace;
        }

        if (compilation.GetTypeByMetadataName($"{Prism8RegionsNamespace}.INavigationAware") is not null)
        {
            return Prism8RegionsNamespace;
        }

        return null;
    }

    internal static string? ResolveRegionManagerMetadataName(Compilation compilation)
    {
        if (compilation.GetTypeByMetadataName($"{Prism9RegionsNamespace}.IRegionManager") is not null)
        {
            return $"{Prism9RegionsNamespace}.IRegionManager";
        }

        if (compilation.GetTypeByMetadataName($"{Prism8RegionsNamespace}.IRegionManager") is not null)
        {
            return $"{Prism8RegionsNamespace}.IRegionManager";
        }

        return null;
    }

    internal static string? FindRegionManagerMemberName(
        Compilation compilation,
        INamedTypeSymbol typeSymbol,
        string regionManagerMetadataName)
    {
        INamedTypeSymbol? regionManagerType = compilation.GetTypeByMetadataName(regionManagerMetadataName);

        foreach (ISymbol member in typeSymbol.GetAllMembers())
        {
            if (member is IFieldSymbol field && IsRegionManager(field.Type, regionManagerType, regionManagerMetadataName))
            {
                return field.Name;
            }

            if (member is IPropertySymbol property && IsRegionManager(property.Type, regionManagerType, regionManagerMetadataName))
            {
                return property.Name;
            }
        }

        return null;
    }

    private static bool IsRegionManager(ITypeSymbol type, INamedTypeSymbol? regionManagerType, string regionManagerMetadataName)
    {
        if (regionManagerType is not null && SymbolEqualityComparer.Default.Equals(type, regionManagerType))
        {
            return true;
        }

        string display = type.ToDisplayString();
        string prism8MetadataName = regionManagerMetadataName.Replace("Navigation.", string.Empty);
        return string.Equals(display, regionManagerMetadataName, StringComparison.Ordinal)
            || string.Equals(display, prism8MetadataName, StringComparison.Ordinal);
    }
}
