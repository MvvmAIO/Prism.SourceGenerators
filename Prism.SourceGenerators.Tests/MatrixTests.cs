using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class MatrixTests
{
    public static TheoryData<LanguageVersion, bool, bool, bool> DelegateCommandMatrix => new()
    {
        // languageVersion, hasAsyncDelegateCommand, expectFieldKeyword, expectPolyfill
        { LanguageVersion.CSharp12, true,  false, false },
        { LanguageVersion.Preview,  true,  true,  false },
        { LanguageVersion.CSharp12, false, false, true  },
        { LanguageVersion.Preview,  false, true,  true  }
    };

    [Theory]
    [MemberData(nameof(DelegateCommandMatrix))]
    public void DelegateCommand_generation_matches_matrix(
        LanguageVersion languageVersion,
        bool hasAsyncDelegateCommand,
        bool expectFieldKeyword,
        bool expectPolyfill)
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

        GeneratorRunOutput output = GeneratorTestHarness.Run(
            source,
            languageVersion: languageVersion,
            hasAsyncDelegateCommand: hasAsyncDelegateCommand);

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".LoadCommand.g.cs")));

        if (expectFieldKeyword)
        {
            Assert.Contains("=> field ??= new global::Prism.Commands.AsyncDelegateCommand(LoadAsync);", commandSource.Source);
            Assert.DoesNotContain("private global::Prism.Commands.AsyncDelegateCommand? _loadCommand;", commandSource.Source);
        }
        else
        {
            Assert.Contains("private global::Prism.Commands.AsyncDelegateCommand? _loadCommand;", commandSource.Source);
            Assert.Contains("=> _loadCommand ??= new global::Prism.Commands.AsyncDelegateCommand(LoadAsync);", commandSource.Source);
        }

        bool hasPolyfill = output.GeneratedSources.Any(s => s.HintName == "AsyncDelegateCommand.Polyfill.g.cs");
        Assert.Equal(expectPolyfill, hasPolyfill);
    }

    public static TheoryData<LanguageVersion, bool> ObservablePropertyMatrix => new()
    {
        // languageVersion, usePartialProperty
        { LanguageVersion.CSharp12, false },
        { LanguageVersion.Preview,  false },
        { LanguageVersion.Preview,  true  }
    };

    [Theory]
    [MemberData(nameof(ObservablePropertyMatrix))]
    public void ObservableProperty_generation_matches_matrix(
        LanguageVersion languageVersion,
        bool usePartialProperty)
    {
        string source = usePartialProperty
            ? """
              namespace Demo;

              public partial class Vm : Prism.Mvvm.BindableBase
              {
                  [ObservableProperty]
                  public partial string Name { get; set; } = "";
              }
              """
            : """
              namespace Demo;

              public partial class Vm : Prism.Mvvm.BindableBase
              {
                  [ObservableProperty]
                  private string _name = "";
              }
              """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: languageVersion);
        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        if (usePartialProperty)
        {
            Assert.Contains("partial string Name", propertySource.Source);
            Assert.Contains("get => field;", propertySource.Source);
            Assert.Contains("set", propertySource.Source);
        }
        else
        {
            Assert.Contains("public string Name", propertySource.Source);
            Assert.Contains("get => _name;", propertySource.Source);
        }
    }

    public static TheoryData<LanguageVersion, string, string> DiagnosticLanguageMatrix => new()
    {
        { LanguageVersion.CSharp12, "PSG0001", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private int _count;
            }
            """ },
        { LanguageVersion.Preview, "PSG0001", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private int _count;
            }
            """ },
        { LanguageVersion.CSharp12, "PSG0002", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private void Save() { }
            }
            """ },
        { LanguageVersion.Preview, "PSG0002", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private void Save() { }
            }
            """ },
        { LanguageVersion.CSharp12, "PSG0003", """
            namespace Demo;

            public partial class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public int Count { get; set; }
            }
            """ },
        { LanguageVersion.Preview, "PSG0003", """
            namespace Demo;

            public partial class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public int Count { get; set; }
            }
            """ },
        { LanguageVersion.CSharp12, "PSG0004", """
            namespace Demo;

            [BindableBase]
            public class Foo
            {
            }
            """ },
        { LanguageVersion.Preview, "PSG0004", """
            namespace Demo;

            [BindableBase]
            public class Foo
            {
            }
            """ }
    };

    [Theory]
    [MemberData(nameof(DiagnosticLanguageMatrix))]
    public void PSG_diagnostics_are_consistent_across_language_versions(
        LanguageVersion languageVersion,
        string diagnosticId,
        string source)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: languageVersion);
        Assert.Contains(output.Diagnostics, d => d.Id == diagnosticId);
    }
}
