namespace Prism.SourceGenerators;

/// <summary>
/// When applied to a partial class, generates <see cref="BindableValidator"/> support:
/// <list type="bullet">
/// <item>If the type has no declared base (only <see cref="object"/>), the generated partial inherits <see cref="BindableValidator"/>.</item>
/// <item>Otherwise, the generator emits <see cref="System.ComponentModel.INotifyDataErrorInfo"/> and validation helpers into the partial type,
/// reusing <see cref="System.ComponentModel.INotifyPropertyChanged"/> from the existing hierarchy when it is already implemented.</item>
/// </list>
/// <para>
/// This attribute is mutually exclusive with generating <c>[BindableBase]</c> infrastructure: when both attributes are present,
/// <c>[BindableValidator]</c> takes precedence for <c>INotifyPropertyChanged</c> when the type inherits <see cref="BindableValidator"/>,
/// and <c>[BindableBase]</c> is suppressed to avoid duplicate members.
/// </para>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BindableValidatorAttribute : global::System.Attribute
{
}
