namespace HansCnc.Mvvm;

/// <summary>
/// Exposes the result returned by a dialog.
/// </summary>
/// <typeparam name="TResultValue">The payload type returned by the dialog.</typeparam>
public interface IRxDialogResult<out TResultValue>
{
    /// <summary>
    /// Gets a value indicating whether the dialog completed successfully.
    /// </summary>
    bool Result { get; }

    /// <summary>
    /// Gets the dialog result payload.
    /// </summary>
    TResultValue ResultValue { get; }
}

/// <summary>
/// Default immutable dialog result.
/// </summary>
/// <typeparam name="TResultValue">The payload type returned by the dialog.</typeparam>
/// <param name="Result">Whether the dialog completed successfully.</param>
/// <param name="ResultValue">The dialog result payload.</param>
public readonly record struct RxDialogResult<TResultValue>(bool Result, TResultValue ResultValue)
    : IRxDialogResult<TResultValue>
{
    /// <summary>
    /// Creates a successful dialog result.
    /// </summary>
    public static RxDialogResult<TResultValue> Ok(TResultValue value) => new(true, value);

    /// <summary>
    /// Creates a canceled dialog result.
    /// </summary>
    public static RxDialogResult<TResultValue> Cancel(TResultValue value = default!) => new(false, value);
}
