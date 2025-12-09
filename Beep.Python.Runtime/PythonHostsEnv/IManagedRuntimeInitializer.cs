using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

/// <summary>
/// Responsible for orchestrating managed runtime initialization (embedded runtime downloads/setup)
/// </summary>
public interface IManagedRuntimeInitializer
{
    /// <summary>
    /// Ensure the managed runtime is initialized. Implementations will typically ensure a runtime exists and, if configured,
    /// initialize the runtime (download/setup) if it's missing.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task EnsureRuntimeInitializedAsync(CancellationToken ct = default);
}
