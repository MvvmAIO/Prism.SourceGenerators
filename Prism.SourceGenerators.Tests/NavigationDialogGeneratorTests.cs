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

    // --- [FromNavigationParameter] tests ---

    [Fact]
    public void FromNavigationParameter_generates_TryGetValue_in_OnNavigatedTo()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter("userId")]
                [ObservableProperty]
                private int _userId;
            }
            """);

        Assert.Empty(output.Diagnostics);
        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"userId\"", generated.Source);
        Assert.Contains("UserId = UserIdValue", generated.Source);
        Assert.Contains("OnNavigatedToCore(navigationContext)", generated.Source);
    }

    [Fact]
    public void FromNavigationParameter_defaults_key_to_property_name()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter]
                [ObservableProperty]
                private string _userName;
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"UserName\"", generated.Source);
        Assert.Contains("UserName = UserNameValue", generated.Source);
    }

    [Fact]
    public void FromNavigationParameter_reports_PSG7007_without_ObservableProperty()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter("userId")]
                private int _userId;
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7007");
        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("INavigationAware", generated.Source);
        Assert.Contains("OnNavigatedToCore(navigationContext)", generated.Source);
        Assert.DoesNotContain("TryGetValue", generated.Source);
        Assert.DoesNotContain("UserId", generated.Source);
    }

    [Fact]
    public void FromNavigationParameter_PSG7007_skips_bad_binding_but_keeps_good_ones()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter("userId")]
                private int _userId;

                [FromNavigationParameter("name")]
                [ObservableProperty]
                private string _userName;
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7007");
        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"name\"", generated.Source);
        Assert.Contains("UserName = UserNameValue", generated.Source);
        Assert.DoesNotContain("\"userId\"", generated.Source);
    }

    [Fact]
    public void FromNavigationParameter_reports_PSG7006_on_method()
    {
        // AttributeUsage is Field|Property, so CS0592 is also emitted by the compiler;
        // the generator must additionally report PSG7006 for the invalid target.
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter("userId")]
                public void DoSomething() { }
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7006");
        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName.EndsWith(".NavigationAware.g.cs"));
    }

    [Fact]
    public void FromNavigationParameter_reports_PSG7008_for_empty_key()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter("")]
                [ObservableProperty]
                private int _userId;
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7008");
        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName.EndsWith(".NavigationAware.g.cs"));
    }

    // --- [FromDialogParameter] tests ---

    [Fact]
    public void FromDialogParameter_generates_TryGetValue_in_OnDialogOpened()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
                [FromDialogParameter("message")]
                [ObservableProperty]
                private string _message;
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".DialogAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"message\"", generated.Source);
        Assert.Contains("Message = MessageValue", generated.Source);
        Assert.Contains("OnDialogOpenedCore(parameters)", generated.Source);
    }

    [Fact]
    public void FromDialogParameter_defaults_key_to_property_name()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
                [FromDialogParameter]
                [ObservableProperty]
                private string _title;
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".DialogAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"Title\"", generated.Source);
        Assert.Contains("Title = TitleValue", generated.Source);
    }

    [Fact]
    public void FromDialogParameter_reports_PSG7104_without_ObservableProperty()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
                [FromDialogParameter("message")]
                private string _message;
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7104");
        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".DialogAware.g.cs")));
        Assert.Contains("IDialogAware", generated.Source);
        Assert.Contains("OnDialogOpenedCore(parameters)", generated.Source);
        Assert.DoesNotContain("TryGetValue", generated.Source);
        Assert.DoesNotContain("\"message\"", generated.Source);
    }

    [Fact]
    public void FromDialogParameter_reports_PSG7103_on_method()
    {
        // AttributeUsage is Field|Property, so CS0592 is also emitted by the compiler;
        // the generator must additionally report PSG7103 for the invalid target.
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
                [FromDialogParameter("message")]
                public void DoSomething() { }
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7103");
        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName.EndsWith(".DialogAware.g.cs"));
    }

    [Fact]
    public void FromDialogParameter_reports_PSG7105_for_empty_key()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
                [FromDialogParameter("")]
                [ObservableProperty]
                private string _message;
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7105");
        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName.EndsWith(".DialogAware.g.cs"));
    }

    // --- partial property mode (C# 13+): attributes must not be forwarded ---

    [Fact]
    public void FromNavigationParameter_on_partial_property_does_not_emit_CS0579()
    {
        // In partial-property mode the generator must NOT forward
        // [FromNavigationParameter] onto the generated implementing partial,
        // otherwise CS0579 (duplicate attribute) is reported.
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
                [FromNavigationParameter("userId")]
                [ObservableProperty]
                public partial int UserId { get; set; }
            }
            """);

        Assert.Empty(output.Diagnostics);
        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"userId\"", generated.Source);
    }

    [Fact]
    public void FromDialogParameter_on_partial_property_does_not_emit_CS0579()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
                [FromDialogParameter("message")]
                [ObservableProperty]
                public partial string Message { get; set; }
            }
            """);

        Assert.Empty(output.Diagnostics);
        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".DialogAware.g.cs")));
        Assert.Contains("TryGetValue", generated.Source);
        Assert.Contains("\"message\"", generated.Source);
    }
}
