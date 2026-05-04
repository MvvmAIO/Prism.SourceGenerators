using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Prism.SourceGenerators.Samples.Prism8.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
