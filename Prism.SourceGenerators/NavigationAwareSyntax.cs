using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

internal static class NavigationAwareSyntax
{
    private static readonly Lazy<ImmutableArray<MemberDeclarationSyntax>> MembersLazy = new(ParseMembers);

    private static ImmutableArray<MemberDeclarationSyntax> ParseMembers()
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration(
                    """
                    public void OnNavigatedTo(global::Prism.Navigation.Regions.NavigationContext navigationContext)
                    {
                        OnNavigatedToCore(navigationContext);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedTo."),
            ParseMemberDeclaration(
                    """
                    public bool IsNavigationTarget(global::Prism.Navigation.Regions.NavigationContext navigationContext)
                    {
                        return IsNavigationTargetCore(navigationContext);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse IsNavigationTarget."),
            ParseMemberDeclaration(
                    """
                    public void OnNavigatedFrom(global::Prism.Navigation.Regions.NavigationContext navigationContext)
                    {
                        OnNavigatedFromCore(navigationContext);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedFrom."),
            ParseMemberDeclaration(
                    "partial void OnNavigatedToCore(global::Prism.Navigation.Regions.NavigationContext navigationContext);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedToCore."),
            ParseMemberDeclaration(
                    """
                    partial bool IsNavigationTargetCore(global::Prism.Navigation.Regions.NavigationContext navigationContext) => true;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse IsNavigationTargetCore."),
            ParseMemberDeclaration(
                    "partial void OnNavigatedFromCore(global::Prism.Navigation.Regions.NavigationContext navigationContext);",
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnNavigatedFromCore."),
        ];

        return ImmutableArray.Create(members);
    }

    public static CompilationUnitSyntax CreateCompilationUnit(HierarchyInfo hierarchy)
    {
        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName("global::Prism.Navigation.Regions.INavigationAware"))));

        return hierarchy.GetCompilationUnit(MembersLazy.Value, baseList);
    }
}
