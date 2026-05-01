using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Prism.Commands;

/// <summary>
/// Prism 9–compatible async command abstraction for Prism.Core 8.1.97.
/// </summary>
public interface IAsyncCommand : ICommand
{
    /// <summary>
    /// Executes the command with a specified parameter and the default <see cref="CancellationToken"/>.
    /// </summary>
    Task ExecuteAsync(object? parameter);

    /// <summary>
    /// Executes the command with a specified parameter and a <see cref="CancellationToken"/>.
    /// </summary>
    Task ExecuteAsync(object? parameter, CancellationToken cancellationToken);
}
