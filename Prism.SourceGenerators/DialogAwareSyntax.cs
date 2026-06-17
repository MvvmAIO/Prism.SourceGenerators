using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

internal static class DialogAwareSyntax
{
    public static CompilationUnitSyntax CreateCompilationUnit(DialogAwareGenerationInfo info)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        string titleInitializer = string.IsNullOrEmpty(info.InitialTitle)
            ? string.Empty
            : $" = {SymbolDisplay.FormatLiteral(info.InitialTitle, quote: true)}";

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration(
                    $$"""
                    private string _dialogTitle{{titleInitializer}};
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse dialog title field."),
            ParseMemberDeclaration(
                    """
                    public string Title
                    {
                        get => _dialogTitle;
                        set => SetProperty(ref _dialogTitle, value);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse Title."),
            ParseMemberDeclaration(
                    """
                    public event global::System.Action<global::Prism.Services.Dialogs.IDialogResult>? RequestClose;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse RequestClose."),
            ParseMemberDeclaration(
                    """
                    public bool CanCloseDialog()
                    {
                        return CanCloseDialogCore();
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse CanCloseDialog."),
            ParseMemberDeclaration(
                    """
                    public void OnDialogClosed()
                    {
                        OnDialogClosedCore();
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogClosed."),
            ParseMemberDeclaration(
                    """
                    public void OnDialogOpened(global::Prism.Services.Dialogs.IDialogParameters parameters)
                    {
                        OnDialogOpenedCore(parameters);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogOpened."),
            ParseMemberDeclaration(
                    """
                    partial bool CanCloseDialogCore() => true;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse CanCloseDialogCore."),
            ParseMemberDeclaration(
                    "partial void OnDialogClosedCore();",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogClosedCore."),
            ParseMemberDeclaration(
                    "partial void OnDialogOpenedCore(global::Prism.Services.Dialogs.IDialogParameters parameters);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogOpenedCore."),
        ];

        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName("global::Prism.Services.Dialogs.IDialogAware"))));

        return info.Hierarchy.GetCompilationUnit(ImmutableArray.Create(members), baseList);
    }
}
