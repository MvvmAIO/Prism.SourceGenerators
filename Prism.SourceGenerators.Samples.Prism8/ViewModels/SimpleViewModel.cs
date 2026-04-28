using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

/// <summary>
/// Demonstrates [BindableBase] attribute usage.
/// This class does NOT inherit from Prism.Mvvm.BindableBase.
/// The source generator will automatically implement INotifyPropertyChanged,
/// SetProperty, RaisePropertyChanged, and OnPropertyChanged.
/// </summary>
[BindableBase]
public partial class SimpleViewModel
{
    private string _message = "Hello from [BindableBase]!";

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private int _count;

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, value);
    }
}
