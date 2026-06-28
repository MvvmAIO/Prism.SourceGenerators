using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Prism.SourceGenerators.Diagnostics;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Models;

namespace Prism.SourceGenerators;

internal static class DialogServiceCommandMetadataExtractor
{
    internal const string ShowDialogCommandAttributeMetadataName = "Prism.SourceGenerators.ShowDialogCommandAttribute";

    internal static Result<ShowDialogCommandGenerationInfo> ExtractShowDialogCommand(
        GeneratorAttributeSyntaxContext context,
        CancellationToken token)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol
            || methodSymbol.ContainingType is not INamedTypeSymbol containingType)
        {
            return new Result<ShowDialogCommandGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        Compilation compilation = context.SemanticModel.Compilation;
        if (!PrismDialogsModel.CompilationHasDialogService(compilation))
        {
            return new Result<ShowDialogCommandGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        AttributeData? attribute = context.Attributes.FirstOrDefault(
            static a => a.AttributeClass?.ToDisplayString() == ShowDialogCommandAttributeMetadataName);
        if (attribute is null)
        {
            return new Result<ShowDialogCommandGenerationInfo>(default!, ImmutableArray<DiagnosticInfo>.Empty);
        }

        string? dialogName = GetRequiredString(attribute, "Name");
        string? commandName = GetOptionalString(attribute, "CommandName");
        string? dialogServiceMember = GetOptionalString(attribute, "DialogServiceMember");

        ImmutableArray<DiagnosticInfo>.Builder diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        if (string.IsNullOrWhiteSpace(dialogName))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.ShowDialogCommandNameRequired,
                methodSymbol,
                methodSymbol.Name));
        }

        dialogServiceMember ??= PrismDialogsModel.FindDialogServiceMemberName(compilation, containingType);
        if (string.IsNullOrWhiteSpace(dialogServiceMember))
        {
            diagnostics.Add(DiagnosticInfo.Create(
                DiagnosticDescriptors.DialogServiceMemberNotFound,
                containingType,
                containingType.Name));
        }

        if (diagnostics.Count > 0)
        {
            return new Result<ShowDialogCommandGenerationInfo>(default!, diagnostics.ToImmutable());
        }

        commandName ??= GetCommandName(methodSymbol.Name);
        string dialogsNamespace = PrismDialogsModel.ResolveDialogsNamespace(compilation)!;
        bool usesExtensionShowDialog = string.Equals(
            dialogsNamespace,
            PrismDialogsModel.Prism9DialogsNamespace,
            StringComparison.Ordinal);

        return new Result<ShowDialogCommandGenerationInfo>(
            new ShowDialogCommandGenerationInfo(
                HierarchyInfo.From(containingType),
                methodSymbol.Name,
                commandName,
                dialogServiceMember!,
                dialogName!,
                dialogsNamespace,
                usesExtensionShowDialog),
            ImmutableArray<DiagnosticInfo>.Empty);
    }

    private static string? GetRequiredString(AttributeData attribute, string name)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == name && pair.Value.Value is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? GetOptionalString(AttributeData attribute, string name)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == name && pair.Value.Value is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string GetCommandName(string methodName) =>
        methodName.EndsWith("Command", StringComparison.Ordinal) ? methodName : $"{methodName}Command";
}
