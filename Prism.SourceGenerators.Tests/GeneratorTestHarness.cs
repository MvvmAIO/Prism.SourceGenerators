using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Tests;

internal static class GeneratorTestHarness
{
    public static GeneratorRunOutput Run(
        string userSource,
        LanguageVersion languageVersion = LanguageVersion.Preview,
        bool hasAsyncDelegateCommand = true)
    {
        string asyncDelegateCommandStubs = hasAsyncDelegateCommand
            ? """
                public class AsyncDelegateCommand
                {
                    public AsyncDelegateCommand(Func<Task> execute) { }
                    public AsyncDelegateCommand(Func<Task> execute, Func<bool> canExecute) { }
                    public AsyncDelegateCommand EnableParallelExecution() => this;
                    public AsyncDelegateCommand CancelAfter(TimeSpan timeout) => this;
                    public AsyncDelegateCommand CancellationTokenSourceFactory(Func<CancellationToken> factory) => this;
                    public AsyncDelegateCommand Catch(Action<Exception> handler) => this;
                    public AsyncDelegateCommand ObservesProperty<T>(Func<T> propertyExpression) => this;
                }

                public class AsyncDelegateCommand<T>
                {
                    public AsyncDelegateCommand(Func<T, Task> execute) { }
                    public AsyncDelegateCommand(Func<T, Task> execute, Func<T, bool> canExecute) { }
                    public AsyncDelegateCommand<T> EnableParallelExecution() => this;
                    public AsyncDelegateCommand<T> CancelAfter(TimeSpan timeout) => this;
                    public AsyncDelegateCommand<T> CancellationTokenSourceFactory(Func<CancellationToken> factory) => this;
                    public AsyncDelegateCommand<T> Catch(Action<Exception> handler) => this;
                    public AsyncDelegateCommand<T> ObservesProperty<TProperty>(Func<TProperty> propertyExpression) => this;
                }
                """
            : string.Empty;

        string source = $$"""
            #nullable enable
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using Prism.SourceGenerators;

            namespace Prism.Commands
            {
                public class DelegateCommand
                {
                    public DelegateCommand(Action execute) { }
                    public DelegateCommand(Action execute, Func<bool> canExecute) { }
                    public DelegateCommand ObservesProperty<T>(Func<T> propertyExpression) => this;
                }

                public class DelegateCommand<T>
                {
                    public DelegateCommand(Action<T> execute) { }
                    public DelegateCommand(Action<T> execute, Func<T, bool> canExecute) { }
                    public DelegateCommand<T> ObservesProperty<TProperty>(Func<TProperty> propertyExpression) => this;
                }
                {{asyncDelegateCommandStubs}}
            }

            namespace Prism.Mvvm
            {
                public abstract class BindableBase
                {
                    protected bool SetProperty<T>(ref T storage, T value, string? propertyName = null)
                    {
                        storage = value;
                        return true;
                    }

                    protected void RaisePropertyChanged(string? propertyName = null) { }
                }
            }

            {{userSource}}
            """;

        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: new[] { syntaxTree },
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator[] generators =
        {
            new ObservablePropertyGenerator(),
            new DelegateCommandGenerator(),
            new BindableBaseGenerator()
        };

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: generators.Select(g => g.AsSourceGenerator()),
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> driverDiagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        ImmutableArray<Diagnostic> generatorDiagnostics = runResult.Diagnostics
            .AddRange(runResult.Results.SelectMany(static r => r.Diagnostics));

        var generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .OrderBy(static item => item.HintName, StringComparer.Ordinal)
            .Select(static item => new GeneratedSource(item.HintName, item.SourceText.ToString()))
            .ToImmutableArray();

        return new GeneratorRunOutput(generatedSources, generatorDiagnostics.AddRange(driverDiagnostics));
    }

    public static string ToSnapshot(GeneratorRunOutput output)
    {
        StringBuilder sb = new();

        ImmutableArray<Diagnostic> diagnostics = output.Diagnostics
            .Where(static d => d.Id.StartsWith("PSG", StringComparison.Ordinal))
            .OrderBy(static d => d.Id, StringComparer.Ordinal)
            .ThenBy(static d => d.GetMessage(), StringComparer.Ordinal)
            .ToImmutableArray();

        sb.AppendLine("Diagnostics:");
        if (diagnostics.IsDefaultOrEmpty)
        {
            sb.AppendLine("  <none>");
        }
        else
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                sb.AppendLine($"  {diagnostic.Id}: {diagnostic.GetMessage()}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Generated Sources:");

        foreach (GeneratedSource source in output.GeneratedSources)
        {
            sb.AppendLine($"--- {source.HintName} ---");
            sb.AppendLine(source.Source.TrimEnd());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is unavailable.");
        }

        return trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path));
    }
}

internal sealed record GeneratorRunOutput(
    ImmutableArray<GeneratedSource> GeneratedSources,
    ImmutableArray<Diagnostic> Diagnostics);

internal sealed record GeneratedSource(string HintName, string Source);
