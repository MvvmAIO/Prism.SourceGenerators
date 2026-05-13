using System;
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

            public partial class Vm : Prism.SourceGenerators.BindableValidator
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

            public partial class Vm : Prism.SourceGenerators.BindableValidator
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
            public partial class Vm : Prism.SourceGenerators.BindableValidator
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
    public void NotifyDataErrorInfo_on_non_validator_does_not_emit_ValidateProperty()
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

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.DoesNotContain("ValidateProperty", propertySource.Source);
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

            public partial class Vm : Prism.SourceGenerators.BindableValidator
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

            public partial class Vm : Prism.SourceGenerators.BindableValidator
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

    [Fact]
    public void NotifyDataErrorInfo_partial_property_on_non_validator_reports_PSG5001_and_suppresses()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                public partial string Name { get; set; }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG5001");

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.DoesNotContain("ValidateProperty", propertySource.Source);
    }

    [Fact]
    public void NotifyDataErrorInfo_class_level_and_member_level_mixed()
    {
        const string source = """
            namespace Demo;

            [NotifyDataErrorInfo]
            public partial class Vm : Prism.SourceGenerators.BindableValidator
            {
                [ObservableProperty]
                private string _firstName = "";

                [ObservableProperty]
                [NotifyDataErrorInfo]
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

        Assert.DoesNotContain(output.Diagnostics, d => d.Id == "PSG5001");
    }

    [Fact]
    public void NotifyDataErrorInfo_PSG5001_message_contains_type_name()
    {
        const string source = """
            namespace Demo;

            public partial class LoginForm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _email = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Diagnostic[] psg5001 = output.Diagnostics.Where(d => d.Id == "PSG5001").ToArray();
        Assert.NotEmpty(psg5001);
        Assert.All(psg5001, d =>
        {
            Assert.Contains("LoginForm", d.GetMessage());
            Assert.Contains("BindableValidator", d.GetMessage());
        });
    }

    [Fact]
    public void NotifyDataErrorInfo_ValidateProperty_appears_after_command_notifications()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.BindableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
                private string _name = "";

                [DelegateCommand]
                private void Save() { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        string src = propertySource.Source;
        int commandIdx = src.IndexOf("RaiseCanExecuteChanged");
        int validateIdx = src.IndexOf("ValidateProperty(value, nameof(Name))");

        Assert.True(commandIdx >= 0, "Expected RaiseCanExecuteChanged in generated source");
        Assert.True(validateIdx >= 0, "Expected ValidateProperty in generated source");
        Assert.True(validateIdx > commandIdx, "ValidateProperty should appear after command notifications");
    }

    [Fact]
    public void NotifyDataErrorInfo_on_validator_no_PSG5001()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.BindableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";

                [ObservableProperty]
                private int _age;
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.DoesNotContain(output.Diagnostics, d => d.Id == "PSG5001");

        GeneratedSource nameSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));
        GeneratedSource ageSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Age.g.cs")));

        Assert.Contains("ValidateProperty", nameSource.Source);
        Assert.DoesNotContain("ValidateProperty", ageSource.Source);
    }

    [Fact]
    public void BindableValidator_attribute_object_base_emits_inherit_BindableValidator()
    {
        const string source = """
            namespace Demo;

            [BindableValidator]
            public partial class Vm
            {
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource gen = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".BindableValidator.g.cs", StringComparison.Ordinal)));

        Assert.Contains(": global::Prism.SourceGenerators.BindableValidator", gen.Source);
    }

    [Fact]
    public void BindableValidator_attribute_on_Prism_bindable_base_emits_INotifyDataErrorInfo_only()
    {
        const string source = """
            namespace Demo;

            [BindableValidator]
            public partial class Vm : Prism.Mvvm.BindableBase
            {
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        GeneratedSource gen = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".BindableValidator.g.cs", StringComparison.Ordinal)));

        Assert.Contains("global::System.ComponentModel.INotifyDataErrorInfo", gen.Source);
        Assert.DoesNotContain("global::System.ComponentModel.INotifyPropertyChanged", gen.Source);
        Assert.Contains("__psg_ValidationContext", gen.Source);
    }

    [Fact]
    public void NotifyDataErrorInfo_with_BindableValidator_attribute_on_bindable_base_emits_ValidateProperty()
    {
        const string source = """
            namespace Demo;

            [BindableValidator]
            public partial class LoginForm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _email = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.DoesNotContain(output.Diagnostics, d => d.Id == "PSG5001");

        GeneratedSource emailSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Email.g.cs")));

        Assert.Contains("ValidateProperty(value, nameof(Email));", emailSource.Source);
    }

    [Fact]
    public void BindableBase_suppressed_when_BindableValidator_attribute_present()
    {
        const string source = """
            namespace Demo;

            [BindableBase]
            [BindableValidator]
            public partial class Vm
            {
                [ObservableProperty]
                private int _x;
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName.EndsWith(".BindableBase.g.cs", StringComparison.Ordinal));
        Assert.Contains(output.GeneratedSources, s => s.HintName.EndsWith(".BindableValidator.g.cs", StringComparison.Ordinal));
    }
}
