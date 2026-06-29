using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Marks a field or partial property for typed parameter binding from
/// <c>NavigationContext.Parameters</c> during <c>OnNavigatedTo</c>.
/// Requires <see cref="NavigationAwareAttribute"/> on the containing class
/// and <see cref="ObservablePropertyAttribute"/> on the same member.
/// </summary>
/// <para>
/// When applied, the generated <c>OnNavigatedTo</c> method will read the
/// parameter via <c>TryGetValue&lt;T&gt;</c> and assign it through the
/// property setter before <c>OnNavigatedToCore</c> is invoked.
/// If the parameter is absent the property retains its initial value.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [NavigationAware]
/// partial class PageVm : BindableBase
/// {
///     [FromNavigationParameter("userId")]
///     [ObservableProperty]
///     public partial int UserId { get; set; }
///
///     [FromNavigationParameter]  // key defaults to "UserName"
///     [ObservableProperty]
///     public partial string UserName { get; set; }
/// }
/// </code>
/// </para>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class FromNavigationParameterAttribute : Attribute
{
    /// <summary>
    /// Creates a new <see cref="FromNavigationParameterAttribute"/> instance.
    /// </summary>
    public FromNavigationParameterAttribute() { }

    /// <summary>
    /// Creates a new <see cref="FromNavigationParameterAttribute"/> instance
    /// with the specified parameter key.
    /// </summary>
    /// <param name="key">The parameter key in <c>NavigationContext.Parameters</c>.</param>
    public FromNavigationParameterAttribute(string key)
    {
        Key = key;
    }

    /// <summary>
    /// The parameter key in <c>NavigationContext.Parameters</c>.
    /// When not specified, the property name is used as the key.
    /// </summary>
    public string? Key { get; set; }
}
