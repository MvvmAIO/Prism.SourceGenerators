using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

/// <summary>Partial properties with <c>NotifyPropertyChangedFor</c> in a navigable profile view.</summary>
public partial class ProfileViewModel : BindableBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    public partial string FirstName { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    public partial string LastName { get; set; } = "";

    public string FullName => $"{FirstName} {LastName}".Trim();

    [ObservableProperty]
    public partial string ProfileTitle { get; set; } = "Profile (partial properties + field keyword)";
}
