namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to a field or partial property in a class inheriting from
/// <c>Prism.Mvvm.BindableBase</c> to generate an observable property that
/// calls <c>SetProperty</c> in the setter.
/// <para>
/// <b>Field usage</b> (all C# versions):
/// <code>
/// [ObservableProperty]
/// private string _name;
/// </code>
/// Generates: <c>public string Name { get =&gt; _name; set =&gt; SetProperty(ref _name, value); }</c>
/// Use <see cref="ObservablePropertyAccess"/> to control the generated property's accessibility; the default is
/// <see cref="ObservablePropertyAccess.Public"/> (including for the parameterless attribute constructor).
/// </para>
/// <para>
/// <b>Partial property usage</b> (C# 13+):
/// <code>
/// [ObservableProperty]
/// public partial string Name { get; set; }
/// </code>
/// Generates: <c>public partial string Name { get =&gt; field; set =&gt; SetProperty(ref field, value); }</c>
/// </para>
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Field | global::System.AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class ObservablePropertyAttribute : global::System.Attribute
{
    /// <summary>
    /// Initializes a new instance with <see cref="PropertyAccess"/> set to <see cref="ObservablePropertyAccess.Public"/>.
    /// </summary>
    public ObservablePropertyAttribute()
    {
        PropertyAccess = ObservablePropertyAccess.Public;
    }

    /// <summary>
    /// Initializes a new instance with the specified accessibility for the generated property (field target mode only).
    /// </summary>
    /// <param name="propertyAccess">Accessibility of the generated property.</param>
    public ObservablePropertyAttribute(ObservablePropertyAccess propertyAccess)
    {
        PropertyAccess = propertyAccess;
    }

    /// <summary>
    /// Gets the accessibility of the property generated for a <b>field</b> target. Ignored for partial property targets.
    /// </summary>
    public ObservablePropertyAccess PropertyAccess { get; }
}