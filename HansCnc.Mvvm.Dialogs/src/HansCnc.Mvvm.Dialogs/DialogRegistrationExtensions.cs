using System.Windows;
using Autofac;
using Autofac.Builder;

namespace HansCnc.Mvvm;

/// <summary>
/// Autofac registration helpers for dialog services and views.
/// </summary>
public static class DialogRegistrationExtensions
{
    /// <summary>
    /// Registers the default <see cref="IDialogService"/> implementation.
    /// </summary>
    public static IRegistrationBuilder<DialogService, ConcreteReflectionActivatorData, SingleRegistrationStyle>
        RegisterDialogService(this ContainerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RegisterType<DialogService>()
            .As<IDialogService>()
            .SingleInstance();
    }

    /// <summary>
    /// Registers a WPF window as the view for a dialog view model.
    /// </summary>
    /// <typeparam name="TView">The dialog window type.</typeparam>
    /// <typeparam name="TViewModel">The dialog view model type.</typeparam>
    public static IRegistrationBuilder<TView, ConcreteReflectionActivatorData, SingleRegistrationStyle>
        RegisterDialogView<TView, TViewModel>(this ContainerBuilder builder)
        where TView : Window, IViewFor<TViewModel>
        where TViewModel : class, IDialogViewModel
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RegisterType<TView>()
            .As<IViewFor<TViewModel>>();
    }
}
