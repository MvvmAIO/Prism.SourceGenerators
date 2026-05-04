using System.Collections.ObjectModel;
using Prism.Mvvm;
using Prism.Navigation.Regions;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism9.Models;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

/// <summary>
/// Shell ViewModel: sidebar + Prism region navigation.
/// Child views (<see cref="DashboardViewModel"/>, <see cref="CommandsViewModel"/>, <see cref="ProfileViewModel"/>) load into the content region.
/// </summary>
public partial class MainViewModel : BindableBase
{
    private readonly IRegionManager _regionManager;
    private bool _navigationReady;

    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new("Dashboard", "Dashboard", "Overview and Prism region navigation."),
        new("Commands", "Commands", "DelegateCommand and AsyncDelegateCommand generation."),
        new("Profile", "Profile", "ObservableProperty on partial properties (C# 14).")
    ];

    [ObservableProperty]
    public partial NavigationItem SelectedNavigationItem { get; set; } =
        new("Dashboard", "Dashboard", "Overview and Prism region navigation.");

    [ObservableProperty]
    public partial string CurrentSectionTitle { get; set; } = "Dashboard";

    [ObservableProperty]
    public partial string CurrentSectionDescription { get; set; } =
        "Overview and Prism region navigation.";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    public MainViewModel(IRegionManager regionManager)
    {
        _regionManager = regionManager;
        SelectedNavigationItem = NavigationItems[0];
    }

    /// <summary>Invoked from <see cref="Views.MainWindow"/> after the region host is in the visual tree.</summary>
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
