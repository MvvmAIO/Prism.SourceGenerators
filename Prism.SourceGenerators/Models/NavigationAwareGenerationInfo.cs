using Prism.SourceGenerators.Helpers;

namespace Prism.SourceGenerators.Models;

/// <summary>Inputs for <c>[NavigationAware]</c> source emission.</summary>
/// <param name="Hierarchy">Type hierarchy metadata for emitted partials.</param>
/// <param name="RegionsNamespace">Fully qualified Prism regions namespace prefix (e.g. <c>Prism.Navigation.Regions</c> or <c>Prism.Regions</c>).</param>
/// <param name="ParameterBindings">Typed parameter bindings from <c>[FromNavigationParameter]</c> members, emitted in <c>OnNavigatedTo</c>.</param>
internal sealed record NavigationAwareGenerationInfo(
    HierarchyInfo Hierarchy,
    string RegionsNamespace,
    EquatableArray<ParameterBindingInfo> ParameterBindings);
