using Prism.SourceGenerators.Helpers;

namespace Prism.SourceGenerators.Models;

/// <summary>Inputs for <c>[DialogAware]</c> source emission.</summary>
/// <param name="Hierarchy">Type hierarchy metadata for emitted partials.</param>
/// <param name="InitialTitle">Optional initial title from the attribute.</param>
/// <param name="DialogsNamespace">Prism 8 (<c>Prism.Services.Dialogs</c>) or Prism 9+ (<c>Prism.Dialogs</c>).</param>
/// <param name="UsesDialogCloseListener">When true, emit <c>DialogCloseListener</c> instead of a <c>RequestClose</c> event.</param>
/// <param name="GeneratesTitle">When false, omit <c>Title</c> because the target contract has no title member.</param>
/// <param name="ParameterBindings">Typed parameter bindings from <c>[FromDialogParameter]</c> members, emitted in <c>OnDialogOpened</c>.</param>
internal sealed record DialogAwareGenerationInfo(
    HierarchyInfo Hierarchy,
    string InitialTitle,
    string DialogsNamespace,
    bool UsesDialogCloseListener,
    bool GeneratesTitle,
    EquatableArray<ParameterBindingInfo> ParameterBindings);
