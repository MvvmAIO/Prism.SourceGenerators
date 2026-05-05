using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism9.ViewModels;
using Prism.SourceGenerators.Samples.Prism9.Views;

namespace Prism.SourceGenerators.Samples.Prism9;

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
        // All registrations come from attributes on partial types (see Services/, Views/, MainViewModel)
        // and are emitted into PrismRegistrationExtensions.RegisterGeneratedTypes().
        containerRegistry.RegisterGeneratedTypes();
    }
}
