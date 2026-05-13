using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class MatrixTests
{
    public static TheoryData<LanguageVersion, bool, bool, bool> DelegateCommandMatrix => new()
    {
        // languageVersion, hasAsyncDelegateCommand, expectFieldKeyword, expectPackageDiagnostic (PSG3002)
        { LanguageVersion.CSharp12, true,  false, false },
        { LanguageVersion.Preview,  true,  true,  false },
        { LanguageVersion.CSharp12, false, false, true },
        { LanguageVersion.Preview,  false, true,  true }
    };

    [Theory]
    [MemberData(nameof(DelegateCommandMatrix))]
    public void DelegateCommand_generation_matches_matrix(
        LanguageVersion languageVersion,
        bool hasAsyncDelegateCommand,
        bool expectFieldKeyword,
        bool expectPackageDiagnostic)
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.Task LoadAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(
            source,
            languageVersion: languageVersion,
            hasAsyncDelegateCommand: hasAsyncDelegateCommand);

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".LoadCommand.g.cs")));

        if (expectFieldKeyword)
        {
            Assert.Contains("=> field ??= new global::Prism.Commands.AsyncDelegateCommand(LoadAsync);", commandSource.Source);
            Assert.DoesNotContain("private global::Prism.Commands.AsyncDelegateCommand? _loadCommand;", commandSource.Source);
        }
        else
        {
            Assert.Contains("private global::Prism.Commands.AsyncDelegateCommand? _loadCommand;", commandSource.Source);
            Assert.Contains("=> _loadCommand ??= new global::Prism.Commands.AsyncDelegateCommand(LoadAsync);", commandSource.Source);
        }

        Assert.False(output.GeneratedSources.Any(s => s.HintName == "AsyncDelegateCommand.Polyfill.g.cs"));
        Assert.Equal(expectPackageDiagnostic, output.Diagnostics.Any(d => d.Id == "PSG3002"));
    }

    [Fact]
    public void DelegateCommand_ValueTask_execute_emits_AsTask_wrapper()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.ValueTask SaveAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);
        Assert.False(output.Diagnostics.Any(static d => d.Id is "PSG1001" or "PSG1002"));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".SaveCommand.g.cs")));
        Assert.Contains("() => SaveAsync().AsTask()", commandSource.Source);
    }

    [Fact]
    public void AsyncDelegateCommand_ValueTaskOfT_execute_emits_parameterized_AsTask_wrapper()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [AsyncDelegateCommand]
                private async System.Threading.Tasks.ValueTask<int> IncAsync(int seed)
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                    return seed + 1;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);
        Assert.False(output.Diagnostics.Any(static d => d.Id is "PSG1001" or "PSG1002"));

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".IncCommand.g.cs")));
        Assert.Contains("(__p) => IncAsync(__p).AsTask()", commandSource.Source);
    }

    public static TheoryData<LanguageVersion, bool> ObservablePropertyMatrix => new()
    {
        // languageVersion, usePartialProperty
        { LanguageVersion.CSharp12, false },
        { LanguageVersion.Preview,  false },
        { LanguageVersion.Preview,  true  }
    };

    [Theory]
    [MemberData(nameof(ObservablePropertyMatrix))]
    public void ObservableProperty_generation_matches_matrix(
        LanguageVersion languageVersion,
        bool usePartialProperty)
    {
        string source = usePartialProperty
            ? """
              namespace Demo;

              public partial class Vm : Prism.Mvvm.BindableBase
              {
                  [ObservableProperty]
                  public partial string Name { get; set; } = "";
              }
              """
            : """
              namespace Demo;

              public partial class Vm : Prism.Mvvm.BindableBase
              {
                  [ObservableProperty]
                  private string _name = "";
              }
              """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: languageVersion);
        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        if (usePartialProperty)
        {
            Assert.Contains("partial string Name", propertySource.Source);
            Assert.Contains("get => field;", propertySource.Source);
            Assert.Contains("set", propertySource.Source);
        }
        else
        {
            Assert.Contains("public string Name", propertySource.Source);
            Assert.Contains("get => _name;", propertySource.Source);
        }

        // OnChanging / OnChanged partial method declarations are always emitted
        Assert.Contains("partial void OnNameChanging(string value);", propertySource.Source);
        Assert.Contains("partial void OnNameChanging(string oldValue, string newValue);", propertySource.Source);
        Assert.Contains("partial void OnNameChanged(string value);", propertySource.Source);
        Assert.Contains("partial void OnNameChanged(string oldValue, string newValue);", propertySource.Source);

        // OnChanging hooks run before SetProperty; OnChanged runs inside SetProperty's onChanged callback
        int changingIndex = propertySource.Source.IndexOf("OnNameChanging(value);", System.StringComparison.Ordinal);
        int setPropertyIndex = propertySource.Source.IndexOf("SetProperty(ref", System.StringComparison.Ordinal);
        int changedIndex = propertySource.Source.IndexOf("OnNameChanged(value);", System.StringComparison.Ordinal);

        Assert.InRange(changingIndex, 1, setPropertyIndex - 1);
        Assert.InRange(changedIndex, setPropertyIndex + 1, propertySource.Source.Length - 1);
        Assert.Contains("RaisePropertyChanging(nameof(Name))", propertySource.Source);
    }

    /// <summary>
    /// GitHub #23: partial On* hooks must use the same nullable reference type as the property/field
    /// so user-defined <c>partial void OnXChanged(string? value)</c> matches the generator declaration.
    /// </summary>
    [Fact]
    public void ObservableProperty_nullable_reference_emits_nullable_partial_hook_parameters_issue_23()
    {
        const string partialPropertySource = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public partial string? Nickname { get; set; }

                partial void OnNicknameChanged(string? value) { _ = value; }
            }
            """;

        GeneratorRunOutput partialPropOutput = GeneratorTestHarness.Run(
            partialPropertySource,
            languageVersion: LanguageVersion.Preview);
        GeneratedSource nickProp = Assert.Single(
            partialPropOutput.GeneratedSources.Where(s => s.HintName.EndsWith(".Nickname.g.cs")));

        Assert.Contains("partial void OnNicknameChanging(", nickProp.Source);
        Assert.Contains("partial void OnNicknameChanged(", nickProp.Source);
        Assert.True(
            nickProp.Source.Contains("OnNicknameChanging(global::System.String? value)", StringComparison.Ordinal)
            || nickProp.Source.Contains("OnNicknameChanging(string? value)", StringComparison.Ordinal),
            $"Expected nullable changing-hook parameter type in:{System.Environment.NewLine}{nickProp.Source}");
        Assert.True(
            nickProp.Source.Contains("OnNicknameChanged(global::System.String? value)", StringComparison.Ordinal)
            || nickProp.Source.Contains("OnNicknameChanged(string? value)", StringComparison.Ordinal),
            $"Expected nullable changed-hook parameter type in:{System.Environment.NewLine}{nickProp.Source}");
        Assert.True(
            nickProp.Source.Contains("public partial global::System.String? Nickname", StringComparison.Ordinal)
            || nickProp.Source.Contains("public partial string? Nickname", StringComparison.Ordinal),
            nickProp.Source);
        Assert.True(
            nickProp.Source.Contains(
                "global::System.Collections.Generic.EqualityComparer<global::System.String?>.Default",
                StringComparison.Ordinal)
            || nickProp.Source.Contains(
                "global::System.Collections.Generic.EqualityComparer<string?>.Default",
                StringComparison.Ordinal),
            nickProp.Source);

        const string fieldSource = """
            namespace Demo;

            public partial class Vm2 : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string? _bio;

                partial void OnBioChanged(string? value) { _ = value; }
            }
            """;

        GeneratorRunOutput fieldOutput = GeneratorTestHarness.Run(fieldSource, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource bioField = Assert.Single(
            fieldOutput.GeneratedSources.Where(s => s.HintName.EndsWith(".Bio.g.cs")));

        Assert.True(
            bioField.Source.Contains("OnBioChanging(global::System.String? value)", StringComparison.Ordinal)
            || bioField.Source.Contains("OnBioChanging(string? value)", StringComparison.Ordinal),
            bioField.Source);
        Assert.True(
            bioField.Source.Contains("OnBioChanged(global::System.String? value)", StringComparison.Ordinal)
            || bioField.Source.Contains("OnBioChanged(string? value)", StringComparison.Ordinal),
            bioField.Source);
        Assert.True(
            bioField.Source.Contains("public global::System.String? Bio", StringComparison.Ordinal)
            || bioField.Source.Contains("public string? Bio", StringComparison.Ordinal),
            bioField.Source);
    }

    [Fact]
    public void ObservableProperty_field_target_uses_attribute_property_access()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty(PropertyAccess.Internal)]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("internal string Name", propertySource.Source);
        Assert.DoesNotContain("public string Name", propertySource.Source);
    }

    [Fact]
    public void ObservableProperty_field_target_accepts_named_PropertyAccess_argument()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty(PropertyAccess = PropertyAccess.Internal)]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        Assert.Contains("internal string Name", propertySource.Source);
    }

    [Fact]
    public void BindableBase_generator_and_observable_property_use_FeatureSwitches_for_PropertyChanging()
    {
        const string source = """
            namespace Demo;

            [BindableBase]
            public partial class Vm
            {
                [ObservableProperty]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.CSharp12);
        GeneratedSource bindableBaseMain = Assert.Single(output.GeneratedSources.Where(static s => s.HintName.EndsWith(".BindableBase.g.cs", System.StringComparison.Ordinal)));
        GeneratedSource bindableChanging = Assert.Single(output.GeneratedSources.Where(static s => s.HintName.EndsWith(".BindableBase.PropertyChanging.g.cs", System.StringComparison.Ordinal)));
        Assert.DoesNotContain(
            "INotifyPropertyChanged, global::System.ComponentModel.INotifyPropertyChanging",
            bindableBaseMain.Source);
        Assert.Contains("INotifyPropertyChanging", bindableChanging.Source);
        Assert.Contains("Prism.SourceGenerators.__Internals.FeatureSwitches.EnableINotifyPropertyChangingSupport", bindableChanging.Source);

        GeneratedSource nameSource = Assert.Single(output.GeneratedSources.Where(static s => s.HintName.EndsWith(".Name.g.cs", System.StringComparison.Ordinal)));
        Assert.Contains("Prism.SourceGenerators.__Internals.FeatureSwitches.EnableINotifyPropertyChangingSupport", nameSource.Source);
        Assert.Contains("RaisePropertyChanging(nameof(Name))", nameSource.Source);
    }

    public static TheoryData<LanguageVersion, string, string> DiagnosticLanguageMatrix => new()
    {
        { LanguageVersion.CSharp12, "PSG0001", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private int _count;
            }
            """ },
        { LanguageVersion.Preview, "PSG0001", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private int _count;
            }
            """ },
        { LanguageVersion.CSharp12, "PSG0002", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private void Save() { }
            }
            """ },
        { LanguageVersion.Preview, "PSG0002", """
            namespace Demo;

            public class Foo : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private void Save() { }
            }
            """ },
        { LanguageVersion.CSharp12, "PSG0003", """
            namespace Demo;

            public partial class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public int Count { get; set; }
            }
            """ },
        { LanguageVersion.Preview, "PSG0003", """
            namespace Demo;

            public partial class Foo : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                public int Count { get; set; }
            }
            """ },
        { LanguageVersion.CSharp12, "PSG0004", """
            namespace Demo;

            [BindableBase]
            public class Foo
            {
            }
            """ },
        { LanguageVersion.Preview, "PSG0004", """
            namespace Demo;

            [BindableBase]
            public class Foo
            {
            }
            """ }
    };

    [Theory]
    [MemberData(nameof(DiagnosticLanguageMatrix))]
    public void PSG_diagnostics_are_consistent_across_language_versions(
        LanguageVersion languageVersion,
        string diagnosticId,
        string source)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: languageVersion);
        Assert.Contains(output.Diagnostics, d => d.Id == diagnosticId);
    }

    [Fact]
    public void AsyncDelegateCommand_catch_uses_generic_overload_for_specific_exception_types()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [AsyncDelegateCommand(Catch = nameof(OnOperationCanceledException), CommandName = nameof(HelloCommand))]
                private async System.Threading.Tasks.Task HelloAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }

                [AsyncDelegateCommand(Catch = nameof(OnOperationCanceledException2), CommandName = nameof(Hello2Command))]
                private async System.Threading.Tasks.Task Hello2Async()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }

                private void OnOperationCanceledException(System.OperationCanceledException ex)
                {
                }

                private void OnOperationCanceledException2<TEx>(TEx ex) where TEx : System.OperationCanceledException
                {
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        GeneratedSource helloCommand = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".HelloCommand.g.cs")));
        Assert.Contains(".Catch<global::System.OperationCanceledException>(OnOperationCanceledException)", helloCommand.Source);

        GeneratedSource hello2Command = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Hello2Command.g.cs")));
        Assert.Contains(".Catch<global::System.OperationCanceledException>(OnOperationCanceledException2)", hello2Command.Source);
    }

    [Fact]
    public void AsyncDelegateCommand_reports_warning_for_missing_catch_handler_without_blocking_generation()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [AsyncDelegateCommand(Catch = "MissingHandler", CommandName = nameof(HelloCommand))]
                private async System.Threading.Tasks.Task HelloAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);
        Assert.Contains(output.Diagnostics, d => d.Id == "PSG2001");
        Assert.Contains(output.GeneratedSources, s => s.HintName.EndsWith(".HelloCommand.g.cs"));
    }

    [Fact]
    public void Field_property_attribute_target_forwards_to_generated_property()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [property: System.ComponentModel.DataAnnotations.Required]
                [property: System.ComponentModel.DataAnnotations.Range(1, 100)]
                private int _age;
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        Assert.Empty(output.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning && d.Id.StartsWith("PSG", System.StringComparison.Ordinal)));

        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Age.g.cs")));
        Assert.Contains("[global::System.ComponentModel.DataAnnotations.RequiredAttribute]", propertySource.Source);
        Assert.Contains("[global::System.ComponentModel.DataAnnotations.RangeAttribute(1, 100)]", propertySource.Source);

        // Forwarded attributes appear immediately above the property declaration
        int requiredIndex = propertySource.Source.IndexOf("RequiredAttribute", System.StringComparison.Ordinal);
        int propertyIndex = propertySource.Source.IndexOf("public int Age", System.StringComparison.Ordinal);
        Assert.InRange(requiredIndex, 1, propertyIndex - 1);
    }

    [Fact]
    public void Partial_property_does_not_duplicate_DataAnnotations_on_generated_implementation()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                [System.ComponentModel.DataAnnotations.Required]
                public partial string Name { get; set; }

                public string FullName => Name + "!";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));

        // [Required] stays on the user's partial declaration; do not re-emit on the implementing partial (CS0579).
        Assert.DoesNotContain("[global::System.ComponentModel.DataAnnotations.RequiredAttribute]", propertySource.Source);

        // Generator-owned attributes are filtered out
        Assert.DoesNotContain("ObservablePropertyAttribute]", propertySource.Source);
        Assert.DoesNotContain("NotifyPropertyChangedForAttribute(", propertySource.Source);
    }

    [Fact]
    public void Field_attributes_without_property_target_are_forwarded_to_generated_property()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [System.ComponentModel.DataAnnotations.Required]
                private int _age;
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Age.g.cs")));

        Assert.Contains("[global::System.ComponentModel.DataAnnotations.RequiredAttribute]", propertySource.Source);
    }

    [Fact]
    public void NotifyCanExecuteChangedFor_emits_RaiseCanExecuteChanged_call_for_known_command()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
                private string _name = "";

                [DelegateCommand(CanExecute = nameof(CanSave))]
                private void Save() { }

                private bool CanSave() => !string.IsNullOrEmpty(Name);
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        Assert.DoesNotContain(output.Diagnostics, d => d.Id == "PSG2005");

        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));
        Assert.Contains("SaveCommand?.RaiseCanExecuteChanged();", propertySource.Source);

        // RaiseCanExecuteChanged must run AFTER RaisePropertyChanged
        int raisePropertyIndex = propertySource.Source.IndexOf("this.RaisePropertyChanged(nameof(Name));", System.StringComparison.Ordinal);
        int raiseCanExecuteIndex = propertySource.Source.IndexOf("SaveCommand?.RaiseCanExecuteChanged();", System.StringComparison.Ordinal);
        Assert.InRange(raiseCanExecuteIndex, raisePropertyIndex + 1, int.MaxValue);
    }

    [Fact]
    public void NotifyCanExecuteChangedFor_supports_multiple_commands_and_existing_member()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                public global::Prism.Commands.DelegateCommand ManualCommand { get; } = null!;

                [ObservableProperty]
                [NotifyCanExecuteChangedFor(nameof(SaveCommand), nameof(ManualCommand))]
                private string _name = "";

                [DelegateCommand]
                private void Save() { }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        Assert.DoesNotContain(output.Diagnostics, d => d.Id == "PSG2005");

        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));
        Assert.Contains("SaveCommand?.RaiseCanExecuteChanged();", propertySource.Source);
        Assert.Contains("ManualCommand?.RaiseCanExecuteChanged();", propertySource.Source);
    }

    [Fact]
    public void NotifyCanExecuteChangedFor_reports_PSG2005_but_still_emits_property()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyCanExecuteChangedFor("MissingCommand")]
                private string _name = "";
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        Diagnostic[] psg2005 = output.Diagnostics.Where(d => d.Id == "PSG2005").ToArray();
        Assert.NotEmpty(psg2005);
        Assert.All(psg2005, d => Assert.Equal(DiagnosticSeverity.Warning, d.Severity));

        // Source still emitted so user code keeps compiling once they fix the name
        GeneratedSource propertySource = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".Name.g.cs")));
        Assert.Contains("MissingCommand?.RaiseCanExecuteChanged();", propertySource.Source);
    }

    [Fact]
    public void AsyncDelegateCommand_reports_package_required_when_prism_async_command_missing()
    {
        const string source = """
            namespace Demo;

            public partial class Vm : Prism.Mvvm.BindableBase
            {
                [DelegateCommand]
                private async System.Threading.Tasks.Task LoadAsync()
                {
                    await System.Threading.Tasks.Task.CompletedTask;
                }
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(
            source,
            languageVersion: LanguageVersion.Preview,
            hasAsyncDelegateCommand: false);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG3002");
        Assert.DoesNotContain(output.GeneratedSources, s => s.HintName == "AsyncDelegateCommand.Polyfill.g.cs");
    }

    public static TheoryData<string, string, string, string> TypeShapeMatrix => new()
    {
        {
            "nested",
            """
            namespace Demo;

            public partial class Outer
            {
                public partial class Vm : Prism.Mvvm.BindableBase
                {
                    [ObservableProperty]
                    private int _count;

                    [DelegateCommand]
                    private void Save()
                    {
                    }
                }
            }
            """,
            "partial class Vm",
            "public int Count"
        },
        {
            "generic",
            """
            namespace Demo;

            public partial class Vm<T> : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private T? _value;

                [DelegateCommand]
                private void Save(T? value)
                {
                }
            }
            """,
            "partial class Vm<T>",
            "public T? Value"
        },
        {
            "abstract",
            """
            namespace Demo;

            public abstract partial class Vm : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                private string _name = "";

                [DelegateCommand]
                private void Save()
                {
                }
            }
            """,
            "partial class Vm",
            "public string Name"
        },
        {
            "inheritance",
            """
            namespace Demo;

            public partial class BaseVm : Prism.Mvvm.BindableBase
            {
                protected bool CanSave()
                {
                    return true;
                }
            }

            public partial class Vm : BaseVm
            {
                [ObservableProperty]
                private int _count;

                [DelegateCommand(CanExecute = nameof(CanSave))]
                private void Save()
                {
                }
            }
            """,
            "partial class Vm",
            "public int Count"
        }
    };

    [Theory]
    [MemberData(nameof(TypeShapeMatrix))]
    public void Generation_supports_type_shape_matrix(
        string scenario,
        string source,
        string expectedTypeDeclarationFragment,
        string expectedPropertyFragment)
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run(source, languageVersion: LanguageVersion.Preview);

        Assert.True(
            output.Diagnostics.All(static d => !d.Id.StartsWith("PSG", System.StringComparison.Ordinal)),
            $"Scenario '{scenario}' produced diagnostics: {string.Join(", ", output.Diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"))}");

        GeneratedSource commandSource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".SaveCommand.g.cs")));
        Assert.Contains(expectedTypeDeclarationFragment, commandSource.Source);
        Assert.Contains("Save", commandSource.Source);

        GeneratedSource propertySource = Assert.Single(
            output.GeneratedSources.Where(s => s.HintName.EndsWith(".Count.g.cs") || s.HintName.EndsWith(".Value.g.cs") || s.HintName.EndsWith(".Name.g.cs")));
        Assert.Contains(expectedTypeDeclarationFragment, propertySource.Source);
        Assert.Contains(expectedPropertyFragment, propertySource.Source);
    }
}
