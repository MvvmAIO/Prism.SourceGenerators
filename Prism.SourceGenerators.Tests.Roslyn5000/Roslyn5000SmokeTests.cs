using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prism.SourceGenerators;
using Xunit;

namespace Prism.SourceGenerators.Tests.Roslyn5000;

/// <summary>
/// Smoke coverage for the Roslyn 5.0 analyzer build. Full snapshot tests remain on Roslyn 4.12 (<see cref="Prism.SourceGenerators.Tests"/>).
/// </summary>
public sealed class Roslyn5000SmokeTests
{
    [Fact]
    public void ObservableProperty_on_Roslyn5000_emits_property_and_no_PSG_errors()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _title = "";
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Title.g.cs")));

        Assert.Contains("public string Title", propertySource.Source);
        Assert.Contains("SetProperty", propertySource.Source);
    }

    [Fact]
    public void DelegateCommand_Task_execute_on_Roslyn5000_emits_AsyncDelegateCommand()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.Task SaveAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource, includeCommands: true);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".SaveCommand.g.cs")));

        Assert.Contains("AsyncDelegateCommand", commandSource.Source);
        Assert.Contains("SaveAsync", commandSource.Source);
    }

    [Fact]
    public void DelegateCommand_TaskOfT_execute_on_Roslyn5000_emits_await_wrapper()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.Task<int> CountAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                    return 0;
                }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource, includeCommands: true);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".CountCommand.g.cs")));

        Assert.Contains("async () => await CountAsync()", commandSource.Source);
    }
}

internal static class Roslyn5000TestHarness
{
    internal static GeneratorRunOutput Run(string userSource, bool includeCommands = false)
    {
        string commandStubs = includeCommands
            ? """

            namespace Prism.Commands
            {
                public class DelegateCommand
                {
                    public DelegateCommand(System.Action execute) { }
                    public DelegateCommand(System.Func<System.Threading.Tasks.Task> execute) { }
                }

                public class AsyncDelegateCommand
                {
                    public AsyncDelegateCommand(System.Func<System.Threading.Tasks.Task> execute) { }
                    public AsyncDelegateCommand(System.Func<System.Threading.Tasks.Task> execute, System.Func<bool> canExecute) { }
                }

                public class AsyncDelegateCommand<T>
                {
                    public AsyncDelegateCommand(System.Func<T, System.Threading.Tasks.Task> execute) { }
                    public AsyncDelegateCommand(System.Func<T, System.Threading.Tasks.Task> execute, System.Func<T, bool> canExecute) { }
                }
            }
            """
            : string.Empty;

        string harness = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using Prism.SourceGenerators;

            namespace Prism.Mvvm
            {
                public abstract class BindableBase : System.ComponentModel.INotifyPropertyChanged
                {
                    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

                    protected bool SetProperty<T>(ref T storage, T value, string? propertyName = null) => false;
                    protected void RaisePropertyChanged(string? propertyName = null) { }
                }
            }
            """ + commandStubs + userSource;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            harness,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Roslyn5000SmokeTests",
            [syntaxTree],
            Roslyn5000MetadataReferences.Get(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator[] generators = includeCommands
            ?
            [
                new ObservablePropertyGenerator(),
                new PropertyChangingGenerator(),
                new BindableBaseGenerator(),
                new DelegateCommandGenerator(),
            ]
            :
            [
                new ObservablePropertyGenerator(),
                new PropertyChangingGenerator(),
                new BindableBaseGenerator(),
            ];

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators.Select(static g => g.AsSourceGenerator()),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation _,
            out ImmutableArray<Diagnostic> driverDiagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        foreach (GeneratorRunResult gr in runResult.Results)
        {
            if (gr.Exception is not null)
            {
                throw gr.Exception;
            }
        }

        ImmutableArray<GeneratedSource> generatedSources = runResult.Results
            .SelectMany(static r => r.GeneratedSources)
            .OrderBy(static s => s.HintName, System.StringComparer.Ordinal)
            .Select(static s => new GeneratedSource(s.HintName, s.SourceText.ToString()))
            .ToImmutableArray();

        ImmutableArray<Diagnostic> allDiagnostics = runResult.Diagnostics
            .AddRange(runResult.Results.SelectMany(static r => r.Diagnostics))
            .AddRange(driverDiagnostics);

        return new GeneratorRunOutput(generatedSources, allDiagnostics);
    }
}

file static class Roslyn5000MetadataReferences
{
    internal static MetadataReference[] Get()
    {
        string? tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(tpa))
        {
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is unavailable.");
        }

        string mvvmCore = Path.Combine(AppContext.BaseDirectory, "MvvmAIO.Prism.Core.dll");
        if (!File.Exists(mvvmCore))
        {
            throw new InvalidOperationException($"Required test reference not found: {mvvmCore}");
        }

        var references = tpa
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path))
            .ToList();
        references.Add(MetadataReference.CreateFromFile(mvvmCore));
        return references.ToArray();
    }
}

internal sealed record GeneratorRunOutput(
    ImmutableArray<GeneratedSource> GeneratedSources,
    ImmutableArray<Diagnostic> Diagnostics);

internal sealed record GeneratedSource(string HintName, string Source);
