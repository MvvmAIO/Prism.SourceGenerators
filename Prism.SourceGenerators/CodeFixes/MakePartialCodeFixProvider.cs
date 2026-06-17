// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
/// Provides a code fix for PSG0001..PSG0005: insert the <c>partial</c> modifier on the offending type
/// or property declaration.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakePartialCodeFixProvider))]
[Shared]
public sealed class MakePartialCodeFixProvider : CodeFixProvider
{
    private const string PSG0001 = "PSG0001";
    private const string PSG0002 = "PSG0002";
    private const string PSG0003 = "PSG0003";
    private const string PSG0004 = "PSG0004";
    private const string PSG0005 = "PSG0005";
    private const string PSG0007 = "PSG0007";
    private const string PSG0008 = "PSG0008";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(PSG0001, PSG0002, PSG0003, PSG0004, PSG0005, PSG0007, PSG0008);

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
            SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            if (node is null)
            {
                continue;
            }

            switch (diagnostic.Id)
            {
                case PSG0001:
                case PSG0002:
                case PSG0004:
                case PSG0005:
                case PSG0007:
                case PSG0008:
                {
                    TypeDeclarationSyntax? type = FindEnclosingTypeDeclaration(node);
                    if (type is null || HasPartialModifier(type.Modifiers))
                    {
                        continue;
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: $"Add 'partial' modifier to '{type.Identifier.Text}'",
                            createChangedDocument: ct => AddPartialToTypeAsync(context.Document, type, ct),
                            equivalenceKey: $"AddPartial:Type:{diagnostic.Id}"),
                        diagnostic);
                    break;
                }

                case PSG0003:
                {
                    PropertyDeclarationSyntax? property = FindEnclosingPropertyDeclaration(node);
                    if (property is null || HasPartialModifier(property.Modifiers))
                    {
                        continue;
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: $"Add 'partial' modifier to property '{property.Identifier.Text}'",
                            createChangedDocument: ct => AddPartialToPropertyAsync(context.Document, property, ct),
                            equivalenceKey: $"AddPartial:Property:{diagnostic.Id}"),
                        diagnostic);
                    break;
                }
            }
        }
    }

    private static TypeDeclarationSyntax? FindEnclosingTypeDeclaration(SyntaxNode start)
    {
        for (SyntaxNode? current = start; current is not null; current = current.Parent)
        {
            if (current is TypeDeclarationSyntax type)
            {
                return type;
            }
        }
        return null;
    }

    private static PropertyDeclarationSyntax? FindEnclosingPropertyDeclaration(SyntaxNode start)
    {
        for (SyntaxNode? current = start; current is not null; current = current.Parent)
        {
            if (current is PropertyDeclarationSyntax property)
            {
                return property;
            }
        }
        return null;
    }

    private static bool HasPartialModifier(SyntaxTokenList modifiers) =>
        modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));

    private static async Task<Document> AddPartialToTypeAsync(
        Document document,
        TypeDeclarationSyntax type,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        TypeDeclarationSyntax updated = type.WithModifiers(InsertPartialModifier(type.Modifiers, type.Keyword));
        SyntaxNode newRoot = root.ReplaceNode(type, updated.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> AddPartialToPropertyAsync(
        Document document,
        PropertyDeclarationSyntax property,
        CancellationToken cancellationToken)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // For a property like 'public string Name { get; set; }', insert 'partial' immediately before the type.
        SyntaxToken partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        PropertyDeclarationSyntax updated;
        if (property.Modifiers.Count == 0)
        {
            // No modifiers (rare): attach the leading trivia of the type to the new modifier list.
            TypeSyntax originalType = property.Type;
            partialToken = partialToken.WithLeadingTrivia(originalType.GetLeadingTrivia());
            TypeSyntax newType = originalType.WithLeadingTrivia();
            updated = property
                .WithModifiers(SyntaxFactory.TokenList(partialToken))
                .WithType(newType);
        }
        else
        {
            updated = property.WithModifiers(property.Modifiers.Add(partialToken.WithLeadingTrivia()));
        }

        SyntaxNode newRoot = root.ReplaceNode(property, updated.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxTokenList InsertPartialModifier(SyntaxTokenList existing, SyntaxToken keyword)
    {
        SyntaxToken partialToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        if (existing.Count == 0)
        {
            // No modifiers (e.g. `class Foo` without 'public'/'internal'): attach leading trivia of the
            // type keyword onto the new partial token.
            return SyntaxFactory.TokenList(partialToken.WithLeadingTrivia(keyword.LeadingTrivia));
        }

        // Standard: insert just before the type keyword (i.e. at the end of the modifier list).
        return existing.Add(partialToken.WithLeadingTrivia());
    }
}
