using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class RegistrationGeneratorTests
{
    [Fact]
    public void RegisterGeneratedTypes_emits_singleton_and_try_transient()
    {
        const string source = """
            namespace Demo;

            public interface ISvc { }

            [Prism.SourceGenerators.RegisterSingleton(ServiceType = typeof(Demo.ISvc))]
            public sealed partial class Svc : ISvc { }

            [Prism.SourceGenerators.RegisterTransient(IfNotRegistered = true)]
            public sealed partial class Other { }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains("RegisterGeneratedTypes", reg.Source);
        Assert.Contains("containerRegistry.RegisterSingleton<global::Demo.ISvc, global::Demo.Svc>();", reg.Source);
        Assert.Contains("containerRegistry.TryRegister<global::Demo.Other>();", reg.Source);
    }

    [Fact]
    public void RegisterForNavigation_emits_pair()
    {
        const string source = """
            namespace Demo;

            public sealed partial class MyVm { }

            [Prism.SourceGenerators.RegisterForNavigation(ViewModelType = typeof(Demo.MyVm), Name = "mine")]
            public sealed partial class MyView { }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource reg = Assert.Single(
            output.GeneratedSources.Where(static s => s.HintName == "PrismRegistrationExtensions.g.cs"));

        Assert.Contains(
            "containerRegistry.RegisterForNavigation<global::Demo.MyView, global::Demo.MyVm>(\"mine\");",
            reg.Source);
    }
}
