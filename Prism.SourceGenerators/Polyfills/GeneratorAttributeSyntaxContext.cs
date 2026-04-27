// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !ROSLYN_4_3_1_OR_GREATER

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A type containing information for a match from <see cref="SyntaxValueProviderExtensions.ForAttributeWithMetadataName"/>.
/// </summary>
internal readonly struct GeneratorAttributeSyntaxContext
{
    /// <summary>
    /// Creates a new <see cref="GeneratorAttributeSyntaxContext"/> instance with the specified parameters.
    /// </summary>
    internal GeneratorAttributeSyntaxContext(
        SyntaxNode targetNode,
        ISymbol targetSymbol,
        SemanticModel semanticModel,
        ImmutableArray<AttributeData> attributes)
    {
        TargetNode = targetNode;
        TargetSymbol = targetSymbol;
        SemanticModel = semanticModel;
        Attributes = attributes;
    }

    /// <summary>
    /// The syntax node the attribute is attached to.
    /// </summary>
    public SyntaxNode TargetNode { get; }

    /// <summary>
    /// The symbol that the attribute is attached to.
    /// </summary>
    public ISymbol TargetSymbol { get; }

    /// <summary>
    /// Semantic model for the file that <see cref="TargetNode"/> is contained within.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// <see cref="AttributeData"/>s for any matching attributes on <see cref="TargetSymbol"/>.
    /// </summary>
    public ImmutableArray<AttributeData> Attributes { get; }
}

#endif
