using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Generates <c>Prism.Navigation.Regions.INavigationAware</c> members with optional partial hooks.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class NavigationAwareAttribute : Attribute
{
}
