using Prism.Mvvm;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism8.Services;

namespace Prism.SourceGenerators.Samples.Prism8.ViewModels;

public partial class DashboardViewModel : BindableBase
{
    [ObservableProperty]
    private string _headline = "Dashboard";

    [ObservableProperty]
    private string _body =
        "This view is shown via Prism region navigation (IRegionManager.RequestNavigate).";

    public DashboardViewModel(ISettingsService settings)
    {
        _body =
            $"{settings.AppSectionTitle} See Prism 9 sample for attribute-driven RegisterGeneratedTypes().";
    }
}
