namespace Prism.SourceGenerators;

/// <summary>
/// An attribute that can be applied to an async method to generate an <c>AsyncDelegateCommand</c> property.
/// Supports Prism 9.0+ fluent configuration methods.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class AsyncDelegateCommandAttribute : global::System.Attribute
{
    /// <summary>
    /// Gets or sets the name of the generated command property.
    /// If not specified, the name is derived from the method name
    /// (e.g., <c>Submit</c> becomes <c>SubmitCommand</c>).
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Gets or sets the name of the <c>CanExecute</c> method or property.
    /// </summary>
    public string? CanExecute { get; set; }

    /// <summary>
    /// Gets or sets the timeout in microseconds for <c>AsyncDelegateCommand.CancelAfter(TimeSpan)</c>.
    /// </summary>
    public double CancelAfterMicroseconds { get; set; }

    /// <summary>
    /// Gets or sets the name of the error handler for <c>AsyncDelegateCommand.Catch</c>.
    /// Can be a method (with Exception parameter), field, or property (Action&lt;Exception&gt;).
    /// </summary>
    public string? Catch { get; set; }

    /// <summary>
    /// Gets or sets the name of the CancellationToken factory for
    /// <c>AsyncDelegateCommand.CancellationTokenSourceFactory</c>.
    /// </summary>
    public string? CancellationTokenSourceFactory { get; set; }

    /// <summary>
    /// Gets or sets whether to enable parallel execution via
    /// <c>AsyncDelegateCommand.EnableParallelExecution()</c>.
    /// </summary>
    public bool EnableParallelExecution { get; set; }
}