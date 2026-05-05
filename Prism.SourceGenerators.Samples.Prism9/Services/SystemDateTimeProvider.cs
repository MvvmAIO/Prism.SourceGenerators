using Prism.SourceGenerators;

namespace Prism.SourceGenerators.Samples.Prism9.Services;

[RegisterSingleton<IDateTimeProvider>(IfNotRegistered = true)]
public sealed partial class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
