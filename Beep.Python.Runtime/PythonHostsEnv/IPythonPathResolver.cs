using System.Threading;
using System.Threading.Tasks;

namespace Beep.Python.RuntimeEngine.Infrastructure;

public interface IPythonPathResolver
{
    /// <summary>
    /// Returns the currently known default runtime path if available, otherwise null.
    /// This method should NOT force runtime initialization.
    /// </summary>
    string? GetDefaultRuntimePath();

    /// <summary>
    /// Optionally ensures the runtime is initialized (e.g., download/setup of embedded runtime).
    /// This can be called by high-level initialization code where blocking behavior is acceptable.
    /// </summary>
    Task EnsureRuntimeInitializedAsync(CancellationToken cancellationToken = default);
}
