using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Models;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Prism.SourceGenerators;

/// <summary>
/// Roslyn syntax for <c>INotifyPropertyChanged</c> companion partials from <see cref="BindableBaseGenerator"/>,
/// replacing string-built source. Uses <see cref="HierarchyInfo.GetCompilationUnit"/> like
/// <see cref="PropertyChangingSyntax"/>.
/// </summary>
internal static class BindableBaseSyntax
{
    private static readonly Lazy<ImmutableArray<MemberDeclarationSyntax>> MembersLazy = new(ParseMembers);

    private static ImmutableArray<MemberDeclarationSyntax> ParseMembers()
    {
        CSharpParseOptions options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        MemberDeclarationSyntax[] members =
        [
            ParseMemberDeclaration(
                    """
                    /// <inheritdoc cref="global::System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
                    public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse PropertyChanged event."),
            ParseMemberDeclaration(
                    """
                    /// <summary>
                    /// Sets the property value and raises <see cref="PropertyChanged"/> if the value has changed.
                    /// </summary>
                    /// <typeparam name="T">The type of the property.</typeparam>
                    /// <param name="storage">Reference to the backing field.</param>
                    /// <param name="value">The new value.</param>
                    /// <param name="propertyName">The property name (auto-filled by the compiler).</param>
                    /// <returns><see langword="true"/> if the value was changed; otherwise <see langword="false"/>.</returns>
                    protected bool SetProperty<T>(ref T storage, T value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
                    {
                        if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(storage, value))
                        {
                            return false;
                        }

                        RaisePropertyChanging(propertyName);
                        storage = value;
                        RaisePropertyChanged(propertyName);
                        return true;
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse SetProperty(ref, value, propertyName)."),
            ParseMemberDeclaration(
                    """
                    /// <summary>
                    /// Sets the property value, invokes <paramref name="onChanged"/> after assignment, then raises <see cref="PropertyChanged"/>.
                    /// Does not raise <see cref="global::System.ComponentModel.INotifyPropertyChanging.PropertyChanging"/> here; callers such as generated <c>[ObservableProperty]</c> setters raise it before changing callbacks.
                    /// </summary>
                    /// <typeparam name="T">The type of the property.</typeparam>
                    /// <param name="storage">Reference to the backing field.</param>
                    /// <param name="value">The new value.</param>
                    /// <param name="onChanged">Optional callback invoked after the value is stored and before <see cref="PropertyChanged"/>.</param>
                    /// <param name="propertyName">The property name (auto-filled by the compiler).</param>
                    /// <returns><see langword="true"/> if the value was changed; otherwise <see langword="false"/>.</returns>
                    protected bool SetProperty<T>(ref T storage, T value, global::System.Action? onChanged, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
                    {
                        if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(storage, value))
                        {
                            return false;
                        }

                        storage = value;
                        onChanged?.Invoke();
                        RaisePropertyChanged(propertyName);
                        return true;
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse SetProperty with onChanged."),
            ParseMemberDeclaration(
                    """
                    /// <summary>
                    /// Raises the <see cref="PropertyChanged"/> event for the specified property.
                    /// </summary>
                    /// <param name="propertyName">The property name.</param>
                    protected void RaisePropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
                    {
                        OnPropertyChanged(new global::System.ComponentModel.PropertyChangedEventArgs(propertyName));
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse RaisePropertyChanged."),
            ParseMemberDeclaration(
                    """
                    /// <summary>
                    /// Raises the <see cref="PropertyChanged"/> event.
                    /// </summary>
                    /// <param name="args">The <see cref="global::System.ComponentModel.PropertyChangedEventArgs"/> instance.</param>
                    protected virtual void OnPropertyChanged(global::System.ComponentModel.PropertyChangedEventArgs args)
                    {
                        PropertyChanged?.Invoke(this, args);
                    }
                    """,
                    options: options)
                ?? throw new InvalidOperationException("Failed to parse OnPropertyChanged."),
        ];

        return ImmutableArray.Create(members);
    }

    public static CompilationUnitSyntax CreateCompilationUnit(HierarchyInfo hierarchy)
    {
        BaseListSyntax baseList = BaseList(
            SingletonSeparatedList<BaseTypeSyntax>(
                SimpleBaseType(ParseTypeName("global::System.ComponentModel.INotifyPropertyChanged"))));

        return hierarchy.GetCompilationUnit(MembersLazy.Value, baseList);
    }
}
