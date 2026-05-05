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
/// </summary>
internal enum PrismRegistrationLifetimeOrdinal : byte
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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ImmutableArray<RegistrationStatement>> perType =
            context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, ct) => ExtractStatements(ctx, ct));

        IncrementalValueProvider<ImmutableArray<RegistrationStatement>> combined =
            perType.Collect().Select(static (batches, _) =>
            {
                ImmutableArray<RegistrationStatement>.Builder builder =
                    ImmutableArray.CreateBuilder<RegistrationStatement>();
                foreach (ImmutableArray<RegistrationStatement> batch in batches)
                {
                    builder.AddRange(batch);
                }

                return builder.ToImmutable();
            });

        context.RegisterSourceOutput(combined, static (spc, statements) =>
        {
            string source = BuildSource(statements);
            spc.AddSource("PrismRegistrationExtensions.g.cs", source);
        });
    }

    private static ImmutableArray<RegistrationStatement> ExtractStatements(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
            return ImmutableArray<RegistrationStatement>.Empty;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is not INamedTypeSymbol typeSymbol)
            return ImmutableArray<RegistrationStatement>.Empty;

        using ImmutableArrayBuilder<RegistrationStatement> builder = ImmutableArrayBuilder<RegistrationStatement>.Rent();

        foreach (AttributeData attribute in typeSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is not { } attributeClass)
                continue;

            string meta = attributeClass.GetFullyQualifiedMetadataName();
            if (TryExtractRegistration(typeSymbol, attribute, attributeClass, meta, out RegistrationStatement? statement)
                && statement is not null)
            {
                builder.Add(statement.Value);
            }
        }

        return builder.ToImmutable();
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
                singleton: false);
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
                singleton: false);
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
                singleton: false);
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
                singleton: false);
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
                singleton: true);
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
                singleton: true);
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
            ITypeSymbol? svc = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            PrismRegistrationLifetimeOrdinal lifetime = GetLifetime(attribute);
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
                return false;

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                $"containerRegistry.RegisterForNavigation<{TypeFq(implementationType)}, {TypeFq(vm)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterForNavigationAttribute`1")
        {
            ITypeSymbol? vm = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            if (vm is not INamedTypeSymbol vmNamed)
                return false;

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                $"containerRegistry.RegisterForNavigation<{TypeFq(implementationType)}, {TypeFq(vmNamed)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogAttribute")
        {
            if (!TryGetTypeNamedArgument(attribute, "ViewModelType", out INamedTypeSymbol? vm) || vm is null)
                return false;

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                $"containerRegistry.RegisterDialog<{TypeFq(implementationType)}, {TypeFq(vm)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogAttribute`1")
        {
            ITypeSymbol? vm = attributeClass.TypeArguments.Length > 0 ? attributeClass.TypeArguments[0] : null;
            if (vm is not INamedTypeSymbol vmNamed)
                return false;

            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
                $"containerRegistry.RegisterDialog<{TypeFq(implementationType)}, {TypeFq(vmNamed)}>({Literal(key)});");
            return true;
        }

        if (metadataName is "Prism.SourceGenerators.RegisterDialogWindowAttribute")
        {
            string key = GetStringName(attribute, defaultKey: implementationType.Name);
            statement = new RegistrationStatement(
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
        bool singleton)
    {
        if (serviceType is null)
        {
            // Self-registration (single type argument overloads).
            if (transient)
                return Single("Register", "TryRegister", useTry, implementationType);
            if (scoped)
                return Single("RegisterScoped", "TryRegisterScoped", useTry, implementationType);
            if (singleton)
                return Single("RegisterSingleton", "TryRegisterSingleton", useTry, implementationType);
        }
        else
        {
            if (transient)
                return Pair("Register", "TryRegister", useTry, serviceType, implementationType);
            if (scoped)
                return Pair("RegisterScoped", "TryRegisterScoped", useTry, serviceType, implementationType);
            if (singleton)
                return Pair("RegisterSingleton", "TryRegisterSingleton", useTry, serviceType, implementationType);
        }

        return null;
    }

    private static RegistrationStatement Single(string register, string tryRegister, bool useTry, INamedTypeSymbol impl)
    {
        string method = useTry ? tryRegister : register;
        return new RegistrationStatement($"containerRegistry.{method}<{TypeFq(impl)}>();");
    }

    private static RegistrationStatement Pair(
        string register,
        string tryRegister,
        bool useTry,
        ITypeSymbol service,
        INamedTypeSymbol impl)
    {
        string method = useTry ? tryRegister : register;
        return new RegistrationStatement(
            $"containerRegistry.{method}<{TypeFq(service)}, {TypeFq(impl)}>();");
    }

    private static RegistrationStatement? BuildRegisterAttribute(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        PrismRegistrationLifetimeOrdinal lifetime)
    {
        bool useTry = GetIfNotRegistered(attribute);
        ITypeSymbol? serviceType = GetServiceType(attribute, implementationType);
        return lifetime switch
        {
            PrismRegistrationLifetimeOrdinal.Transient => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: true, scoped: false, singleton: false),
            PrismRegistrationLifetimeOrdinal.Scoped => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: true, singleton: false),
            PrismRegistrationLifetimeOrdinal.Singleton => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: false, singleton: true),
            _ => null
        };
    }

    private static RegistrationStatement? BuildRegisterAttributeWithService(
        INamedTypeSymbol implementationType,
        AttributeData attribute,
        ITypeSymbol? serviceType,
        PrismRegistrationLifetimeOrdinal lifetime)
    {
        bool useTry = GetIfNotRegistered(attribute);
        return lifetime switch
        {
            PrismRegistrationLifetimeOrdinal.Transient => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: true, scoped: false, singleton: false),
            PrismRegistrationLifetimeOrdinal.Scoped => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: true, singleton: false),
            PrismRegistrationLifetimeOrdinal.Singleton => BuildTwoTypeRegistration(
                implementationType, attribute, serviceType, useTry, transient: false, scoped: false, singleton: true),
            _ => null
        };
    }

    private static ITypeSymbol? GetServiceType(AttributeData attribute, INamedTypeSymbol implementationType)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == "ServiceType" && pair.Value.Value is INamedTypeSymbol named)
                return named;
        }

        return null;
    }

    private static bool GetIfNotRegistered(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key == "IfNotRegistered" && pair.Value.Value is bool b)
                return b;
        }

        return false;
    }

    private static PrismRegistrationLifetimeOrdinal GetLifetime(AttributeData attribute)
    {
        foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
        {
            if (pair.Key != "ServiceLifetime")
                continue;

            if (pair.Value.Value is int i && Enum.IsDefined(typeof(PrismRegistrationLifetimeOrdinal), i))
                return (PrismRegistrationLifetimeOrdinal)i;

            if (pair.Value.Value is not null)
            {
                string? display = pair.Value.ToString();
                if (display?.Contains("Transient", System.StringComparison.Ordinal) == true)
                    return PrismRegistrationLifetimeOrdinal.Transient;
                if (display?.Contains("Scoped", System.StringComparison.Ordinal) == true)
                    return PrismRegistrationLifetimeOrdinal.Scoped;
                if (display?.Contains("Singleton", System.StringComparison.Ordinal) == true)
                    return PrismRegistrationLifetimeOrdinal.Singleton;
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
                return s;
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

        foreach (RegistrationStatement line in statements.OrderBy(static s => s.Text, StringComparer.Ordinal))
        {
            sb.Append("            ");
            sb.AppendLine(line.Text);
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private readonly struct RegistrationStatement(string text)
    {
        public string Text { get; } = text;
    }
}
