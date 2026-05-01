using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Prism.Common;

namespace Prism.Commands;

/// <summary>
/// Prism 9–aligned async delegate command for Prism.Core 8.1.97 (MIT reference implementation).
/// </summary>
public class AsyncDelegateCommand : DelegateCommandBase, IAsyncCommand
{
    private bool _enableParallelExecution;
    private bool _isExecuting;
    private readonly Func<CancellationToken, Task> _executeMethod;
    private Func<bool> _canExecuteMethod;
    private Func<CancellationToken> _getCancellationToken = () => CancellationToken.None;
    private readonly MulticastExceptionHandler _exceptionHandler = new MulticastExceptionHandler();

    private const string DelegateNullMessage = "Delegate commands cannot contain null method references.";

    /// <summary>
    /// Creates a command that invokes <paramref name="executeMethod"/>.
    /// </summary>
    public AsyncDelegateCommand(Func<Task> executeMethod)
#if NET6_0_OR_GREATER
        : this(c => executeMethod().WaitAsync(c), () => true)
#else
        : this(c => executeMethod(), () => true)
#endif
    {
    }

    /// <summary>
    /// Creates a command that invokes <paramref name="executeMethod"/> with a cancellation token.
    /// </summary>
    public AsyncDelegateCommand(Func<CancellationToken, Task> executeMethod)
        : this(executeMethod, () => true)
    {
    }

    /// <summary>
    /// Creates a command with execute and can-execute delegates.
    /// </summary>
    public AsyncDelegateCommand(Func<Task> executeMethod, Func<bool> canExecuteMethod)
#if NET6_0_OR_GREATER
        : this(c => executeMethod().WaitAsync(c), canExecuteMethod)
#else
        : this(c => executeMethod(), canExecuteMethod)
#endif
    {
    }

    /// <summary>
    /// Creates a command with execute (token) and can-execute delegates.
    /// </summary>
    public AsyncDelegateCommand(Func<CancellationToken, Task> executeMethod, Func<bool> canExecuteMethod)
    {
        if (executeMethod is null || canExecuteMethod is null)
            throw new ArgumentNullException(nameof(executeMethod), DelegateNullMessage);

        _executeMethod = executeMethod;
        _canExecuteMethod = canExecuteMethod;
    }

    /// <summary>
    /// Gets whether an execution is in progress.
    /// </summary>
    public bool IsExecuting
    {
        get => _isExecuting;
        private set
        {
            if (_isExecuting == value)
                return;
            _isExecuting = value;
            OnCanExecuteChanged();
        }
    }

    /// <summary>
    /// Executes the command.
    /// </summary>
    public async Task Execute(CancellationToken? cancellationToken = null)
    {
        CancellationToken token = cancellationToken ?? _getCancellationToken();
        try
        {
            if (!_enableParallelExecution && IsExecuting)
                return;

            IsExecuting = true;
            await _executeMethod(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, null);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Determines if the command can be executed.
    /// </summary>
    public bool CanExecute()
    {
        try
        {
            if (!_enableParallelExecution && IsExecuting)
                return false;

            return _canExecuteMethod?.Invoke() ?? true;
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, null);

            return false;
        }
    }

    /// <inheritdoc/>
    protected override async void Execute(object? parameter)
    {
        CancellationToken cancellationToken = _getCancellationToken();
        await Execute(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override bool CanExecute(object? parameter) => CanExecute();

    /// <summary>
    /// Enables parallel execution of async work.
    /// </summary>
    public AsyncDelegateCommand EnableParallelExecution()
    {
        _enableParallelExecution = true;
        return this;
    }

    /// <summary>
    /// Uses a timed cancellation token when the command runs.
    /// </summary>
    public AsyncDelegateCommand CancelAfter(TimeSpan timeout) =>
        CancellationTokenSourceFactory(() => new CancellationTokenSource(timeout).Token);

    /// <summary>
    /// Sets the factory used to obtain the cancellation token when the command runs.
    /// </summary>
    public AsyncDelegateCommand CancellationTokenSourceFactory(Func<CancellationToken> factory)
    {
        _getCancellationToken = factory;
        return this;
    }

    /// <summary>
    /// Observes a property for CanExecute refresh notifications.
    /// </summary>
    public AsyncDelegateCommand ObservesProperty<T>(Expression<Func<T>> propertyExpression)
    {
        ObservesPropertyInternal(propertyExpression);
        return this;
    }

    /// <summary>
    /// Observes an expression used for can-execute evaluation.
    /// </summary>
    public AsyncDelegateCommand ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        _canExecuteMethod = canExecuteExpression.Compile();
        ObservesPropertyInternal(canExecuteExpression);
        return this;
    }

    /// <summary>
    /// Registers a strongly typed exception handler for execute/can-execute failures.
    /// </summary>
    public AsyncDelegateCommand Catch<TException>(Action<TException> @catch)
        where TException : Exception
    {
        _exceptionHandler.Register<TException>(@catch);
        return this;
    }

    /// <summary>
    /// Registers a general exception handler for execute/can-execute failures.
    /// </summary>
    public AsyncDelegateCommand Catch(Action<Exception> @catch)
    {
        _exceptionHandler.Register<Exception>(@catch);
        return this;
    }

    Task IAsyncCommand.ExecuteAsync(object? parameter) =>
        Execute(_getCancellationToken());

    Task IAsyncCommand.ExecuteAsync(object? parameter, CancellationToken cancellationToken) =>
        Execute(cancellationToken);
}
