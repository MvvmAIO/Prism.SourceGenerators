namespace Prism.SourceGenerators.Models;

/// <summary>
/// Emission mode for <c>[BindableValidator]</c>.
/// </summary>
internal enum BindableValidatorEmitMode
{
    /// <summary>The generated partial declares <c>: global::Prism.SourceGenerators.BindableValidator</c> with no extra members.</summary>
    InheritBindableValidator,

    /// <summary>Emit INPC plus <c>INotifyDataErrorInfo</c> and validation helpers into the partial.</summary>
    InlineFull,

    /// <summary>Emit only <c>INotifyDataErrorInfo</c> and validation helpers; INPC comes from the existing hierarchy.</summary>
    InlineValidationOnly,
}

/// <summary>
/// Inputs for <c>[BindableValidator]</c> source emission.
/// </summary>
/// <param name="Hierarchy">Type hierarchy for nested partial declarations.</param>
/// <param name="EmitMode">How to emit BindableValidator support.</param>
internal sealed record BindableValidatorGenerationInfo(
    HierarchyInfo Hierarchy,
    BindableValidatorEmitMode EmitMode);
