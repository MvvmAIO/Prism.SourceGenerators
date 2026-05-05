using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.SourceGenerators.Samples.Prism8.Services;
using Prism.SourceGenerators.Samples.Prism8.ViewModels;
using Prism.SourceGenerators.Samples.Prism8.Views;

namespace Prism.SourceGenerators.Samples.Prism8;

public partial class App : PrismApplication
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override AvaloniaObject CreateShell()
    {
        var shell = Container.Resolve<MainWindow>();
        shell.DataContext = Container.Resolve<MainViewModel>();
        return shell;
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<MainViewModel>();
        containerRegistry.RegisterSingleton<ISettingsService, SettingsService>();

        containerRegistry.RegisterForNavigation<DashboardView, DashboardViewModel>("Dashboard");
        containerRegistry.RegisterForNavigation<CommandsView, CommandsViewModel>("Commands");
        containerRegistry.RegisterForNavigation<ProfileView, ProfileViewModel>("Profile");
    }
}
