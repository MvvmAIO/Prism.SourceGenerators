namespace HansCnc.Mvvm;

/// <summary>
/// Represents a view that can expose its view model without knowing the view model type.
/// </summary>
public interface IViewFor
{
    /// <summary>
    /// Gets or sets the view model bound to the view.
    /// </summary>
    object? ViewModel { get; set; }
}

/// <summary>
/// Represents a view that is bound to a specific view model type.
/// </summary>
/// <typeparam name="TViewModel">The view model type.</typeparam>
public interface IViewFor<TViewModel> : IViewFor
    where TViewModel : class
{
    /// <summary>
    /// Gets or sets the strongly typed view model bound to the view.
    /// </summary>
    new TViewModel? ViewModel { get; set; }
}
