using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism9.ViewModels;

namespace Prism.SourceGenerators.Samples.Prism9.Views;

[RegisterForNavigation<ProfileViewModel>(Name = "Profile")]
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
