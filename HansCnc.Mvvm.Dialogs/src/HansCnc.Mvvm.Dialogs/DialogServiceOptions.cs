using System.Windows;

namespace HansCnc.Mvvm;

/// <summary>
/// Options used by <see cref="DialogService"/>.
/// </summary>
public sealed class DialogServiceOptions
{
    /// <summary>
    /// Gets a default options instance.
    /// </summary>
    public static DialogServiceOptions Default { get; } = new();

    /// <summary>
    /// Gets or initializes a value indicating whether the active application window should be assigned as dialog owner.
    /// </summary>
    public bool AssignOwner { get; init; } = true;

    /// <summary>
    /// Gets or initializes the owner provider. If null, the active WPF window is used.
    /// </summary>
    public Func<Window?>? OwnerProvider { get; init; }
}
