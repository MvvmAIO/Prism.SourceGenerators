using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Prism.Common;

namespace Prism.Commands;

/// <summary>
/// Prism 9–aligned generic async delegate command for Prism.Core 8.1.97 (MIT reference implementation).
/// </summary>
/// <typeparam name="T">Command parameter type.</typeparam>
public class AsyncDelegateCommand<T> : DelegateCommandBase, IAsyncCommand
{
    private bool _enableParallelExecution;
    private bool _isExecuting;
    private readonly Func<T, CancellationToken, Task> _executeMethod;
    private Func<T, bool> _canExecuteMethod;
    private Func<CancellationToken> _getCancellationToken = () => CancellationToken.None;
    private readonly MulticastExceptionHandler _exceptionHandler = new MulticastExceptionHandler();

    private const string DelegateNullMessage = "Delegate commands cannot contain null method references.";

    /// <summary>
    /// Creates a command that invokes <paramref name="executeMethod"/>.
    /// </summary>
    public AsyncDelegateCommand(Func<T, Task> executeMethod)
#if NET6_0_OR_GREATER
        : this((p, t) => executeMethod(p).WaitAsync(t), _ => true)
#else
        : this((p, t) => executeMethod(p), _ => true)
#endif
    {
    }

    /// <summary>
    /// Creates a command that invokes <paramref name="executeMethod"/> with parameter and cancellation token.
    /// </summary>
    public AsyncDelegateCommand(Func<T, CancellationToken, Task> executeMethod)
        : this(executeMethod, _ => true)
    {
    }

    /// <summary>
    /// Creates a command with execute and can-execute delegates.
    /// </summary>
    public AsyncDelegateCommand(Func<T, Task> executeMethod, Func<T, bool> canExecuteMethod)
#if NET6_0_OR_GREATER
        : this((p, c) => executeMethod(p).WaitAsync(c), canExecuteMethod)
#else
        : this((p, c) => executeMethod(p), canExecuteMethod)
#endif
    {
    }

    /// <summary>
    /// Creates a command with execute (parameter + token) and can-execute delegates.
    /// </summary>
    public AsyncDelegateCommand(Func<T, CancellationToken, Task> executeMethod, Func<T, bool> canExecuteMethod)
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
    /// Executes the command with <paramref name="parameter"/>.
    /// </summary>
    public async Task Execute(T parameter, CancellationToken? cancellationToken = null)
    {
        CancellationToken token = cancellationToken ?? _getCancellationToken();

        try
        {
            if (!_enableParallelExecution && IsExecuting)
                return;

            IsExecuting = true;
            await _executeMethod(parameter, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, parameter);
        }
        finally
        {
            IsExecuting = false;
        }
    }

    /// <summary>
    /// Determines if the command can execute for <paramref name="parameter"/>.
    /// </summary>
    public bool CanExecute(T parameter)
    {
        try
        {
            if (!_enableParallelExecution && IsExecuting)
                return false;

            return _canExecuteMethod?.Invoke(parameter) ?? true;
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, parameter);

            return false;
        }
    }

    /// <inheritdoc/>
    protected override async void Execute(object? parameter)
    {
        CancellationToken cancellationToken = _getCancellationToken();
        T parameterAsT;
        try
        {
            parameterAsT = (T)parameter!;
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, parameter);
            return;
        }

        await Execute(parameterAsT, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override bool CanExecute(object? parameter)
    {
        try
        {
            return CanExecute((T)parameter!);
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, parameter);

            return false;
        }
    }

    /// <summary>
    /// Enables parallel execution of async work.
    /// </summary>
    public AsyncDelegateCommand<T> EnableParallelExecution()
    {
        _enableParallelExecution = true;
        return this;
    }

    /// <summary>
    /// Uses a timed cancellation token when the command runs.
    /// </summary>
    public AsyncDelegateCommand<T> CancelAfter(TimeSpan timeout) =>
        CancellationTokenSourceFactory(() => new CancellationTokenSource(timeout).Token);

    /// <summary>
    /// Sets the factory used to obtain the cancellation token when the command runs.
    /// </summary>
    public AsyncDelegateCommand<T> CancellationTokenSourceFactory(Func<CancellationToken> factory)
    {
        _getCancellationToken = factory;
        return this;
    }

    /// <summary>
    /// Observes a property for CanExecute refresh notifications.
    /// </summary>
    public AsyncDelegateCommand<T> ObservesProperty<TType>(Expression<Func<TType>> propertyExpression)
    {
        ObservesPropertyInternal(propertyExpression);
        return this;
    }

    /// <summary>
    /// Observes an expression used for can-execute evaluation.
    /// </summary>
    public AsyncDelegateCommand<T> ObservesCanExecute(Expression<Func<bool>> canExecuteExpression)
    {
        Expression<Func<T, bool>> expression = Expression.Lambda<Func<T, bool>>(canExecuteExpression.Body, Expression.Parameter(typeof(T), "o"));
        _canExecuteMethod = expression.Compile();
        ObservesPropertyInternal(canExecuteExpression);
        return this;
    }

    /// <summary>
    /// Registers a strongly typed exception handler for execute/can-execute failures.
    /// </summary>
    public AsyncDelegateCommand<T> Catch<TException>(Action<TException> @catch)
        where TException : Exception
    {
        _exceptionHandler.Register<TException>(@catch);
        return this;
    }

    /// <summary>
    /// Registers a general exception handler for execute/can-execute failures.
    /// </summary>
    public AsyncDelegateCommand<T> Catch(Action<Exception> @catch)
    {
        _exceptionHandler.Register<Exception>(@catch);
        return this;
    }

    async Task IAsyncCommand.ExecuteAsync(object? parameter)
    {
        try
        {
            await Execute((T)parameter!, _getCancellationToken()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, parameter);
        }
    }

    async Task IAsyncCommand.ExecuteAsync(object? parameter, CancellationToken cancellationToken)
    {
        try
        {
            await Execute((T)parameter!, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (!_exceptionHandler.CanHandle(ex))
                throw;

            _exceptionHandler.Handle(ex, parameter);
        }
    }
}
