using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

internal static class DialogServiceCommandSyntax
{
    public static CompilationUnitSyntax CreateCompilationUnit(ShowDialogCommandGenerationInfo info)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        string dialogLiteral = SymbolDisplay.FormatLiteral(info.DialogNameLiteral, quote: true);
        string fieldName = GetBackingFieldName(info.CommandName);
        string executeMethod = $"{info.MethodName}ShowDialogExecute";
        string closedCoreMethod = $"On{info.MethodName}DialogClosedCore";
        string dialogResult = $"global::{info.DialogsNamespace}.IDialogResult";
        string showDialogStatement = info.UsesExtensionShowDialog
            ? $"global::{info.DialogsNamespace}.IDialogServiceExtensions.ShowDialog({info.DialogServiceMember}, {dialogLiteral}, null, {closedCoreMethod});"
            : $"{info.DialogServiceMember}.ShowDialog({dialogLiteral}, null, {closedCoreMethod});";

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration($"private global::Prism.Commands.DelegateCommand? {fieldName};", options: options)
                ?? throw new InvalidOperationException("Failed to parse command field."),
            ParseMemberDeclaration(
                    $$"""
                    public global::Prism.Commands.DelegateCommand {{info.CommandName}} => {{fieldName}} ??= new global::Prism.Commands.DelegateCommand({{executeMethod}});
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse command property."),
            ParseMemberDeclaration(
                    $$"""
                    private void {{executeMethod}}()
                    {
                        {{showDialogStatement}}
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse show dialog execute."),
            ParseMemberDeclaration(
                    $"partial void On{info.MethodName}DialogClosed({dialogResult} result);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse dialog closed hook."),
            ParseMemberDeclaration(
                    $$"""
                    private void {{closedCoreMethod}}({{dialogResult}} result)
                    {
                        On{{info.MethodName}}DialogClosed(result);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse dialog closed core."),
        ];

        return info.Hierarchy.GetCompilationUnit(ImmutableArray.Create(members));
    }

    private static string GetBackingFieldName(string commandName) =>
        $"_{char.ToLowerInvariant(commandName[0])}{commandName.Substring(1)}";
}
