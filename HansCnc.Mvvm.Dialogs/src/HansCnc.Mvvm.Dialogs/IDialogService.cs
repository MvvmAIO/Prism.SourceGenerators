using R3;

namespace HansCnc.Mvvm;

/// <summary>
/// Shows modal and modeless WPF dialogs by resolving view models and views from Autofac.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a modal dialog with no input and no payload result.
    /// </summary>
    bool ShowDialog<TViewModel>()
        where TViewModel : class, IDialogViewModel<Unit, Unit>;

    /// <summary>
    /// Shows a modal dialog with input and no payload result.
    /// </summary>
    bool ShowDialog<TViewModel, TInputValue>(TInputValue input)
        where TViewModel : class, IDialogViewModel<TInputValue, Unit>;

    /// <summary>
    /// Shows a modal dialog with no input and a typed payload result.
    /// </summary>
    RxDialogResult<TResultValue> ShowDialog<TViewModel, TResultValue>()
        where TViewModel : class, IDialogViewModel<Unit, TResultValue>;

    /// <summary>
    /// Shows a modal dialog with input and a typed payload result.
    /// </summary>
    RxDialogResult<TResultValue> ShowDialog<TViewModel, TInputValue, TResultValue>(TInputValue input)
        where TViewModel : class, IDialogViewModel<TInputValue, TResultValue>;

    /// <summary>
    /// Shows a modeless dialog with no input and no payload result.
    /// </summary>
    void Show<TViewModel>(Action<bool>? callback = null)
        where TViewModel : class, IDialogViewModel<Unit, Unit>;

    /// <summary>
    /// Shows a modeless dialog with input and no payload result.
    /// </summary>
    void Show<TViewModel, TInputValue>(TInputValue input, Action<bool>? callback = null)
        where TViewModel : class, IDialogViewModel<TInputValue, Unit>;

    /// <summary>
    /// Shows a modeless dialog with no input and a typed payload result.
    /// </summary>
    void Show<TViewModel, TResultValue>(Action<RxDialogResult<TResultValue>>? callback = null)
        where TViewModel : class, IDialogViewModel<Unit, TResultValue>;

    /// <summary>
    /// Shows a modeless dialog with input and a typed payload result.
    /// </summary>
    void Show<TViewModel, TInputValue, TResultValue>(
        TInputValue input,
        Action<RxDialogResult<TResultValue>>? callback = null)
        where TViewModel : class, IDialogViewModel<TInputValue, TResultValue>;
}
