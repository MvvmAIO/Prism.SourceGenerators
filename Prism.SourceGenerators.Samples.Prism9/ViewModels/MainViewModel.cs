using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

/// <summary>
/// Prism 9.0 sample ViewModel.
/// AsyncDelegateCommand is provided natively by Prism.Core 9.0.537.
/// </summary>
public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello Prism 9.0 Source Generators!";

    [ObservableProperty]
    private int _counter;

    [ObservableProperty]
    private bool _isActive;

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

    /// <summary>
    /// Async command - uses native Prism.Commands.AsyncDelegateCommand (Prism 9.0+).
    /// </summary>
    [DelegateCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(500);
        Title = "Data loaded! (Prism 9.0 native AsyncDelegateCommand)";
    }

    [DelegateCommand(CanExecute = nameof(CanToggle))]
    private void Toggle()
    {
        IsActive = !IsActive;
    }

    private bool CanToggle() => Counter > 0;
}
