using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Generates a <c>DelegateCommand</c> that calls <c>IDialogService.ShowDialog</c> for the given dialog name.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ShowDialogCommandAttribute : Attribute
{
    /// <summary>Dialog name registered with <c>RegisterDialog</c>.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional generated command property name. Defaults to <c>{MethodName}Command</c>.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Optional <c>IDialogService</c> field or property name on the containing type.
    /// </summary>
    public string? DialogServiceMember { get; set; }
}
