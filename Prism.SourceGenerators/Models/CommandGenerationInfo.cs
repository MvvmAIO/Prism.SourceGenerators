using System;
using Prism.SourceGenerators.Helpers;

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
    bool HasAsyncDelegateCommand,
    double? CancelAfterMicroseconds,
    string? Catch,
    string? CancellationTokenSourceFactory,
    bool EnableParallelExecution,
    EquatableArray<string> ObservesProperties,
    bool UseFieldKeyword) : IEquatable<CommandGenerationInfo>;
