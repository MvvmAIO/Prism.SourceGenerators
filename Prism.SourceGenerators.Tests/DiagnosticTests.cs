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
}
