namespace Prism.SourceGenerators.Models;

/// <summary>Inputs for <c>[ShowDialogCommand]</c> command emission.</summary>
internal sealed record ShowDialogCommandGenerationInfo(
    HierarchyInfo Hierarchy,
    string MethodName,
    string CommandName,
    string DialogServiceMember,
    string DialogNameLiteral,
    string DialogsNamespace,
    bool UsesExtensionShowDialog);
