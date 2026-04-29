using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

/// <summary>
/// Prism 9.0 sample ViewModel.
/// Demonstrates [DelegateCommand], [AsyncDelegateCommand], [ObservesProperty],
/// [NotifyPropertyChangedFor], OnChanged partial methods,
/// and C# 14 partial property + field keyword support.
/// AsyncDelegateCommand is provided natively by Prism.Core 9.0.537.
/// </summary>
public partial class MainViewModel : BindableBase
{
    public ObservableCollection<NavigationItem> NavigationItems { get; } =
    [
        new("dashboard", "Dashboard", "Overview and quick status of the sample."),
        new("commands", "Commands", "Exercise DelegateCommand and AsyncDelegateCommand generation."),
        new("profile", "Profile", "Demo area for observable properties and dependent notifications.")
    ];

    [ObservableProperty]
    public partial NavigationItem SelectedNavigationItem { get; set; } = new("dashboard", "Dashboard", "Overview and quick status of the sample.");

    [ObservableProperty]
    public partial string CurrentSectionTitle { get; set; } = "Dashboard";

    [ObservableProperty]
    public partial string CurrentSectionDescription { get; set; } = "Overview and quick status of the sample.";

    // --- [ObservableProperty] on partial properties (C# 14, field keyword) ---

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    public partial string FirstName { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FullName))]
    public partial string LastName { get; set; } = "";

    public string FullName => $"{FirstName} {LastName}";

    [ObservableProperty]
    public partial string Title { get; set; } = "Hello Prism 9.0 Source Generators!";

    [ObservableProperty]
    public partial int Counter { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    public MainViewModel()
    {
        SelectedNavigationItem = NavigationItems[0];
    }

    partial void OnSelectedNavigationItemChanged(NavigationItem value)
    {
        CurrentSectionTitle = value.Title;
        CurrentSectionDescription = value.Description;
        StatusMessage = $"Switched to {value.Title}.";
    }

    // OnChanged partial methods are auto-generated.
    // Implement them to react to property changes:
    partial void OnCounterChanged(int value)
    {
        StatusMessage = $"Counter changed to {value}";
    }

    // --- [DelegateCommand] examples ---
    // With LangVersion >= 14, command properties are generated using the 'field' keyword:
    //   public DelegateCommand IncrementCommand => field ??= new DelegateCommand(Increment);

    [DelegateCommand]
    private void Increment()
    {
        Counter++;
    }

    [DelegateCommand]
    private void Reset()
    {
        Counter = 0;
    }

    // --- [DelegateCommand] with auto async detection ---

    [DelegateCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(500);
        Title = "Data loaded! (Prism 9.0 native AsyncDelegateCommand)";
    }

    // --- [DelegateCommand] with CanExecute + ObservesProperty ---

    [DelegateCommand(CanExecute = nameof(CanToggle))]
    [ObservesProperty(nameof(Counter))]
    private void Toggle()
    {
        IsActive = !IsActive;
    }

    private bool CanToggle() => Counter > 0;

    // --- [AsyncDelegateCommand] with advanced features ---

    [AsyncDelegateCommand(EnableParallelExecution = true)]
    private async Task FetchDataAsync()
    {
        StatusMessage = "Fetching...";
        await Task.Delay(1000);
        StatusMessage = "Fetch complete! (parallel execution enabled)";
    }

    [AsyncDelegateCommand(
        CanExecute = nameof(CanSave),
        Catch = nameof(HandleSaveError))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync()
    {
        StatusMessage = "Saving...";
        await Task.Delay(800);
        StatusMessage = $"Saved! Counter={Counter}, IsActive={IsActive}";
    }

    private bool CanSave() => Counter > 0 && IsActive;

    private void HandleSaveError(Exception ex)
    {
        StatusMessage = $"Save failed: {ex.Message}";
    }
}

public sealed record NavigationItem(string Key, string Title, string Description);
