namespace Prism.SourceGenerators.__Internals;

/// <summary>
/// Runtime feature switches for optional MVVM behaviors shipped with <c>MvvmAIO.Prism.Core</c>.
/// Mirrors <c>CommunityToolkit.Mvvm.ComponentModel.__Internals.FeatureSwitches</c> (including the default for INotifyPropertyChanging).
/// </summary>
public static class FeatureSwitches
{
    /// <summary>
    /// When <see langword="true"/> (default), code paths that participate in
    /// <see cref="System.ComponentModel.INotifyPropertyChanging"/> may raise <c>PropertyChanging</c>.
    /// When <see langword="false"/>, those paths are skipped to avoid allocations and subscriber work (same trade-off as CommunityToolkit.Mvvm).
    /// </summary>
    public static bool EnableINotifyPropertyChangingSupport { get; set; } = true;
}
