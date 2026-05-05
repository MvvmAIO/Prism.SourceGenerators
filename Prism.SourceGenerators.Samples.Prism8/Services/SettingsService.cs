namespace Prism.SourceGenerators.Samples.Prism8.Services;

/// <summary>
/// Registered manually in <see cref="App.RegisterTypes"/> — Prism 8 uses different
/// <c>IContainerRegistry</c> overloads than the source generator emits for Prism 9+.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    public string AppSectionTitle =>
        "Prism 8 sample — ISettingsService registered next to region navigation.";
}
