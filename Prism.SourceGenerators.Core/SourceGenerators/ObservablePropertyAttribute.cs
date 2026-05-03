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
/// Pass <see cref="PropertyAccess"/> positionally, or set the <see cref="ObservablePropertyAttribute.PropertyAccess"/>
/// named argument, to control the generated property's accessibility. The default is <see cref="PropertyAccess.Public"/>.
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
    /// <summary>Initializes a new instance; generated property accessibility defaults to <see cref="PropertyAccess.Public"/>.</summary>
    public ObservablePropertyAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified accessibility for the generated property (field target mode only).
    /// </summary>
    /// <param name="propertyAccess">Accessibility of the generated property.</param>
    public ObservablePropertyAttribute(PropertyAccess propertyAccess)
    {
        PropertyAccess = propertyAccess;
    }

    /// <summary>
    /// Gets or sets the accessibility of the property generated for a <b>field</b> target. Ignored for partial property targets.
    /// </summary>
    /// <remarks>
    /// A public setter allows <c>[ObservableProperty(PropertyAccess = PropertyAccess.Internal)]</c> syntax in addition to the positional constructor.
    /// </remarks>
    public PropertyAccess PropertyAccess { get; set; } = PropertyAccess.Public;
}
