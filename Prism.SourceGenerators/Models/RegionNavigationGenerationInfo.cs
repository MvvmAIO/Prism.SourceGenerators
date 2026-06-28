namespace Prism.SourceGenerators.Models;

/// <summary>Inputs for <c>[NavigateCommand]</c> command emission.</summary>
internal sealed record NavigateCommandGenerationInfo(
    HierarchyInfo Hierarchy,
    string MethodName,
    string CommandName,
    string RegionManagerMember,
    string RegionLiteral,
    string TargetLiteral,
    string RegionsNamespace);

/// <summary>Inputs for <c>[NavigateOnChanged]</c> partial hook emission.</summary>
internal sealed record NavigateOnChangedGenerationInfo(
    HierarchyInfo Hierarchy,
    string PropertyName,
    string FieldType,
    string RegionManagerMember,
    string RegionLiteral,
    string TargetMemberExpression,
    string RegionsNamespace);
