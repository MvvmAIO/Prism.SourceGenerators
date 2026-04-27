// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Prism.SourceGenerators.Helpers;

/// <summary>
/// Factory for generating type syntax from symbols.
/// </summary>
internal static class TypeSyntaxFactory
{
    /// <summary>
    /// Creates a <see cref="TypeSyntax"/> from an <see cref="ITypeSymbol"/>.
    /// Fully qualifies the type with global:: prefix.
    /// </summary>
    public static TypeSyntax CreateTypeSyntax(ITypeSymbol symbol)
    {
        // Handle arrays
        if (symbol is IArrayTypeSymbol arrayType)
        {
            return SyntaxFactory.ArrayType(CreateTypeSyntax(arrayType.ElementType))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(
                        SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression()))));
        }

        // Predefined types
        if (symbol.SpecialType == SpecialType.System_Object)
        {
            return SyntaxFactory.PredefinedType(
                SyntaxFactory.Token(SyntaxKind.ObjectKeyword));
        }

        if (symbol is INamedTypeSymbol named)
        {
            // Create the simple name (generic or identifier)
            SimpleNameSyntax simpleName = CreateSimpleName(named);

            // Build namespace parts
            var nsParts = CollectNamespaceParts(named.ContainingNamespace);

            // Build containing type parts
            var typeParts = CollectContainingTypeParts(named);
            typeParts.Add(simpleName);

            // Build resulting name starting with global alias if namespace exists
            NameSyntax? resultName = null;
            
            if (nsParts.Count > 0)
            {
                resultName = SyntaxFactory.AliasQualifiedName(
                    SyntaxFactory.IdentifierName("global"),
                    SyntaxFactory.IdentifierName(nsParts[0]));
                
                for (int i = 1; i < nsParts.Count; i++)
                {
                    resultName = SyntaxFactory.QualifiedName(
                        resultName,
                        SyntaxFactory.IdentifierName(nsParts[i]));
                }
            }

            // Append type parts
            foreach (var part in typeParts)
            {
                resultName = resultName is null
                    ? (NameSyntax)part
                    : SyntaxFactory.QualifiedName(resultName, part);
            }

            return (TypeSyntax)(resultName ?? simpleName);
        }

        // Fallback to parse if unknown symbol kind
        return SyntaxFactory.ParseTypeName(
            symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    /// <summary>
    /// Creates a qualified name with global:: prefix for a fully qualified name.
    /// Example: global::System.Windows.DependencyProperty
    /// </summary>
    public static NameSyntax CreateGlobalQualifiedName(params string[] parts)
    {
        if (parts.Length == 0)
        {
            throw new ArgumentException("Parts cannot be empty", nameof(parts));
        }

        NameSyntax name = SyntaxFactory.AliasQualifiedName(
            SyntaxFactory.IdentifierName("global"),
            SyntaxFactory.IdentifierName(parts[0]));

        for (int i = 1; i < parts.Length; i++)
        {
            name = SyntaxFactory.QualifiedName(
                name,
                SyntaxFactory.IdentifierName(parts[i]));
        }

        return name;
    }

    private static SimpleNameSyntax CreateSimpleName(INamedTypeSymbol named)
    {
        if (named.TypeArguments.Length > 0)
        {
            var args = SyntaxFactory.SeparatedList(
                named.TypeArguments.Select(ta => CreateTypeSyntax(ta)));
            
            string baseName = named.MetadataName.Split('`')[0];
            
            return SyntaxFactory.GenericName(SyntaxFactory.Identifier(baseName))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(args));
        }
        else
        {
            return SyntaxFactory.IdentifierName(named.MetadataName);
        }
    }

    private static List<string> CollectNamespaceParts(INamespaceSymbol? nsSymbol)
    {
        var parts = new List<string>();
        var curNs = nsSymbol;
        
        while (curNs != null && !curNs.IsGlobalNamespace)
        {
            parts.Add(curNs.Name);
            curNs = curNs.ContainingNamespace;
        }
        
        parts.Reverse();
        return parts;
    }

    private static List<SimpleNameSyntax> CollectContainingTypeParts(INamedTypeSymbol named)
    {
        var typeParts = new List<SimpleNameSyntax>();
        var containingType = named.ContainingType;
        var stack = new Stack<INamedTypeSymbol>();
        
        while (containingType != null)
        {
            stack.Push(containingType);
            containingType = containingType.ContainingType;
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            typeParts.Add(CreateSimpleName(t));
        }

        return typeParts;
    }
}
