using System.Windows;

namespace HansCnc.Mvvm;

/// <summary>
/// Base <see cref="Window"/> implementation for dialog views that bind to a typed view model.
/// </summary>
/// <typeparam name="TViewModel">The view model type.</typeparam>
public abstract class DialogWindowFor<TViewModel> : Window, IViewFor<TViewModel>
    where TViewModel : class
{
    /// <inheritdoc />
    public TViewModel? ViewModel
    {
        get => DataContext as TViewModel;
        set => DataContext = value;
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value is null ? null : (TViewModel)value;
    }
}
