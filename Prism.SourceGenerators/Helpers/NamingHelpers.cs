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

    /// <summary>
    /// Derives a PascalCase property name from a backing field name.
    /// Handles <c>m_</c>, <c>_</c>, and bare-name prefixes consistently across
    /// <c>[ObservableProperty]</c>, <c>[NavigateOnChanged]</c>,
    /// <c>[FromNavigationParameter]</c>, and <c>[FromDialogParameter]</c>.
    /// </summary>
    /// <param name="fieldName">The backing field name (e.g. "_userId", "m_userId", "userId").</param>
    /// <returns>The derived property name (e.g. "UserId").</returns>
    public static string GetPropertyNameFromField(string fieldName)
    {
        if (fieldName.StartsWith("m_") && fieldName.Length > 2)
            return char.ToUpperInvariant(fieldName[2]) + fieldName.Substring(3);
        if (fieldName.StartsWith('_') && fieldName.Length > 1)
            return char.ToUpperInvariant(fieldName[1]) + fieldName.Substring(2);
        return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
    }
}
