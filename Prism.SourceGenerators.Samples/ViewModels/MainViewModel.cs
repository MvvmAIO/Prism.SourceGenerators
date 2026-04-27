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
}
