namespace SourceGit.Models
{
    /// <summary>
    /// Abstraction over the wtcraft governance source.
    ///
    /// The MVP only reads checked-in fixtures, but the panel is written against
    /// this interface so the real (and still evolving) wtcraft machine protocol
    /// can be wired in later without touching the UI. Implementations must never
    /// throw from <see cref="GetSnapshot"/>; they return <c>null</c> to signal
    /// "unavailable or unsupported" so callers can degrade to Git-native data.
    /// </summary>
    public interface IWtcraftClient
    {
        /// <summary>
        /// True when a usable snapshot could be loaded. When false, the panel
        /// shows Git-native worktree facts only.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Returns governance state for the given repository, or <c>null</c>
        /// when wtcraft is unavailable or its schema is unsupported.
        /// </summary>
        WtcraftSnapshot GetSnapshot(string repoPath);
    }
}
