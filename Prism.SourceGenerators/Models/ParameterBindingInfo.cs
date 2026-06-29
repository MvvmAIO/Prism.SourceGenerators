namespace Prism.SourceGenerators.Models;

/// <summary>
/// Describes a single typed parameter binding for
/// <c>[FromNavigationParameter]</c> or <c>[FromDialogParameter]</c>.
/// </summary>
/// <param name="PropertyName">The generated property name (used for setter assignment).</param>
/// <param name="PropertyType">The fully qualified property type used as the <c>TryGetValue&lt;T&gt;</c> generic argument.</param>
/// <param name="ParameterKey">The parameter key; defaults to <paramref name="PropertyName"/> when not specified on the attribute.</param>
internal sealed record ParameterBindingInfo(
    string PropertyName,
    string PropertyType,
    string ParameterKey);
