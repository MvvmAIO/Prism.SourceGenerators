using System.Linq;
using Xunit;

namespace Prism.SourceGenerators.Tests;

public sealed class DialogServiceCommandGeneratorTests
{
    [Fact]
    public void ShowDialogCommand_generates_ShowDialog_command()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                private readonly Prism.Services.Dialogs.IDialogService _dialogService;

                public ShellVm(Prism.Services.Dialogs.IDialogService dialogService) => _dialogService = dialogService;

                [ShowDialogCommand(Name = "ConfirmDelete")]
                private void ConfirmDelete() { }
            }
            """);

        GeneratedSource generated = Assert.Single(output.GeneratedSources.Where(s => s.HintName.EndsWith(".ConfirmDeleteCommand.g.cs")));
        Assert.Contains("ShowDialog(\"ConfirmDelete\"", generated.Source);
        Assert.Contains("OnConfirmDeleteDialogClosed", generated.Source);
        Assert.Contains("ConfirmDeleteCommand", generated.Source);
    }

    [Fact]
    public void ShowDialogCommand_reports_PSG7101_when_dialog_service_missing()
    {
        GeneratorRunOutput output = GeneratorTestHarness.Run("""
            public partial class ShellVm : Prism.Mvvm.BindableBase
            {
                [ShowDialogCommand(Name = "ConfirmDelete")]
                private void ConfirmDelete() { }
            }
            """);

        Assert.Contains(output.Diagnostics, d => d.Id == "PSG7101");
    }
}
