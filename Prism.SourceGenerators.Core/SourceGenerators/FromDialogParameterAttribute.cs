using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Marks a field or partial property for typed parameter binding from
/// <c>IDialogParameters</c> during <c>OnDialogOpened</c>.
/// Requires <see cref="DialogAwareAttribute"/> on the containing class
/// and <see cref="ObservablePropertyAttribute"/> on the same member.
/// </summary>
/// <para>
/// When applied, the generated <c>OnDialogOpened</c> method will read the
/// parameter via <c>TryGetValue&lt;T&gt;</c> and assign it through the
/// property setter before <c>OnDialogOpenedCore</c> is invoked.
/// If the parameter is absent the property retains its initial value.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [DialogAware(Title = "Confirm")]
/// partial class ConfirmVm : BindableBase
/// {
///     [FromDialogParameter("message")]
///     [ObservableProperty]
///     public partial string Message { get; set; } = "Delete this item?";
/// }
/// </code>
/// </para>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class FromDialogParameterAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="FromDialogParameterAttribute"/> instance.
    /// </summary>
    public FromDialogParameterAttribute() { }

    /// <summary>
    /// Creates a new <see cref="FromDialogParameterAttribute"/> instance
    /// with the specified parameter key.
    /// </summary>
    /// <param name="key">The parameter key in <c>IDialogParameters</c>.</param>
    public FromDialogParameterAttribute(string key)
    {
        Key = key;
    }

    /// <summary>
    /// The parameter key in <c>IDialogParameters</c>.
    /// When not specified, the property name is used as the key.
    /// </summary>
    public string? Key { get; set; }
}
