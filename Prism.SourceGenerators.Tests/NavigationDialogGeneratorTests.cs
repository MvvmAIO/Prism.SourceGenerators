using System.Linq;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public class NavigationDialogGeneratorTests
{
    [Fact]
    public void NavigationAware_generates_INavigationAware_members()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("INavigationAware", generated.Source);
        Assert.Contains("Prism.Navigation.Regions", generated.Source);
        Assert.Contains("OnNavigatedToCore", generated.Source);
        Assert.Contains("IsNavigationTargetCore", generated.Source);
    }

    [Fact]
    public void NavigationAware_generates_Prism8_regions_namespace()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(
            """
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
            }
            """,
            regionsApi: GeneratorTestHarness.HarnessRegionsApi.Prism8Only);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("global::Prism.Regions.INavigationAware", generated.Source);
        Assert.Contains("global::Prism.Regions.NavigationContext", generated.Source);
        Assert.DoesNotContain("Prism.Navigation.Regions", generated.Source);
    }

    [Fact]
    public void DialogAware_generates_IDialogAware_members()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".DialogAware.g.cs")));
        Assert.Contains("IDialogAware", generated.Source);
        Assert.Contains("_dialogTitle = \"Confirm\"", generated.Source);
        Assert.Contains("RequestClose", generated.Source);
    }

    [Fact]
    public void NavigationAware_reports_PSG0007_when_not_partial()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public class PageVm : Prism.Mvvm.BindableBase
            {
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG0007");
    }

    [Fact]
    public void DialogAware_reports_PSG0008_when_not_partial()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public class ConfirmVm : Prism.Mvvm.BindableBase
            {
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG0008");
    }
}
