using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Diagnostics;
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
/// Emits <c>PrismRegistrationExtensions.RegisterGeneratedTypes</c> from MvvmAIO.Prism.Core registration attributes,
/// using Prism <c>IContainerRegistry</c> APIs compatible with both Prism 8 and Prism 9+.
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

    private static readonly string[] AttributeMetadataNames =
    {
        "Prism.SourceGenerators.RegisterTransientAttribute",
        "Prism.SourceGenerators.RegisterTransientAttribute`1",
        "Prism.SourceGenerators.RegisterScopedAttribute",
        "Prism.SourceGenerators.RegisterScopedAttribute`1",
        "Prism.SourceGenerators.RegisterSingletonAttribute",
        "Prism.SourceGenerators.RegisterSingletonAttribute`1",
        "Prism.SourceGenerators.RegisterAttribute",
        "Prism.SourceGenerators.RegisterAttribute`1",
        "Prism.SourceGenerators.RegisterForNavigationAttribute",
        "Prism.SourceGenerators.RegisterForNavigationAttribute`1",
        "Prism.SourceGenerators.RegisterDialogAttribute",
        "Prism.SourceGenerators.RegisterDialogAttribute`1",
        "Prism.SourceGenerators.RegisterDialogWindowAttribute",
    };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Use ForAttributeWithMetadataName for targeted incremental attribute lookup.
        // Each attribute metadata name gets its own provider; results are merged into a
        // single ImmutableArray<RegistrationStatement> for deterministic source emission.
        IncrementalValueProvider<ImmutableArray<RegistrationStatement>> combined =
            CreateAttributeProvider(context, AttributeMetadataNames[0]).Collect();

        for (int i = 1; i < AttributeMetadataNames.Length; i++)
        {
            combined = combined
                .Combine(CreateAttributeProvider(context, AttributeMetadataNames[i]).Collect())
                .Select(static (pair, _) => pair.Left.AddRange(pair.Right));
        }

        context.RegisterSourceOutput(combined, static (spc, statements) =>
        {
            if (statements.IsDefaultOrEmpty)
            {
                return;
            }

            spc.AddSource("PrismRegistrationExtensions.g.cs", BuildSource(statements));
        });
    }

    private static IncrementalValuesProvider<RegistrationStatement> CreateAttributeProvider(
        IncrementalGeneratorInitializationContext context,
        string metadataName)
    {
        return context.SyntaxProvider
            .ForAttributeWithMetadataName(
                metadataName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => ExtractFromAttributeContext(ctx))
            .SelectMany(static (arr, _) => arr);
    }

    private static ImmutableArray<RegistrationStatement> ExtractFromAttributeContext(
        GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return ImmutableArray<RegistrationStatement>.Empty;
        }

        using ImmutableArrayBuilder<RegistrationStatement> builder =
            ImmutableArrayBuilder<RegistrationStatement>.Rent();

        // Iterate over all matching attributes on the target symbol.
        // For AllowMultiple attributes, ctx.Attributes may only contain the first match
        // on older Roslyn polyfills; fall back to scanning all attributes on the symbol
        // and filtering by the known metadata names.
        foreach (AttributeData attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is not { } attributeClass)
            {
                continue;
            }

            string meta = attributeClass.GetFullyQualifiedMetadataName();
            if (!IsKnownRegistrationAttribute(meta))
            {
                continue;
            }

            if (TryExtractRegistration(typeSymbol, attribute, attributeClass, meta, out RegistrationStatement? statement)
                && statement is not null)
            {
                builder.Add(statement.Value);
            }
        }

        return builder.ToImmutable();
    }

    private static bool IsKnownRegistrationAttribute(string metadataName)
    {
        for (int i = 0; i < AttributeMetadataNames.Length; i++)
        {
            if (string.Equals(metadataName, AttributeMetadataNames[i], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
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
                ifNotRegistered: GetIfNotRegistered(attribute),
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
                ifNotRegistered: GetIfNotRegistered(attribute),
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
                ifNotRegistered: GetIfNotRegistered(attribute),
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
                ifNotRegistered: GetIfNotRegistered(attribute),
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
                ifNotRegistered: GetIfNotRegistered(attribute),
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
                ifNotRegistered: GetIfNotRegistered(attribute),
                transient: false,
                scoped: false,
                singleton: true,
                sortGroup: SortSingleton);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterAttribute")
        {
            PrismRegistrationLifetimeOrdinal lifetime = GetLifetime(attribute);
            statement = BuildRegisterAttribute(implementationType, attribute, lifetime);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterAttribute`1")
        {
            PrismRegistrationLifetimeOrdinal lifetime = GetLifetime(attribute);
            ITypeSymbol? svc = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            statement = BuildRegisterAttributeWithService(
                implementationType,
                attribute,
                svc ?? GetServiceType(attribute, implementationType),
                lifetime);
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
                $"containerRegistry.RegisterForNavigation<{TypeFq(implementationType)}, {TypeFq(vm)}>({Literal(key)});",
                checkType: null);
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
                $"containerRegistry.RegisterForNavigation<{TypeFq(implementationType)}, {TypeFq(vmNamed)}>({Literal(key)});",
                checkType: null);
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
                $"containerRegistry.RegisterDialog<{TypeFq(implementationType)}, {TypeFq(vm)}>({Literal(key)});",
                checkType: null);
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
                $"containerRegistry.RegisterDialog<{TypeFq(implementationType)}, {TypeFq(vmNamed)}>({Literal(key)});",
                checkType: null);
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogWindowAttribute")
        {
            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                SortDialog,
                $"containerRegistry.RegisterDialogWindow<{TypeFq(implementationType)}>({Literal(key)});",
                checkType: null);
            return true;
        }

        return false;
    }

    private static RegistrationStatement? BuildTwoTypeRegistration(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ITypeSymbol? serviceType,
        bool ifNotRegistered,
        bool transient,
        bool scoped,
        bool singleton,
        byte sortGroup)
    {
        string? name = GetStringNameOrNull(attribute);
        string? checkType = ifNotRegistered
            ? TypeFq(serviceType ?? implementationType)
            : null;

        if (serviceType is null)
        {
            // Self-registration (single type argument overloads).
            if (transient)
            {
                return SingleRegistration("Register", sortGroup, implementationType, name, checkType);
            }

            if (scoped)
            {
                return SingleRegistration("RegisterScoped", sortGroup, implementationType, name, checkType);
            }

            if (singleton)
            {
                return SingleRegistration("RegisterSingleton", sortGroup, implementationType, name, checkType);
            }
        }
        else
        {
            if (transient)
            {
                return PairRegistration("Register", sortGroup, serviceType, implementationType, name, checkType);
            }

            if (scoped)
            {
                return PairRegistration("RegisterScoped", sortGroup, serviceType, implementationType, name, checkType);
            }

            if (singleton)
            {
                return PairRegistration("RegisterSingleton", sortGroup, serviceType, implementationType, name, checkType);
            }
        }

        return null;
    }

    private static RegistrationStatement SingleRegistration(
        string method,
        byte sortGroup,
        INamedTypeSymbol impl,
        string? name,
        string? checkType)
    {
        string call = name is null
            ? $"containerRegistry.{method}<{TypeFq(impl)}>();"
            : $"containerRegistry.{method}(typeof({TypeFq(impl)}), typeof({TypeFq(impl)}), {Literal(name)});";
        return new RegistrationStatement(sortGroup, call, checkType);
    }

    private static RegistrationStatement PairRegistration(
        string method,
        byte sortGroup,
        ITypeSymbol service,
        INamedTypeSymbol impl,
        string? name,
        string? checkType)
    {
        string call = name is null
            ? $"containerRegistry.{method}<{TypeFq(service)}, {TypeFq(impl)}>();"
            : $"containerRegistry.{method}(typeof({TypeFq(service)}), typeof({TypeFq(impl)}), {Literal(name)});";
        return new RegistrationStatement(sortGroup, call, checkType);
    }

    private static RegistrationStatement? BuildRegisterAttribute(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        PrismRegistrationLifetimeOrdinal lifetime)
    {
        bool ifNotRegistered = GetIfNotRegistered(attribute);
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
                implementationType, attribute, serviceType, ifNotRegistered, transient: true, scoped: false, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Scoped => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, ifNotRegistered, transient: false, scoped: true, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Singleton => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, ifNotRegistered, transient: false, scoped: false, singleton: true, sort),
            _ => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, ifNotRegistered, transient: true, scoped: false, singleton: false, SortTransient)
        };
    }

    private static RegistrationStatement? BuildRegisterAttributeWithService(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ITypeSymbol? serviceType,
        PrismRegistrationLifetimeOrdinal lifetime)
    {
        bool ifNotRegistered = GetIfNotRegistered(attribute);
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
                implementationType, attribute, serviceType, ifNotRegistered, transient: true, scoped: false, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Scoped => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, ifNotRegistered, transient: false, scoped: true, singleton: false, sort),
            PrismRegistrationLifetimeOrdinal.Singleton => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, ifNotRegistered, transient: false, scoped: false, singleton: true, sort),
            _ => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, ifNotRegistered, transient: true, scoped: false, singleton: false, SortTransient)
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

    private static string? GetStringNameOrNull(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == "Name" && pair.Value.Value is string s && !string.IsNullOrEmpty(s))
            {
                return s;
            }
        }

        return null;
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
            if (line.CheckType is not null)
            {
                sb.Append("            if (!containerRegistry.IsRegistered(typeof(");
                sb.Append(line.CheckType);
                sb.AppendLine(")))");
                sb.AppendLine("            {");
                sb.Append("                ");
                sb.AppendLine(line.Text);
                sb.AppendLine("            }");
                sb.AppendLine();
            }
            else
            {
                sb.Append("            ");
                sb.AppendLine(line.Text);
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private readonly struct RegistrationStatement(byte sortGroup, string text, string? checkType) : IEquatable<RegistrationStatement>
    {
        public byte SortGroup { get; } = sortGroup;

        public string Text { get; } = text;

        /// <summary>
        /// Fully-qualified type name to check with <c>IsRegistered</c> before registering (Prism 8/9 compatible).
        /// <see langword="null"/> means the registration is unconditional.
        /// </summary>
        public string? CheckType { get; } = checkType;

        /// <summary>Equality uses <see cref="Text"/> and <see cref="CheckType"/> so incremental caching matches emitted source lines.</summary>
        public bool Equals(RegistrationStatement other) =>
            string.Equals(Text, other.Text, StringComparison.Ordinal) &&
            string.Equals(CheckType, other.CheckType, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is RegistrationStatement other && Equals(other);

        public override int GetHashCode()
        {
            int h = StringComparer.Ordinal.GetHashCode(Text);
            if (CheckType is not null)
            {
                h = (h * 397) ^ StringComparer.Ordinal.GetHashCode(CheckType);
            }

            return h;
        }

        public static bool operator ==(RegistrationStatement left, RegistrationStatement right) => left.Equals(right);

        public static bool operator !=(RegistrationStatement left, RegistrationStatement right) => !left.Equals(right);
    }
}
