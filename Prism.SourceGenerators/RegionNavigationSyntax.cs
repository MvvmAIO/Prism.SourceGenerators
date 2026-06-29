using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Helpers;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

internal static class RegionNavigationSyntax
{
    public static CompilationUnitSyntax CreateNavigateCommandCompilationUnit(NavigateCommandGenerationInfo info)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        string regionLiteral = SymbolDisplay.FormatLiteral(info.RegionLiteral, quote: true);
        string targetLiteral = SymbolDisplay.FormatLiteral(info.TargetLiteral, quote: true);
        string fieldName = NamingHelpers.GetBackingFieldName(info.CommandName);
        string executeMethod = $"{info.MethodName}NavigateExecute";

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
                        {{info.RegionManagerMember}}.RequestNavigate({{regionLiteral}}, {{targetLiteral}});
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse navigate execute."),
        ];

        return info.Hierarchy.GetCompilationUnit(ImmutableArray.Create(members));
    }

    public static CompilationUnitSyntax CreateNavigateOnChangedCompilationUnit(NavigateOnChangedGenerationInfo info)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        string regionLiteral = SymbolDisplay.FormatLiteral(info.RegionLiteral, quote: true);

        MemberDeclarationSyntax member =
            ParseMemberDeclaration(
                    $$"""
                    partial void On{{info.PropertyName}}Changed({{info.FieldType}} value)
                    {
                        {{info.RegionManagerMember}}.RequestNavigate({{regionLiteral}}, {{info.TargetMemberExpression}});
                    }
                    """,
                    options: options)
            ?? throw new InvalidOperationException("Failed to parse NavigateOnChanged hook.");

        return info.Hierarchy.GetCompilationUnit(ImmutableArray.Create(member));
    }
}
