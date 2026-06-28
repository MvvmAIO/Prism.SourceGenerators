using System;

namespace Prism.SourceGenerators;

/// <summary>
/// When applied alongside <c>[ObservableProperty]</c>, generates an <c>On{Property}Changed</c> implementation
/// that calls <c>IRegionManager.RequestNavigate</c> using a member of the new property value.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class NavigateOnChangedAttribute : Attribute
{
    /// <summary>Region name passed to <c>RequestNavigate</c>.</summary>
    public required string Region { get; init; }

    /// <summary>
    /// Member path on the changed value used as the navigation target (e.g. <c>nameof(NavigationItem.Key)</c>).
    /// </summary>
    public required string TargetMember { get; init; }

    /// <summary>
    /// Optional <c>IRegionManager</c> field or property name on the containing type.
    /// </summary>
    public string? RegionManagerMember { get; set; }
}
