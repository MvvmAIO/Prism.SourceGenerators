namespace Prism.SourceGenerators.Samples.Prism9.Services;

/// <summary>Registered with <c>IfNotRegistered</c> to emit Prism 9 <c>TryRegisterSingleton</c>.</summary>
public interface IDateTimeProvider
{
    DateTimeOffset Now { get; }
}
