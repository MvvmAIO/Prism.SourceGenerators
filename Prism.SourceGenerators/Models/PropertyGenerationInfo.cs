using System;

namespace Prism.SourceGenerators.Models;

/// <summary>
/// A model representing the information needed to generate an observable property.
/// </summary>
internal sealed record PropertyGenerationInfo(
    HierarchyInfo Hierarchy,
    string FieldName,
    string PropertyName,
    string FieldType) : IEquatable<PropertyGenerationInfo>;
