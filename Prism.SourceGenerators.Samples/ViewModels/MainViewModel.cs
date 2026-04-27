using System.Threading.Tasks;
using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.ViewModels;

public partial class MainViewModel : BindableBase
{
    [ObservableProperty]
    private string _title = "Hello Prism Source Generators!";

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

    [DelegateCommand]
    private async Task LoadDataAsync()
    {
        await Task.Delay(500);
        Title = "Data loaded!";
    }

    [DelegateCommand(CanExecute = nameof(CanToggle))]
    private void Toggle()
    {
        IsActive = !IsActive;
    }

    private bool CanToggle() => Counter > 0;
}
