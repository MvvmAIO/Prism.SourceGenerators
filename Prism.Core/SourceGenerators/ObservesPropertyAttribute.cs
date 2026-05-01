namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to a method (alongside [DelegateCommand] or [AsyncDelegateCommand])
/// to observe property changes and automatically re-evaluate CanExecute.
/// Corresponds to <c>DelegateCommand.ObservesProperty</c> / <c>AsyncDelegateCommand.ObservesProperty</c>.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ObservesPropertyAttribute : global::System.Attribute
{
    public ObservesPropertyAttribute(string propertyName, params string[] otherPropertyNames)
    {
        PropertyName = propertyName;
        OtherPropertyNames = otherPropertyNames;
    }

    public string PropertyName { get; }
    public string[] OtherPropertyNames { get; }
}