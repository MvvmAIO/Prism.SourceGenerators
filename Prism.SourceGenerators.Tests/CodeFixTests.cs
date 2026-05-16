using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

using Prism.SourceGenerators.CodeFixes;
using Prism.SourceGenerators.Diagnostics;

using Xunit;

namespace Prism.SourceGenerators.Tests;

public class CodeFixTests
{
    [Fact]
    public Task PSG0001_inserts_partial_on_class_with_observable_property() =>
        AssertFixAsync(
            diagnosticId: "PSG0001",
            source: """
                namespace Demo;

                public class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "";
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "";
                }
                """);

    [Fact]
    public Task PSG0002_inserts_partial_on_class_with_delegate_command() =>
        AssertFixAsync(
            diagnosticId: "PSG0002",
            source: """
                namespace Demo;

                public class Vm : Prism.Mvvm.BindableBase
                {
                    [DelegateCommand]
                    private void Hello() { }
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [DelegateCommand]
                    private void Hello() { }
                }
                """);

    [Fact]
    public Task PSG0003_inserts_partial_on_property_with_observable_property() =>
        AssertFixAsync(
            diagnosticId: "PSG0003",
            source: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    public string Name { get; set; }
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    public partial string Name { get; set; }
                }
                """);

    [Fact]
    public Task PSG0004_inserts_partial_on_class_with_bindable_base() =>
        AssertFixAsync(
            diagnosticId: "PSG0004",
            source: """
                namespace Demo;

                [BindableBase]
                public class Vm
                {
                }
                """,
            expected: """
                namespace Demo;

                [BindableBase]
                public partial class Vm
                {
                }
                """);

    [Fact]
    public Task PSG0002_inserts_partial_on_class_with_async_delegate_command() =>
        AssertFixAsync(
            diagnosticId: "PSG0002",
            source: """
                namespace Demo;

                public class Vm : Prism.Mvvm.BindableBase
                {
                    [AsyncDelegateCommand]
                    private async System.Threading.Tasks.Task RunAsync()
                    {
                        await System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [AsyncDelegateCommand]
                    private async System.Threading.Tasks.Task RunAsync()
                    {
                        await System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                """);

    [Fact]
    public Task PSG0001_inserts_partial_on_sealed_class() =>
        AssertFixAsync(
            diagnosticId: "PSG0001",
            source: """
                namespace Demo;

                public sealed class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "";
                }
                """,
            expected: """
                namespace Demo;

                public sealed partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "";
                }
                """);

    [Fact]
    public Task PSG0003_inserts_partial_on_internal_property() =>
        AssertFixAsync(
            diagnosticId: "PSG0003",
            source: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    internal string Title { get; set; }
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    internal partial string Title { get; set; }
                }
                """);

    [Fact]
    public Task PSG0004_inserts_partial_on_internal_class() =>
        AssertFixAsync(
            diagnosticId: "PSG0004",
            source: """
                namespace Demo;

                [BindableBase]
                internal class Vm
                {
                }
                """,
            expected: """
                namespace Demo;

                [BindableBase]
                internal partial class Vm
                {
                }
                """);

