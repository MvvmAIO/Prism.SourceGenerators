namespace Prism.SourceGenerators.Models;

/// <summary>Inputs for <c>[DialogAware]</c> source emission.</summary>
/// <param name="Hierarchy">Type hierarchy metadata for emitted partials.</param>
/// <param name="InitialTitle">Optional initial title from the attribute.</param>
internal sealed record DialogAwareGenerationInfo(HierarchyInfo Hierarchy, string InitialTitle);
