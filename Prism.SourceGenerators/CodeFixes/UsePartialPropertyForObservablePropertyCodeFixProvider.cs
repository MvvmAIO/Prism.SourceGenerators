using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace Prism.SourceGenerators.CodeFixes;

/// <summary>
/// Converts a private field with <c>[ObservableProperty]</c> into a partial property (C# 13+).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UsePartialPropertyForObservablePropertyCodeFixProvider))]
[Shared]
public sealed class UsePartialPropertyForObservablePropertyCodeFixProvider : CodeFixProvider
{
    private const string DiagnosticId = "PSG6001";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(DiagnosticId);

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        foreach (Diagnostic diagnostic in context.Diagnostics)
        {
            if (diagnostic.Id != DiagnosticId)
            {
                continue;
            }

            SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            VariableDeclaratorSyntax? variable = node as VariableDeclaratorSyntax
                ?? node?.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();

            if (variable?.Parent is not VariableDeclarationSyntax declaration
                || declaration.Parent is not FieldDeclarationSyntax fieldDeclaration)
            {
                continue;
            }

            string propertyName = GetPropertyName(variable.Identifier.Text);
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"Convert '{variable.Identifier.Text}' to partial property '{propertyName}'",
                    createChangedDocument: ct => ConvertToPartialPropertyAsync(context.Document, fieldDeclaration, variable, propertyName, ct),
                    equivalenceKey: $"ConvertToPartialProperty:{propertyName}"),
                diagnostic);
        }
    }

    private static async Task<Document> ConvertToPartialPropertyAsync(
        Document document,
        FieldDeclarationSyntax fieldDeclaration,
        VariableDeclaratorSyntax variable,
        string propertyName,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        SyntaxList<AttributeListSyntax> attributeLists = fieldDeclaration.AttributeLists;
        TypeSyntax type = fieldDeclaration.Declaration.Type;
        EqualsValueClauseSyntax? initializer = variable.Initializer;

        PropertyDeclarationSyntax property = SyntaxFactory.PropertyDeclaration(type, SyntaxFactory.Identifier(propertyName))
            .WithAttributeLists(attributeLists)
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space)))
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
            {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            })))
            .WithInitializer(initializer)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithAdditionalAnnotations(Formatter.Annotation);

        SyntaxNode newRoot = root.ReplaceNode(fieldDeclaration, property);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string GetPropertyName(string fieldName)
    {
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
        {
            return char.ToUpperInvariant(fieldName[2]) + fieldName[2..];
        }

        if (fieldName.StartsWith('_') && fieldName.Length > 1)
        {
            return char.ToUpperInvariant(fieldName[1]) + fieldName[2..];
        }

        return char.ToUpperInvariant(fieldName[0]) + fieldName[1..];
    }
}
