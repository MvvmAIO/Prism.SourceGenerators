using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Generates a <c>DelegateCommand</c> that calls <c>IRegionManager.RequestNavigate</c> for the given region and target.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class NavigateCommandAttribute : Attribute
{
    /// <summary>Region name passed to <c>RequestNavigate</c>.</summary>
    public required string Region { get; init; }

    /// <summary>Navigation target (view name) passed to <c>RequestNavigate</c>.</summary>
    public required string Target { get; init; }

    /// <summary>
    /// Optional generated command property name. Defaults to <c>{MethodName}Command</c>.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Optional <c>IRegionManager</c> field or property name on the containing type.
    /// When omitted, the generator looks for a member typed as <c>IRegionManager</c>.
    /// </summary>
    public string? RegionManagerMember { get; set; }
}
