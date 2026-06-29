using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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

        // Build the OnNavigatedTo body with parameter binding injection
        string onNavigatedToBody = BuildOnNavigatedToBody(info.ParameterBindings.AsImmutableArray(), "navigationContext");
        string onNavigatedTo = "public void OnNavigatedTo(" + navigationContext + " navigationContext)\n{\n" + onNavigatedToBody + "\n}";

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration(onNavigatedTo, options: options)
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

    /// <summary>
    /// Builds the body of <c>OnNavigatedTo</c> with parameter binding reads
    /// followed by the <c>OnNavigatedToCore</c> call.
    /// </summary>
    private static string BuildOnNavigatedToBody(ImmutableArray<ParameterBindingInfo> bindings, string contextVar)
    {
        var sb = new StringBuilder();

        foreach (ParameterBindingInfo binding in bindings)
        {
            sb.AppendLine($"    if ({contextVar}.Parameters.TryGetValue<{binding.PropertyType}>(\"{binding.ParameterKey}\", out var {binding.PropertyName}Value))");
            sb.AppendLine($"        {binding.PropertyName} = {binding.PropertyName}Value;");
        }

        sb.Append("    OnNavigatedToCore(navigationContext);");
        return sb.ToString();
    }
}
