using Prism.Mvvm;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Samples.Prism9.Services;

namespace Prism.SourceGenerators.Samples.Prism9.ViewModels;

public partial class DashboardViewModel : BindableBase
{
    [ObservableProperty]
    public partial string Headline { get; set; } = "Dashboard";

    [ObservableProperty]
    public partial string Body { get; set; } =
        "This view is shown via Prism region navigation (IRegionManager.RequestNavigate).";

    public DashboardViewModel(ISettingsService settings, IDateTimeProvider clock)
    {
        Body =
            $"{settings.AppSectionTitle} Resolved services: {clock.Now:yyyy-MM-dd HH:mm} (time from IDateTimeProvider registered with IfNotRegistered → TryRegisterSingleton).";
    }
}
