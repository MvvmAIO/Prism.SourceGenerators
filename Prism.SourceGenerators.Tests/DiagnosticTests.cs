using System.Linq;
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
    public void Reports_expected_diagnostic_for_invalid_input(string diagnosticId, string source)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source);

        bool containsExpectedDiagnostic = output.Diagnostics.Any(d => d.Id == diagnosticId);

        Assert.True(
            containsExpectedDiagnostic,
            $"Expected diagnostic '{diagnosticId}' was not reported. Actual diagnostics: {string.Join(", ", output.Diagnostics.Select(d => d.Id))}");
    }
}
