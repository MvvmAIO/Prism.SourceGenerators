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

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

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

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".CountCommand.g.cs")));

        Assert.Contains("async () => await CountAsync()", commandSource.Source);
    }

    [Fact]
    public void AsyncDelegateCommand_attribute_on_Roslyn5000_emits_command()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [AsyncDelegateCommand]
                private async System.Threading.Tasks.Task LoadAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".LoadCommand.g.cs")));

        Assert.Contains("AsyncDelegateCommand", commandSource.Source);
    }

    [Fact]
    public void ValueTask_execute_on_Roslyn5000_emits_AsTask_wrapper()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.ValueTask SaveAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Id is "PSG1001" or "PSG1002"));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".SaveCommand.g.cs")));

        Assert.Contains("() => SaveAsync().AsTask()", commandSource.Source);
    }

    [Fact]
    public void Partial_property_ObservableProperty_on_Roslyn5000_uses_field_keyword()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public partial string Name { get; set; } = "";
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("partial string Name", propertySource.Source);
        Assert.Contains("get => field;", propertySource.Source);
    }

    [Fact]
    public void NotifyPropertyChangedFor_on_Roslyn5000_emits_extra_notification()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                private string _firstName = "";

                public string FullName => FirstName;
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".FirstName.g.cs")));

        Assert.Contains("RaisePropertyChanged(nameof(FullName))", propertySource.Source);
    }

    [Fact]
    public void NotifyCanExecuteChangedFor_on_Roslyn5000_emits_command_refresh()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
                private string _name = "";

                [DelegateCommand(CanExecute = nameof(CanSave))]
                private void Save() { }

                private bool CanSave() => !string.IsNullOrEmpty(Name);
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("SaveCommand?.RaiseCanExecuteChanged()", propertySource.Source);
    }

    [Fact]
    public void BindableBase_attribute_on_Roslyn5000_generates_INPC()
    {
        const string userSource = """
            namespace Demo;

            [BindableBase]
            public partial class LightVm
            {
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        Assert.Contains(
            output.GeneratedSources,
            static s => s.HintName.EndsWith(".BindableBase.g.cs"));
    }

    [Fact]
    public void BindableValidator_NotifyDataErrorInfo_on_Roslyn5000_emits_ValidateProperty()
    {
        const string userSource = """
            namespace Demo;

            public partial class Vm : Prism.SourceGenerators.BindableValidator
            {
                [ObservableProperty]
                [NotifyDataErrorInfo]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d => d.Id == "PSG5001"));

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("ValidateProperty(value, nameof(Name))", propertySource.Source);
    }

    [Fact]
    public void NavigationAware_on_Roslyn5000_generates_INavigationAware_members()
    {
        const string userSource = """
            namespace Demo;

            [NavigationAware]
            public partial class PageVm : Prism.Mvvm.BindableBase
            {
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource generated = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".NavigationAware.g.cs")));

        Assert.Contains("INavigationAware", generated.Source);
        Assert.Contains("Prism.Navigation.Regions", generated.Source);
        Assert.Contains("OnNavigatedToCore", generated.Source);
    }

    [Fact]
    public void DialogAware_on_Roslyn5000_generates_IDialogAware_members()
    {
        const string userSource = """
            namespace Demo;

            [DialogAware(Title = "Confirm")]
            public partial class ConfirmVm : Prism.Mvvm.BindableBase
            {
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource generated = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".DialogAware.g.cs")));

        Assert.Contains("IDialogAware", generated.Source);
        Assert.Contains("RequestClose", generated.Source);
    }

    [Fact]
    public void NavigateCommand_on_Roslyn5000_generates_RequestNavigate_command()
    {
        const string userSource = """
            namespace Demo;

            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                private readonly Prism.Navigation.Regions.IRegionManager _regionManager;

                public ShellVm(Prism.Navigation.Regions.IRegionManager regionManager) => _regionManager = regionManager;

                [NavigateCommand(Region = "Content", Target = "Dashboard")]
                private void GoDashboard() { }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource generated = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".GoDashboardCommand.g.cs")));

        Assert.Contains("RequestNavigate(\"Content\", \"Dashboard\")", generated.Source);
    }

    [Fact]
    public void ShowDialogCommand_on_Roslyn5000_generates_ShowDialog_command()
    {
        const string userSource = """
            namespace Demo;

            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                private readonly Prism.Services.Dialogs.IDialogService _dialogService;

                public ShellVm(Prism.Services.Dialogs.IDialogService dialogService) => _dialogService = dialogService;

                [ShowDialogCommand(Name = "ConfirmDelete")]
                private void ConfirmDelete() { }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Empty(output.Diagnostics.Where(static d =>
            d.Severity >= DiagnosticSeverity.Error &&
            d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource generated = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".ConfirmDeleteCommand.g.cs")));

        Assert.Contains("ShowDialog(\"ConfirmDelete\"", generated.Source);
        Assert.Contains("OnConfirmDeleteDialogClosed", generated.Source);
    }

    [Fact]
    public void Non_partial_class_reports_PSG0001_on_Roslyn5000()
    {
        const string userSource = """
            namespace Demo;

            public class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Contains(output.Diagnostics, static d => d.Id == "PSG0001");
    }

    [Fact]
    public void Non_partial_class_reports_PSG0002_on_Roslyn5000()
    {
        const string userSource = """
            namespace Demo;

            public class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private void Save() { }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Contains(output.Diagnostics, static d => d.Id == "PSG0002");
    }

    [Fact]
    public void NavigateCommand_reports_PSG7001_when_region_manager_missing_on_Roslyn5000()
    {
        const string userSource = """
            namespace Demo;

            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                [NavigateCommand(Region = "Content", Target = "Dashboard")]
                private void GoDashboard() { }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Contains(output.Diagnostics, static d => d.Id == "PSG7001");
    }

    [Fact]
    public void ShowDialogCommand_reports_PSG7101_when_dialog_service_missing_on_Roslyn5000()
    {
        const string userSource = """
            namespace Demo;

            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                [ShowDialogCommand(Name = "ConfirmDelete")]
                private void ConfirmDelete() { }
            }
            """;

        GeneratorRunOutput output = Roslyn5000TestHarness.Run(userSource);

        Assert.Contains(output.Diagnostics, static d => d.Id == "PSG7101");
    }
}

internal static class Roslyn5000TestHarness
{
    internal enum RegionsApi
    {
        Both,
        Prism8Only,
        Prism9Only,
    }

    internal static GeneratorRunOutput Run(
        string userSource,
        RegionsApi regionsApi = RegionsApi.Both)
    {
        string prism9Regions = regionsApi is RegionsApi.Prism8Only
            ? string.Empty
            : """
            namespace Prism.Navigation.Regions
            {
                public sealed class NavigationContext { }

                public interface INavigationAware
                {
                    void OnNavigatedTo(NavigationContext navigationContext);
                    bool IsNavigationTarget(NavigationContext navigationContext);
                    void OnNavigatedFrom(NavigationContext navigationContext);
                }

                public interface IRegionManager
                {
                    void RequestNavigate(string regionName, string target);
                }
            }
            """;

        string prism8Regions = regionsApi is RegionsApi.Prism9Only
            ? string.Empty
            : """
            namespace Prism.Regions
            {
                public sealed class NavigationContext { }

                public interface INavigationAware
                {
                    void OnNavigatedTo(NavigationContext navigationContext);
                    bool IsNavigationTarget(NavigationContext navigationContext);
                    void OnNavigatedFrom(NavigationContext navigationContext);
                }

                public interface IRegionManager
                {
                    void RequestNavigate(string regionName, string target);
                }
            }
            """;

        string harness = """
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

                public class AsyncDelegateCommand
                {
                    public AsyncDelegateCommand(Func<Task> execute) { }
                    public AsyncDelegateCommand(Func<Task> execute, Func<bool> canExecute) { }
                    public AsyncDelegateCommand EnableParallelExecution() => this;
                    public AsyncDelegateCommand CancelAfter(TimeSpan timeout) => this;
                    public AsyncDelegateCommand CancellationTokenSourceFactory(Func<CancellationToken> factory) => this;
                    public AsyncDelegateCommand Catch(Action<Exception> handler) => this;
                    public AsyncDelegateCommand Catch<TException>(Action<TException> handler) where TException : Exception => this;
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
                    public AsyncDelegateCommand<T> Catch<TException>(Action<TException> handler) where TException : Exception => this;
                    public AsyncDelegateCommand<T> ObservesProperty<TProperty>(Func<TProperty> propertyExpression) => this;
                }
            }

            namespace Prism.Mvvm
            {
                public abstract class BindableBase : System.ComponentModel.INotifyPropertyChanged
                {
                    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

                    protected bool SetProperty<T>(ref T storage, T value, string? propertyName = null) => false;

                    protected bool SetProperty<T>(ref T storage, T value, Action? onChanged, string? propertyName = null) => false;

                    protected void RaisePropertyChanged(string? propertyName = null) { }
                }
            }

            namespace Prism.Services.Dialogs
            {
                public interface IDialogParameters { }
                public interface IDialogResult { }

                public interface IDialogAware
                {
                    string Title { get; }
                    event System.Action<IDialogResult>? RequestClose;
                    bool CanCloseDialog();
                    void OnDialogClosed();
                    void OnDialogOpened(IDialogParameters parameters);
                }

                public interface IDialogService
                {
                    void ShowDialog(string name, IDialogParameters? parameters, System.Action<IDialogResult>? callback);
                }
            }
            """ + prism9Regions + prism8Regions + userSource;

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            harness,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Roslyn5000SmokeTests",
            [syntaxTree],
            Roslyn5000MetadataReferences.Get(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        IIncrementalGenerator[] generators =
        [
            new BindableValidatorGenerator(),
            new ObservablePropertyGenerator(),
            new PropertyChangingGenerator(),
            new DelegateCommandGenerator(),
            new BindableBaseGenerator(),
            new ContainerRegistryRegistrationGenerator(),
            new NavigationAwareGenerator(),
            new DialogAwareGenerator(),
            new RegionNavigationGenerator(),
            new DialogServiceCommandGenerator(),
        ];

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators.Select(static g => g.AsSourceGenerator()),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> driverDiagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        foreach (GeneratorRunResult gr in runResult.Results)
        {
            if (gr.Exception is not null)
            {
                throw gr.Exception;
            }
        }

        ImmutableArray<Diagnostic> compilationErrors = outputCompilation
            .GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();

        ImmutableArray<GeneratedSource> generatedSources = runResult.Results
            .SelectMany(static r => r.GeneratedSources)
            .OrderBy(static s => s.HintName, System.StringComparer.Ordinal)
            .Select(static s => new GeneratedSource(s.HintName, s.SourceText.ToString()))
            .ToImmutableArray();

        ImmutableArray<Diagnostic> allDiagnostics = runResult.Diagnostics
            .AddRange(runResult.Results.SelectMany(static r => r.Diagnostics))
            .AddRange(driverDiagnostics)
            .AddRange(compilationErrors);

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
