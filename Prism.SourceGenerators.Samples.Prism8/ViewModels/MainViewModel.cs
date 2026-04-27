using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

/// <summary>
/// Prism 8.0 sample ViewModel.
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
    /// Async command - uses auto-generated polyfill AsyncDelegateCommand (Prism &lt; 9.0).
    /// </summary>
    [DelegateCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(500);
        Title = "Data loaded! (Prism 8.0 polyfill AsyncDelegateCommand)";
    }

    [DelegateCommand(CanExecute = nameof(CanToggle))]
    private void Toggle()
    {
        IsActive = !IsActive;
    }

    private bool CanToggle() => Counter > 0;
}
