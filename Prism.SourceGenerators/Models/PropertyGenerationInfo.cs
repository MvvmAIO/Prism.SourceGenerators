using System;
using Microsoft.CodeAnalysis;
using Prism.SourceGenerators.Helpers;

namespace Prism.SourceGenerators.Models;

/// <summary>
/// A model representing the information needed to generate an observable property.
/// </summary>
/// <param name="Hierarchy">The type hierarchy info.</param>
/// <param name="FieldName">The backing field name (for field-based) or property name (for partial property).</param>
/// <param name="PropertyName">The generated property name.</param>
/// <param name="FieldType">The fully qualified type name.</param>
/// <param name="IsPartialProperty">Whether this is a partial property declaration (uses <c>field</c> keyword).</param>
/// <param name="DeclaredAccessibility">The declared accessibility of the property (used for partial property generation).</param>
/// <param name="SetterAccessibility">The declared accessibility of the setter (e.g. <c>private set</c>). <see cref="Accessibility.NotApplicable"/> when same as property.</param>
/// <param name="NotifyPropertyChangedFor">Property names to also raise <c>PropertyChanged</c> for when this property changes.</param>
/// <param name="NotifyCanExecuteChangedFor">Command property names whose <c>RaiseCanExecuteChanged()</c> should be invoked when this property changes.</param>
/// <param name="ForwardedAttributes">Attributes (rendered as fully-qualified C# source) to emit on the generated property declaration. From <c>[property: Xxx]</c> on the field, or from any non-generator attribute on the partial property.</param>
/// <param name="NotifyDataErrorInfo">Whether the generated setter should call <c>ValidateProperty</c> after setting the value (requires the containing type to inherit from <c>ObservableValidator</c>).</param>
internal sealed record PropertyGenerationInfo(
    HierarchyInfo Hierarchy,
    string FieldName,
    string PropertyName,
    string FieldType,
    bool IsPartialProperty,
    Accessibility DeclaredAccessibility,
    Accessibility SetterAccessibility,
    EquatableArray<string> NotifyPropertyChangedFor,
    EquatableArray<string> NotifyCanExecuteChangedFor,
    EquatableArray<string> ForwardedAttributes,
    bool NotifyDataErrorInfo) : IEquatable<PropertyGenerationInfo>;
