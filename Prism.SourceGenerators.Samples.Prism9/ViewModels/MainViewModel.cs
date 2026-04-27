using System;
using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

/// <summary>
/// Prism 9.0 sample ViewModel.
/// Demonstrates [DelegateCommand], [AsyncDelegateCommand], [ObservesProperty],
/// and C# 14 partial property + field keyword support.
/// AsyncDelegateCommand is provided natively by Prism.Core 9.0.537.
/// </summary>
public partial class MainViewModel : BindableBase
{
    // --- [ObservableProperty] on partial properties (C# 14, field keyword) ---

    [ObservableProperty]
    public partial string Title { get; set; } = "Hello Prism 9.0 Source Generators!";

    [ObservableProperty]
    public partial int Counter { get; set; }

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "";

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
