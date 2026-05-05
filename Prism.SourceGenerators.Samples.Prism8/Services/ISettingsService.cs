namespace Prism.SourceGenerators.Samples.Prism8.Services;

/// <summary>Sample service registered via <c>[RegisterSingleton(ServiceType = …)]</c>.</summary>
public interface ISettingsService
{
    string AppSectionTitle { get; }
}
