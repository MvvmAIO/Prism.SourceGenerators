using System;

namespace Prism.SourceGenerators.Helpers;

/// <summary>
/// Shared naming helpers for command and backing field name derivation.
/// </summary>
internal static class NamingHelpers
{
    /// <summary>
    /// Derives a command name from a method name. If the method name already
    /// ends with "Command", it is returned as-is; otherwise "Command" is appended.
    /// </summary>
    /// <param name="methodName">The source method name.</param>
    /// <returns>The derived command property name.</returns>
    public static string GetCommandName(string methodName) =>
        methodName.EndsWith("Command", StringComparison.Ordinal) ? methodName : $"{methodName}Command";

    /// <summary>
    /// Derives a camelCase backing field name from a PascalCase command name
    /// by prepending an underscore and lowercasing the first character.
    /// </summary>
    /// <param name="commandName">The command property name (e.g. "SaveCommand").</param>
    /// <returns>The backing field name (e.g. "_saveCommand").</returns>
    public static string GetBackingFieldName(string commandName) =>
        $"_{char.ToLowerInvariant(commandName[0])}{commandName.Substring(1)}";
}
