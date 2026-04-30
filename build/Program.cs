using System.IO;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[ShutdownDotNetAfterServerBuild]
[UnsetVisualStudioEnvironmentVariables]
sealed class Build : NukeBuild
{
    [Parameter("Build configuration (Debug/Release)")]
    string Configuration { get; } = IsLocalBuild ? "Debug" : "Release";

    [Parameter("Package version override (for release/tag builds)")]
    string? Version { get; }

    [Parameter("NuGet API key (required for Publish target)")]
    string? NuGetApiKey { get; }

    AbsolutePath Root => RootDirectory;
    AbsolutePath SolutionFile => Root / "Prism.SourceGenerators.slnx";
    AbsolutePath PackageProject => Root / "Prism.SourceGenerators.Package" / "Prism.SourceGenerators.Package.csproj";
    AbsolutePath TestsProject => Root / "Prism.SourceGenerators.Tests" / "Prism.SourceGenerators.Tests.csproj";
    AbsolutePath TestResultsDirectory => Root / "TestResults";

    public static int Main() => Execute<Build>(x => x.Ci);

    Target Clean => _ => _
        .Executes(() =>
        {
            if (Directory.Exists(TestResultsDirectory))
            {
                Directory.Delete(TestResultsDirectory, recursive: true);
            }

            Directory.CreateDirectory(TestResultsDirectory);
        });

    Target Restore => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(SolutionFile));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(SolutionFile)
                .SetConfiguration(Configuration)
                .EnableNoRestore()
                .SetProperty("TreatWarningsAsErrors", "true"));
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(TestsProject)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .SetResultsDirectory(TestResultsDirectory)
                .SetLoggers("trx;LogFileName=test-results.trx"));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetPack(s =>
            {
                s = s
                    .SetProject(PackageProject)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .SetProperty("ContinuousIntegrationBuild", "true");

                if (!string.IsNullOrWhiteSpace(Version))
                {
                    s = s.SetVersion(Version);
                }

                return s;
            });
        });

    Target Publish => _ => _
        .DependsOn(Pack)
        .Requires(() => !string.IsNullOrWhiteSpace(NuGetApiKey))
        .Executes(() =>
        {
            DotNetNuGetPush(s => s
                .SetTargetPath(Root / "Prism.SourceGenerators.Package" / "bin" / Configuration / "*.nupkg")
                .SetApiKey(NuGetApiKey)
                .SetSource("https://api.nuget.org/v3/index.json")
                .EnableSkipDuplicate());
        });

    Target Ci => _ => _
        .DependsOn(Test);
}
