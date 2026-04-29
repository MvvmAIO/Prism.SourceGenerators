using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Prism.SourceGenerators.Samples.Prism9;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
