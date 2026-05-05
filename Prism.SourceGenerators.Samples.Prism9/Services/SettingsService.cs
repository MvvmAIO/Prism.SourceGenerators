using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.Services;

[RegisterSingleton(ServiceType = typeof(ISettingsService))]
public sealed partial class SettingsService : ISettingsService
{
    public string AppSectionTitle =>
        "Prism 9 sample — attributes on types generate IContainerRegistry calls.";
}
