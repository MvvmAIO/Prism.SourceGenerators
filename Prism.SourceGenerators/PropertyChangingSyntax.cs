using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

/// <summary>
/// Roslyn syntax for <c>INotifyPropertyChanging</c> companion partials (replaces string-built source in
/// <see cref="PropertyChangingGenerator"/>), aligned with CommunityToolkit.Mvvm's use of syntax trees +
/// <see cref="HierarchyInfo.GetCompilationUnit"/>.
/// </summary>
internal static class PropertyChangingSyntax
{
    private static readonly Lazy<ImmutableArray<MemberDeclarationSyntax>> MembersLazy = new(ParseMembers);

    private static ImmutableArray<MemberDeclarationSyntax> ParseMembers()
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration(
                    """
                    /// <inheritdoc cref="global::System.ComponentModel.INotifyPropertyChanging.PropertyChanging"/>
                    public event global::System.ComponentModel.PropertyChangingEventHandler? PropertyChanging;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse PropertyChanging event."),
            ParseMemberDeclaration(
                    """
                    /// <summary>
                    /// Raises the <see cref="PropertyChanging"/> event for the specified property when <c>FeatureSwitches.EnableINotifyPropertyChangingSupport</c> is <see langword="true"/>.
                    /// </summary>
                    protected void RaisePropertyChanging([global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
                    {
                        if (!global::Prism.SourceGenerators.__Internals.FeatureSwitches.EnableINotifyPropertyChangingSupport)
                        {
                            return;
                        }

                        OnPropertyChanging(new global::System.ComponentModel.PropertyChangingEventArgs(propertyName));
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse RaisePropertyChanging."),
            ParseMemberDeclaration(
                    """
                    /// <summary>
                    /// Raises the <see cref="PropertyChanging"/> event.
                    /// </summary>
                    protected virtual void OnPropertyChanging(global::System.ComponentModel.PropertyChangingEventArgs args)
                    {
                        if (args is null)
                        {
                            throw new global::System.ArgumentNullException(nameof(args));
                        }

                        if (!global::Prism.SourceGenerators.__Internals.FeatureSwitches.EnableINotifyPropertyChangingSupport)
                        {
                            return;
                        }

                        PropertyChanging?.Invoke(this, args);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnPropertyChanging."),
        ];

        return ImmutableArray.Create(members);
    }

    public static CompilationUnitSyntax CreateCompilationUnit(HierarchyInfo hierarchy)
    {
        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName("global::System.ComponentModel.INotifyPropertyChanging"))));

        return hierarchy.GetCompilationUnit(MembersLazy.Value, baseList);
    }
}
