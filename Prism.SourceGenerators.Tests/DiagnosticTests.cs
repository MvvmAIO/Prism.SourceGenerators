using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Prism.SourceGenerators.Diagnostics;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class DiagnosticTests
{
    [Theory]
    [InlineData("PSG0001", """
        namespace Demo;

        public class Foo : Prism.Mvvm.BindableBase
        {
            [ObservableProperty]
            private int _count;
        }
        """)]
    [InlineData("PSG0002", """
        namespace Demo;

        public class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand]
            private void Save()
            {
            }
        }
        """)]
    [InlineData("PSG0003", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [ObservableProperty]
            public int Count { get; set; }
        }
        """)]
    [InlineData("PSG0004", """
        namespace Demo;

        [BindableBase]
        public class Foo
        {
        }
        """)]
    [InlineData("PSG1001", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand]
            private int Save()
            {
                return 1;
            }
        }
        """)]
    [InlineData("PSG1002", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [AsyncDelegateCommand]
            private void Save()
            {
            }
        }
        """)]
    [InlineData("PSG1001", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand]
            private async System.Threading.Tasks.ValueTask SaveAsync(System.Threading.CancellationToken ct)
            {
                await System.Threading.Tasks.Task.CompletedTask;
            }
        }
        """)]
    [InlineData("PSG2002", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [AsyncDelegateCommand(Catch = nameof(OnError))]
            private async System.Threading.Tasks.Task SaveAsync()
            {
                await System.Threading.Tasks.Task.CompletedTask;
            }

            private void OnError(int code)
            {
            }
        }
        """)]
    [InlineData("PSG2003", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand(CanExecute = nameof(CanSaveMissing))]
            private void Save()
            {
            }
        }
        """)]
    [InlineData("PSG2004", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand]
            [ObservesProperty(nameof(NotExistingProperty))]
            private void Save()
            {
            }
        }
        """)]
    [InlineData("PSG2005", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [ObservableProperty]
            [NotifyCanExecuteChangedFor("MissingCommand")]
            private string _name = "";
        }
        """)]
    [InlineData("PSG2006", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand(CanExecute = nameof(CanSave))]
            private void Save()
            {
            }

            private int CanSave() => 1;
        }
        """)]
    [InlineData("PSG5001", """
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [ObservableProperty]
            [NotifyDataErrorInfo]
            private string _name = "";
        }
        """)]
    [InlineData("PSG5001", """
        namespace Demo;

        [NotifyDataErrorInfo]
        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [ObservableProperty]
            private string _name = "";
        }
        """)]
    [InlineData("PSG0002", """
        namespace Demo;

        public class Foo : Prism.Mvvm.BindableBase
        {
            [AsyncDelegateCommand]
            private async System.Threading.Tasks.Task RunAsync()
            {
                await System.Threading.Tasks.Task.CompletedTask;
            }
        }
        """)]
    public void Reports_expected_diagnostic_for_invalid_input(string diagnosticId, string source)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        bool containsExpectedDiagnostic = output.Diagnostics.Any(d => d.Id == diagnosticId);

        Assert.True(
            containsExpectedDiagnostic,
            $"Expected diagnostic '{diagnosticId}' was not reported. Actual diagnostics: {string.Join(", ", output.Diagnostics.Select(d => d.Id))}");
    }

    [Theory]
    [InlineData("""
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [ObservableProperty]
            private string _name = "";
        }
        """)]
    [InlineData("""
        namespace Demo;

        public partial class Foo : Prism.Mvvm.BindableBase
        {
            [DelegateCommand]
            private void Save()
            {
            }
        }
        """)]
    [InlineData("""
        namespace Demo;

        public partial class Foo : Prism.SourceGenerators.BindableValidator
        {
            [ObservableProperty]
            [NotifyDataErrorInfo]
            private string _name = "";
        }
        """)]
    public void Valid_code_does_not_report_PSG_diagnostics(string source)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Diagnostic[] psgDiagnostics = output.Diagnostics
            .Where(d => d.Id.StartsWith("PSG", System.StringComparison.Ordinal))
            .ToArray();

        Assert.Empty(psgDiagnostics);
    }

    [Fact]
    public void PSG5001_message_format_includes_type_name()
    {
        const string source = """
            namespace Demo;

            public partial class MyViewModel : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Diagnostic[] psg5001 = output.Diagnostics.Where(d => d.Id == "PSG5001").ToArray();
        Assert.NotEmpty(psg5001);
        Assert.All(psg5001, d =>
        {
            Assert.Contains("MyViewModel", d.GetMessage());
            Assert.Contains("BindableValidator", d.GetMessage());
        });
    }

    [Fact]
    public void Multiple_diagnostics_reported_on_same_type()
    {
        const string source = """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";

                [DelegateCommand]
                private void Save()
                {
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG0001");
        Assert.Contains(output.Diagnostics, d => d.Id == "PSG0002");
    }

    [Fact]
    public void PSG5001_on_partial_property_non_validator()
    {
        const string source = """
            namespace Demo;

            public partial class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                public partial string Name { get; set; }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG5001");
    }

    [Fact]
    public void PSG2005_message_format_includes_command_and_type_names()
    {
        const string source = """
            namespace Demo;

            public partial class MyVm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyCanExecuteChangedFor("MissingCmd")]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        Diagnostic[] psg2005 = output.Diagnostics.Where(d => d.Id == "PSG2005").ToArray();
        Assert.NotEmpty(psg2005);
        Assert.All(psg2005, d =>
        {
            Assert.Contains("MissingCmd", d.GetMessage());
            Assert.Contains("MyVm", d.GetMessage());
        });
    }

    [Fact]
    public void PSG3002_message_matches_descriptor_format_with_package_ids()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.Task LoadAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, hasAsyncDelegateCommand: false);

        Diagnostic[] psg3002 = output.Diagnostics.Where(static d => d.Id == "PSG3002").ToArray();
        Assert.NotEmpty(psg3002);

        LocalizableString format = DiagnosticDescriptors.AsyncDelegateCommandPackageRequired.MessageFormat;
        string expected = string.Format(
            CultureInfo.InvariantCulture,
            format.ToString(CultureInfo.InvariantCulture),
            "MvvmAIO.Prism.SourceGenerators");

        Assert.All(psg3002, d =>
            Assert.Equal(expected, d.GetMessage(CultureInfo.InvariantCulture)));
    }
}
