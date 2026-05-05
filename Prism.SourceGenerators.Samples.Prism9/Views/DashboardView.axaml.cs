using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism9.ViewModels;

namespace Prism.SourceGenerators.Samples.Prism9.Views;

[RegisterForNavigation<DashboardViewModel>(Name = "Dashboard")]
public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
