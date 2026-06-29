using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

internal static class RegionNavigationMetadataExtractor
{
    internal const string NavigateCommandAttributeMetadataName = "Prism.SourceGenerators.NavigateCommandAttribute";
    internal const string NavigateOnChangedAttributeMetadataName = "Prism.SourceGenerators.NavigateOnChangedAttribute";
    internal const string ObservablePropertyAttributeMetadataName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    internal static Result<NavigateCommandGenerationInfo> ExtractNavigateCommand(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol
            || methodSymbol.ContainingType is not INamedTypeSymbol containingType)
        {
            return new Result<NavigateCommandGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        string? regionsNamespace = PrismRegionsModel.ResolveRegionsNamespace(compilation);
        string? regionManagerMetadata = PrismRegionsModel.ResolveRegionManagerMetadataName(compilation);
        if (regionsNamespace is null || regionManagerMetadata is null)
        {
            return new Result<NavigateCommandGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(
            static a => a.AttributeClass?.ToDisplayString() == NavigateCommandAttributeMetadataName);
        if (attribute is null)
        {
            return new Result<NavigateCommandGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        string? region = attribute.TryGetNamedString("Region");
        string? target = attribute.TryGetNamedString("Target");
        string? commandName = attribute.TryGetNamedString("CommandName");
        string? regionManagerMember = attribute.TryGetNamedString("RegionManagerMember");

        ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        if (string.IsNullOrWhiteSpace(region))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.NavigateCommandRegionRequired,
                methodSymbol,
                methodSymbol.Name));
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.NavigateCommandTargetRequired,
                methodSymbol,
                methodSymbol.Name));
        }

        regionManagerMember ??= PrismRegionsModel.FindRegionManagerMemberName(compilation, containingType, regionManagerMetadata);
        if (string.IsNullOrWhiteSpace(regionManagerMember))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.RegionManagerMemberNotFound,
                containingType,
                containingType.Name));
        }

        if (diagnostics.Count > 0)
        {
            return new Result<NavigateCommandGenerationInfo>(default!, diagnostics.ToImmutable());
        }

        commandName ??= NamingHelpers.GetCommandName(methodSymbol.Name);
        return new Result<NavigateCommandGenerationInfo>(
            new NavigateCommandGenerationInfo(
                HierarchyInfo.From(containingType),
                methodSymbol.Name,
                commandName,
                regionManagerMember!,
                region!,
                target!,
                regionsNamespace),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    internal static Result<NavigateOnChangedGenerationInfo> ExtractNavigateOnChanged(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        ISymbol? targetSymbol = context.TargetSymbol;
        INamedTypeSymbol? containingType = targetSymbol?.ContainingType as INamedTypeSymbol;
        if (containingType is null)
        {
            return new Result<NavigateOnChangedGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        string? regionsNamespace = PrismRegionsModel.ResolveRegionsNamespace(compilation);
        string? regionManagerMetadata = PrismRegionsModel.ResolveRegionManagerMetadataName(compilation);
        if (regionsNamespace is null || regionManagerMetadata is null)
        {
            return new Result<NavigateOnChangedGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(
            static a => a.AttributeClass?.ToDisplayString() == NavigateOnChangedAttributeMetadataName);
        if (attribute is null)
        {
            return new Result<NavigateOnChangedGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        if (targetSymbol is null)
        {
            return new Result<NavigateOnChangedGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        if (!HasObservableProperty(targetSymbol))
        {
            return new Result<NavigateOnChangedGenerationInfo>(
                default!,
                ImmutableArray.Create(
                    DiagnosticInfo.Create(
                        DiagnosticDescriptors.NavigateOnChangedRequiresObservableProperty,
                        targetSymbol,
                        targetSymbol.Name)));
        }

        string? region = attribute.TryGetNamedString("Region");
        string? targetMember = attribute.TryGetNamedString("TargetMember");
        string? regionManagerMember = attribute.TryGetNamedString("RegionManagerMember");

        ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        if (string.IsNullOrWhiteSpace(region))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.NavigateCommandRegionRequired,
                targetSymbol,
                targetSymbol.Name));
        }

        if (string.IsNullOrWhiteSpace(targetMember))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.NavigateOnChangedTargetMemberRequired,
                targetSymbol,
                targetSymbol.Name));
        }

        regionManagerMember ??= PrismRegionsModel.FindRegionManagerMemberName(compilation, containingType, regionManagerMetadata);
        if (string.IsNullOrWhiteSpace(regionManagerMember))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.RegionManagerMemberNotFound,
                containingType,
                containingType.Name));
        }

        if (diagnostics.Count > 0)
        {
            return new Result<NavigateOnChangedGenerationInfo>(default!, diagnostics.ToImmutable());
        }

        (string propertyName, string fieldType) = GetPropertyInfo(targetSymbol);
        string targetExpression = BuildTargetMemberExpression(targetMember!);

        return new Result<NavigateOnChangedGenerationInfo>(
            new NavigateOnChangedGenerationInfo(
                HierarchyInfo.From(containingType),
                propertyName,
                fieldType,
                regionManagerMember!,
                region!,
                targetExpression,
                regionsNamespace),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static bool HasObservableProperty(ISymbol symbol) =>
        symbol.GetAttributes().Any(static a =>
            a.AttributeClass?.ToDisplayString() == ObservablePropertyAttributeMetadataName);

    private static (string PropertyName, string FieldType) GetPropertyInfo(ISymbol symbol) =>
        symbol switch
        {
            IFieldSymbol field => (GetPropertyNameFromField(field.Name), field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            IPropertySymbol property => (property.Name, property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)),
            _ => throw new InvalidOperationException("Unexpected symbol for NavigateOnChanged."),
        };

    private static string GetPropertyNameFromField(string fieldName)
    {
        // Align with ObservablePropertyGenerator.GetPropertyName so [NavigateOnChanged]
        // emits the same On{Property}Changed hook that [ObservableProperty] generates.
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToUpperInvariant(fieldName[2]) + fieldName.Substring(3);
        if (fieldName.StartsWith('_') && fieldName.Length > 1)
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }

    private static string BuildTargetMemberExpression(string targetMember)
    {
        string trimmed = targetMember.Trim();
        if (trimmed.StartsWith("nameof(", StringComparison.Ordinal))
        {
            int start = trimmed.IndexOf('(') + 1;
            int end = trimmed.LastIndexOf(')');
            trimmed = end > start ? trimmed.Substring(start, end - start) : trimmed;
        }

        string[] parts = trimmed.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].Trim();
        }
        if (parts.Length == 0)
        {
            return "value";
        }

        if (parts.Length == 1)
        {
            return $"value.{parts[0]}";
        }

        return $"value.{string.Join('.', parts.Skip(1))}";
    }

}
