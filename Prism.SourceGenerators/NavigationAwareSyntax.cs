using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

internal static class NavigationAwareSyntax
{
    public static CompilationUnitSyntax CreateCompilationUnit(NavigationAwareGenerationInfo info)
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        string ns = info.RegionsNamespace;
        string navigationContext = $"global::{ns}.NavigationContext";
        string navigationAware = $"global::{ns}.INavigationAware";

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration(
                    $$"""
                    public void OnNavigatedTo({{navigationContext}} navigationContext)
                    {
                        OnNavigatedToCore(navigationContext);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedTo."),
            ParseMemberDeclaration(
                    $$"""
                    public bool IsNavigationTarget({{navigationContext}} navigationContext)
                    {
                        return IsNavigationTargetCore(navigationContext);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse IsNavigationTarget."),
            ParseMemberDeclaration(
                    $$"""
                    public void OnNavigatedFrom({{navigationContext}} navigationContext)
                    {
                        OnNavigatedFromCore(navigationContext);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedFrom."),
            ParseMemberDeclaration(
                    $"partial void OnNavigatedToCore({navigationContext} navigationContext);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedToCore."),
            ParseMemberDeclaration(
                    $"private partial bool IsNavigationTargetCore({navigationContext} navigationContext);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse IsNavigationTargetCore declaration."),
            ParseMemberDeclaration(
                    $$"""
                    private partial bool IsNavigationTargetCore({{navigationContext}} navigationContext) => true;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse IsNavigationTargetCore."),
            ParseMemberDeclaration(
                    $"partial void OnNavigatedFromCore({navigationContext} navigationContext);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedFromCore."),
        ];

        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName(navigationAware))));

        return info.Hierarchy.GetCompilationUnit(ImmutableArray.Create(members), baseList);
    }
}
