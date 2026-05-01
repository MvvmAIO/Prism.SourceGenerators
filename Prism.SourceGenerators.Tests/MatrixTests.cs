using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class MatrixTests
{
    public static TheoryData<LanguageVersion, bool, bool, bool> DelegateCommandMatrix => new()
    {
        // languageVersion, hasAsyncDelegateCommand, expectFieldKeyword, expectPackageDiagnostic (PSG3002)
        { LanguageVersion.CSharp12, true,  false, false },
        { LanguageVersion.Preview,  true,  true,  false },
        { LanguageVersion.CSharp12, false, false, true },
        { LanguageVersion.Preview,  false, true,  true }
    };

    [Theory]
    [MemberData(nameof(DelegateCommandMatrix))]
    public void DelegateCommand_generation_matches_matrix(
        LanguageVersion languageVersion,
        bool hasAsyncDelegateCommand,
        bool expectFieldKeyword,
        bool expectPackageDiagnostic)
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

        Assert.False(output.GeneratedSources.Any(s => s.HintName == "AsyncDelegateCommand.Polyfill.g.cs"));
        Assert.Equal(expectPackageDiagnostic, output.Diagnostics.Any(d => d.Id == "PSG3002"));
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

    [Fact]
    public void AsyncDelegateCommand_catch_uses_generic_overload_for_specific_exception_types()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [AsyncDelegateCommand(Catch = nameof(OnOperationCanceledException), CommandName = nameof(HelloCommand))]
                private async System.Threading.Tasks.Task HelloAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }

                [AsyncDelegateCommand(Catch = nameof(OnOperationCanceledException2), CommandName = nameof(Hello2Command))]
                private async System.Threading.Tasks.Task Hello2Async()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }

                private void OnOperationCanceledException(System.OperationCanceledException ex)
                {
                }

                private void OnOperationCanceledException2<TEx>(TEx ex) where TEx : System.OperationCanceledException
                {
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        GeneratedSource helloCommand = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".HelloCommand.g.cs")));
        Assert.Contains(".Catch<global::System.OperationCanceledException>(OnOperationCanceledException)", helloCommand.Source);

        GeneratedSource hello2Command = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Hello2Command.g.cs")));
        Assert.Contains(".Catch<global::System.OperationCanceledException>(OnOperationCanceledException2)", hello2Command.Source);
    }

    [Fact]
    public void AsyncDelegateCommand_reports_warning_for_missing_catch_handler_without_blocking_generation()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [AsyncDelegateCommand(Catch = "MissingHandler", CommandName = nameof(HelloCommand))]
                private async System.Threading.Tasks.Task HelloAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);
        Assert.Contains(output.Diagnostics, d => d.Id == "PSG2001");
        Assert.Contains(output.GeneratedSources, s => s.HintName.EndsWith(".HelloCommand.g.cs"));
    }

    [Fact]
    public void AsyncDelegateCommand_reports_package_required_when_prism_async_command_missing()
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
            languageVersion: LanguageVersion.Preview,
            hasAsyncDelegateCommand: false);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG3002");
        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName == "AsyncDelegateCommand.Polyfill.g.cs");
    }

    public static TheoryData<string, string, string, string> TypeShapeMatrix => new()
    {
        {
            "nested",
            """
            namespace Demo;

            public partial class Outer
            {
                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private int _count;

                    [DelegateCommand]
                    private void Save()
                    {
                    }
                }
            }
            """,
            "partial class Vm",
            "public int Count"
        },
        {
            "generic",
            """
            namespace Demo;

            public partial class Vm<T> : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private T? _value;

                [DelegateCommand]
                private void Save(T? value)
                {
                }
            }
            """,
            "partial class Vm<T>",
            "public T Value"
        },
        {
            "abstract",
            """
            namespace Demo;

            public abstract partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";

                [DelegateCommand]
                private void Save()
                {
                }
            }
            """,
            "partial class Vm",
            "public string Name"
        },
        {
            "inheritance",
            """
            namespace Demo;

            public partial class BaseVm : Prism.Mvvm.BindableBase
            {
                protected bool CanSave()
                {
                    return true;
                }
            }

            public partial class Vm : BaseVm
            {
                [ObservableProperty]
                private int _count;

                [DelegateCommand(CanExecute = nameof(CanSave))]
                private void Save()
                {
                }
            }
            """,
            "partial class Vm",
            "public int Count"
        }
    };

    [Theory]
    [MemberData(nameof(TypeShapeMatrix))]
    public void Generation_supports_type_shape_matrix(
        string scenario,
        string source,
        string expectedTypeDeclarationFragment,
        string expectedPropertyFragment)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        Assert.True(
            output.Diagnostics.All(static d => !d.Id.StartsWith("PSG", System.StringComparison.Ordinal)),
            $"Scenario '{scenario}' produced diagnostics: {string.Join(", ", output.Diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"))}");

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".SaveCommand.g.cs")));
        Assert.Contains(expectedTypeDeclarationFragment, commandSource.Source);
        Assert.Contains("Save", commandSource.Source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Count.g.cs") || s.HintName.EndsWith(".Value.g.cs") || s.HintName.EndsWith(".Name.g.cs")));
        Assert.Contains(expectedTypeDeclarationFragment, propertySource.Source);
        Assert.Contains(expectedPropertyFragment, propertySource.Source);
    }
}
