using System;

namespace Prism.SourceGenerators;

/// <summary>
/// Generates <c>Prism.Services.Dialogs.IDialogAware</c> members with optional partial hooks.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DialogAwareAttribute : Attribute
{
    /// <summary>
    /// Initial dialog title when the generated <see cref="Prism.Services.Dialogs.IDialogAware.Title"/> is used.
    /// </summary>
    public string Title { get; set; } = string.Empty;
}
