using System.Windows;
using System.Windows.Threading;
using Autofac;
using R3;
using Serilog;

namespace HansCnc.Mvvm;

/// <summary>
/// Autofac-backed WPF dialog service.
/// </summary>
public sealed class DialogService : IDialogService
{
    private readonly IComponentContext _context;
    private readonly ILogger _logger;
    private readonly DialogServiceOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    public DialogService(IComponentContext context, ILogger logger)
        : this(context, logger, DialogServiceOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DialogService"/> class.
    /// </summary>
    public DialogService(IComponentContext context, ILogger logger, DialogServiceOptions options)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger?.ForContext<DialogService>() ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool ShowDialog<TViewModel>()
        where TViewModel : class, IDialogViewModel<Unit, Unit>
    {
        return ShowDialog<TViewModel, Unit, Unit>(Unit.Default).Result;
    }

    /// <inheritdoc />
    public bool ShowDialog<TViewModel, TInputValue>(TInputValue input)
        where TViewModel : class, IDialogViewModel<TInputValue, Unit>
    {
        return ShowDialog<TViewModel, TInputValue, Unit>(input).Result;
    }

    /// <inheritdoc />
    public RxDialogResult<TResultValue> ShowDialog<TViewModel, TResultValue>()
        where TViewModel : class, IDialogViewModel<Unit, TResultValue>
    {
        return ShowDialog<TViewModel, Unit, TResultValue>(Unit.Default);
    }

    /// <inheritdoc />
    public RxDialogResult<TResultValue> ShowDialog<TViewModel, TInputValue, TResultValue>(TInputValue input)
        where TViewModel : class, IDialogViewModel<TInputValue, TResultValue>
    {
        return InvokeOnUiThread(() =>
        {
            TViewModel viewModel = CreateViewModel<TViewModel, TInputValue, TResultValue>(input);
            Window window = CreateWindow(viewModel);
            EventHandler requestCloseHandler = (_, _) => CloseWindow(window, viewModel, isModal: true);

            try
            {
                viewModel.OnDialogInitialized();
                using DialogWindowSubscription subscription = WireWindow(window, viewModel, requestCloseHandler);

                ApplyOwner(window);
                _logger.Debug("Showing modal dialog {ViewModelType}", typeof(TViewModel).FullName);
                return MergeDialogResult(viewModel.ResultContext, window.ShowDialog());
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to show modal dialog {ViewModelType}", typeof(TViewModel).FullName);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public void Show<TViewModel>(Action<bool>? callback = null)
        where TViewModel : class, IDialogViewModel<Unit, Unit>
    {
        Show<TViewModel, Unit, Unit>(Unit.Default, result => callback?.Invoke(result.Result));
    }

    /// <inheritdoc />
    public void Show<TViewModel, TInputValue>(TInputValue input, Action<bool>? callback = null)
        where TViewModel : class, IDialogViewModel<TInputValue, Unit>
    {
        Show<TViewModel, TInputValue, Unit>(input, result => callback?.Invoke(result.Result));
    }

    /// <inheritdoc />
    public void Show<TViewModel, TResultValue>(Action<RxDialogResult<TResultValue>>? callback = null)
        where TViewModel : class, IDialogViewModel<Unit, TResultValue>
    {
        Show<TViewModel, Unit, TResultValue>(Unit.Default, callback);
    }

    /// <inheritdoc />
    public void Show<TViewModel, TInputValue, TResultValue>(
        TInputValue input,
        Action<RxDialogResult<TResultValue>>? callback = null)
        where TViewModel : class, IDialogViewModel<TInputValue, TResultValue>
    {
        InvokeOnUiThread(() =>
        {
            TViewModel viewModel = CreateViewModel<TViewModel, TInputValue, TResultValue>(input);
            Window window = CreateWindow(viewModel);
            EventHandler requestCloseHandler = (_, _) => CloseWindow(window, viewModel, isModal: false);
            DialogWindowSubscription? subscription = null;

            void ClosedCallback(object? sender, EventArgs args)
            {
                try
                {
                    callback?.Invoke(MergeDialogResult(viewModel.ResultContext, TryGetWindowDialogResult(window)));
                }
                finally
                {
                    window.Closed -= ClosedCallback;
                    subscription?.Dispose();
                }
            }

            try
            {
                viewModel.OnDialogInitialized();
                subscription = WireWindow(window, viewModel, requestCloseHandler);
                window.Closed += ClosedCallback;
                ApplyOwner(window);
                _logger.Debug("Showing modeless dialog {ViewModelType}", typeof(TViewModel).FullName);
                window.Show();
            }
            catch (Exception ex)
            {
                window.Closed -= ClosedCallback;
                subscription?.Dispose();
                _logger.Error(ex, "Failed to show modeless dialog {ViewModelType}", typeof(TViewModel).FullName);
                throw;
            }

            return true;
        });
    }

    private TViewModel CreateViewModel<TViewModel, TInputValue, TResultValue>(TInputValue input)
        where TViewModel : class, IDialogViewModel<TInputValue, TResultValue>
    {
        TViewModel viewModel = _context.Resolve<TViewModel>();
        viewModel.Initialize(input);
        return viewModel;
    }

    private Window CreateWindow<TViewModel>(TViewModel viewModel)
        where TViewModel : class, IDialogViewModel
    {
        IViewFor<TViewModel> view = _context.Resolve<IViewFor<TViewModel>>();
        view.ViewModel = viewModel;

        if (view is not Window window)
        {
            throw new InvalidOperationException(
                $"The view registered for {typeof(TViewModel).FullName} must inherit from {typeof(Window).FullName}.");
        }

        return window;
    }

    private static DialogWindowSubscription WireWindow(
        Window window,
        IDialogViewModel viewModel,
        EventHandler requestCloseHandler)
    {
        return new DialogWindowSubscription(window, viewModel, requestCloseHandler);
    }

    private void ApplyOwner(Window window)
    {
        if (!_options.AssignOwner || window.Owner is not null)
        {
            return;
        }

        Window? owner = _options.OwnerProvider?.Invoke() ?? GetActiveWindow();
        if (owner is not null && !ReferenceEquals(owner, window))
        {
            window.Owner = owner;
        }
    }

    private static Window? GetActiveWindow()
    {
        return Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(window => window.IsActive);
    }

    private static void CloseWindow<TInputValue, TResultValue>(
        Window window,
        IDialogViewModel<TInputValue, TResultValue> viewModel,
        bool isModal)
    {
        CloseWindowCore(window, viewModel.ResultContext.Result, isModal);
    }

    private static void CloseWindowCore(Window window, bool result, bool isModal)
    {
        if (!window.Dispatcher.CheckAccess())
        {
            window.Dispatcher.Invoke(() => CloseWindowCore(window, result, isModal));
            return;
        }

        if (isModal && window.IsVisible)
        {
            try
            {
                window.DialogResult = result;
                return;
            }
            catch (InvalidOperationException)
            {
                // Fall through to Close when the window is not currently shown as a dialog.
            }
        }

        window.Close();
    }

    private static RxDialogResult<TResultValue> MergeDialogResult<TResultValue>(
        RxDialogResult<TResultValue> viewModelResult,
        bool? windowResult)
    {
        if (viewModelResult.Equals(default(RxDialogResult<TResultValue>)) && windowResult.HasValue)
        {
            return new RxDialogResult<TResultValue>(windowResult.Value, default!);
        }

        return viewModelResult;
    }

    private static bool? TryGetWindowDialogResult(Window window)
    {
        try
        {
            return window.DialogResult;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static T InvokeOnUiThread<T>(Func<T> action)
    {
        Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        return dispatcher.CheckAccess() ? action() : dispatcher.Invoke(action);
    }

    private sealed class DialogWindowSubscription : IDisposable
    {
        private readonly Window _window;
        private readonly IDialogViewModel _viewModel;
        private readonly EventHandler _requestCloseHandler;
        private readonly RoutedEventHandler _loadedHandler;
        private readonly System.ComponentModel.CancelEventHandler _closingHandler;
        private readonly EventHandler _closedHandler;
        private bool _disposed;

        public DialogWindowSubscription(Window window, IDialogViewModel viewModel, EventHandler requestCloseHandler)
        {
            _window = window;
            _viewModel = viewModel;
            _requestCloseHandler = requestCloseHandler;
            _loadedHandler = (_, _) => _viewModel.OnDialogLoaded();
            _closingHandler = (_, _) => _viewModel.OnDialogClosing();
            _closedHandler = (_, _) => _viewModel.OnDialogClosed();

            _viewModel.RequestClosed += _requestCloseHandler;
            _window.Loaded += _loadedHandler;
            _window.Closing += _closingHandler;
            _window.Closed += _closedHandler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _viewModel.RequestClosed -= _requestCloseHandler;
            _window.Loaded -= _loadedHandler;
            _window.Closing -= _closingHandler;
            _window.Closed -= _closedHandler;
            _disposed = true;
        }
    }
}
