using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prism.Mvvm;
using Prism.SourceGenerators;
using Xunit;

namespace Prism.SourceGenerators.Integration.Tests;

/// <summary>
/// End-to-end integration tests that compile user source against real Prism.Core 8.1.97
/// and MvvmAIO.Prism.Core assemblies, run all generators, and verify the combined output
/// compiles without errors and produces the expected generated sources.
/// </summary>
public sealed class GeneratorIntegrationTests
{
    #region ObservableProperty

    [Fact]
    public void ObservableProperty_field_target_compiles_with_real_BindableBase()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                private string _name = "Hello";

                [ObservableProperty]
                private int _count;
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource nameSource = AssertSingleHintNameEnding(output, ".Name.g.cs");
        GeneratedSource countSource = AssertSingleHintNameEnding(output, ".Count.g.cs");

        Assert.Contains("SetProperty", nameSource.Source);
        Assert.Contains("SetProperty", countSource.Source);

        AssertOutputCompiles(output, source);
    }

    [Fact]
    public void ObservableProperty_partial_property_target_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                public partial string Title { get; set; }

                [ObservableProperty]
                public partial int Age { get; set; }
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, LanguageVersion.Preview);

        AssertNoPsgErrors(output);

        GeneratedSource titleSource = AssertSingleHintNameEnding(output, ".Title.g.cs");
        Assert.Contains("SetProperty", titleSource.Source);

        AssertOutputCompiles(output, source, languageVersion: LanguageVersion.Preview);
    }

    [Fact]
    public void ObservableProperty_with_PropertyAccess_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty(PropertyAccess.Internal)]
                private string _secret = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource secretSource = AssertSingleHintNameEnding(output, ".Secret.g.cs");
        Assert.Contains("internal", secretSource.Source);

        AssertOutputCompiles(output, source);
    }

    #endregion

    #region NotifyPropertyChangedFor

    [Fact]
    public void NotifyPropertyChangedFor_generates_extra_PropertyChanged()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                private string _firstName = "";

                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                private string _lastName = "";

                public string FullName => $"{FirstName} {LastName}";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource firstNameSource = AssertSingleHintNameEnding(output, ".FirstName.g.cs");
        Assert.Contains("RaisePropertyChanged(nameof(FullName))", firstNameSource.Source);

        AssertOutputCompiles(output, source);
    }

    #endregion

    #region DelegateCommand

    [Fact]
    public void DelegateCommand_sync_with_CanExecute_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [DelegateCommand(CanExecute = nameof(CanSave))]
                private void Save() { }

                private bool CanSave() => true;
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource commandSource = AssertSingleHintNameEnding(output, ".SaveCommand.g.cs");
        Assert.Contains("global::Prism.Commands.DelegateCommand", commandSource.Source);
        Assert.Contains("CanSave", commandSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    [Fact]
    public void DelegateCommand_with_parameter_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [DelegateCommand]
                private void Select(string item) { }
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource commandSource = AssertSingleHintNameEnding(output, ".SelectCommand.g.cs");
        Assert.Contains("global::Prism.Commands.DelegateCommand<string>", commandSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    #endregion

    #region AsyncDelegateCommand

    [Fact]
    public void AsyncDelegateCommand_with_fluent_features_compiles()
    {
        const string source = """
            #nullable enable
            using System.Threading.Tasks;
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [AsyncDelegateCommand(EnableParallelExecution = true, CanExecute = nameof(CanLoad))]
                private async Task LoadAsync()
                {
                    await Task.CompletedTask;
                }

                private bool CanLoad() => true;
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource commandSource = AssertSingleHintNameEnding(output, ".LoadCommand.g.cs");
        Assert.Contains("global::Prism.Commands.AsyncDelegateCommand", commandSource.Source);
        Assert.Contains("EnableParallelExecution", commandSource.Source);
        Assert.Contains("CanLoad", commandSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    [Fact]
    public void AsyncDelegateCommand_with_CancellationToken_compiles()
    {
        const string source = """
            #nullable enable
            using System.Threading;
            using System.Threading.Tasks;
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [AsyncDelegateCommand]
                private async Task FetchAsync(CancellationToken ct)
                {
                    await Task.Delay(1, ct);
                }
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource commandSource = AssertSingleHintNameEnding(output, ".FetchCommand.g.cs");
        Assert.Contains("global::Prism.Commands.AsyncDelegateCommand", commandSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    [Fact]
    public void AsyncDelegateCommand_with_Catch_compiles()
    {
        const string source = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [AsyncDelegateCommand(Catch = nameof(OnError))]
                private async Task SubmitAsync()
                {
                    await Task.CompletedTask;
                }

                private void OnError(Exception ex) { }
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource commandSource = AssertSingleHintNameEnding(output, ".SubmitCommand.g.cs");
        Assert.Contains("Catch", commandSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    #endregion

    #region ObservesProperty

    [Fact]
    public void ObservesProperty_generates_chain_that_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                private bool _isReady;

                [DelegateCommand(CanExecute = nameof(CanGo))]
                [ObservesProperty(nameof(IsReady))]
                private void Go() { }

                private bool CanGo() => IsReady;
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource commandSource = AssertSingleHintNameEnding(output, ".GoCommand.g.cs");
        Assert.Contains("ObservesProperty", commandSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    #endregion

    #region NotifyCanExecuteChangedFor

    [Fact]
    public void NotifyCanExecuteChangedFor_generates_RaiseCanExecuteChanged()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
                private string _text = "";

                [DelegateCommand(CanExecute = nameof(CanSave))]
                private void Save() { }

                private bool CanSave() => !string.IsNullOrEmpty(Text);
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource textSource = AssertSingleHintNameEnding(output, ".Text.g.cs");
        Assert.Contains("RaiseCanExecuteChanged", textSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    #endregion

    #region BindableBase

    [Fact]
    public void BindableBase_attribute_generates_INotifyPropertyChanged_on_plain_class()
    {
        const string source = """
            #nullable enable
            using Prism.SourceGenerators;

            namespace Demo;

            [BindableBase]
            public partial class Vm
            {
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        Assert.Contains(output.GeneratedSources, s =>
            s.Source.Contains("INotifyPropertyChanged"));

        AssertOutputCompiles(output, source);
    }

    #endregion

    #region Validation (NotifyDataErrorInfo)

    [Fact]
    public void NotifyDataErrorInfo_field_target_compiles_with_real_BindableValidator()
    {
        const string source = """
            #nullable enable
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource nameSource = AssertSingleHintNameEnding(output, ".Name.g.cs");
        Assert.Contains("ValidateProperty(value, nameof(Name))", nameSource.Source);

        AssertOutputCompiles(output, source);
    }

    [Fact]
    public void NotifyDataErrorInfo_partial_property_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                public partial string Email { get; set; }
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, LanguageVersion.Preview);

        AssertNoPsgErrors(output);

        GeneratedSource emailSource = AssertSingleHintNameEnding(output, ".Email.g.cs");
        Assert.Contains("ValidateProperty(value, nameof(Email))", emailSource.Source);

        AssertOutputCompiles(output, source, languageVersion: LanguageVersion.Preview);
    }

    [Fact]
    public void NotifyDataErrorInfo_partial_property_with_DataAnnotations_compiles()
    {
        const string source = """
            #nullable enable
            using System.ComponentModel.DataAnnotations;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                [Required]
                [EmailAddress]
                public partial string Email { get; set; } = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, LanguageVersion.Preview);

        AssertNoPsgErrors(output);

        GeneratedSource emailSource = AssertSingleHintNameEnding(output, ".Email.g.cs");
        Assert.DoesNotContain("[global::System.ComponentModel.DataAnnotations.RequiredAttribute]", emailSource.Source);
        Assert.DoesNotContain("[global::System.ComponentModel.DataAnnotations.EmailAddressAttribute]", emailSource.Source);
        Assert.Contains("ValidateProperty(value, nameof(Email))", emailSource.Source);

        AssertOutputCompiles(output, source, languageVersion: LanguageVersion.Preview);
    }

    [Fact]
    public void NotifyDataErrorInfo_class_level_applies_to_all_properties()
    {
        const string source = """
            #nullable enable
            using Prism.SourceGenerators;

            namespace Demo;

            [NotifyDataErrorInfo]
            public partial class Vm : BindableValidator
            {
                [ObservableProperty]
                private string _first = "";

                [ObservableProperty]
                private string _last = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource firstSource = AssertSingleHintNameEnding(output, ".First.g.cs");
        GeneratedSource lastSource = AssertSingleHintNameEnding(output, ".Last.g.cs");

        Assert.Contains("ValidateProperty", firstSource.Source);
        Assert.Contains("ValidateProperty", lastSource.Source);

        AssertOutputCompiles(output, source);
    }

    [Fact]
    public void NotifyDataErrorInfo_on_non_validator_reports_PSG5001_and_compiles()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG5001");

        GeneratedSource nameSource = AssertSingleHintNameEnding(output, ".Name.g.cs");
        Assert.DoesNotContain("ValidateProperty", nameSource.Source);

        AssertOutputCompiles(output, source);
    }

    #endregion

    #region INotifyPropertyChanging

    [Fact]
    public void ObservableProperty_emits_PropertyChanging_support()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                private string _title = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource titleSource = AssertSingleHintNameEnding(output, ".Title.g.cs");
        Assert.Contains("OnTitleChanging", titleSource.Source);

        AssertOutputCompiles(output, source);
    }

    #endregion

    #region Combined Scenarios

    [Fact]
    public void Full_ViewModel_with_all_features_compiles()
    {
        const string source = """
            #nullable enable
            using System;
            using System.Threading.Tasks;
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class MainViewModel : BindableBase
            {
                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
                private string _firstName = "";

                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                private string _lastName = "";

                [ObservableProperty]
                private bool _isBusy;

                public string FullName => $"{FirstName} {LastName}";

                [DelegateCommand(CanExecute = nameof(CanSave))]
                [ObservesProperty(nameof(FirstName))]
                private void Save() { }

                [AsyncDelegateCommand(CanExecute = nameof(CanLoad), EnableParallelExecution = true)]
                private async Task LoadAsync()
                {
                    await Task.CompletedTask;
                }

                private bool CanSave() => !string.IsNullOrEmpty(FirstName);
                private bool CanLoad() => !IsBusy;
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        AssertSingleHintNameEnding(output, ".FirstName.g.cs");
        AssertSingleHintNameEnding(output, ".LastName.g.cs");
        AssertSingleHintNameEnding(output, ".IsBusy.g.cs");
        AssertSingleHintNameEnding(output, ".SaveCommand.g.cs");
        AssertSingleHintNameEnding(output, ".LoadCommand.g.cs");

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    [Fact]
    public void Validation_ViewModel_with_commands_compiles()
    {
        const string source = """
            #nullable enable
            using System.Threading.Tasks;
            using Prism.SourceGenerators;

            namespace Demo;

            [NotifyDataErrorInfo]
            public partial class FormViewModel : BindableValidator
            {
                [ObservableProperty]
                [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
                private string _username = "";

                [ObservableProperty]
                private string _email = "";

                [DelegateCommand(CanExecute = nameof(CanSubmit))]
                private void Submit() { }

                private bool CanSubmit() => !string.IsNullOrEmpty(Username);
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        GeneratedSource usernameSource = AssertSingleHintNameEnding(output, ".Username.g.cs");
        Assert.Contains("ValidateProperty", usernameSource.Source);
        Assert.Contains("RaiseCanExecuteChanged", usernameSource.Source);

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    [Fact]
    public void Multiple_ViewModels_in_same_compilation_compile()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class VmA : BindableBase
            {
                [ObservableProperty]
                private string _title = "";
            }

            public partial class VmB : BindableBase
            {
                [ObservableProperty]
                private int _count;

                [DelegateCommand]
                private void Increment() { }
            }

            [BindableBase]
            public partial class VmC
            {
                [ObservableProperty]
                private double _value;
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source, includeBclCommands: true);

        AssertNoPsgErrors(output);

        AssertSingleHintNameEnding(output, ".Title.g.cs");
        AssertSingleHintNameEnding(output, ".Count.g.cs");
        AssertSingleHintNameEnding(output, ".IncrementCommand.g.cs");
        AssertSingleHintNameEnding(output, ".Value.g.cs");

        AssertOutputCompiles(output, source, includeBclCommands: true);
    }

    [Fact]
    public void Attribute_forwarding_compiles_with_real_types()
    {
        const string source = """
            #nullable enable
            using Prism.Mvvm;
            using Prism.SourceGenerators;

            namespace Demo;

            public partial class Vm : BindableBase
            {
                [ObservableProperty]
                [property: System.ComponentModel.DataAnnotations.Required]
                [property: System.ComponentModel.DataAnnotations.MaxLength(100)]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = RunAllGenerators(source);

        AssertNoPsgErrors(output);

        GeneratedSource nameSource = AssertSingleHintNameEnding(output, ".Name.g.cs");
        Assert.Contains("Required", nameSource.Source);
        Assert.Contains("MaxLength", nameSource.Source);

        AssertOutputCompiles(output, source);
    }

    #endregion

    #region Helpers

    private static GeneratorRunOutput RunAllGenerators(
        string source,
        LanguageVersion languageVersion = LanguageVersion.CSharp12,
        bool includeBclCommands = false)
    {
        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        IEnumerable<MetadataReference> references = BuildReferences(includeBclCommands);

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "IntegrationTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<IIncrementalGenerator> generators =
            ImmutableArray.Create<IIncrementalGenerator>(
                new ObservablePropertyGenerator(),
                new PropertyChangingGenerator(),
                new DelegateCommandGenerator(),
                new BindableBaseGenerator(),
                new ContainerRegistryRegistrationGenerator());

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: generators.Select(static g => g.AsSourceGenerator()),
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> driverDiagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        foreach (GeneratorRunResult gr in runResult.Results)
        {
            if (gr.Exception is not null)
                throw gr.Exception;
        }

        ImmutableArray<Diagnostic> generatorDiagnostics = runResult.Diagnostics
            .AddRange(runResult.Results.SelectMany(static r => r.Diagnostics));

        ImmutableArray<Diagnostic> compilationErrors = outputCompilation
            .GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        ImmutableArray<GeneratedSource> generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .OrderBy(static item => item.HintName, StringComparer.Ordinal)
            .Select(static item => new GeneratedSource(item.HintName, item.SourceText.ToString()))
            .ToImmutableArray();

        return new GeneratorRunOutput(
            generatedSources,
            generatorDiagnostics.AddRange(driverDiagnostics).AddRange(compilationErrors));
    }

    private static void AssertOutputCompiles(
        GeneratorRunOutput output,
        string source,
        LanguageVersion languageVersion = LanguageVersion.CSharp12,
        bool includeBclCommands = false)
    {
        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(languageVersion);

        List<SyntaxTree> trees = new()
        {
            CSharpSyntaxTree.ParseText(source, parseOptions)
        };

        foreach (GeneratedSource gs in output.GeneratedSources)
        {
            trees.Add(CSharpSyntaxTree.ParseText(gs.Source, parseOptions));
        }

        CSharpCompilation finalCompilation = CSharpCompilation.Create(
            assemblyName: "IntegrationTests.Verify",
            syntaxTrees: trees,
            references: BuildReferences(includeBclCommands),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<Diagnostic> errors = finalCompilation
            .GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        Assert.True(
            errors.IsEmpty,
            "Generated code does not compile:\n"
                + string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));
    }

    private static void AssertNoPsgErrors(GeneratorRunOutput output)
    {
        Diagnostic[] psgErrors = output.Diagnostics
            .Where(d => d.Id.StartsWith("PSG", StringComparison.Ordinal)
                        && d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.True(
            psgErrors.Length == 0,
            "Unexpected PSG errors: "
                + string.Join("; ", psgErrors.Select(d => $"{d.Id}: {d.GetMessage()}")));
    }

    private static GeneratedSource AssertSingleHintNameEnding(GeneratorRunOutput output, string suffix)
    {
        return Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(suffix, StringComparison.Ordinal)));
    }

    private static IEnumerable<MetadataReference> BuildReferences(bool includeBclCommands)
    {
        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is unavailable.");

        IEnumerable<MetadataReference> platform = trustedPlatformAssemblies
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(static path => MetadataReference.CreateFromFile(path));

        string mvvmCore = Path.Combine(AppContext.BaseDirectory, "MvvmAIO.Prism.Core.dll");
        if (!File.Exists(mvvmCore))
            throw new InvalidOperationException($"Required test reference not found: {mvvmCore}");

        string prismDll = typeof(BindableBase).Assembly.Location;
        if (!File.Exists(prismDll))
            throw new InvalidOperationException($"Prism.Core assembly not found: {prismDll}");

        IEnumerable<MetadataReference> refs = platform
            .Append(MetadataReference.CreateFromFile(mvvmCore))
            .Append(MetadataReference.CreateFromFile(prismDll));

        if (!includeBclCommands)
            return refs;

        string configuration =
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        string bcl = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "Prism.Bcl.Commands", "bin", configuration, "netstandard2.0",
                "MvvmAIO.Prism.Bcl.Commands.dll"));

        if (!File.Exists(bcl))
            throw new InvalidOperationException(
                $"MvvmAIO.Prism.Bcl.Commands.dll not found at repo build output ({bcl}). Build Prism.Bcl.Commands first.");

        return refs.Append(MetadataReference.CreateFromFile(bcl));
    }

    private sealed record GeneratedSource(string HintName, string Source);

    private sealed record GeneratorRunOutput(
        ImmutableArray<GeneratedSource> GeneratedSources,
        ImmutableArray<Diagnostic> Diagnostics);

    #endregion
}
