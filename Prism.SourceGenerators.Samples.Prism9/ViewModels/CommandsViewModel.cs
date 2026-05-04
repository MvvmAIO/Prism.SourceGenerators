using System;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

/// <summary>Demonstrates generated commands in a dedicated navigable view.</summary>
public partial class CommandsViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Title { get; set; } = "Commands sample";

    [ObservableProperty]
    public partial int Counter { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

    partial void OnCounterChanged(int value)
    {
        StatusMessage = $"Counter changed to {value}";
    }

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

    [DelegateCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(500);
        Title = "Data loaded! (Prism 9.0 native AsyncDelegateCommand)";
    }

    [DelegateCommand(CanExecute = nameof(CanToggle))]
    [ObservesProperty(nameof(Counter))]
    private void Toggle()
    {
        IsActive = !IsActive;
    }

    private bool CanToggle() => Counter > 0;

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
