using System.Linq;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class RegionNavigationGeneratorTests
{
    [Fact]
    public void NavigateCommand_generates_RequestNavigate_command()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                private readonly Prism.Navigation.Regions.IRegionManager _regionManager;

                public ShellVm(Prism.Navigation.Regions.IRegionManager regionManager) => _regionManager = regionManager;

                [NavigateCommand(Region = "Content", Target = "Dashboard")]
                private void GoDashboard() { }
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".GoDashboardCommand.g.cs")));
        Assert.Contains("RequestNavigate(\"Content\", \"Dashboard\")", generated.Source);
        Assert.Contains("GoDashboardCommand", generated.Source);
    }

    [Fact]
    public void NavigateOnChanged_generates_OnChanged_hook()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                private readonly Prism.Navigation.Regions.IRegionManager _regionManager;
                private bool _ready;

                public ShellVm(Prism.Navigation.Regions.IRegionManager regionManager) => _regionManager = regionManager;

                [ObservableProperty]
                [NavigateOnChanged(Region = "Content", TargetMember = nameof(Item.Key))]
                private Item _item = new();

                private sealed class Item
                {
                    public string Key { get; set; } = "Dashboard";
                }
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Item.NavigateOnChanged.g.cs")));
        Assert.Contains("partial void OnItemChanged", generated.Source);
        Assert.Contains("RequestNavigate(\"Content\", value.Key)", generated.Source);
    }

    [Fact]
    public void NavigateCommand_reports_PSG7001_when_region_manager_missing()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                [NavigateCommand(Region = "Content", Target = "Dashboard")]
                private void GoDashboard() { }
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7001");
    }

    [Fact]
    public void NavigateCommand_strips_Async_suffix_for_command_name()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                private readonly Prism.Navigation.Regions.IRegionManager _regionManager;

                public ShellVm(Prism.Navigation.Regions.IRegionManager regionManager) => _regionManager = regionManager;

                [NavigateCommand(Region = "Content", Target = "Dashboard")]
                private void GoDashboardAsync() { }
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".GoDashboardCommand.g.cs")));
        Assert.Contains("GoDashboardCommand", generated.Source);
        Assert.DoesNotContain("GoDashboardAsyncCommand", generated.Source);
    }
}
