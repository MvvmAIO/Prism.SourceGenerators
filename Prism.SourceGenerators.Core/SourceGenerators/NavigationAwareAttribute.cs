using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Generates <c>INavigationAware</c> members with optional partial hooks.
/// Supports Prism 8 (<c>Prism.Regions</c>) and Prism 9+ (<c>Prism.Navigation.Regions</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NavigationAwareAttribute : Attribute
{
}
