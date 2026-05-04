using System.Collections.ObjectModel;
using Prism.Mvvm;
using Prism.Regions;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism8.Models;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

/// <summary>Shell ViewModel with Prism region navigation.</summary>
public partial class MainViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private bool _navigationReady;

    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new("Dashboard", "Dashboard", "Overview and Prism region navigation."),
        new("Commands", "Commands", "DelegateCommand and AsyncDelegateCommand generation."),
        new("Profile", "Profile", "ObservableProperty on backing fields.")
    ];

    [ObservableProperty]
    private NavigationItem _selectedNavigationItem =
        new("Dashboard", "Dashboard", "Overview and Prism region navigation.");

    [ObservableProperty]
    private string _currentSectionTitle = "Dashboard";

    [ObservableProperty]
    private string _currentSectionDescription = "Overview and Prism region navigation.";

    [ObservableProperty]
    private string _statusMessage = "";

    public MainViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
        SelectedNavigationItem = NavigationItems[0];
    }

    public void OnShellLoaded()
    {
        _navigationReady = true;
        NavigateToSection(SelectedNavigationItem);
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem value)
    {
        NavigateToSection(value);
    }

    private void NavigateToSection(NavigationItem value)
    {
        CurrentSectionTitle = value.Title;
        CurrentSectionDescription = value.Description;
        StatusMessage = $"RequestNavigate → {value.Key}";

        if (!_navigationReady)
            return;

        _regionManager.RequestNavigate(RegionNames.Content, value.Key);
    }
}
