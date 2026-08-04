using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

/// <summary>
/// Which <c>From*</c> attribute and PSG diagnostic trio apply to a Parameter Binding extract.
/// </summary>
/// <param name="AttributeMetadataName">Fully qualified attribute metadata name.</param>
/// <param name="InvalidTarget">Diagnostic when the attribute is not on a field or property.</param>
/// <param name="RequiresObservableProperty">Warning when <c>[ObservableProperty]</c> is missing.</param>
/// <param name="EmptyKey">Error when an explicit key is empty or whitespace.</param>
internal readonly record struct ParameterBindingKind(
    string AttributeMetadataName,
    DiagnosticDescriptor InvalidTarget,
    DiagnosticDescriptor RequiresObservableProperty,
    DiagnosticDescriptor EmptyKey)
{
    internal const string FromNavigationParameterAttributeMetadataName = "Prism.SourceGenerators.FromNavigationParameterAttribute";
    internal const string FromDialogParameterAttributeMetadataName = "Prism.SourceGenerators.FromDialogParameterAttribute";

    /// <summary>
    /// <c>[FromNavigationParameter]</c> with PSG7006–PSG7008.
    /// </summary>
    public static ParameterBindingKind Navigation { get; } = new(
        FromNavigationParameterAttributeMetadataName,
        DiagnosticDescriptors.FromNavigationParameterOnInvalidTarget,
        DiagnosticDescriptors.FromNavigationParameterRequiresObservableProperty,
        DiagnosticDescriptors.FromNavigationParameterEmptyKey);

    /// <summary>
    /// <c>[FromDialogParameter]</c> with PSG7103–PSG7105.
    /// </summary>
    public static ParameterBindingKind Dialog { get; } = new(
        FromDialogParameterAttributeMetadataName,
        DiagnosticDescriptors.FromDialogParameterOnInvalidTarget,
        DiagnosticDescriptors.FromDialogParameterRequiresObservableProperty,
        DiagnosticDescriptors.FromDialogParameterEmptyKey);
}

/// <summary>
/// Parameter Binding extract and emit helpers shared by NavigationAware and DialogAware.
/// </summary>
internal static class ParameterBinding
{
    private const string ObservablePropertyAttributeMetadataName = "Prism.SourceGenerators.ObservablePropertyAttribute";

    /// <summary>
    /// Extracts typed Parameter Bindings for members annotated with the Kind's <c>From*</c> attribute.
    /// Warnings omit only the offending binding; Errors are returned for the caller to treat as blocking.
    /// </summary>
    internal static (EquatableArray<ParameterBindingInfo> Bindings, ImmutableArray<DiagnosticInfo> Diagnostics)
        Extract(INamedTypeSymbol classSymbol, ParameterBindingKind kind)
    {
        var bindingBuilder = ImmutableArray.CreateBuilder<ParameterBindingInfo>();
        var diagnosticBuilder = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        foreach (ISymbol member in classSymbol.GetAllMembers())
        {
            AttributeData? attr = null;
            foreach (AttributeData a in member.GetAttributes())
            {
                if (a.AttributeClass?.ToDisplayString() == kind.AttributeMetadataName)
                {
                    attr = a;
                    break;
                }
            }

            if (attr is null)
            {
                continue;
            }

            if (member is not IFieldSymbol and not IPropertySymbol)
            {
                diagnosticBuilder.Add(
                    DiagnosticInfo.Create(
                        kind.InvalidTarget,
                        member,
                        member.Name));
                continue;
            }

            if (!HasObservableProperty(member))
            {
                diagnosticBuilder.Add(
                    DiagnosticInfo.Create(
                        kind.RequiresObservableProperty,
                        member,
                        member.Name));
                continue;
            }

            string? explicitKey = attr.TryGetNamedString("Key");
            if (explicitKey is null && attr.TryGetConstructorArgument<string>(0, out string? constructorKey))
            {
                explicitKey = constructorKey;
            }

            string propertyName;
            string propertyType;

            if (member is IFieldSymbol field)
            {
                propertyName = NamingHelpers.GetPropertyNameFromField(field.Name);
                propertyType = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
            else
            {
                IPropertySymbol property = (IPropertySymbol)member;
                propertyName = property.Name;
                propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            if (explicitKey is not null && string.IsNullOrWhiteSpace(explicitKey))
            {
                diagnosticBuilder.Add(
                    DiagnosticInfo.Create(
                        kind.EmptyKey,
                        member,
                        member.Name));
                continue;
            }

            string parameterKey = explicitKey ?? propertyName;
            bindingBuilder.Add(new ParameterBindingInfo(propertyName, propertyType, parameterKey));
        }

        return (bindingBuilder.ToImmutable().AsEquatableArray(), diagnosticBuilder.ToImmutable());
    }

    /// <summary>
    /// Builds indented <c>TryGetValue</c> assignment statements for the given bindings.
    /// Does not include the Aware <c>*Core</c> call — callers append that.
    /// </summary>
    /// <param name="bindings">Clean Parameter Bindings to emit.</param>
    /// <param name="parametersAccessExpression">
    /// Expression that exposes <c>TryGetValue&lt;T&gt;</c>
    /// (e.g. <c>navigationContext.Parameters</c> or <c>parameters</c>).
    /// </param>
    internal static string BuildBindingStatements(
        ImmutableArray<ParameterBindingInfo> bindings,
        string parametersAccessExpression)
    {
        if (bindings.IsDefaultOrEmpty)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (ParameterBindingInfo binding in bindings)
        {
            sb.AppendLine($"    if ({parametersAccessExpression}.TryGetValue<{binding.PropertyType}>(\"{binding.ParameterKey}\", out var {binding.PropertyName}Value))");
            sb.AppendLine($"        {binding.PropertyName} = {binding.PropertyName}Value;");
        }

        return sb.ToString();
    }

    private static bool HasObservableProperty(ISymbol symbol) =>
        symbol.GetAttributes().Any(static a =>
            a.AttributeClass?.ToDisplayString() == ObservablePropertyAttributeMetadataName);
}
