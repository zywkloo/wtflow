using System.Threading.Tasks;

namespace SourceGit.Models
{
    /// <summary>
    /// Abstraction over the wtcraft governance source.
    ///
    /// The panel is written against this interface so the real (still evolving)
    /// wtcraft machine protocol can be swapped in without touching the UI.
    /// Implementations must never throw from <see cref="GetSnapshotAsync"/>; they
    /// return <c>null</c> to signal "unavailable or unsupported" so callers can
    /// degrade to Git-native data.
    /// </summary>
    public interface IWtcraftClient
    {
        /// <summary>
        /// Returns governance state for the given repository, or <c>null</c> when
        /// wtcraft is unavailable or its output is unsupported. Runs off the UI
        /// thread; implementations may shell out to the wtcraft CLI.
        /// </summary>
        Task<WtcraftSnapshot> GetSnapshotAsync(string repoPath);
    }
}
