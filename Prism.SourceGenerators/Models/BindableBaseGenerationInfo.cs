namespace Prism.SourceGenerators.Models;

/// <summary>
/// Inputs for <c>[BindableBase]</c> source emission (aligned with CommunityToolkit.Mvvm <c>ObservableObject</c>: INotifyPropertyChanging is always generated when the type hierarchy does not already implement it).
/// </summary>
/// <param name="Hierarchy">Type hierarchy for nested partial declarations.</param>
/// <param name="EmitChangingInterfaceAndMembers">
/// When <see langword="true"/>, <see cref="PropertyChangingGenerator"/> emits a companion <c>*.BindableBase.PropertyChanging.g.cs</c> partial with
/// <see cref="System.ComponentModel.INotifyPropertyChanging"/> plus <c>PropertyChanging</c>, <c>RaisePropertyChanging</c>, and <c>OnPropertyChanging</c>.
/// When <see langword="false"/>, the type hierarchy already implements the interface (e.g. from a base class).
/// </param>
internal sealed record BindableBaseGenerationInfo(
    HierarchyInfo Hierarchy,
    bool EmitChangingInterfaceAndMembers);
