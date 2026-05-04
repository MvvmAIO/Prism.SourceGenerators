using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Prism.SourceGenerators.Samples.Prism8.Views;

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
