using System.ComponentModel;

namespace HansCnc.Mvvm;

/// <summary>
/// Base contract for dialog view models.
/// </summary>
public interface IDialogViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// Raised when the view model requests its dialog window to close.
    /// </summary>
    event EventHandler? RequestClosed;

    /// <summary>
    /// Requests the dialog window to close.
    /// </summary>
    void RequestClose();

    /// <summary>
    /// Called after the view and view model are created and connected.
    /// </summary>
    void OnDialogInitialized();

    /// <summary>
    /// Called when the dialog window is loaded.
    /// </summary>
    void OnDialogLoaded();

    /// <summary>
    /// Called while the dialog window is closing.
    /// </summary>
    void OnDialogClosing();

    /// <summary>
    /// Called after the dialog window is closed.
    /// </summary>
    void OnDialogClosed();
}

/// <summary>
/// Contract for dialog view models that accept input and return a typed result.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TResult">The result payload type.</typeparam>
public interface IDialogViewModel<TInput, TResult> : IDialogViewModel
{
    /// <summary>
    /// Gets or sets the input used to initialize the dialog.
    /// </summary>
    TInput Input { get; set; }

    /// <summary>
    /// Gets the current dialog result context.
    /// </summary>
    RxDialogResult<TResult> ResultContext { get; }

    /// <summary>
    /// Initializes the dialog view model with input.
    /// </summary>
    void Initialize(TInput input);
}
