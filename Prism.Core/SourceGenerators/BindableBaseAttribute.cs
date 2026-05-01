namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to a class to generate an <c>INotifyPropertyChanged</c>
/// implementation when the class does not inherit from <c>Prism.Mvvm.BindableBase</c>.
/// <para>
/// The generated code includes:
/// <list type="bullet">
/// <item><c>PropertyChanged</c> event</item>
/// <item><c>SetProperty&lt;T&gt;</c> method for updating backing fields with change notification</item>
/// <item><c>RaisePropertyChanged</c> method for manually raising <c>PropertyChanged</c></item>
/// <item><c>OnPropertyChanged</c> virtual method for subclass customization</item>
/// </list>
/// </para>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BindableBaseAttribute : global::System.Attribute
{
}