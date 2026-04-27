using System;

namespace Prism.SourceGenerators.Models;

/// <summary>
/// A model representing the information needed to generate a command property.
/// </summary>
internal sealed record CommandGenerationInfo(
    HierarchyInfo Hierarchy,
    string MethodName,
    string CommandName,
    string? ParameterType,
    bool IsAsync,
    string? CanExecute,
    bool HasAsyncDelegateCommand) : IEquatable<CommandGenerationInfo>;
