namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be used to support <see cref="ObservablePropertyAttribute"/> in generated properties,
/// when applied to fields or partial properties contained in a type that inherits from
/// <see cref="Prism.SourceGenerators.BindableValidator"/> and uses validation attributes
/// (e.g. <c>[Required]</c>, <c>[MinLength]</c>).
/// <para>
/// When this attribute is used, the generated property setter will also call
/// <c>ValidateProperty(value, nameof(Property))</c> after setting the value.
/// </para>
/// <para>
/// This attribute can also be used on a class, which will enable the validation on all generated
/// properties contained in it.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// partial class MyViewModel : BindableValidator
/// {
///     [ObservableProperty]
///     [NotifyDataErrorInfo]
///     [Required]
///     [MinLength(2)]
///     public partial string Username { get; set; }
/// }
/// </code>
/// </para>
/// </summary>
[global::System.AttributeUsage(
    global::System.AttributeTargets.Field | global::System.AttributeTargets.Property | global::System.AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class NotifyDataErrorInfoAttribute : global::System.Attribute
{
}
