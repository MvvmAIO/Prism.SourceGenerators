using System;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

/// <summary>
/// Prism 8.0 sample ViewModel.
/// Demonstrates [DelegateCommand], [AsyncDelegateCommand], and [ObservesProperty].
/// AsyncDelegateCommand is NOT available in Prism.Core 8.1.97,
/// so the source generator will automatically generate a polyfill.
/// </summary>
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello Prism 8.0 Source Generators!";

    [ObservableProperty]
    private int _counter;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _statusMessage = "";

    // --- [DelegateCommand] examples ---
    // With LangVersion < 14, command properties use a traditional backing field:
    //   private DelegateCommand? _incrementCommand;
    //   public DelegateCommand IncrementCommand => _incrementCommand ??= new DelegateCommand(Increment);

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
        Title = "Data loaded! (Prism 8.0 polyfill AsyncDelegateCommand)";
    }

    // --- [DelegateCommand] with CanExecute + ObservesProperty ---

    [DelegateCommand(CanExecute = nameof(CanToggle))]
    [ObservesProperty(nameof(Counter))]
    private void Toggle()
    {
        IsActive = !IsActive;
    }

    private bool CanToggle() => Counter > 0;

    // --- [AsyncDelegateCommand] with advanced features (polyfill) ---

    [AsyncDelegateCommand(EnableParallelExecution = true)]
    private async Task FetchDataAsync()
    {
        StatusMessage = "Fetching...";
        await Task.Delay(1000);
        StatusMessage = "Fetch complete! (parallel execution enabled, polyfill)";
    }

    [AsyncDelegateCommand(
        CanExecute = nameof(CanSave),
        Catch = nameof(HandleSaveError))]
    [ObservesProperty(nameof(Counter), nameof(IsActive))]
    private async Task SaveAsync()
    {
        StatusMessage = "Saving...";
        await Task.Delay(800);
        StatusMessage = $"Saved! Counter={Counter}, IsActive={IsActive} (polyfill)";
    }

    private bool CanSave() => Counter > 0 && IsActive;

    private void HandleSaveError(Exception ex)
    {
        StatusMessage = $"Save failed: {ex.Message}";
    }
}