    [Fact]
    public async Task PSG0001_does_not_register_fix_when_class_already_partial()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";
            }
            """;

        ImmutableArray<CodeAction> actions = await GetCodeActionsAsync(source, "PSG0001");

        // The partial keyword is already present, so the analyzer should not even fire — but defensively,
        // ensure no fix is registered (and no exception thrown if we *did* try to apply it).
        Assert.Empty(actions);
    }

    [Fact]
    public async Task PSG0002_does_not_register_fix_when_class_already_partial()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private void Hello() { }
            }
            """;

        ImmutableArray<CodeAction> actions = await GetCodeActionsAsync(source, "PSG0002");
        Assert.Empty(actions);
    }

    [Fact]
    public async Task PSG0003_does_not_register_fix_when_property_already_partial()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public partial string Name { get; set; }
            }
            """;

        ImmutableArray<CodeAction> actions = await GetCodeActionsAsync(source, "PSG0003");
        Assert.Empty(actions);
    }

    [Fact]
    public async Task PSG0004_does_not_register_fix_when_class_already_partial()
    {
        const string source = """
            namespace Demo;

            [BindableBase]
            public partial class Vm
            {
            }
            """;

        ImmutableArray<CodeAction> actions = await GetCodeActionsAsync(source, "PSG0004");
        Assert.Empty(actions);
    }

    [Fact]
    public Task PSG0005_inserts_partial_on_class_with_bindable_validator() =>
        AssertFixAsync(
            diagnosticId: "PSG0005",
            source: """
                namespace Demo;

                [BindableValidator]
                public class Vm
                {
                }
                """,
            expected: """
                namespace Demo;

                [BindableValidator]
                public partial class Vm
                {
                }
                """);

    [Fact]
    public Task PSG0005_inserts_partial_on_internal_class_with_bindable_validator() =>
        AssertFixAsync(
            diagnosticId: "PSG0005",
            source: """
                namespace Demo;

                [BindableValidator]
                internal class Vm
                {
                }
                """,
            expected: """
                namespace Demo;

                [BindableValidator]
                internal partial class Vm
                {
                }
                """);

    [Fact]
    public async Task PSG0005_does_not_register_fix_when_class_already_partial()
    {
        const string source = """
            namespace Demo;

            [BindableValidator]
            public partial class Vm
            {
            }
            """;

        ImmutableArray<CodeAction> actions = await GetCodeActionsAsync(source, "PSG0005");
        Assert.Empty(actions);
    }

    [Fact]
    public Task PSG0001_and_PSG0002_on_same_class_both_fixable()
    {
        // Class has both [ObservableProperty] and [DelegateCommand] → PSG0001 + PSG0002
        // Fixing PSG0001 inserts partial, which also resolves PSG0002
        return AssertFixAsync(
            diagnosticId: "PSG0001",
            source: """
                namespace Demo;

                public class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "";

                    [DelegateCommand]
                    private void Save() { }
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "";

                    [DelegateCommand]
                    private void Save() { }
                }
                """);
    }

    private static async Task AssertFixAsync(string diagnosticId, string source, string expected)
    {
        Document document = CreateDocument(source);
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(document, diagnosticId);
        Diagnostic diagnostic = Assert.Single(diagnostics);

        ImmutableArray<CodeAction>.Builder actions = ImmutableArray.CreateBuilder<CodeAction>();
        CodeFixContext context = new(
            document,
            diagnostic,
            (action, _) => actions.Add(action),
            CancellationToken.None);

        MakePartialCodeFixProvider provider = new();
        await provider.RegisterCodeFixesAsync(context);

        Assert.NotEmpty(actions);
        CodeAction action = actions[0];

        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation applyChanges = operations.OfType<ApplyChangesOperation>().Single();

        Document fixedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        fixedDocument = await Formatter.FormatAsync(fixedDocument);

        SourceText resultText = await fixedDocument.GetTextAsync();
        string actual = resultText.ToString();

        // Strip the prepended 'using Prism.SourceGenerators;' line that CreateDocument added.
        string actualUserPortion = StripFirstLineIfMatch(actual, "using Prism.SourceGenerators;");

        Assert.Equal(Normalize(expected), Normalize(actualUserPortion));
    }

    private static string StripFirstLineIfMatch(string text, string prefix)
    {
        text = text.Replace("\r\n", "\n");
        if (text.StartsWith(prefix + "\n", System.StringComparison.Ordinal))
        {
            return text.Substring(prefix.Length + 1);
        }
        return text;
    }

    private static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(string source, string diagnosticId)
    {
        Document document = CreateDocument(source);
        ImmutableArray<Diagnostic> diagnostics = await GetAnalyzerDiagnosticsAsync(document, diagnosticId);

        if (diagnostics.IsEmpty)
        {
            return ImmutableArray<CodeAction>.Empty;
        }

        ImmutableArray<CodeAction>.Builder actions = ImmutableArray.CreateBuilder<CodeAction>();
        foreach (Diagnostic diagnostic in diagnostics)
        {
            CodeFixContext context = new(
                document,
                diagnostic,
                (action, _) => actions.Add(action),
                CancellationToken.None);

            MakePartialCodeFixProvider provider = new();
            await provider.RegisterCodeFixesAsync(context);
        }

        return actions.ToImmutable();
    }

    private static Document CreateDocument(string userSource)
    {
        // Wrap the user source with stub Prism types so the analyzer sees attribute symbols.
        const string preamble = """
            using System;
            namespace Prism.Mvvm
            {
                public abstract class BindableBase
                {
                    protected void RaisePropertyChanged(string? propertyName = null) { }
                }
            }
            """;

        // Prepend the using directives so the user source can use unqualified attribute names.
        string userSourceWithUsings = "using Prism.SourceGenerators;\n" + userSource;

        AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();

        const string attributesStub = """
            namespace Prism.SourceGenerators
            {
                [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
                public sealed class ObservablePropertyAttribute : System.Attribute { }

                [System.AttributeUsage(System.AttributeTargets.Method)]
                public sealed class DelegateCommandAttribute : System.Attribute { }

                [System.AttributeUsage(System.AttributeTargets.Method)]
                public sealed class AsyncDelegateCommandAttribute : System.Attribute { }

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class BindableBaseAttribute : System.Attribute { }

                [System.AttributeUsage(System.AttributeTargets.Class)]
                public sealed class BindableValidatorAttribute : System.Attribute { }
            }
            """;

        ProjectInfo projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: "CodeFixTests",
            assemblyName: "CodeFixTests",
            language: LanguageNames.CSharp,
            compilationOptions: new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            metadataReferences: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.INotifyPropertyChanged).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.PropertyChangedEventArgs).Assembly.Location),
            });

        workspace.AddProject(projectInfo);
        workspace.AddDocument(projectId, "Preamble.cs", SourceText.From(preamble));
        workspace.AddDocument(projectId, "Attributes.cs", SourceText.From(attributesStub));
        Document userDocument = workspace.AddDocument(projectId, "User.cs", SourceText.From(userSourceWithUsings));

        return userDocument;
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(Document document, string diagnosticId)
    {
        Project project = document.Project;
        Compilation compilation = (await project.GetCompilationAsync())!;
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new PrismAttributeUsageAnalyzer()));

        ImmutableArray<Diagnostic> all = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        SyntaxTree userTree = (await document.GetSyntaxTreeAsync())!;
        return all
            .Where(d => d.Id == diagnosticId && d.Location.SourceTree == userTree)
            .ToImmutableArray();
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n").Trim();
    }
}
