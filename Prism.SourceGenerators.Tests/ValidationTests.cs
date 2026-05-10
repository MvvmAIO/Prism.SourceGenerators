using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void NotifyDataErrorInfo_field_target_emits_ValidateProperty_call()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.ObservableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("ValidateProperty(value, nameof(Name));", propertySource.Source);
        Assert.DoesNotContain("PSG5001", output.Diagnostics.Select(d => d.Id).ToArray());
    }

    [Fact]
    public void NotifyDataErrorInfo_partial_property_target_emits_ValidateProperty_call()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.ObservableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                public partial string Name { get; set; }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("ValidateProperty(value, nameof(Name));", propertySource.Source);
    }

    [Fact]
    public void NotifyDataErrorInfo_on_class_applies_to_all_properties()
    {
        const string source = """
            namespace Demo;

            [NotifyDataErrorInfo]
            public partial class Vm : Prism.SourceGenerators.ObservableValidator
            {
                [ObservableProperty]
                private string _firstName = "";

                [ObservableProperty]
                private string _lastName = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource firstNameSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".FirstName.g.cs")));
        GeneratedSource lastNameSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".LastName.g.cs")));

        Assert.Contains("ValidateProperty(value, nameof(FirstName));", firstNameSource.Source);
        Assert.Contains("ValidateProperty(value, nameof(LastName));", lastNameSource.Source);
    }

    [Fact]
    public void Without_NotifyDataErrorInfo_no_ValidateProperty_call()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.DoesNotContain("ValidateProperty", propertySource.Source);
    }

    [Fact]
    public void NotifyDataErrorInfo_on_non_validator_reports_PSG5001()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG5001");
    }

    [Fact]
    public void NotifyDataErrorInfo_class_level_on_non_validator_reports_PSG5001()
    {
        const string source = """
            namespace Demo;

            [NotifyDataErrorInfo]
            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG5001");
    }

    [Fact]
    public void NotifyDataErrorInfo_is_not_forwarded_as_attribute()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.ObservableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                public partial string Name { get; set; }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.DoesNotContain("NotifyDataErrorInfo", propertySource.Source.Replace("ValidateProperty", ""));
    }

    [Fact]
    public void NotifyDataErrorInfo_with_other_attributes_works_together()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.ObservableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                [NotifyPropertyChangedFor(nameof(FullName))]
                private string _firstName = "";

                public string FullName => FirstName;
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".FirstName.g.cs")));

        Assert.Contains("ValidateProperty(value, nameof(FirstName));", propertySource.Source);
        Assert.Contains("RaisePropertyChanged(nameof(FullName));", propertySource.Source);
    }
}
