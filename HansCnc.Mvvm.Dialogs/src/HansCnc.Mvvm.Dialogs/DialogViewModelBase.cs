using CommunityToolkit.Mvvm.ComponentModel;
using R3;

namespace HansCnc.Mvvm;

/// <summary>
/// Default dialog view model base class built on CommunityToolkit.Mvvm.
/// </summary>
/// <typeparam name="TInput">The input type.</typeparam>
/// <typeparam name="TResult">The result payload type.</typeparam>
public abstract partial class DialogViewModelBase<TInput, TResult> : ObservableObject, IDialogViewModel<TInput, TResult>
{
    private TInput _input = default!;
    private RxDialogResult<TResult> _resultContext;

    /// <inheritdoc />
    public event EventHandler? RequestClosed;

    /// <inheritdoc />
    public virtual TInput Input
    {
        get => _input;
        set => SetProperty(ref _input, value);
    }

    /// <inheritdoc />
    public RxDialogResult<TResult> ResultContext
    {
        get => _resultContext;
        private set => SetProperty(ref _resultContext, value);
    }

    /// <inheritdoc />
    public virtual void Initialize(TInput input)
    {
        Input = input;
    }

    /// <inheritdoc />
    public virtual void RequestClose()
    {
        RequestClosed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public virtual void OnDialogInitialized()
    {
    }

    /// <inheritdoc />
    public virtual void OnDialogLoaded()
    {
    }

    /// <inheritdoc />
    public virtual void OnDialogClosing()
    {
    }

    /// <inheritdoc />
    public virtual void OnDialogClosed()
    {
    }

    /// <summary>
    /// Updates the dialog result without closing the dialog.
    /// </summary>
    protected void SetDialogResult(bool result, TResult resultValue)
    {
        ResultContext = new RxDialogResult<TResult>(result, resultValue);
    }

    /// <summary>
    /// Sets a successful result and requests the window to close.
    /// </summary>
    protected void CloseWithResult(TResult resultValue)
    {
        SetDialogResult(true, resultValue);
        RequestClose();
    }

    /// <summary>
    /// Sets a canceled result and requests the window to close.
    /// </summary>
    protected void Cancel(TResult resultValue = default!)
    {
        SetDialogResult(false, resultValue);
        RequestClose();
    }
}

/// <summary>
/// Convenience base class for dialogs with no input and no payload result.
/// </summary>
public abstract partial class DialogViewModelBase : DialogViewModelBase<Unit, Unit>
{
}
