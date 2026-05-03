namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to a field or partial property with <c>[ObservableProperty]</c>
/// to indicate that the generated property setter should also raise <c>PropertyChanged</c>
/// for the specified property names.
/// <para>
/// <code>
/// [ObservableProperty]
/// [NotifyPropertyChangedFor(nameof(FullName))]
/// private string _firstName;
/// </code>
/// The generated setter for <c>FirstName</c> will also call
/// <c>RaisePropertyChanged(nameof(FullName))</c>.
/// </para>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
public sealed class NotifyPropertyChangedForAttribute : global::System.Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyPropertyChangedForAttribute"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the property to also notify.</param>
    public NotifyPropertyChangedForAttribute(string propertyName)
    {
        PropertyNames = new[] { propertyName };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotifyPropertyChangedForAttribute"/> class.
    /// </summary>
    /// <param name="propertyName">The name of the first property to also notify.</param>
    /// <param name="otherPropertyNames">The names of other properties to also notify.</param>
    public NotifyPropertyChangedForAttribute(string propertyName, params string[] otherPropertyNames)
    {
        var names = new string[otherPropertyNames.Length + 1];
        names[0] = propertyName;
        otherPropertyNames.CopyTo(names, 1);
        PropertyNames = names;
    }

    /// <summary>
    /// Gets the property names to also raise <c>PropertyChanged</c> for.
    /// </summary>
    public string[] PropertyNames { get; }
}