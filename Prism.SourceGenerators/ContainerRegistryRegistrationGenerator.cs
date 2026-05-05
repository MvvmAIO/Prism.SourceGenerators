using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Extensions;
using Prism.SourceGenerators.Helpers;

namespace Prism.SourceGenerators;

/// <summary>
/// Ordinal alignment with <c>PrismRegistrationLifetime</c> in MvvmAIO.Prism.Core (generator assembly does not reference attribute enum type).
/// Int underlying type matches Roslyn <see cref="Microsoft.CodeAnalysis.TypedConstant"/> enum values passed to <see cref="Enum.IsDefined"/>.
/// </summary>
internal enum PrismRegistrationLifetimeOrdinal
{
    Transient = 0,
    Scoped = 1,
    Singleton = 2,
}

/// <summary>
/// Emits <see cref="PrismRegistrationExtensions.RegisterGeneratedTypes"/> from MvvmAIO.Prism.Core registration attributes,
/// using Prism <c>IContainerRegistry</c> only (Prism 9+ <c>Try*</c> APIs when <c>IfNotRegistered</c> is set).
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class ContainerRegistryRegistrationGenerator : IIncrementalGenerator
{
    /// <summary>Deterministic emission order: navigation, dialogs, then lifetime-based registrations.</summary>
    private const byte SortNavigation = 0;

    private const byte SortDialog = 1;

    private const byte SortSingleton = 2;

    private const byte SortScoped = 3;

    private const byte SortTransient = 4;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Walk the full compilation per CompilationProvider update. CreateSyntaxProvider + Collect
        // did not reliably surface some attributes (e.g. RegisterAttribute) in the test harness;
        // CompilationProvider matches normal GetSemanticModel binding. Prefer ForAttributeWithMetadataName
        // once a stable multi-attribute merge exists (see Devin review).
        IncrementalValueProvider<ImmutableArray<RegistrationStatement>> combined =
            context.CompilationProvider.Select(static (compilation, cancellationToken) =>
                ExtractAllRegistrationStatements(compilation, cancellationToken));

        context.RegisterSourceOutput(combined, static (spc, statements) =>
        {
            if (statements.IsDefaultOrEmpty)
            {
                return;
            }

            spc.AddSource("PrismRegistrationExtensions.g.cs", BuildSource(statements));
        });
    }

    private static ImmutableArray<RegistrationStatement> ExtractAllRegistrationStatements(
        Compilation compilation,
        System.Threading.CancellationToken cancellationToken)
    {
        using ImmutableArrayBuilder<RegistrationStatement> builder =
            ImmutableArrayBuilder<RegistrationStatement>.Rent();

        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(tree);
            SyntaxNode root = tree.GetRoot(cancellationToken);
            foreach (ClassDeclarationSyntax classDeclaration in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                if (classDeclaration.AttributeLists.Count == 0)
                {
                    continue;
                }

                if (semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol typeSymbol)
                {
                    continue;
                }

                builder.AddRange(ExtractStatementsForNamedType(typeSymbol).AsSpan());
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<RegistrationStatement> ExtractStatementsForNamedType(INamedTypeSymbol typeSymbol)
    {
        using ImmutableArrayBuilder<RegistrationStatement> part =
            ImmutableArrayBuilder<RegistrationStatement>.Rent();

        foreach (AttributeData attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is not { } attributeClass)
            {
                continue;
            }

            string meta = attributeClass.GetFullyQualifiedMetadataName();

            if (TryExtractRegistration(typeSymbol, attribute, attributeClass, meta, out RegistrationStatement? statement)
                && statement is not null)
            {
                part.Add(statement.Value);
            }
        }

        return part.ToImmutable();
    }

    private static bool TryExtractRegistration(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        INamedTypeSymbol attributeClass,
        string metadataName,
        out RegistrationStatement? statement)
    {
        statement = null;

        // --- Service / lifetime attributes ---
        if (metadataName is "Prism.SourceGenerators.RegisterTransientAttribute")
        {
            statement = BuildTwoTypeRegistration(
                implementationType,
                attribute,
                serviceType: GetServiceType(attribute, implementationType),
                useTry: GetIfNotRegistered(attribute),
                transient: true,
                scoped: false,
                singleton: false,
                sortGroup: SortTransient);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterTransientAttribute`1")
        {
            ITypeSymbol? svc = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            statement = BuildTwoTypeRegistration(
                implementationType,
                attribute,
                serviceType: svc ?? GetServiceType(attribute, implementationType),
                useTry: GetIfNotRegistered(attribute),
                transient: true,
                scoped: false,
                singleton: false,
                sortGroup: SortTransient);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterScopedAttribute")
        {
            statement = BuildTwoTypeRegistration(
                implementationType,
                attribute,
                serviceType: GetServiceType(attribute, implementationType),
                useTry: GetIfNotRegistered(attribute),
                transient: false,
                scoped: true,
                singleton: false,
                sortGroup: SortScoped);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterScopedAttribute`1")
        {
            ITypeSymbol? svc = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            statement = BuildTwoTypeRegistration(
                implementationType,
                attribute,
                serviceType: svc ?? GetServiceType(attribute, implementationType),
                useTry: GetIfNotRegistered(attribute),
                transient: false,
                scoped: true,
                singleton: false,
                sortGroup: SortScoped);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterSingletonAttribute")
        {
            statement = BuildTwoTypeRegistration(
                implementationType,
                attribute,
                serviceType: GetServiceType(attribute, implementationType),
                useTry: GetIfNotRegistered(attribute),
                transient: false,
                scoped: false,
                singleton: true,
                sortGroup: SortSingleton);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterSingletonAttribute`1")
        {
            ITypeSymbol? svc = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            statement = BuildTwoTypeRegistration(
                implementationType,
                attribute,
                serviceType: svc ?? GetServiceType(attribute, implementationType),
                useTry: GetIfNotRegistered(attribute),
                transient: false,
                scoped: false,
                singleton: true,
                sortGroup: SortSingleton);
            return true;
        }

        // Metadata name may be reported with generic arity (`RegisterAttribute`1`); use prefix match.
        if (metadataName.StartsWith("Prism.SourceGenerators.RegisterAttribute", StringComparison.Ordinal))
        {
            PrismRegistrationLifetimeOrdinal lifetime = GetLifetime(attribute);
            if (metadataName.Contains('`', StringComparison.Ordinal))
            {
                ITypeSymbol? svc = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
                statement = BuildRegisterAttributeWithService(
                    implementationType,
                    attribute,
                    svc ?? GetServiceType(attribute, implementationType),
                    lifetime);
            }
            else
            {
                statement = BuildRegisterAttribute(implementationType, attribute, lifetime);
            }

            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterForNavigationAttribute")
        {
            if (!TryGetTypeNamedArgument(attribute, "ViewModelType", out INamedTypeSymbol? vm) || vm is null)
            {
                return false;
            }

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                SortNavigation,
                $"containerRegistry.RegisterForNavigation<{TypeFq(implementationType)}, {TypeFq(vm)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterForNavigationAttribute`1")
        {
            ITypeSymbol? vm = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            if (vm is not INamedTypeSymbol vmNamed)
            {
                return false;
            }

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                SortNavigation,
                $"containerRegistry.RegisterForNavigation<{TypeFq(implementationType)}, {TypeFq(vmNamed)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogAttribute")
        {
            if (!TryGetTypeNamedArgument(attribute, "ViewModelType", out INamedTypeSymbol? vm) || vm is null)
            {
                return false;
            }

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                SortDialog,
                $"containerRegistry.RegisterDialog<{TypeFq(implementationType)}, {TypeFq(vm)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogAttribute`1")
        {
            ITypeSymbol? vm = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            if (vm is not INamedTypeSymbol vmNamed)
            {
                return false;
            }

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                SortDialog,
                $"containerRegistry.RegisterDialog<{TypeFq(implementationType)}, {TypeFq(vmNamed)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogWindowAttribute")
        {
            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                SortDialog,
                $"containerRegistry.RegisterDialogWindow<{TypeFq(implementationType)}>({Literal(key)});");
            return true;
        }

        return false;
    }

    private static RegistrationStatement? BuildTwoTypeRegistration(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ITypeSymbol? serviceType,
        bool useTry,
        bool transient,
        bool scoped,
        bool singleton,
        byte sortGroup)
    {
        if (serviceType is null)
        {
            // Self-registration (single type argument overloads).
            if (transient)
            {
                return Single("Register", "TryRegister", useTry, sortGroup, implementationType);
            }

            if (scoped)
            {
                return Single("RegisterScoped", "TryRegisterScoped", useTry, sortGroup, implementationType);
            }

            if (singleton)
            {
                return Single("RegisterSingleton", "TryRegisterSingleton", useTry, sortGroup, implementationType);
            }
        }
        else
        {
            if (transient)
            {
                return Pair("Register", "TryRegister", useTry, sortGroup, serviceType, implementationType);
            }

            if (scoped)
            {
                return Pair("RegisterScoped", "TryRegisterScoped", useTry, sortGroup, serviceType, implementationType);
            }

            if (singleton)
            {
                return Pair("RegisterSingleton", "TryRegisterSingleton", useTry, sortGroup, serviceType, implementationType);
            }
        }

        return null;
    }

    private static RegistrationStatement Single(
        string register,
        string tryRegister,
        bool useTry,
        byte sortGroup,
        INamedTypeSymbol impl)
    {
        string method = useTry ? tryRegister : register;
        return new RegistrationStatement(sortGroup, $"containerRegistry.{method}<{TypeFq(impl)}>();");
    }

    private static RegistrationStatement Pair(
        string register,
        string tryRegister,
        bool useTry,
        byte sortGroup,
        ITypeSymbol service,
        INamedTypeSymbol impl)
    {
        string method = useTry ? tryRegister : register;
        return new RegistrationStatement(
            sortGroup,
            $"containerRegistry.{method}<{TypeFq(service)}, {TypeFq(impl)}>();");
    }

    private static RegistrationStatement? BuildRegisterAttribute(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        PrismRegistrationLifetimeOrdinal lifetime)
    {
        bool useTry = GetIfNotRegistered(attribute);
        ITypeSymbol? serviceType = GetServiceType(attribute, implementationType);
        byte sort = lifetime switch
        {
            PrismRegistrationLifetimeOrdinal.Transient => SortTransient,
            PrismRegistrationLifetimeOrdinal.Scoped => SortScoped,
            PrismRegistrationLifetimeOrdinal.Singleton => SortSingleton,
            _ => SortTransient,
        };

        return lifetime switch
        {
            PrismRegistrationLifetimeOrdinal.Transient => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: true, scoped: false, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Scoped => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: true, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Singleton => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: false, singleton: true, sort),
            _ => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: true, scoped: false, singleton: false, SortTransient)
        };
    }

    private static RegistrationStatement? BuildRegisterAttributeWithService(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ITypeSymbol? serviceType,
        PrismRegistrationLifetimeOrdinal lifetime)
    {
        bool useTry = GetIfNotRegistered(attribute);
        byte sort = lifetime switch
        {
            PrismRegistrationLifetimeOrdinal.Transient => SortTransient,
            PrismRegistrationLifetimeOrdinal.Scoped => SortScoped,
            PrismRegistrationLifetimeOrdinal.Singleton => SortSingleton,
            _ => SortTransient,
        };

        return lifetime switch
        {
            PrismRegistrationLifetimeOrdinal.Transient => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: true, scoped: false, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Scoped => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: true, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Singleton => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: false, singleton: true, sort),
            _ => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: true, scoped: false, singleton: false, SortTransient)
        };
    }

    private static ITypeSymbol? GetServiceType(AttributeData attribute, INamedTypeSymbol implementationType)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (!string.Equals(pair.Key, "ServiceType", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pair.Value.Value is INamedTypeSymbol named)
            {
                return named;
            }
        }

        return null;
    }

    private static bool GetIfNotRegistered(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == "IfNotRegistered" && pair.Value.Value is bool b)
            {
                return b;
            }
        }

        return false;
    }

    private static PrismRegistrationLifetimeOrdinal GetLifetime(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (!string.Equals(pair.Key, "ServiceLifetime", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (pair.Value.Value is int i && Enum.IsDefined(typeof(PrismRegistrationLifetimeOrdinal), i))
            {
                return (PrismRegistrationLifetimeOrdinal)i;
            }

            if (pair.Value.Value is not null)
            {
                string? display = pair.Value.ToString();
                if (display?.Contains("Transient", StringComparison.Ordinal) == true)
                {
                    return PrismRegistrationLifetimeOrdinal.Transient;
                }

                if (display?.Contains("Scoped", StringComparison.Ordinal) == true)
                {
                    return PrismRegistrationLifetimeOrdinal.Scoped;
                }

                if (display?.Contains("Singleton", StringComparison.Ordinal) == true)
                {
                    return PrismRegistrationLifetimeOrdinal.Singleton;
                }
            }
        }

        return PrismRegistrationLifetimeOrdinal.Transient;
    }

    private static bool TryGetTypeNamedArgument(
        AttributeData attribute,
        string name,
        out INamedTypeSymbol? type)
    {
        type = null;
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == name && pair.Value.Value is INamedTypeSymbol named)
            {
                type = named;
                return true;
            }
        }

        return false;
    }

    private static string GetStringName(AttributeData attribute, string defaultKey)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == "Name" && pair.Value.Value is string s && !string.IsNullOrEmpty(s))
            {
                return s;
            }
        }

        return defaultKey;
    }

    private static string TypeFq(ITypeSymbol symbol) =>
        symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string Literal(string value) =>
        "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private static string BuildSource(ImmutableArray<RegistrationStatement> statements)
    {
        StringBuilder sb = new();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace Prism.SourceGenerators");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Source-generated registrations for Prism <c>IContainerRegistry</c>.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static partial class PrismRegistrationExtensions");
        sb.AppendLine("    {");
        sb.AppendLine("        /// <summary>");
        sb.AppendLine("        /// Registers all types decorated with MvvmAIO Prism registration attributes in this compilation.");
        sb.AppendLine("        /// </summary>");
        sb.AppendLine("        /// <param name=\"containerRegistry\">The Prism container registry.</param>");
        sb.AppendLine("        public static void RegisterGeneratedTypes(this global::Prism.Ioc.IContainerRegistry containerRegistry)");
        sb.AppendLine("        {");

        foreach (RegistrationStatement line in statements
                     .OrderBy(static s => s.SortGroup)
                     .ThenBy(static s => s.Text, StringComparer.Ordinal))
        {
            sb.Append("            ");
            sb.AppendLine(line.Text);
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private readonly struct RegistrationStatement(byte sortGroup, string text) : IEquatable<RegistrationStatement>
    {
        public byte SortGroup { get; } = sortGroup;

        public string Text { get; } = text;

        /// <summary>Equality uses <see cref="Text"/> only so incremental caching matches emitted source lines.</summary>
        public bool Equals(RegistrationStatement other) =>
            string.Equals(Text, other.Text, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is RegistrationStatement other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Text);

        public static bool operator ==(RegistrationStatement left, RegistrationStatement right) => left.Equals(right);

        public static bool operator !=(RegistrationStatement left, RegistrationStatement right) => !left.Equals(right);
    }
}
