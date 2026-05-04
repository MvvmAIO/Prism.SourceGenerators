using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

public partial class DashboardViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Headline { get; set; } = "Dashboard";

    [ObservableProperty]
    public partial string Body { get; set; } =
        "This view is shown via Prism region navigation (IRegionManager.RequestNavigate).";
}
