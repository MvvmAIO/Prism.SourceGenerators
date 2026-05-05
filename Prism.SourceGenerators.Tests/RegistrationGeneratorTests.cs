using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Prism.SourceGenerators.Extensions;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class RegistrationGeneratorTests
{
    [Fact]
    public void Register_attribute_on_class_A_is_bound_in_full_harness_compilation()
    {
        const string userSource = """
            namespace Demo
            {
                public interface IA { }

                [Prism.SourceGenerators.RegisterAttribute(ServiceType = typeof(Demo.IA), ServiceLifetime = Prism.SourceGenerators.PrismRegistrationLifetime.Singleton)]
                public sealed partial class A : IA { }
            }
            """;

        (CSharpCompilation compilation, SyntaxTree tree) =
            GeneratorTestHarness.CreateHarnessCompilation(userSource, LanguageVersion.CSharp12);

        Assert.Empty(compilation.GetDiagnostics().Where(static d => d.Severity == DiagnosticSeverity.Error));

        SemanticModel model = compilation.GetSemanticModel(tree);
        ClassDeclarationSyntax classA = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>()
            .First(c => c.Identifier.ValueText == "A");
        INamedTypeSymbol? sym = model.GetDeclaredSymbol(classA, default) as INamedTypeSymbol;
        Assert.NotNull(sym);
        ImmutableArray<AttributeData> attrs = sym.GetAttributes();
        Assert.NotEmpty(attrs);
        string joined = string.Join(
            ", ",
            attrs.Select(a => a.AttributeClass?.GetFullyQualifiedMetadataName() ?? "<null>"));
        Assert.True(
            joined.Contains("RegisterAttribute", StringComparison.Ordinal),
            $"Expected RegisterAttribute metadata on Demo.A; got: {joined}");
    }

    [Fact]
    public void No_registration_attributes_does_not_emit_PrismRegistrationExtensions()
    {
        const string source = """
            namespace Demo
            {
                public sealed class Plain { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        Assert.DoesNotContain(
            output.GeneratedSources,
            static s => s.HintName == "PrismRegistrationExtensions.g.cs");
    }

    [Fact]
    public void RegisterGeneratedTypes_emits_singleton_and_try_transient()
    {
        const string source = """
            namespace Demo
            {
                public interface ISvc { }

                [Prism.SourceGenerators.RegisterSingleton(ServiceType = typeof(Demo.ISvc))]
                public sealed partial class Svc : ISvc { }

                [Prism.SourceGenerators.RegisterTransient(IfNotRegistered = true)]
                public sealed partial class Other { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("RegisterGeneratedTypes", reg.Source);
        Assert.Contains("containerRegistry.RegisterSingleton<global::Demo.ISvc, global::Demo.Svc>();", reg.Source);
        Assert.Contains("containerRegistry.TryRegister<global::Demo.Other>();", reg.Source);
    }

    [Fact]
    public void RegisterSingleton_generic_attribute_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public interface ICounter { }

                [Prism.SourceGenerators.RegisterSingleton<Demo.ICounter>]
                public sealed partial class Counter : ICounter { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterSingleton<global::Demo.ICounter, global::Demo.Counter>();",
            reg.Source);
    }

    [Fact]
    public void RegisterScoped_emits_scoped_pair_and_self()
    {
        const string source = """
            namespace Demo
            {
                public interface IUnit { }

                [Prism.SourceGenerators.RegisterScoped(ServiceType = typeof(Demo.IUnit))]
                public sealed partial class Unit : IUnit { }

                [Prism.SourceGenerators.RegisterScoped]
                public sealed partial class ScopeMarker { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.RegisterScoped<global::Demo.IUnit, global::Demo.Unit>();", reg.Source);
        Assert.Contains("containerRegistry.RegisterScoped<global::Demo.ScopeMarker>();", reg.Source);
    }

    [Fact]
    public void RegisterScoped_generic_attribute_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public interface IJob { }

                [Prism.SourceGenerators.RegisterScoped<Demo.IJob>]
                public sealed partial class Job : IJob { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.RegisterScoped<global::Demo.IJob, global::Demo.Job>();", reg.Source);
    }

    [Fact]
    public void Register_attribute_singleton_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public interface IA { }

                [Prism.SourceGenerators.RegisterAttribute(ServiceType = typeof(Demo.IA), ServiceLifetime = Prism.SourceGenerators.PrismRegistrationLifetime.Singleton)]
                public sealed partial class A : IA { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.RegisterSingleton<global::Demo.IA, global::Demo.A>();", reg.Source);
    }

    [Fact]
    public void Register_attribute_scoped_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public interface IB { }

                [Prism.SourceGenerators.RegisterAttribute(ServiceType = typeof(Demo.IB), ServiceLifetime = Prism.SourceGenerators.PrismRegistrationLifetime.Scoped)]
                public sealed partial class B : IB { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.RegisterScoped<global::Demo.IB, global::Demo.B>();", reg.Source);
    }

    [Fact]
    public void Register_attribute_transient_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public interface IC { }

                [Prism.SourceGenerators.RegisterAttribute(ServiceType = typeof(Demo.IC), ServiceLifetime = Prism.SourceGenerators.PrismRegistrationLifetime.Transient)]
                public sealed partial class C : IC { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.Register<global::Demo.IC, global::Demo.C>();", reg.Source);
    }

    [Fact]
    public void Register_generic_attribute_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public interface IStore { }

                [Prism.SourceGenerators.RegisterAttribute<Demo.IStore>(ServiceLifetime = Prism.SourceGenerators.PrismRegistrationLifetime.Singleton)]
                public sealed partial class Store : IStore { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.RegisterSingleton<global::Demo.IStore, global::Demo.Store>();", reg.Source);
    }

    [Fact]
    public void RegisterForNavigation_non_generic_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public sealed partial class MyVm { }

                [Prism.SourceGenerators.RegisterForNavigation(ViewModelType = typeof(Demo.MyVm), Name = "mine")]
                public sealed partial class MyView { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterForNavigation<global::Demo.MyView, global::Demo.MyVm>(\"mine\");",
            reg.Source);
    }

    [Fact]
    public void RegisterForNavigation_generic_attribute_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public sealed partial class PageVm { }

                [Prism.SourceGenerators.RegisterForNavigation<Demo.PageVm>(Name = "page")]
                public sealed partial class PageView { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterForNavigation<global::Demo.PageView, global::Demo.PageVm>(\"page\");",
            reg.Source);
    }

    [Fact]
    public void RegisterDialog_non_generic_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public sealed partial class DlgVm { }

                [Prism.SourceGenerators.RegisterDialog(ViewModelType = typeof(Demo.DlgVm), Name = "dlg")]
                public sealed partial class DlgView { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterDialog<global::Demo.DlgView, global::Demo.DlgVm>(\"dlg\");",
            reg.Source);
    }

    [Fact]
    public void RegisterDialog_generic_attribute_emits_pair()
    {
        const string source = """
            namespace Demo
            {
                public sealed partial class AlertVm { }

                [Prism.SourceGenerators.RegisterDialog<Demo.AlertVm>(Name = "alert")]
                public sealed partial class AlertView { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterDialog<global::Demo.AlertView, global::Demo.AlertVm>(\"alert\");",
            reg.Source);
    }

    [Fact]
    public void RegisterDialogWindow_emits_single_type()
    {
        const string source = """
            namespace Demo
            {
                [Prism.SourceGenerators.RegisterDialogWindow(Name = "host")]
                public sealed partial class HostWindow { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterDialogWindow<global::Demo.HostWindow>(\"host\");",
            reg.Source);
    }

    [Fact]
    public void AllowMultiple_registration_attributes_on_same_type()
    {
        const string source = """
            namespace Demo
            {
                public interface ILeft { }
                public interface IRight { }

                [Prism.SourceGenerators.RegisterTransient(ServiceType = typeof(Demo.ILeft))]
                [Prism.SourceGenerators.RegisterTransient(ServiceType = typeof(Demo.IRight))]
                public sealed partial class Both : ILeft, IRight { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("containerRegistry.Register<global::Demo.ILeft, global::Demo.Both>();", reg.Source);
        Assert.Contains("containerRegistry.Register<global::Demo.IRight, global::Demo.Both>();", reg.Source);
    }
}
