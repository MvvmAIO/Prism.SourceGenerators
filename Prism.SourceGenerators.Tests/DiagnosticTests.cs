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
    public void Reports_expected_diagnostic_for_invalid_input(string diagnosticId, string source)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        bool containsExpectedDiagnostic = output.Diagnostics.Any(d => d.Id == diagnosticId);

        Assert.True(
            containsExpectedDiagnostic,
            $"Expected diagnostic '{diagnosticId}' was not reported. Actual diagnostics: {string.Join(", ", output.Diagnostics.Select(d => d.Id))}");
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
            "MvvmAIO.Prism.Prism.SourceGenerators");

        Assert.All(psg3002, d =>
            Assert.Equal(expected, d.GetMessage(CultureInfo.InvariantCulture)));
    }
}
