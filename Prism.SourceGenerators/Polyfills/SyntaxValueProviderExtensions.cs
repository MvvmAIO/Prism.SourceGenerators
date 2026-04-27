// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !ROSLYN_4_3_1_OR_GREATER

using System;
using System.Collections.Immutable;
using System.Threading;

using Prism.SourceGenerators.Extensions;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Extension methods for the <see cref="SyntaxValueProvider"/> type.
/// </summary>
internal static class SyntaxValueProviderExtensions
{
    /// <summary>
    /// Creates an <see cref="IncrementalValuesProvider{T}"/> that can provide a transform over all <see
    /// cref="SyntaxNode"/>s if that node has an attribute on it that binds to a <see cref="INamedTypeSymbol"/> with the
    /// same fully-qualified metadata as the provided <paramref name="fullyQualifiedMetadataName"/>.
    /// </summary>
    public static IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
        this SyntaxValueProvider syntaxValueProvider,
        string fullyQualifiedMetadataName,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
    {
        return
            syntaxValueProvider
            .CreateSyntaxProvider(
                predicate,
                (context, token) =>
                {
                    ISymbol? symbol = context.SemanticModel.GetDeclaredSymbol(context.Node, token);

                    if (symbol is null)
                    {
                        return null;
                    }

                    if (!symbol.TryGetAttributeWithFullyQualifiedMetadataName(fullyQualifiedMetadataName, out AttributeData? attributeData))
                    {
                        return null;
                    }

                    if (symbol is IMethodSymbol { IsPartialDefinition: false, PartialDefinitionPart: not null })
                    {
                        return null;
                    }

                    GeneratorAttributeSyntaxContext syntaxContext = new(
                        targetNode: context.Node,
                        targetSymbol: symbol,
                        semanticModel: context.SemanticModel,
                        attributes: ImmutableArray.Create(attributeData));

                    return new Option<T>(transform(syntaxContext, token));
                })
            .Where(static item => item is not null)
            .Select(static (item, _) => item!.Value)!;
    }

    /// <summary>
    /// A simple record to wrap a value that might be missing.
    /// </summary>
    /// <typeparam name="T">The type of values to wrap.</typeparam>
    /// <param name="Value">The wrapped value.</param>
    private sealed record Option<T>(T Value);
}

#endif
