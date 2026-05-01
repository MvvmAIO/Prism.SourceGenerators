using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prism.Mvvm;
using Prism.SourceGenerators;
using Prism.SourceGenerators.Diagnostics;
using Xunit;

namespace Prism.SourceGenerators.Integration.Tests;

/// <summary>
/// Uses real Prism.Core 8.1.97 (<see cref="BindableBase"/>) plus optional MvvmAIO.Prism.Bcl.Commands,
/// mirroring consumer installs (generators + attributes vs. manual BCL commands package).
/// </summary>
public sealed class Prism8PackagingIntegrationTests
{
    private const string UserSource = """
        #nullable enable
        using Prism.Mvvm;
        using Prism.SourceGenerators;

        namespace Demo;

        public partial class Vm : BindableBase
        {
            [DelegateCommand]
            private async System.Threading.Tasks.Task LoadAsync()
            {
                await System.Threading.Tasks.Task.CompletedTask;
            }
        }
        """;

    [Fact]
    public void Prism_Core_8_without_Bcl_Commands_reports_PSG3002_with_expected_message()
    {
        GeneratorRunOutput output = RunGenerators(includeMvvmBclCommands: false);

        Diagnostic[] psg3002 = output.Diagnostics.Where(static d => d.Id == "PSG3002").ToArray();
        Assert.True(
            psg3002.Length > 0,
            "Expected PSG3002. Diagnostics: "
                + string.Join(
                    "; ",
                    output.Diagnostics.Select(static d => $"{d.Id}: {d.GetMessage(CultureInfo.InvariantCulture)}")));

        LocalizableString format = DiagnosticDescriptors.AsyncDelegateCommandPackageRequired.MessageFormat;
        string expected = string.Format(
            CultureInfo.InvariantCulture,
            format.ToString(CultureInfo.InvariantCulture),
            "MvvmAIO.Prism.SourceGenerators");

        Assert.All(psg3002, d =>
            Assert.Equal(expected, d.GetMessage(CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Prism_Core_8_with_Bcl_Commands_does_not_report_PSG3002()
    {
        GeneratorRunOutput output = RunGenerators(includeMvvmBclCommands: true);

        Assert.DoesNotContain(output.Diagnostics, d => d.Id == "PSG3002");

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".LoadCommand.g.cs", StringComparison.Ordinal)));

        Assert.Contains("global::Prism.Commands.AsyncDelegateCommand", commandSource.Source);
    }

    private static GeneratorRunOutput RunGenerators(bool includeMvvmBclCommands)
    {
        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(UserSource, parseOptions);

        IEnumerable<MetadataReference> references = BuildReferences(includeMvvmBclCommands);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "IntegrationConsumer",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<Diagnostic> compilationErrors = compilation
            .GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        Assert.True(
            compilationErrors.IsEmpty,
            string.Join(Environment.NewLine, compilationErrors.Select(static d => d.ToString())));

        ImmutableArray<IIncrementalGenerator> generators =
            ImmutableArray.Create<IIncrementalGenerator>(
                new ObservablePropertyGenerator(),
                new DelegateCommandGenerator(),
                new BindableBaseGenerator());

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: generators.Select(static g => g.AsSourceGenerator()),
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out ImmutableArray<Diagnostic> driverDiagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        ImmutableArray<Diagnostic> generatorDiagnostics = runResult.Diagnostics
            .AddRange(runResult.Results.SelectMany(static r => r.Diagnostics));

        ImmutableArray<GeneratedSource> generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .OrderBy(static item => item.HintName, StringComparer.Ordinal)
            .Select(static item => new GeneratedSource(item.HintName, item.SourceText.ToString()))
            .ToImmutableArray();

        return new GeneratorRunOutput(generatedSources, generatorDiagnostics.AddRange(driverDiagnostics));
    }

    private static IEnumerable<MetadataReference> BuildReferences(bool includeMvvmBclCommands)
    {
        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is unavailable.");
        }

        IEnumerable<MetadataReference> platform = trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path));

        string mvvmCore = Path.Combine(AppContext.BaseDirectory, "MvvmAIO.Prism.Core.dll");
        if (!File.Exists(mvvmCore))
        {
            throw new InvalidOperationException($"Required test reference not found: {mvvmCore}");
        }

        string prismDll = typeof(BindableBase).Assembly.Location;
        if (!File.Exists(prismDll))
        {
            throw new InvalidOperationException($"Prism.Core assembly not found: {prismDll}");
        }

        IEnumerable<MetadataReference> refs = platform
            .Append(MetadataReference.CreateFromFile(mvvmCore))
            .Append(MetadataReference.CreateFromFile(prismDll));

        if (!includeMvvmBclCommands)
        {
            return refs;
        }

        string configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        string bcl = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Prism.Bcl.Commands",
                "bin",
                configuration,
                "netstandard2.0",
                "MvvmAIO.Prism.Bcl.Commands.dll"));

        if (!File.Exists(bcl))
        {
            throw new InvalidOperationException(
                $"MvvmAIO.Prism.Bcl.Commands.dll not found at repo build output ({bcl}). Build Prism.Bcl.Commands first.");
        }

        return refs.Append(MetadataReference.CreateFromFile(bcl));
    }

    private sealed record GeneratedSource(string HintName, string Source);

    private sealed record GeneratorRunOutput(ImmutableArray<GeneratedSource> GeneratedSources, ImmutableArray<Diagnostic> Diagnostics);
}
