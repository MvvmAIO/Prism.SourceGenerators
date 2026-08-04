using System;
using System.Collections.Immutable;
using System.Text;
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
        string ns = info.DialogsNamespace;
        string dialogAware = $"global::{ns}.IDialogAware";
        string dialogParameters = $"global::{ns}.IDialogParameters";
        string dialogResult = $"global::{ns}.IDialogResult";

        ImmutableArray<MemberDeclarationSyntax>.Builder members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        if (info.GeneratesTitle)
        {
            string titleInitializer = string.IsNullOrEmpty(info.InitialTitle)
                ? string.Empty
                : $" = {SymbolDisplay.FormatLiteral(info.InitialTitle, quote: true)}";

            members.Add(
                ParseMemberDeclaration(
                        $$"""
                        private string _dialogTitle{{titleInitializer}};
                        """,
                        options: options)
                    ?? throw new InvalidOperationException("Failed to parse dialog title field."));

            members.Add(
                ParseMemberDeclaration(
                        """
                        public string Title
                        {
                            get => _dialogTitle;
                            set => SetProperty(ref _dialogTitle, value);
                        }
                        """,
                        options: options)
                    ?? throw new InvalidOperationException("Failed to parse Title."));
        }

        if (info.UsesDialogCloseListener)
        {
            members.Add(
                ParseMemberDeclaration(
                        $$"""
                        public global::{{ns}}.DialogCloseListener RequestClose { get; set; } = new global::{{ns}}.DialogCloseListener();
                        """,
                        options: options)
                    ?? throw new InvalidOperationException("Failed to parse RequestClose listener."));
        }
        else
        {
            members.Add(
                ParseMemberDeclaration(
                        $$"""
                        public event global::System.Action<{{dialogResult}}>? RequestClose;
                        """,
                        options: options)
                    ?? throw new InvalidOperationException("Failed to parse RequestClose."));
        }

        members.Add(
            ParseMemberDeclaration(
                    """
                    public bool CanCloseDialog()
                    {
                        return CanCloseDialogCore();
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse CanCloseDialog."));

        members.Add(
            ParseMemberDeclaration(
                    """
                    public void OnDialogClosed()
                    {
                        OnDialogClosedCore();
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogClosed."));

        // Build OnDialogOpened body with parameter binding injection
        string onDialogOpenedBody = BuildOnDialogOpenedBody(info.ParameterBindings.AsImmutableArray());
        string onDialogOpened = "public void OnDialogOpened(" + dialogParameters + " parameters)\n{\n" + onDialogOpenedBody + "\n}";

        members.Add(
            ParseMemberDeclaration(onDialogOpened, options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogOpened."));

        members.Add(
            ParseMemberDeclaration(
                    "private partial bool CanCloseDialogCore();",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse CanCloseDialogCore declaration."));

        members.Add(
            ParseMemberDeclaration(
                    """
                    private partial bool CanCloseDialogCore() => true;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse CanCloseDialogCore."));

        members.Add(
            ParseMemberDeclaration(
                    "partial void OnDialogClosedCore();",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogClosedCore."));

        members.Add(
            ParseMemberDeclaration(
                    $"partial void OnDialogOpenedCore({dialogParameters} parameters);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnDialogOpenedCore."));

        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName(dialogAware))));

        return info.Hierarchy.GetCompilationUnit(members.ToImmutable(), baseList);
    }

    /// <summary>
    /// Builds the body of <c>OnDialogOpened</c> with parameter binding reads
    /// followed by the <c>OnDialogOpenedCore</c> call.
    /// </summary>
    private static string BuildOnDialogOpenedBody(ImmutableArray<ParameterBindingInfo> bindings)
    {
        var sb = new StringBuilder();
        sb.Append(ParameterBinding.BuildBindingStatements(bindings, "parameters"));
        sb.Append("    OnDialogOpenedCore(parameters);");
        return sb.ToString();
    }
}
