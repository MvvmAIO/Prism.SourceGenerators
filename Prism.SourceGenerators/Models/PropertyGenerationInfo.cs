using System;

namespace Prism.SourceGenerators.Models;

/// <summary>
/// A model representing the information needed to generate an observable property.
/// </summary>
/// <param name="Hierarchy">The type hierarchy info.</param>
/// <param name="FieldName">The backing field name (for field-based) or property name (for partial property).</param>
/// <param name="PropertyName">The generated property name.</param>
/// <param name="FieldType">The fully qualified type name.</param>
/// <param name="IsPartialProperty">Whether this is a partial property declaration (uses <c>field</c> keyword).</param>
internal sealed record PropertyGenerationInfo(
    HierarchyInfo Hierarchy,
    string FieldName,
    string PropertyName,
    string FieldType,
    bool IsPartialProperty) : IEquatable<PropertyGenerationInfo>;
