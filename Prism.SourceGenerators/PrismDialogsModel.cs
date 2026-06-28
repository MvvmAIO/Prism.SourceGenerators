using System;
using Microsoft.CodeAnalysis;
using Prism.SourceGenerators.Extensions;

namespace Prism.SourceGenerators;

/// <summary>Resolves Prism 8 vs Prism 9+ dialog API namespaces for source generation.</summary>
internal static class PrismDialogsModel
{
    internal const string Prism9DialogsNamespace = "Prism.Dialogs";

    internal const string Prism8DialogsNamespace = "Prism.Services.Dialogs";

    internal static string? ResolveDialogsNamespace(Compilation compilation)
    {
        if (compilation.GetTypeByMetadataName($"{Prism9DialogsNamespace}.IDialogAware") is not null)
        {
            return Prism9DialogsNamespace;
        }

        if (compilation.GetTypeByMetadataName($"{Prism8DialogsNamespace}.IDialogAware") is not null)
        {
            return Prism8DialogsNamespace;
        }

        return null;
    }

    internal static bool CompilationHasDialogService(Compilation compilation) =>
        compilation.GetTypeByMetadataName($"{Prism9DialogsNamespace}.IDialogService") is not null
        || compilation.GetTypeByMetadataName($"{Prism8DialogsNamespace}.IDialogService") is not null;

    internal static bool UsesDialogCloseListener(Compilation compilation, string dialogsNamespace) =>
        compilation.GetTypeByMetadataName($"{dialogsNamespace}.DialogCloseListener") is not null;

    internal static bool DialogAwareHasTitle(Compilation compilation, string dialogsNamespace)
    {
        INamedTypeSymbol? dialogAware = compilation.GetTypeByMetadataName($"{dialogsNamespace}.IDialogAware");
        if (dialogAware is null)
        {
            return false;
        }

        foreach (ISymbol member in dialogAware.GetMembers("Title"))
        {
            if (member is IPropertySymbol)
            {
                return true;
            }
        }

        return false;
    }

    internal static string? FindDialogServiceMemberName(Compilation compilation, INamedTypeSymbol typeSymbol)
    {
        INamedTypeSymbol? prism9DialogService =
            compilation.GetTypeByMetadataName($"{Prism9DialogsNamespace}.IDialogService");
        INamedTypeSymbol? prism8DialogService =
            compilation.GetTypeByMetadataName($"{Prism8DialogsNamespace}.IDialogService");

        foreach (ISymbol member in typeSymbol.GetAllMembers())
        {
            if (member is IFieldSymbol field && IsDialogService(field.Type, prism9DialogService, prism8DialogService))
            {
                return field.Name;
            }

            if (member is IPropertySymbol property && IsDialogService(property.Type, prism9DialogService, prism8DialogService))
            {
                return property.Name;
            }
        }

        return null;
    }

    private static bool IsDialogService(
        ITypeSymbol type,
        INamedTypeSymbol? prism9DialogService,
        INamedTypeSymbol? prism8DialogService)
    {
        if (prism9DialogService is not null && SymbolEqualityComparer.Default.Equals(type, prism9DialogService))
        {
            return true;
        }

        if (prism8DialogService is not null && SymbolEqualityComparer.Default.Equals(type, prism8DialogService))
        {
            return true;
        }

        string display = type.ToDisplayString();
        return string.Equals(display, $"{Prism9DialogsNamespace}.IDialogService", StringComparison.Ordinal)
            || string.Equals(display, $"{Prism8DialogsNamespace}.IDialogService", StringComparison.Ordinal);
    }
}
