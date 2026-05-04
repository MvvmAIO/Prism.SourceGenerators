using System;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

public partial class CommandsViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Commands sample";

    [ObservableProperty]
    private int _counter;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _statusMessage = "";

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
        Title = "Data loaded! (Prism 8.1.97 MvvmAIO.Prism.Bcl.Commands)";
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
        StatusMessage = "Fetch complete! (parallel execution enabled, MvvmAIO.Prism.Bcl.Commands)";
    }

    [AsyncDelegateCommand(
        CanExecute = nameof(CanSave),
        Catch = nameof(HandleSaveError))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync()
    {
        StatusMessage = "Saving...";
        await Task.Delay(800);
        StatusMessage = $"Saved! Counter={Counter}, IsActive={IsActive} (MvvmAIO.Prism.Bcl.Commands)";
    }

    private bool CanSave() => Counter > 0 && IsActive;

    private void HandleSaveError(Exception ex)
    {
        StatusMessage = $"Save failed: {ex.Message}";
    }
}
