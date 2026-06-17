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
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using Prism.SourceGenerators.CodeFixes;
using Prism.SourceGenerators.Diagnostics;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public class PartialPropertyCodeFixTests
{
    [Fact]
    public Task PSG6001_converts_field_to_partial_property() =>
        AssertFixAsync(
            diagnosticId: "PSG6001",
            languageVersion: LanguageVersion.Preview,
            source: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private string _name = "hello";
                }
                """,
            expected: """
                namespace Demo;

                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    public partial string Name { get; set; } = "hello";
                }
                """);

    private static async Task AssertFixAsync(
        string diagnosticId,
        string source,
        string expected,
        LanguageVersion languageVersion)
    {
        Document document = CreateDocument(source, languageVersion);
        ImmutableArray<CodeAction> actions = await GetCodeActionsAsync(document, diagnosticId);
        CodeAction action = Assert.Single(actions);

        ImmutableArray<CodeActionOperation> operations = await action.GetOperationsAsync(CancellationToken.None);
        ApplyChangesOperation applyChanges = operations.OfType<ApplyChangesOperation>().Single();

        Document fixedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)!;
        fixedDocument = await Formatter.FormatAsync(fixedDocument);
        string actual = StripUserPortion(await fixedDocument.GetTextAsync());
        Assert.Equal(Normalize(expected), Normalize(actual));
    }

    private static async Task<ImmutableArray<CodeAction>> GetCodeActionsAsync(Document document, string diagnosticId)
    {
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

            UsePartialPropertyForObservablePropertyCodeFixProvider provider = new();
            await provider.RegisterCodeFixesAsync(context);
        }

        return actions.ToImmutable();
    }

    private static Document CreateDocument(string userSource, LanguageVersion languageVersion)
    {
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

        string userSourceWithUsings = "using Prism.SourceGenerators;\n" + userSource;

        const string attributesStub = """
            namespace Prism.SourceGenerators
            {
                [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
                public sealed class ObservablePropertyAttribute : System.Attribute { }
            }
            """;

        AdhocWorkspace workspace = new();
        ProjectId projectId = ProjectId.CreateNewId();

        ProjectInfo projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name: "PartialPropertyCodeFixTests",
            assemblyName: "PartialPropertyCodeFixTests",
            language: LanguageNames.CSharp,
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(languageVersion),
            metadataReferences: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            });

        workspace.AddProject(projectInfo);
        workspace.AddDocument(projectId, "Preamble.cs", SourceText.From(preamble));
        workspace.AddDocument(projectId, "Attributes.cs", SourceText.From(attributesStub));
        return workspace.AddDocument(projectId, "User.cs", SourceText.From(userSourceWithUsings));
    }

    private static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(Document document, string diagnosticId)
    {
        Compilation compilation = (await document.Project.GetCompilationAsync())!;
        CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new UsePartialPropertyForObservablePropertyAnalyzer()));

        ImmutableArray<Diagnostic> all = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        SyntaxTree? userTree = await document.GetSyntaxTreeAsync();
        return all
            .Where(d => d.Id == diagnosticId && d.Location.SourceTree == userTree)
            .ToImmutableArray();
    }

    private static string StripUserPortion(SourceText text)
    {
        string full = text.ToString().Replace("\r\n", "\n");
        const string marker = "using Prism.SourceGenerators;\n";
        int index = full.IndexOf(marker, System.StringComparison.Ordinal);
        if (index >= 0)
        {
            return full[(index + marker.Length)..];
        }

        return full;
    }

    private static string Normalize(string value) => value.Replace("\r\n", "\n").Trim();
}
