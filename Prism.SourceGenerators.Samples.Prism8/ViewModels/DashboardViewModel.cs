using Prism.Mvvm;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

public partial class DashboardViewModel : BindableBase
{
    [ObservableProperty]
    private string _headline = "Dashboard";

    [ObservableProperty]
    private string _body =
        "This view is shown via Prism region navigation (IRegionManager.RequestNavigate).";
}
