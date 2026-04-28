using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class SnapshotTests
{
    [Fact]
    public Task Generates_expected_sources_for_valid_inputs()
    {
        const string source = """
            namespace Demo;

            [BindableBase]
            public partial class PersonViewModel : Prism.Mvvm.BindableBase
            {
                [ObservableProperty]
                [NotifyPropertyChangedFor(nameof(FullName))]
                private string _firstName = "Ada";

                [ObservableProperty]
                private string _lastName = "Lovelace";

                public string FullName => $"{FirstName} {LastName}";

                [DelegateCommand(CanExecute = nameof(CanSave))]
                [ObservesProperty(nameof(FirstName), nameof(LastName))]
                private void Save()
                {
                }

                private bool CanSave() => true;
            }
            """;

        GeneratorRunOutput output = GeneratorTestHarness.Run(source);
        return Verifier.Verify(GeneratorTestHarness.ToSnapshot(output));
    }
}
